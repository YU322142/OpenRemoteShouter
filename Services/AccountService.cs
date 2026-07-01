using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using RemoteShouter.Models;

namespace RemoteShouter.Services;

public sealed class AccountService
{
    public const string SessionCookieName = "ors_session";
    public const string CsrfHeaderName = "X-OpenRemoteShouter-CSRF";

    private const int PasswordHashIterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan LoginLockDuration = TimeSpan.FromMinutes(5);
    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9._-]{3,32}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _lock = new();
    private readonly string _databasePath;
    private readonly ConcurrentDictionary<string, SessionRecord> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, LoginAttemptRecord> _loginAttempts = new(StringComparer.Ordinal);
    private readonly string _dummyPasswordHash;
    private AccountDatabase _database;
    private bool _databaseLoadFailed;

    public AccountService()
    {
        var dataDirectory = AppLogService.DataDirectory;
        _databasePath = Path.Combine(dataDirectory, "accounts.json");
        _dummyPasswordHash = CreatePasswordHash(RandomToken());
        _database = LoadDatabase();
    }

    public bool SetupRequired
    {
        get
        {
            lock (_lock)
            {
                if (_databaseLoadFailed)
                {
                    return false;
                }

                return _database.Users.Count == 0;
            }
        }
    }

    public AuthState GetAuthState(HttpRequest request)
    {
        var session = GetSession(request);
        return new AuthState(SetupRequired, session?.User, session?.CsrfToken);
    }

    public (bool Ok, int StatusCode, string? Error, AccountSession? Session) SetupAdmin(
        SetupAdminRequest request,
        string remoteKey)
    {
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
            }

            if (_database.Users.Count > 0)
            {
                return (false, StatusCodes.Status409Conflict, "Setup has already been completed.", null);
            }

            var normalizedUsername = NormalizeUsername(request.Username);
            var validationError = ValidateUsername(normalizedUsername)
                                  ?? ValidatePassword(request.Password);
            if (validationError is not null)
            {
                return (false, StatusCodes.Status400BadRequest, validationError, null);
            }

            var now = DateTimeOffset.UtcNow;
            var user = new StoredUser
            {
                Username = normalizedUsername,
                DisplayName = NormalizeDisplayName(request.DisplayName, normalizedUsername),
                PasswordHash = CreatePasswordHash(request.Password!),
                IsAdmin = true,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordChangedAt = now
            };

            _database.Users.Add(user);
            SaveDatabase();
            AppLogService.Info($"Admin account created. username={user.Username}, remote={remoteKey}");
            return (true, StatusCodes.Status200OK, null, CreateSession(user));
        }
    }

    public (bool Ok, int StatusCode, string? Error, AccountSession? Session) Login(
        LoginRequest request,
        string remoteKey)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        if (_databaseLoadFailed)
        {
            return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
        }

        var limiterKey = BuildLoginLimiterKey(remoteKey, normalizedUsername);
        var loginAttempt = GetLoginAttempt(limiterKey);
        if (loginAttempt.LockedUntil > DateTimeOffset.UtcNow)
        {
            return (false, StatusCodes.Status429TooManyRequests, "Too many unsuccessful sign-in attempts. Try again later.", null);
        }

        StoredUser? user;
        lock (_lock)
        {
            user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));
        }

        var password = request.Password ?? string.Empty;
        var passwordHash = user?.PasswordHash ?? _dummyPasswordHash;
        var validPassword = VerifyPassword(password, passwordHash);
        if (user is null || !user.IsEnabled || !validPassword)
        {
            RegisterFailedLogin(limiterKey);
            AppLogService.Info($"Login failed. username={normalizedUsername}, remote={remoteKey}");
            return (false, StatusCodes.Status401Unauthorized, "Invalid username or password.", null);
        }

        lock (_lock)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            SaveDatabase();
        }

        _loginAttempts.TryRemove(limiterKey, out _);
        AppLogService.Info($"Login succeeded. username={user.Username}, remote={remoteKey}");
        return (true, StatusCodes.Status200OK, null, CreateSession(user));
    }

    public void Logout(AccountSession session)
    {
        _sessions.TryRemove(session.Token, out _);
    }

    public AccountSession? GetSession(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(SessionCookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (!_sessions.TryGetValue(token, out var sessionRecord))
        {
            return null;
        }

        if (sessionRecord.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        StoredUser? user;
        lock (_lock)
        {
            user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, sessionRecord.Username, StringComparison.OrdinalIgnoreCase));
        }

        if (user is null
            || !user.IsEnabled
            || user.PasswordChangedAt != sessionRecord.PasswordChangedAt)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return new AccountSession(token, sessionRecord.CsrfToken, sessionRecord.ExpiresAt, ToAccountUser(user));
    }

    public bool ValidateCsrf(AccountSession session, HttpRequest request)
    {
        if (!request.Headers.TryGetValue(CsrfHeaderName, out var headerValue))
        {
            return false;
        }

        var suppliedToken = headerValue.ToString();
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(suppliedToken),
            System.Text.Encoding.UTF8.GetBytes(session.CsrfToken));
    }

    public IReadOnlyList<AccountUser> ListUsers()
    {
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return [];
            }

            return _database.Users
                .OrderByDescending(x => x.IsAdmin)
                .ThenBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
                .Select(ToAccountUser)
                .ToArray();
        }
    }

    public (bool Ok, int StatusCode, string? Error, AccountUser? User) CreateUser(CreateUserRequest request)
    {
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
            }

            var normalizedUsername = NormalizeUsername(request.Username);
            var validationError = ValidateUsername(normalizedUsername)
                                  ?? ValidatePassword(request.Password);
            if (validationError is not null)
            {
                return (false, StatusCodes.Status400BadRequest, validationError, null);
            }

            if (_database.Users.Any(x => string.Equals(x.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, StatusCodes.Status409Conflict, "Username already exists.", null);
            }

            var now = DateTimeOffset.UtcNow;
            var user = new StoredUser
            {
                Username = normalizedUsername,
                DisplayName = NormalizeDisplayName(request.DisplayName, normalizedUsername),
                PasswordHash = CreatePasswordHash(request.Password!),
                IsAdmin = request.IsAdmin,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordChangedAt = now
            };

            _database.Users.Add(user);
            SaveDatabase();
            AppLogService.Info($"User account created. username={user.Username}, isAdmin={user.IsAdmin}");
            return (true, StatusCodes.Status200OK, null, ToAccountUser(user));
        }
    }

    public (bool Ok, int StatusCode, string? Error, AccountUser? User) UpdateUser(
        string username,
        UpdateUserRequest request,
        AccountUser actor)
    {
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
            }

            var user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return (false, StatusCodes.Status404NotFound, "User not found.", null);
            }

            var changingOwnAdmin = string.Equals(user.Username, actor.Username, StringComparison.OrdinalIgnoreCase);
            if (request.IsEnabled == false && changingOwnAdmin)
            {
                return (false, StatusCodes.Status400BadRequest, "You cannot disable your current account.", null);
            }

            if (request.IsAdmin == false && changingOwnAdmin)
            {
                return (false, StatusCodes.Status400BadRequest, "You cannot remove admin permission from your current account.", null);
            }

            var originalDisplayName = user.DisplayName;
            var originalIsAdmin = user.IsAdmin;
            var originalIsEnabled = user.IsEnabled;
            var originalPasswordHash = user.PasswordHash;
            var originalPasswordChangedAt = user.PasswordChangedAt;
            var shouldInvalidateSessions = false;

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.DisplayName = NormalizeDisplayName(request.DisplayName, user.Username);
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var passwordError = ValidatePassword(request.Password);
                if (passwordError is not null)
                {
                    return (false, StatusCodes.Status400BadRequest, passwordError, null);
                }

                user.PasswordHash = CreatePasswordHash(request.Password);
                user.PasswordChangedAt = DateTimeOffset.UtcNow;
                shouldInvalidateSessions = true;
            }

            if (request.IsAdmin is not null)
            {
                user.IsAdmin = request.IsAdmin.Value;
            }

            if (request.IsEnabled is not null)
            {
                user.IsEnabled = request.IsEnabled.Value;
                if (!user.IsEnabled)
                {
                    shouldInvalidateSessions = true;
                }
            }

            if (!HasEnabledAdmin())
            {
                user.DisplayName = originalDisplayName;
                user.IsAdmin = originalIsAdmin;
                user.IsEnabled = originalIsEnabled;
                user.PasswordHash = originalPasswordHash;
                user.PasswordChangedAt = originalPasswordChangedAt;
                return (false, StatusCodes.Status400BadRequest, "At least one enabled admin account is required.", null);
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            if (shouldInvalidateSessions)
            {
                InvalidateUserSessions(user.Username);
            }

            SaveDatabase();
            return (true, StatusCodes.Status200OK, null, ToAccountUser(user));
        }
    }

    public (bool Ok, int StatusCode, string? Error) DeleteUser(string username, AccountUser actor)
    {
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.");
            }

            var user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return (false, StatusCodes.Status404NotFound, "User not found.");
            }

            if (string.Equals(user.Username, actor.Username, StringComparison.OrdinalIgnoreCase))
            {
                return (false, StatusCodes.Status400BadRequest, "You cannot delete your current account.");
            }

            _database.Users.Remove(user);
            if (!HasEnabledAdmin())
            {
                _database.Users.Add(user);
                return (false, StatusCodes.Status400BadRequest, "At least one enabled admin account is required.");
            }

            InvalidateUserSessions(user.Username);
            SaveDatabase();
            AppLogService.Info($"User account deleted. username={user.Username}, actor={actor.Username}");
            return (true, StatusCodes.Status200OK, null);
        }
    }

    public (bool Ok, int StatusCode, string? Error) ChangePassword(
        AccountUser actor,
        ChangePasswordRequest request)
    {
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.");
            }

            var user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, actor.Username, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return (false, StatusCodes.Status404NotFound, "User not found.");
            }

            if (!VerifyPassword(request.CurrentPassword ?? string.Empty, user.PasswordHash))
            {
                return (false, StatusCodes.Status401Unauthorized, "Invalid current password.");
            }

            var passwordError = ValidatePassword(request.NewPassword);
            if (passwordError is not null)
            {
                return (false, StatusCodes.Status400BadRequest, passwordError);
            }

            user.PasswordHash = CreatePasswordHash(request.NewPassword!);
            user.PasswordChangedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            InvalidateUserSessions(user.Username);
            SaveDatabase();
            return (true, StatusCodes.Status200OK, null);
        }
    }

    public void SetSessionCookie(HttpResponse response, HttpRequest request, AccountSession session)
    {
        response.Cookies.Append(SessionCookieName, session.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = session.ExpiresAt,
            IsEssential = true
        });
    }

    public void ClearSessionCookie(HttpResponse response, HttpRequest request)
    {
        response.Cookies.Delete(SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    private AccountSession CreateSession(StoredUser user)
    {
        var token = RandomToken();
        var csrfToken = RandomToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        var sessionRecord = new SessionRecord(user.Username, csrfToken, expiresAt, user.PasswordChangedAt);
        _sessions[token] = sessionRecord;
        return new AccountSession(token, csrfToken, expiresAt, ToAccountUser(user));
    }

    private AccountDatabase LoadDatabase()
    {
        EnsureDataDirectory();
        if (!File.Exists(_databasePath))
        {
            return new AccountDatabase();
        }

        try
        {
            var json = File.ReadAllText(_databasePath);
            return JsonSerializer.Deserialize<AccountDatabase>(json, JsonOptions) ?? new AccountDatabase();
        }
        catch (Exception ex)
        {
            AppLogService.Error("Failed to load accounts database", ex);
            _databaseLoadFailed = true;
            return new AccountDatabase();
        }
    }

    private void SaveDatabase()
    {
        EnsureDataDirectory();
        var json = JsonSerializer.Serialize(_database, JsonOptions);
        var tempPath = _databasePath + ".tmp";
        File.WriteAllText(tempPath, json);
        TryApplyPrivateFilePermissions(tempPath);
        File.Move(tempPath, _databasePath, overwrite: true);
        TryApplyPrivateFilePermissions(_databasePath);
    }

    private static string NormalizeUsername(string? username)
    {
        return (username ?? string.Empty).Trim();
    }

    private static string NormalizeDisplayName(string? displayName, string username)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? username : normalized;
    }

    private static string? ValidateUsername(string username)
    {
        return UsernamePattern.IsMatch(username)
            ? null
            : "Username must be 3-32 characters and may contain letters, numbers, dots, underscores, or hyphens.";
    }

    private static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "Password is required.";
        }

        if (password.Length < 10)
        {
            return "Password must be at least 10 characters.";
        }

        if (password.Length > 256)
        {
            return "Password is too long.";
        }

        return null;
    }

    private LoginAttemptRecord GetLoginAttempt(string key)
    {
        return _loginAttempts.GetOrAdd(key, _ => new LoginAttemptRecord(0, DateTimeOffset.MinValue));
    }

    private void RegisterFailedLogin(string key)
    {
        _loginAttempts.AddOrUpdate(
            key,
            _ => new LoginAttemptRecord(1, DateTimeOffset.MinValue),
            (_, current) =>
            {
                var attempts = current.LockedUntil <= DateTimeOffset.UtcNow ? current.Attempts + 1 : current.Attempts;
                var lockedUntil = attempts >= 5 ? DateTimeOffset.UtcNow.Add(LoginLockDuration) : DateTimeOffset.MinValue;
                return new LoginAttemptRecord(attempts, lockedUntil);
            });
    }

    private static string BuildLoginLimiterKey(string remoteKey, string username)
    {
        return $"{remoteKey.ToLowerInvariant()}|{username.ToLowerInvariant()}";
    }

    private bool HasEnabledAdmin()
    {
        return _database.Users.Any(x => x.IsAdmin && x.IsEnabled);
    }

    private void InvalidateUserSessions(string username)
    {
        foreach (var session in _sessions)
        {
            if (string.Equals(session.Value.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }
    }

    private static AccountUser ToAccountUser(StoredUser user)
    {
        return new AccountUser(
            user.Username,
            user.DisplayName,
            user.IsAdmin,
            user.IsEnabled,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt);
    }

    private static string CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordHashIterations,
            HashAlgorithmName.SHA256,
            HashBytes);
        return $"pbkdf2-sha256${PasswordHashIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string encodedHash)
    {
        var parts = encodedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static string RandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static void EnsureDataDirectory()
    {
        Directory.CreateDirectory(AppLogService.DataDirectory);
        TryApplyPrivateDirectoryPermissions(AppLogService.DataDirectory);
    }

    private static void TryApplyPrivateDirectoryPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch
        {
            // Best-effort hardening only.
        }
    }

    private static void TryApplyPrivateFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Best-effort hardening only.
        }
    }

    private sealed class AccountDatabase
    {
        public int Version { get; set; } = 1;

        public List<StoredUser> Users { get; set; } = [];
    }

    private sealed class StoredUser
    {
        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset PasswordChangedAt { get; set; }

        public DateTimeOffset? LastLoginAt { get; set; }
    }

    private sealed record SessionRecord(
        string Username,
        string CsrfToken,
        DateTimeOffset ExpiresAt,
        DateTimeOffset PasswordChangedAt);

    private sealed record LoginAttemptRecord(
        int Attempts,
        DateTimeOffset LockedUntil);
}
