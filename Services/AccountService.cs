using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    private const int MinimumPasswordHashIterations = 100_000;
    private const int MaximumPasswordHashIterations = 2_000_000;
    private const int MaxDisplayNameLength = 48;
    private const int MaxUsers = 1_024;
    private const long MaxAccountDatabaseBytes = 1 * 1024 * 1024;
    private const int MaxSessionEntries = 4_096;
    private const int MaxSessionsPerUser = 32;
    private const int MaxLoginVerificationConcurrency = 4;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan SessionCleanupInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LoginLockDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LoginAttemptCleanupInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LoginAttemptRetention = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AccountLoginVerificationInterval = TimeSpan.FromSeconds(10);
    private const int PairLoginFailureLimit = 5;
    private const int AccountLoginFailureLimit = 20;
    private const int RemoteLoginFailureLimit = 100;
    private const int MaxLoginAttemptEntries = 4096;
    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9._-]{3,32}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        MaxDepth = 16
    };

    private readonly object _lock = new();
    private readonly object _sessionsLock = new();
    private readonly SemaphoreSlim _loginVerificationGate = new(MaxLoginVerificationConcurrency);
    private readonly string _databasePath;
    private readonly ConcurrentDictionary<string, SessionRecord> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, LoginAttemptRecord> _loginAttempts = new(StringComparer.Ordinal);
    private readonly object _loginAttemptsLock = new();
    private readonly ConcurrentDictionary<string, LoginAttemptRecord> _accountLoginAttempts = new(StringComparer.Ordinal);
    private readonly object _accountLoginAttemptsLock = new();
    private readonly string _dummyPasswordHash;
    private AccountDatabase _database;
    private bool _databaseLoadFailed;
    private long _lastLoginAttemptCleanupTicks;
    private long _lastSessionCleanupTicks;

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

    public bool IsDatabaseAvailable
    {
        get
        {
            lock (_lock)
            {
                return !_databaseLoadFailed;
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
        var normalizedUsername = NormalizeUsername(request.Username);
        var validationError = ValidateUsername(normalizedUsername)
                              ?? ValidateDisplayName(request.DisplayName)
                              ?? ValidatePassword(request.Password);
        if (validationError is not null)
        {
            return (false, StatusCodes.Status400BadRequest, validationError, null);
        }

        lock (_lock)
        {
            var stateError = ValidateSetupStateLocked();
            if (stateError is not null)
            {
                return (false, stateError.Value.StatusCode, stateError.Value.Error, null);
            }
        }

        if (!TryCreatePasswordHash(request.Password!, out var passwordHash))
        {
            return (false, StatusCodes.Status429TooManyRequests, "Too many password operations. Try again later.", null);
        }

        lock (_lock)
        {
            var stateError = ValidateSetupStateLocked();
            if (stateError is not null)
            {
                return (false, stateError.Value.StatusCode, stateError.Value.Error, null);
            }

            var now = DateTimeOffset.UtcNow;
            var user = new StoredUser
            {
                Username = normalizedUsername,
                DisplayName = NormalizeDisplayName(request.DisplayName, normalizedUsername),
                PasswordHash = passwordHash,
                IsAdmin = true,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordChangedAt = now
            };

            _database.Users.Add(user);
            try
            {
                SaveDatabase();
            }
            catch
            {
                _database.Users.Remove(user);
                throw;
            }

            ClearAccountLoginAttempts(normalizedUsername);
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

        // Reject malformed usernames before doing the expensive password hash
        // verification.  This also keeps attacker-controlled limiter keys
        // bounded to the documented username size.
        var usernameError = ValidateUsername(normalizedUsername);
        if (usernameError is not null)
        {
            return (false, StatusCodes.Status400BadRequest, usernameError, null);
        }

        var now = DateTimeOffset.UtcNow;
        CleanupLoginAttempts(now);
        var limiterKey = BuildLoginLimiterKey(remoteKey, normalizedUsername);
        var accountLimiterKey = BuildAccountLoginLimiterKey(normalizedUsername);
        var remoteLimiterKey = BuildRemoteLoginLimiterKey(remoteKey);
        var pairLocked = IsLoginLocked(_loginAttempts, limiterKey, now);
        var remoteLocked = IsLoginLocked(_loginAttempts, remoteLimiterKey, now);
        if (pairLocked || remoteLocked)
        {
            return (false, StatusCodes.Status429TooManyRequests, "Too many unsuccessful sign-in attempts. Try again later.", null);
        }

        if (request.Password is { Length: > 256 })
        {
            return (false, StatusCodes.Status400BadRequest, "Password is too long.", null);
        }

        StoredUser? user;
        DateTimeOffset passwordVersion = default;
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
            }

            user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));
            if (user is not null)
            {
                passwordVersion = user.PasswordChangedAt;
            }
        }

        var password = request.Password ?? string.Empty;
        var passwordHash = user?.PasswordHash ?? _dummyPasswordHash;
        if (!_loginVerificationGate.Wait(0))
        {
            return (false, StatusCodes.Status429TooManyRequests, "Too many sign-in requests. Try again later.", null);
        }

        bool validPassword;
        try
        {
            if (user is not null
                && !TryReserveAccountLoginVerification(accountLimiterKey, DateTimeOffset.UtcNow))
            {
                return (false, StatusCodes.Status429TooManyRequests, "Too many unsuccessful sign-in attempts. Try again later.", null);
            }

            validPassword = VerifyPassword(password, passwordHash);
        }
        finally
        {
            _loginVerificationGate.Release();
        }
        if (user is null || !user.IsEnabled || !validPassword)
        {
            var accountThrottled = false;
            var limiterCapacityExceeded = !RegisterFailedLogin(
                _loginAttempts,
                _loginAttemptsLock,
                limiterKey,
                PairLoginFailureLimit);
            limiterCapacityExceeded |= !RegisterFailedLogin(
                _loginAttempts,
                _loginAttemptsLock,
                remoteLimiterKey,
                RemoteLoginFailureLimit);
            // Do not create account-wide lockout records for arbitrary
            // usernames.  Otherwise an unauthenticated caller could fill
            // the account table or deliberately lock a name that does not
            // exist.  Known (including disabled) accounts still receive the
            // stronger account-scoped protection.
            if (user is not null)
            {
                limiterCapacityExceeded |= !RegisterFailedLogin(
                    _accountLoginAttempts,
                    _accountLoginAttemptsLock,
                    accountLimiterKey,
                    AccountLoginFailureLimit);
                accountThrottled = IsLoginLocked(
                    _accountLoginAttempts,
                    accountLimiterKey,
                    DateTimeOffset.UtcNow);
            }
            AppLogService.Info($"Login failed. username={normalizedUsername}, remote={remoteKey}");
            return (
                false,
                accountThrottled || limiterCapacityExceeded
                    ? StatusCodes.Status429TooManyRequests
                    : StatusCodes.Status401Unauthorized,
                accountThrottled || limiterCapacityExceeded
                    ? "Too many unsuccessful sign-in attempts. Try again later."
                    : "Invalid username or password.",
                null);
        }

        AccountSession session;
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
            }

            // The password can change while PBKDF2 runs.  Re-read the
            // canonical record before issuing a session so an old password
            // cannot become valid again after a concurrent reset.
            var currentUser = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));
            if (currentUser is null
                || !ReferenceEquals(currentUser, user)
                || !currentUser.IsEnabled
                || currentUser.PasswordChangedAt != passwordVersion
                || !string.Equals(currentUser.PasswordHash, passwordHash, StringComparison.Ordinal))
            {
                AppLogService.Info($"Login rejected after account changed. username={normalizedUsername}, remote={remoteKey}");
                return (false, StatusCodes.Status401Unauthorized, "Invalid username or password.", null);
            }

            var originalLastLoginAt = user.LastLoginAt;
            var originalUpdatedAt = user.UpdatedAt;
            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            try
            {
                SaveDatabase();
            }
            catch
            {
                user.LastLoginAt = originalLastLoginAt;
                user.UpdatedAt = originalUpdatedAt;
                throw;
            }

            session = CreateSession(user);
        }

        lock (_loginAttemptsLock)
        {
            _loginAttempts.TryRemove(limiterKey, out _);
        }

        lock (_accountLoginAttemptsLock)
        {
            _accountLoginAttempts.TryRemove(accountLimiterKey, out _);
        }
        AppLogService.Info($"Login succeeded. username={user.Username}, remote={remoteKey}");
        return (true, StatusCodes.Status200OK, null, session);
    }

    public void Logout(AccountSession session)
    {
        RemoveSession(session.Token);
    }

    public AccountSession? GetSession(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(SessionCookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (!IsSessionToken(token))
        {
            return null;
        }

        CleanupExpiredSessions(DateTimeOffset.UtcNow);

        if (!_sessions.TryGetValue(token, out var sessionRecord))
        {
            return null;
        }

        if (sessionRecord.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            RemoveSession(token);
            return null;
        }

        StoredUser? user;
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return null;
            }

            user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, sessionRecord.Username, StringComparison.OrdinalIgnoreCase));
        }

        if (user is null
            || !user.IsEnabled
            || user.PasswordChangedAt != sessionRecord.PasswordChangedAt
            || user.CreatedAt != sessionRecord.CreatedAt)
        {
            RemoveSession(token);
            return null;
        }

        return new AccountSession(
            token,
            sessionRecord.CsrfToken,
            sessionRecord.ExpiresAt,
            ToAccountUser(
                user,
                sessionRecord.PasswordChangedAt,
                sessionRecord.CreatedAt));
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

    public (bool Ok, IReadOnlyList<AccountUser> Users) ListUsers(AccountUser actor)
    {
        lock (_lock)
        {
            if (_databaseLoadFailed || !IsEnabledAdmin(actor))
            {
                return (false, []);
            }

            return (true, _database.Users
                .OrderByDescending(x => x.IsAdmin)
                .ThenBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
                .Select(user => ToAccountUser(user))
                .ToArray());
        }
    }

    public bool IsCurrentAdmin(AccountUser actor)
    {
        lock (_lock)
        {
            return !_databaseLoadFailed && IsEnabledAdmin(actor);
        }
    }

    public (bool Ok, int StatusCode, string? Error, AccountUser? User) CreateUser(
        CreateUserRequest request,
        AccountUser actor)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        var validationError = ValidateUsername(normalizedUsername)
                              ?? ValidateDisplayName(request.DisplayName)
                              ?? ValidatePassword(request.Password);
        if (validationError is not null)
        {
            return (false, StatusCodes.Status400BadRequest, validationError, null);
        }

        lock (_lock)
        {
            var stateError = ValidateCreateUserStateLocked(normalizedUsername, actor);
            if (stateError is not null)
            {
                return (false, stateError.Value.StatusCode, stateError.Value.Error, null);
            }
        }

        if (!TryCreatePasswordHash(request.Password!, out var passwordHash))
        {
            return (false, StatusCodes.Status429TooManyRequests, "Too many password operations. Try again later.", null);
        }

        lock (_lock)
        {
            var stateError = ValidateCreateUserStateLocked(normalizedUsername, actor);
            if (stateError is not null)
            {
                return (false, stateError.Value.StatusCode, stateError.Value.Error, null);
            }

            var now = DateTimeOffset.UtcNow;
            var user = new StoredUser
            {
                Username = normalizedUsername,
                DisplayName = NormalizeDisplayName(request.DisplayName, normalizedUsername),
                PasswordHash = passwordHash,
                IsAdmin = request.IsAdmin,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordChangedAt = now
            };

            _database.Users.Add(user);
            try
            {
                SaveDatabase();
            }
            catch
            {
                _database.Users.Remove(user);
                throw;
            }

            ClearAccountLoginAttempts(normalizedUsername);
            AppLogService.Info($"User account created. username={user.Username}, isAdmin={user.IsAdmin}");
            return (true, StatusCodes.Status200OK, null, ToAccountUser(user));
        }
    }

    public (bool Ok, int StatusCode, string? Error, AccountUser? User) UpdateUser(
        string username,
        UpdateUserRequest request,
        AccountUser actor)
    {
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var displayNameError = ValidateDisplayName(request.DisplayName);
            if (displayNameError is not null)
            {
                return (false, StatusCodes.Status400BadRequest, displayNameError, null);
            }
        }

        lock (_lock)
        {
            var state = ValidateUpdateStateLocked(username, request, actor);
            if (state.Error is not null)
            {
                return (false, state.StatusCode, state.Error, null);
            }
        }

        string? newPasswordHash = null;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var passwordError = ValidatePassword(request.Password);
            if (passwordError is not null)
            {
                return (false, StatusCodes.Status400BadRequest, passwordError, null);
            }

            if (!TryCreatePasswordHash(request.Password!, out newPasswordHash))
            {
                return (false, StatusCodes.Status429TooManyRequests, "Too many password operations. Try again later.", null);
            }
        }

        lock (_lock)
        {
            var state = ValidateUpdateStateLocked(username, request, actor);
            if (state.Error is not null || state.User is null)
            {
                return (false, state.StatusCode, state.Error ?? "User not found.", null);
            }

            var user = state.User;

            var originalDisplayName = user.DisplayName;
            var originalIsAdmin = user.IsAdmin;
            var originalIsEnabled = user.IsEnabled;
            var originalPasswordHash = user.PasswordHash;
            var originalPasswordChangedAt = user.PasswordChangedAt;
            var originalUpdatedAt = user.UpdatedAt;
            var shouldInvalidateSessions = false;

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.DisplayName = NormalizeDisplayName(request.DisplayName, user.Username);
            }

            if (newPasswordHash is not null)
            {
                user.PasswordHash = newPasswordHash;
                user.PasswordChangedAt = NextPasswordChangedAt(user.PasswordChangedAt);
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
                user.UpdatedAt = originalUpdatedAt;
                return (false, StatusCodes.Status400BadRequest, "At least one enabled admin account is required.", null);
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            try
            {
                SaveDatabase();
            }
            catch
            {
                user.DisplayName = originalDisplayName;
                user.IsAdmin = originalIsAdmin;
                user.IsEnabled = originalIsEnabled;
                user.PasswordHash = originalPasswordHash;
                user.PasswordChangedAt = originalPasswordChangedAt;
                user.UpdatedAt = originalUpdatedAt;
                throw;
            }

            if (shouldInvalidateSessions)
            {
                InvalidateUserSessions(user.Username);
            }

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

            if (!IsEnabledAdmin(actor))
            {
                return (false, StatusCodes.Status403Forbidden, "Admin permission required.");
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

            var originalIndex = _database.Users.IndexOf(user);
            _database.Users.RemoveAt(originalIndex);
            if (!HasEnabledAdmin())
            {
                _database.Users.Insert(originalIndex, user);
                return (false, StatusCodes.Status400BadRequest, "At least one enabled admin account is required.");
            }

            try
            {
                SaveDatabase();
            }
            catch
            {
                _database.Users.Insert(Math.Min(originalIndex, _database.Users.Count), user);
                throw;
            }

            InvalidateUserSessions(user.Username);
            ClearAccountLoginAttempts(user.Username);
            AppLogService.Info($"User account deleted. username={user.Username}, actor={actor.Username}");
            return (true, StatusCodes.Status200OK, null);
        }
    }

    public (bool Ok, int StatusCode, string? Error) ChangePassword(
        AccountUser actor,
        ChangePasswordRequest request)
    {
        var passwordError = ValidatePassword(request.NewPassword);
        if (passwordError is not null)
        {
            return (false, StatusCodes.Status400BadRequest, passwordError);
        }

        if (request.CurrentPassword is { Length: > 256 })
        {
            return (false, StatusCodes.Status400BadRequest, "Password is too long.");
        }

        StoredUser? user;
        string currentPasswordHash;
        DateTimeOffset passwordVersion;
        lock (_lock)
        {
            if (_databaseLoadFailed)
            {
                return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.");
            }

            user = _database.Users.FirstOrDefault(x =>
                string.Equals(x.Username, actor.Username, StringComparison.OrdinalIgnoreCase));
            if (user is null || !user.IsEnabled || !IsCurrentSessionSnapshot(user, actor))
            {
                return (false, StatusCodes.Status401Unauthorized, "Login required.");
            }

            currentPasswordHash = user.PasswordHash;
            passwordVersion = user.PasswordChangedAt;
        }

        if (!_loginVerificationGate.Wait(0))
        {
            return (false, StatusCodes.Status429TooManyRequests, "Too many password operations. Try again later.");
        }

        try
        {
            if (!VerifyPassword(request.CurrentPassword ?? string.Empty, currentPasswordHash))
            {
                return (false, StatusCodes.Status401Unauthorized, "Invalid current password.");
            }

            var newPasswordHash = CreatePasswordHash(request.NewPassword!);

            lock (_lock)
            {
                if (_databaseLoadFailed)
                {
                    return (false, StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.");
                }

                var currentUser = _database.Users.FirstOrDefault(x =>
                    string.Equals(x.Username, actor.Username, StringComparison.OrdinalIgnoreCase));
                if (currentUser is null
                    || !currentUser.IsEnabled
                    || !ReferenceEquals(currentUser, user)
                    || currentUser.PasswordChangedAt != passwordVersion
                    || !string.Equals(currentUser.PasswordHash, currentPasswordHash, StringComparison.Ordinal))
                {
                    return (false, StatusCodes.Status401Unauthorized, "Invalid current password.");
                }

                var originalPasswordHash = currentUser.PasswordHash;
                var originalPasswordChangedAt = currentUser.PasswordChangedAt;
                var originalUpdatedAt = currentUser.UpdatedAt;
                currentUser.PasswordHash = newPasswordHash;
                currentUser.PasswordChangedAt = NextPasswordChangedAt(currentUser.PasswordChangedAt);
                currentUser.UpdatedAt = DateTimeOffset.UtcNow;
                try
                {
                    SaveDatabase();
                }
                catch
                {
                    currentUser.PasswordHash = originalPasswordHash;
                    currentUser.PasswordChangedAt = originalPasswordChangedAt;
                    currentUser.UpdatedAt = originalUpdatedAt;
                    throw;
                }

                InvalidateUserSessions(currentUser.Username);
                return (true, StatusCodes.Status200OK, null);
            }
        }
        finally
        {
            _loginVerificationGate.Release();
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
        var now = DateTimeOffset.UtcNow;
        var token = RandomToken();
        var csrfToken = RandomToken();
        var expiresAt = now.Add(SessionLifetime);
        var sessionRecord = new SessionRecord(
            user.Username,
            csrfToken,
            expiresAt,
            user.PasswordChangedAt,
            user.CreatedAt);

        lock (_sessionsLock)
        {
            CleanupExpiredSessionsCore(now);

            var userSessions = _sessions
                .Where(entry => string.Equals(entry.Value.Username, user.Username, StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Value.ExpiresAt)
                .ToArray();

            while (_sessions.Count >= MaxSessionEntries || userSessions.Length >= MaxSessionsPerUser)
            {
                var candidate = userSessions.Length > 0
                    ? userSessions[0]
                    : _sessions.OrderBy(entry => entry.Value.ExpiresAt).FirstOrDefault();

                if (candidate.Equals(default(KeyValuePair<string, SessionRecord>)))
                {
                    break;
                }

                _sessions.TryRemove(candidate.Key, out _);
                userSessions = userSessions.Skip(1).ToArray();
            }

            _sessions[token] = sessionRecord;
        }

        return new AccountSession(
            token,
            csrfToken,
            expiresAt,
            ToAccountUser(user, user.PasswordChangedAt, user.CreatedAt));
    }

    private void CleanupExpiredSessions(DateTimeOffset now)
    {
        var lastCleanupTicks = Interlocked.Read(ref _lastSessionCleanupTicks);
        if (now.Ticks - lastCleanupTicks < SessionCleanupInterval.Ticks)
        {
            return;
        }

        if (Interlocked.CompareExchange(
                ref _lastSessionCleanupTicks,
                now.Ticks,
                lastCleanupTicks) != lastCleanupTicks)
        {
            return;
        }

        lock (_sessionsLock)
        {
            CleanupExpiredSessionsCore(now);
        }
    }

    private void CleanupExpiredSessionsCore(DateTimeOffset now)
    {
        foreach (var entry in _sessions)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _sessions.TryRemove(entry.Key, out _);
            }
        }
    }

    private AccountDatabase LoadDatabase()
    {
        EnsureDataDirectory();
        try
        {
            if (IsReparsePoint(_databasePath))
            {
                throw new InvalidDataException("Accounts database must be a regular file.");
            }

            if (!File.Exists(_databasePath))
            {
                return new AccountDatabase();
            }

            var fileInfo = new FileInfo(_databasePath);
            if (fileInfo.Length <= 0 || fileInfo.Length > MaxAccountDatabaseBytes)
            {
                throw new InvalidDataException("Accounts database size is outside the supported range.");
            }

            TryApplyPrivateFilePermissions(_databasePath);
            var json = File.ReadAllText(_databasePath);
            var database = JsonSerializer.Deserialize<AccountDatabase>(json, JsonOptions);
            if (!ValidateLoadedDatabase(database, out var validationError))
            {
                throw new InvalidDataException(validationError);
            }

            return database!;
        }
        catch (Exception ex)
        {
            AppLogService.Error("Failed to load accounts database", ex);
            _databaseLoadFailed = true;
            return new AccountDatabase();
        }
    }

    private static bool ValidateLoadedDatabase(AccountDatabase? database, out string error)
    {
        if (database is null)
        {
            error = "Accounts database is empty.";
            return false;
        }

        if (database.Version != 1)
        {
            error = "Accounts database version is unsupported.";
            return false;
        }

        if (database.Users is null || database.Users.Count == 0)
        {
            // An existing empty database must not silently re-enter setup;
            // otherwise a truncated or overwritten file could reset admin
            // protection on the next launch.
            error = "Accounts database contains no users.";
            return false;
        }

        if (database.Users.Count > MaxUsers)
        {
            error = "Accounts database contains too many users.";
            return false;
        }

        var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in database.Users)
        {
            if (user is null)
            {
                error = "Accounts database contains an invalid user entry.";
                return false;
            }

            user.Username = NormalizeUsername(user.Username);
            if (ValidateUsername(user.Username) is not null || !usernames.Add(user.Username))
            {
                error = "Accounts database contains an invalid or duplicate username.";
                return false;
            }

            var displayNameError = ValidateDisplayName(user.DisplayName);
            if (displayNameError is not null)
            {
                error = displayNameError;
                return false;
            }

            user.DisplayName = NormalizeDisplayName(user.DisplayName, user.Username);
            if (!TryDecodePasswordHash(user.PasswordHash, out _, out _, out _))
            {
                error = "Accounts database contains an invalid password hash.";
                return false;
            }
        }

        if (!database.Users.Any(x => x.IsAdmin && x.IsEnabled))
        {
            error = "Accounts database contains no enabled admin account.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void SaveDatabase()
    {
        // Use an unpredictable, create-once temporary name.  A fixed
        // accounts.json.tmp can be pre-created as a symlink on a misconfigured
        // shared data directory and would make the write follow that link.
        var tempPath = $"{_databasePath}.{RandomToken()}.tmp";
        var replacementCompleted = false;
        try
        {
            EnsureDataDirectory();
            var json = JsonSerializer.Serialize(_database, JsonOptions);
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (IsReparsePoint(tempPath))
            {
                throw new IOException("Accounts database temporary file is a reparse point.");
            }

            TryApplyPrivateFilePermissions(tempPath);
            if (IsReparsePoint(_databasePath))
            {
                throw new IOException("Accounts database target must be a regular file.");
            }

            File.Move(tempPath, _databasePath, overwrite: true);
            replacementCompleted = true;
            if (!File.Exists(_databasePath) || IsReparsePoint(_databasePath))
            {
                throw new IOException("Accounts database replacement could not be verified as a regular file.");
            }

            TryApplyPrivateFilePermissions(_databasePath);
        }
        catch
        {
            // A mutation is made in memory before it is persisted.  Before the
            // atomic replacement succeeds, the previous database is still
            // authoritative and callers can roll back and retry a transient
            // write failure.  Once replacement succeeds, a failed post-write
            // validation leaves the durable state uncertain, so fail closed.
            if (replacementCompleted)
            {
                _databaseLoadFailed = true;
                lock (_sessionsLock)
                {
                    _sessions.Clear();
                }
            }

            throw;
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                // The database has already been replaced; stale temp cleanup
                // must not turn a successful account update into an error.
            }
        }
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

    private static DateTimeOffset NextPasswordChangedAt(DateTimeOffset previous)
    {
        var now = DateTimeOffset.UtcNow;
        return now > previous ? now : previous.AddTicks(1);
    }

    private static string? ValidateDisplayName(string? displayName)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        if (normalized.Length > MaxDisplayNameLength)
        {
            return $"Display name must be at most {MaxDisplayNameLength} characters.";
        }

        return normalized.Any(char.IsControl)
            ? "Display name contains unsupported control characters."
            : null;
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

    private static bool IsLoginLocked(
        ConcurrentDictionary<string, LoginAttemptRecord> attempts,
        string key,
        DateTimeOffset now)
    {
        return attempts.TryGetValue(key, out var attempt)
               && attempt.LockedUntil > now;
    }

    private bool TryReserveAccountLoginVerification(string key, DateTimeOffset now)
    {
        lock (_accountLoginAttemptsLock)
        {
            CleanupLoginAttemptsCore(_accountLoginAttempts, now);
            if (!_accountLoginAttempts.TryGetValue(key, out var attempt)
                || attempt.LockedUntil <= now)
            {
                return true;
            }

            if (attempt.LastAttemptAt + AccountLoginVerificationInterval > now)
            {
                return false;
            }

            // Reserve the single verification slot before PBKDF2 so parallel
            // requests for the same protected account cannot all pass the
            // interval check at once. A failed verification extends the
            // protection window; a successful one removes the bucket.
            _accountLoginAttempts[key] = attempt with { LastAttemptAt = now };
            return true;
        }
    }

    private static bool RegisterFailedLogin(
        ConcurrentDictionary<string, LoginAttemptRecord> attempts,
        object attemptsLock,
        string key,
        int failureLimit)
    {
        // Keep this bounded even when a caller sprays unique usernames or
        // source addresses. Cleanup runs before every insertion. If the
        // table is full of active locks, preserve those locks and fail closed
        // for this new key instead of evicting a valid lock.
        lock (attemptsLock)
        {
            // Take the timestamp after password verification.  Requests can
            // finish out of order; using a timestamp captured before PBKDF2
            // would let a slow request move the window or lock expiry back
            // into the past.
            var attemptNow = DateTimeOffset.UtcNow;
            CleanupLoginAttemptsCore(attempts, attemptNow);
            if (!attempts.ContainsKey(key)
                && !EnsureLoginAttemptCapacity(attempts, attemptNow, reserveEntry: true))
            {
                // Preserve active lock records under a large spray. Callers
                // convert this into a 429 rather than allowing an untracked
                // attempt to proceed indefinitely.
                return false;
            }

            attempts.AddOrUpdate(
                key,
                _ => new LoginAttemptRecord(1, DateTimeOffset.MinValue, attemptNow),
                (_, current) =>
                {
                    var attempts = current.LastAttemptAt + LoginAttemptWindow <= attemptNow
                        ? 1
                        : Math.Min(current.Attempts + 1, failureLimit);
                    var lockedUntil = attempts >= failureLimit
                        ? attemptNow.Add(LoginLockDuration)
                        : DateTimeOffset.MinValue;
                    return new LoginAttemptRecord(attempts, lockedUntil, attemptNow);
                });
            return true;
        }
    }

    private void CleanupLoginAttempts(DateTimeOffset now)
    {
        var lastCleanupTicks = Interlocked.Read(ref _lastLoginAttemptCleanupTicks);
        if (now.Ticks - lastCleanupTicks < LoginAttemptCleanupInterval.Ticks)
        {
            return;
        }

        if (Interlocked.CompareExchange(
                ref _lastLoginAttemptCleanupTicks,
                now.Ticks,
                lastCleanupTicks) != lastCleanupTicks)
        {
            return;
        }

        lock (_loginAttemptsLock)
        {
            CleanupLoginAttemptsCore(_loginAttempts, now);
            EnsureLoginAttemptCapacity(_loginAttempts, now, reserveEntry: false);
        }

        lock (_accountLoginAttemptsLock)
        {
            CleanupLoginAttemptsCore(_accountLoginAttempts, now);
            EnsureLoginAttemptCapacity(_accountLoginAttempts, now, reserveEntry: false);
        }
    }

    private static void CleanupLoginAttemptsCore(
        ConcurrentDictionary<string, LoginAttemptRecord> attempts,
        DateTimeOffset now)
    {
        foreach (var entry in attempts)
        {
            if (entry.Value.LastAttemptAt + LoginAttemptRetention <= now)
            {
                attempts.TryRemove(entry.Key, out _);
            }
        }
    }

    private static bool EnsureLoginAttemptCapacity(
        ConcurrentDictionary<string, LoginAttemptRecord> attempts,
        DateTimeOffset now,
        bool reserveEntry)
    {
        var targetCapacity = MaxLoginAttemptEntries - (reserveEntry ? 1 : 0);
        var excess = attempts.Count - targetCapacity;
        if (excess <= 0)
        {
            return true;
        }

        // Never evict a still-active lock just to admit a new spray key.  If
        // the table contains only active locks, leave it intact and reject
        // the new record instead of weakening an existing lock.
        var candidates = attempts
            .Where(entry => entry.Value.LockedUntil <= now)
            .OrderBy(entry => entry.Value.LastAttemptAt)
            .Take(excess)
            .ToArray();

        if (candidates.Length < excess)
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            attempts.TryRemove(candidate.Key, out _);
        }

        return true;
    }

    private static string BuildLoginLimiterKey(string remoteKey, string username)
    {
        return $"pair|{remoteKey.ToLowerInvariant()}|{username.ToLowerInvariant()}";
    }

    private static string BuildAccountLoginLimiterKey(string username)
    {
        return username.ToLowerInvariant();
    }

    private static string BuildRemoteLoginLimiterKey(string remoteKey)
    {
        return $"remote|{remoteKey.ToLowerInvariant()}";
    }

    private bool HasEnabledAdmin()
    {
        return _database.Users.Any(x => x.IsAdmin && x.IsEnabled);
    }

    private (int StatusCode, string Error)? ValidateSetupStateLocked()
    {
        if (_databaseLoadFailed)
        {
            return (StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.");
        }

        return _database.Users.Count > 0
            ? (StatusCodes.Status409Conflict, "Setup has already been completed.")
            : null;
    }

    private (int StatusCode, string Error)? ValidateCreateUserStateLocked(
        string normalizedUsername,
        AccountUser actor)
    {
        if (_databaseLoadFailed)
        {
            return (StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.");
        }

        if (!IsEnabledAdmin(actor))
        {
            return (StatusCodes.Status403Forbidden, "Admin permission required.");
        }

        if (_database.Users.Count >= MaxUsers)
        {
            return (StatusCodes.Status409Conflict, "The maximum number of users has been reached.");
        }

        return _database.Users.Any(x => string.Equals(x.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase))
            ? (StatusCodes.Status409Conflict, "Username already exists.")
            : null;
    }

    private (int StatusCode, string? Error, StoredUser? User) ValidateUpdateStateLocked(
        string username,
        UpdateUserRequest request,
        AccountUser actor)
    {
        if (_databaseLoadFailed)
        {
            return (StatusCodes.Status500InternalServerError, "Accounts database could not be loaded.", null);
        }

        if (!IsEnabledAdmin(actor))
        {
            return (StatusCodes.Status403Forbidden, "Admin permission required.", null);
        }

        var user = _database.Users.FirstOrDefault(x =>
            string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return (StatusCodes.Status404NotFound, "User not found.", null);
        }

        var changingOwnAdmin = string.Equals(user.Username, actor.Username, StringComparison.OrdinalIgnoreCase);
        if (request.IsEnabled == false && changingOwnAdmin)
        {
            return (StatusCodes.Status400BadRequest, "You cannot disable your current account.", null);
        }

        if (request.IsAdmin == false && changingOwnAdmin)
        {
            return (StatusCodes.Status400BadRequest, "You cannot remove admin permission from your current account.", null);
        }

        return (StatusCodes.Status200OK, null, user);
    }

    private bool IsEnabledAdmin(AccountUser actor)
    {
        return _database.Users.Any(x =>
            x.IsEnabled
            && x.IsAdmin
            && string.Equals(x.Username, actor.Username, StringComparison.OrdinalIgnoreCase)
            && IsCurrentSessionSnapshot(x, actor));
    }

    private static bool IsCurrentSessionSnapshot(StoredUser user, AccountUser actor)
    {
        return actor.SessionPasswordChangedAt is { } passwordChangedAt
               && actor.SessionUserCreatedAt is { } createdAt
               && user.PasswordChangedAt == passwordChangedAt
               && user.CreatedAt == createdAt;
    }

    private void InvalidateUserSessions(string username)
    {
        lock (_sessionsLock)
        {
            foreach (var session in _sessions)
            {
                if (string.Equals(session.Value.Username, username, StringComparison.OrdinalIgnoreCase))
                {
                    _sessions.TryRemove(session.Key, out _);
                }
            }
        }
    }

    private void ClearAccountLoginAttempts(string username)
    {
        lock (_accountLoginAttemptsLock)
        {
            _accountLoginAttempts.TryRemove(BuildAccountLoginLimiterKey(username), out _);
        }
    }

    private void RemoveSession(string token)
    {
        lock (_sessionsLock)
        {
            _sessions.TryRemove(token, out _);
        }
    }

    private static AccountUser ToAccountUser(
        StoredUser user,
        DateTimeOffset? sessionPasswordChangedAt = null,
        DateTimeOffset? sessionUserCreatedAt = null)
    {
        return new AccountUser(
            user.Username,
            user.DisplayName,
            user.IsAdmin,
            user.IsEnabled,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt)
        {
            SessionPasswordChangedAt = sessionPasswordChangedAt,
            SessionUserCreatedAt = sessionUserCreatedAt
        };
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

    private bool TryCreatePasswordHash(string password, out string passwordHash)
    {
        passwordHash = string.Empty;
        if (!_loginVerificationGate.Wait(0))
        {
            return false;
        }

        try
        {
            passwordHash = CreatePasswordHash(password);
            return true;
        }
        finally
        {
            _loginVerificationGate.Release();
        }
    }

    private static bool VerifyPassword(string password, string encodedHash)
    {
        if (!TryDecodePasswordHash(encodedHash, out var iterations, out var salt, out var expectedHash))
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static bool TryDecodePasswordHash(
        string? encodedHash,
        out int iterations,
        out byte[] salt,
        out byte[] expectedHash)
    {
        iterations = 0;
        salt = [];
        expectedHash = [];

        if (string.IsNullOrEmpty(encodedHash) || encodedHash.Length > 256)
        {
            return false;
        }

        var parts = encodedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
        {
            return false;
        }

        if (!int.TryParse(
                parts[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out iterations)
            || iterations < MinimumPasswordHashIterations
            || iterations > MaximumPasswordHashIterations
            || parts[2].Length > 128
            || parts[3].Length > 128)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
            return salt.Length == SaltBytes && expectedHash.Length == HashBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string RandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static bool IsSessionToken(string token)
    {
        if (token.Length != 64)
        {
            return false;
        }

        foreach (var character in token)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureDataDirectory()
    {
        var dataDirectory = AppLogService.DataDirectory;
        Directory.CreateDirectory(dataDirectory);
        if (IsReparsePoint(dataDirectory))
        {
            throw new InvalidDataException("Accounts data directory must not be a reparse point.");
        }

        TryApplyPrivateDirectoryPermissions(dataDirectory);
    }

    private static void TryApplyPrivateDirectoryPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            if (IsReparsePoint(path))
            {
                return;
            }

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
            if (IsReparsePoint(path))
            {
                return;
            }

            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Best-effort hardening only.
        }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
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
        DateTimeOffset PasswordChangedAt,
        DateTimeOffset CreatedAt);

    private sealed record LoginAttemptRecord(
        int Attempts,
        DateTimeOffset LockedUntil,
        DateTimeOffset LastAttemptAt);
}
