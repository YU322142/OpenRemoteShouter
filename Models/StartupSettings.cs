namespace RemoteShouter.Models;

public sealed record StartupSettings(
    bool AllowLan,
    string TrustedProxyIps,
    bool AllowTrustedProxySetup,
    string TrustedProxySetupToken,
    string HttpsCertificatePath,
    string HttpsCertificatePassword,
    bool RequireHttps);
