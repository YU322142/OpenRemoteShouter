namespace RemoteShouter.Models;

public sealed record AccountUser(
    string Username,
    string DisplayName,
    bool IsAdmin,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record AccountSession(
    string Token,
    string CsrfToken,
    DateTimeOffset ExpiresAt,
    AccountUser User);

public sealed record AuthState(
    bool SetupRequired,
    AccountUser? User,
    string? CsrfToken);

public sealed record SetupAdminRequest(
    string? Username,
    string? DisplayName,
    string? Password);

public sealed record LoginRequest(
    string? Username,
    string? Password);

public sealed record CreateUserRequest(
    string? Username,
    string? DisplayName,
    string? Password,
    bool IsAdmin);

public sealed record UpdateUserRequest(
    string? DisplayName,
    string? Password,
    bool? IsAdmin,
    bool? IsEnabled);

public sealed record ChangePasswordRequest(
    string? CurrentPassword,
    string? NewPassword);
