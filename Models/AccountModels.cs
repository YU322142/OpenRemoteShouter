using System.Text.Json.Serialization;

namespace RemoteShouter.Models;

public sealed record AccountUser(
    string Username,
    string DisplayName,
    bool IsAdmin,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt)
{
    // Kept out of the public JSON contract. AccountService uses these values
    // to reject in-flight requests after password reset or account replacement.
    [JsonIgnore]
    internal DateTimeOffset? SessionPasswordChangedAt { get; init; }

    [JsonIgnore]
    internal DateTimeOffset? SessionUserCreatedAt { get; init; }
}

public sealed record AccountSession(
    string Token,
    string CsrfToken,
    DateTimeOffset ExpiresAt,
    AccountUser User);

public sealed record AuthState(
    bool SetupRequired,
    AccountUser? User,
    string? CsrfToken,
    bool RemoteSetupEnabled = false);

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
