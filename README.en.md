# OpenRemoteShouter

[English](README.en.md) | [简体中文](README.md)

OpenRemoteShouter is a local-network remote announcement tool. It starts a local web service on a computer so that other devices can send text through a browser or HTTP API. The target computer then shows a full-screen or windowed notice and can read it aloud with EdgeTTS.

<div align="center">

> Warning: This project was written with assistance from artificial intelligence.

> Warning: Apart from Windows and the LoongArch64 Old World ABI 1.0 build, builds for other platforms have only been confirmed to package successfully in GitHub Actions and have not yet been tested on physical hardware.

</div>

## Features

- **Dedicated LoongArch64 Old World ABI 1.0 package.**
- Remote announcements through a LAN web interface, listening on port `21212` by default.
- Full-screen topmost display or a regular popup window.
- Automatic close countdown; `0` means the notice must be closed manually.
- Chinese speech playback with EdgeTTS.
- Web forms, JSON API, and form POST support.
- Multi-architecture builds for Windows, Linux, and macOS.

## Screenshots

#### Console

![1](screenshots/1.png)

#### Window display

![2](screenshots/2.png)

#### Full-screen display

![3](screenshots/3.png)

#### Web announcement interface

![4](screenshots/4.jpeg)

## Usage

1. Download the build package for your operating system.
2. Extract it and run:
   - Windows: run `run.bat` (not recommended) or `OpenRemoteShouter.exe`; `run.bat` exits automatically after starting the program.
   - Linux/macOS: run `./run.sh`, which starts in the background by default. Use `./run.sh --foreground` to keep output in the terminal.
   - Portable package: install the .NET 8 Runtime first, then run `run.sh` or `run.bat`.
3. Open the console window or tray menu and copy the displayed access address.
4. By default, the service listens only on the local loopback interface. You can initialize it locally, or explicitly configure a trusted relay for remote first-run setup as described below.

If remote access still does not work, check that the firewall allows port `21212` and that the certificate and listener mode are configured correctly.

## Accounts and security

The first WebUI visit must create an administrator account. By default, only `localhost` or `127.0.0.1` may initialize it; remote teacher setup requires an explicitly allowlisted relay, HTTPS, and a high-entropy token. After initialization, both the WebUI and announcement APIs require authentication.

Implemented protections include:

- Passwords are stored with PBKDF2-SHA256 and a random salt; plaintext passwords are never stored.
- Five consecutive failures for the same source address and username lock that pair for five minutes. An existing account that accumulates 20 failures enters a five-minute account-level soft throttle; during the protection period, at most one real password verification runs every 10 seconds. Failed attempts return `429`, while a correct password can log in during the next verification window and clear the account bucket. The source address also has a limit of 100 failures per five minutes. The account bucket is created only for existing accounts, so exposing the service directly to the Internet can make the `401`/`429` difference a username-existence side channel. Use a trusted gateway to normalize responses and apply centralized rate limiting. Limiter records are process-local and are not shared across restarts or instances, so they cannot replace gateway/WAF controls.
- Login sessions use an HttpOnly, SameSite=Strict cookie with an expiration time.
- All state-changing APIs require a CSRF token.
- Changing a password, disabling a user, or deleting a user invalidates the affected sessions.
- An administrator cannot disable or delete the current account, and the system always keeps at least one enabled administrator.
- Initial administrator setup is local-only by default; remote setup requires a fixed relay IP, an explicit opt-in, HTTPS, and a high-entropy token.
- WebUI responses include baseline security headers and a Content Security Policy (CSP).
- Login verification limits failed attempts per source and bounds the number of in-memory limiter and session records.
- At startup, the account database is checked for file size, structure, user count, and password-hash parameters. A corrupt file is rejected instead of silently returning to setup mode.
- TTS cache and log files have size limits. Old files are removed or rotated when limits are reached, preventing repeated requests from filling the disk indefinitely.

If a freshly extracted copy shows “account service unavailable” or unexpectedly shows the login screen, do not keep retrying sign-in. Check the data directory used by the actual process (on Windows the default is `%LOCALAPPDATA%\\OpenRemoteShouter\\accounts.json`), its permissions, integrity, and the log. Upgrades and re-extraction do not clear existing accounts; showing the login screen is normal when a valid account file already exists, while a corrupt or empty `accounts.json` is rejected fail-closed.

### Transport security

Without a certificate, the service defaults to HTTP on the local loopback interface only. This is suitable for first-time setup or local use. HTTP does not encrypt passwords, session cookies, or CSRF tokens; a "LAN" should not be treated as a trusted network. To allow other devices to connect, prefer an HTTPS PFX certificate:

```bash
export OPEN_REMOTE_SHOUTER_HTTPS_CERT_PATH=/path/to/server.pfx
read -r -s -p 'PFX password: ' OPEN_REMOTE_SHOUTER_HTTPS_CERT_PASSWORD
printf '\n'
export OPEN_REMOTE_SHOUTER_HTTPS_CERT_PASSWORD
./OpenRemoteShouter
unset OPEN_REMOTE_SHOUTER_HTTPS_CERT_PASSWORD
```

The password is entered silently and interactively, so it is not written to shell history. In production, a service manager or secret store should preferably inject the environment variables.

A PFX file contains a private key. Restrict it to the service account (`chmod 600 /path/to/server.pfx` on Linux/macOS) and ensure that its parent directory is not writable by other accounts.

### Trusted relay

By default, the program does not trust forwarding headers. To let a fixed relay terminate HTTPS, set `OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS` and list only the exact TCP peer IPs observed by the application, such as `127.0.0.1` for a same-host proxy or a VPN relay address. Only `X-Forwarded-For`, `X-Forwarded-Host`, and `X-Forwarded-Proto` from those exact IPs are honored.

If first-run admin creation should also go through that relay, additionally set `OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1` and a random `OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN` of at least 32 bytes. Any request whose original TCP peer matches the relay allowlist is always treated as a relay request; changing `Host` or stripping forwarding headers cannot downgrade it to the token-free local path. The request is rejected while remote setup is disabled, and requires both HTTPS and a valid token when enabled. The token is sent only in the `X-OpenRemoteShouter-Setup-Token` header, is accepted only while the account database is empty, and is consumed after successful setup in the current process. Startup fails when the opt-in is enabled without both the IP allowlist and token.

```bash
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=127.0.0.1
export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN="$(openssl rand -base64 48)"
```

The relay must overwrite (rather than append) client-supplied `X-Forwarded-Host`, `X-Forwarded-Proto`, and `X-Forwarded-For`, and its connection to the classroom host must use FRP TLS, WireGuard, an SSH tunnel, or another protected transport. Do not put the token in a URL or use plaintext HTTP across machines. The setup page shows a token field for remote initialization. When `127.0.0.1` is allowlisted for a same-host relay, the app cannot distinguish that proxy from a direct local browser on the same address, so both are treated as relay requests. For token-free local setup, initialize before adding that loopback address, or temporarily remove it and disable remote setup before restarting the service.

For command-line remote setup, send the token as a header (never in the URL or logs):

```bash
read -r -s -p 'Setup token: ' ORS_SETUP_TOKEN
printf '\n'
curl --fail-with-body -sS \
  -H 'Content-Type: application/json' \
  -H 'Origin: https://class.example.test' \
  -H "X-OpenRemoteShouter-Setup-Token: $ORS_SETUP_TOKEN" \
  -X POST 'https://class.example.test/api/auth/setup' \
  -d '{"username":"teacher","displayName":"Teacher","password":"CHANGE-ME"}'
unset ORS_SETUP_TOKEN
```

An Nginx/Caddy-style relay should set the external host and scheme explicitly (for example, `Host $host`, `X-Forwarded-Host $host`, `X-Forwarded-Proto $scheme`, and `X-Forwarded-For $remote_addr`) and prevent clients from injecting duplicate values. With FRP, the application usually sees the same-host `frpc` peer as `127.0.0.1`, so that is the address to allowlist; do not assume the public `frps` address is visible to the classroom process. Confirm the actual TCP peer in the deployment logs.

#### FRP relay example

Recommended layout: `frps` on the public relay, `frpc` on the classroom computer, and Nginx/Caddy terminating HTTPS on the public relay. The app listens on `127.0.0.1:21212`, `frpc` maps it to `127.0.0.1:22122` on the relay, and the reverse proxy serves `https://class.example.test`.

Relay `frps.toml`:

```toml
bindPort = 7000
auth.method = "token"
auth.token = "CHANGE_TO_A_LONG_RANDOM_FRP_TOKEN"
```

Classroom `frpc.toml`:

```toml
serverAddr = "relay.example.test"
serverPort = 7000
auth.method = "token"
auth.token = "CHANGE_TO_A_LONG_RANDOM_FRP_TOKEN"

[[proxies]]
name = "open-remote-shouter"
type = "tcp"
localIP = "127.0.0.1"
localPort = 21212
remoteIP = "127.0.0.1"
remotePort = 22122
```

Public relay Nginx example:

```nginx
location / {
    proxy_pass http://127.0.0.1:22122;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Host $host;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-For $remote_addr;
    proxy_set_header X-OpenRemoteShouter-Setup-Token $http_x_openremoteshouter_setup_token;
}
```

Classroom environment variables:

```bash
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=<actual-frpc-peer-ip-seen-by-the-app>
export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN='at-least-32-random-bytes'
```

The following steps are split by operating system. Download the matching FRP release binaries and keep `frps` and `frpc` on the same FRP version.

**Windows public relay**

1. Put `frps.exe` and `frps.toml` in `C:\frp\`.
2. Allow inbound TCP `7000` in Windows Firewall. Keep `22122` local-only; do not open it to the Internet.
3. Start from PowerShell:

   ```powershell
   C:\frp\frps.exe -c C:\frp\frps.toml
   ```

   For autostart, create a Task Scheduler task that runs at system startup with program `C:\frp\frps.exe` and arguments `-c C:\frp\frps.toml`.

**Linux public relay**

1. Store `frps` at `/opt/frp/frps` and the config at `/etc/frp/frps.toml`.
2. Open only the FRP and HTTPS ports, for example:

   ```bash
   sudo ufw allow 7000/tcp
   sudo ufw allow 80,443/tcp
   sudo ufw deny 22122/tcp
   ```

3. Verify in the foreground with `sudo /opt/frp/frps -c /etc/frp/frps.toml`. For systemd, create `/etc/systemd/system/frps.service`:

   ```ini
   [Unit]
   Description=FRP server
   After=network-online.target
   [Service]
   ExecStart=/opt/frp/frps -c /etc/frp/frps.toml
   Restart=on-failure
   [Install]
   WantedBy=multi-user.target
   ```

   Then run `sudo systemctl daemon-reload && sudo systemctl enable --now frps`.

**macOS public relay**

1. Put `frps` and `frps.toml` in `~/frp/` (or `/usr/local/etc/frp/`).
2. Allow TCP `7000`, `80`, and `443` in the host firewall/security group; do not expose `22122`.
3. Start with `~/frp/frps -c ~/frp/frps.toml`. For autostart, use Login Items or a launchd agent under `~/Library/LaunchAgents/` invoking the same command.

**Windows classroom computer (OpenRemoteShouter and frpc)**

Put `frpc.exe` and `frpc.toml` in `C:\frp\`, then run in PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS", "127.0.0.1", "User")
[Environment]::SetEnvironmentVariable("OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP", "1", "User")
[Environment]::SetEnvironmentVariable("OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN", "at-least-32-random-bytes", "User")
C:\frp\frpc.exe -c C:\frp\frpc.toml
```

Restart OpenRemoteShouter after changing environment variables. If the app and `frpc` are on different machines, replace `127.0.0.1` with the TCP peer IP shown in the app logs.

**Linux classroom computer**

Save the config as `/etc/frp/frpc.toml` and run:

```bash
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=127.0.0.1
export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN='at-least-32-random-bytes'
/opt/frp/frpc -c /etc/frp/frpc.toml
```

When the app runs under systemd, put these variables in its `Environment=` or `EnvironmentFile=` instead of only in an interactive shell.

**macOS classroom computer**

Save `frpc` and `frpc.toml` under `~/frp/`, then start from the same terminal as the app:

```zsh
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=127.0.0.1
export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN='at-least-32-random-bytes'
~/frp/frpc -c ~/frp/frpc.toml
```

If launchd starts the app, put the same variables in the plist's `EnvironmentVariables` and reload the plist.

**Verify the relay**

Windows PowerShell:

```powershell
Invoke-RestMethod https://class.example.test/api/auth/state | ConvertTo-Json
```

Linux/macOS:

```bash
curl -fsS https://class.example.test/api/auth/state
```

The response should include `setupRequired: true` for an empty account database and `remoteSetupEnabled: true`. Submit the setup token in the page to create the first administrator. The FRP `auth.token` and OpenRemoteShouter setup token are separate secrets and must not be reused.

Source-address throttling uses the TCP peer address actually observed by the application. If the relay does not forward the client address correctly, multiple users may share one source bucket. A successful login clears only the source-plus-username bucket, not the source-wide failure counter. If many users share one egress address, use finer-grained limiting at a trusted gateway and avoid exposing the application over plaintext HTTP.

For deployment scripts that should refuse to start without a certificate, also set `OPEN_REMOTE_SHOUTER_REQUIRE_HTTPS=1`.

With a certificate configured, the service provides HTTPS on that port and displays `https://` access addresses; cookies automatically receive the `Secure` attribute. If a trusted relay terminates TLS, the app can keep listening on loopback HTTP, but you must apply the allowlist above and have the relay set the forwarding headers correctly; the app will then use the external HTTPS scheme for same-origin checks and secure cookies. Remote first-run setup additionally requires the explicit opt-in and token.

If compatibility with legacy plaintext LAN deployments is required, explicitly set `OPEN_REMOTE_SHOUTER_ALLOW_INSECURE_HTTP=1` to listen on all interfaces. Startup logs continuously warn about this mode. Passwords, session cookies, and CSRF tokens can be sniffed on the network, so this mode should not be used in production.

Account data is stored in `accounts.json` under the system user-data directory by default. To choose another data directory:

```bash
OPEN_REMOTE_SHOUTER_DATA_DIR=/path/to/data ./OpenRemoteShouter
```

The custom data directory and the parent directory of `OPEN_REMOTE_SHOUTER_LOG_FILE` must be private to the service account. Do not place them in a directory writable by other accounts. On Windows, the program does not enforce ACL changes; file permissions depend on the directory security configuration.

## Linux audio dependencies

EdgeTTS generates MP3 audio by default. On Linux, the program searches for these players in order:

When speech playback is enabled, announcement text is sent to Microsoft's Bing EdgeTTS service for synthesis. Disable speech for sensitive content, or use a self-hosted TTS service.

- `ffplay`
- `mpv`
- `pw-play`
- `cvlc`
- `vlc`

For older environments that support WAV only, change the EdgeTTS output format:

```bash
OPEN_REMOTE_SHOUTER_EDGE_TTS_FORMAT=wav ./OpenRemoteShouter
```

With WAV selected, the program also tries `paplay` and `aplay`. The LoongArch64 Old World ABI 1.0 packaging script uses WAV by default.

To override the selected player temporarily:

```bash
OPEN_REMOTE_SHOUTER_AUDIO_PLAYER=ffplay ./OpenRemoteShouter
```

Common installation commands:

```bash
# Debian/Ubuntu
sudo apt install pulseaudio-utils alsa-utils ffmpeg

# Fedora
sudo dnf install pulseaudio-utils alsa-utils ffmpeg

# Arch Linux
sudo pacman -S libpulse alsa-utils ffmpeg
```

If no player is available, the console reports a speech-backend error while text announcements continue to work.

## Logs and troubleshooting

The program writes lightweight runtime logs for troubleshooting EdgeTTS synthesis, audio caching, player selection, and player errors.

Common log locations:

- Packaged launch: `logs/openremoteshouter.log` inside the extracted directory.
- Windows: `%LOCALAPPDATA%\\OpenRemoteShouter\\OpenRemoteShouter.log`
- Linux/macOS: `~/.local/share/OpenRemoteShouter/OpenRemoteShouter.log`

When the main program is run directly from a Linux/macOS terminal, or with `./run.sh --foreground`, logs are also printed to the terminal. Set `OPEN_REMOTE_SHOUTER_LOG_CONSOLE=1` to force console logging.

Logs default to 10 MiB with up to three rotated files (for example `.1`, `.2`, `.3`). Adjust the byte and file-count limits for the deployment:

```bash
export OPEN_REMOTE_SHOUTER_LOG_MAX_BYTES=10485760
export OPEN_REMOTE_SHOUTER_LOG_MAX_FILES=3
```

The TTS cache is limited by default to 256 MiB and 512 audio files. It keeps only program-generated audio in the current cache directory and removes the oldest files first when limits are exceeded:

```bash
export OPEN_REMOTE_SHOUTER_TTS_CACHE_MAX_BYTES=268435456
export OPEN_REMOTE_SHOUTER_TTS_CACHE_MAX_FILES=512
```

Each TTS WebSocket message and audio response is also limited to 16 MiB, preventing a faulty or uncontrolled upstream response from consuming memory without bound.

Invalid or undersized values fall back to safe defaults. Cache and log limits affect only persistent files; they do not disable text announcements.

Administrators calling `GET /api/status` can see the actual `logFilePath`; ordinary users receive a redacted value. A speech error in the console also includes the log path.

If TTS produces no sound on Linux x64 or LoongArch, first inspect log entries containing `Audio backend candidates`, `Trying audio player`, `Audio player failed`, and `EdgeTTS synthesis completed`.

## LoongArch64 Old World ABI 1.0 compatibility

The LoongArch Old World build follows ClassIsland's X11 compatibility approach:

- Avalonia software rendering is enabled by default.
- `LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`, and `AVALONIA_RENDERING_FORCE_SOFTWARE=1` are set by default.
- DBus menus and DBus file pickers are disabled by default.
- `OPEN_REMOTE_SHOUTER_X11_ENABLE_IME=auto` detects Fcitx DBus automatically. IME is disabled when Fcitx is unavailable so an X11 input-method failure does not affect the UI.
- WAV EdgeTTS output remains the default to reduce audio-decoder differences on older systems.

Overrides are available:

```bash
OPEN_REMOTE_SHOUTER_SOFTWARE_RENDERING=0 ./run.sh --foreground
OPEN_REMOTE_SHOUTER_X11_ENABLE_IME=1 ./run.sh --foreground
```

## HTTP API

After startup:

- `GET /`: web announcement form.
- `GET /api/auth/state`: current login and initialization state.
- `POST /api/auth/login`: log in and create a session.
- `POST /api/auth/logout`: log out the current session.
- `POST /api/auth/password`: change the current account password.
- `GET /api/status`: service status.
- `GET /api/voices`: available voices.
- `POST /api/shout`: send an announcement.
- `POST /api/close`: close the current display.
- `GET/POST/PUT/DELETE /api/users`: administrator user management.

Except for login and first-time setup, state-changing APIs require both the login cookie and the `X-OpenRemoteShouter-CSRF` token. The `state.csrfToken` value in the login response is the token for the current session. The following `curl` example avoids placing the password directly in command-line arguments and requires `jq`:

```bash
set -eu
umask 077
base_url=http://127.0.0.1:21212
cookie_file="$(mktemp)"
trap 'rm -f "$cookie_file"; unset ORS_PASSWORD' EXIT

read -r -s -p 'Password: ' ORS_PASSWORD
printf '\n'
login_json="$(jq -n --arg username 'admin' --arg password "$ORS_PASSWORD" \
  '{username: $username, password: $password}')"
login_response="$(curl --fail-with-body -sS -c "$cookie_file" \
  -H 'Content-Type: application/json' \
  -X POST "$base_url/api/auth/login" -d "$login_json")"
csrf_token="$(printf '%s' "$login_response" | jq -r '.state.csrfToken')"

curl --fail-with-body -sS -b "$cookie_file" \
  -H 'Content-Type: application/json' \
  -H "X-OpenRemoteShouter-CSRF: $csrf_token" \
  -X POST "$base_url/api/shout" \
  -d '{
    "title": "Notice",
    "message": "This is a remote announcement.",
    "mode": "fullscreen",
    "durationSeconds": 10,
    "topmost": true,
    "speechEnabled": true,
    "voiceName": "zh-CN-XiaoyiNeural",
    "speechRate": 0,
    "speechVolume": 1.0,
    "theme": "cyan"
  }'
```

If HTTPS is enabled, change `base_url` to `https://hostname:21212` and configure `curl` certificate verification according to the certificate deployment policy. Initial setup can be performed locally or through an explicitly allowlisted trusted relay; remote `curl` requests must also send the `X-OpenRemoteShouter-Setup-Token` header.

Field reference:

| Field | Description |
| --- | --- |
| `title` | Display title; the default title is used when empty. |
| `message` | Required announcement text. |
| `mode` | `fullscreen` or `popup`. |
| `durationSeconds` | Automatic close delay, from `0` to `3600`. |
| `topmost` | Whether the window stays on top. |
| `speechEnabled` | Whether to read the announcement aloud. |
| `voiceName` | EdgeTTS voice, for example `zh-CN-XiaoyiNeural`. |
| `speechRate` | Speech rate, from `-100` to `100`. |
| `speechVolume` | Volume, from `0.0` to `1.0`. |
| `theme` | `cyan`, `blue`, `green`, `amber`, `rose`, or `violet`. |

## Build artifacts

The GitHub Actions `Build OpenRemoteShouter` workflow builds and uploads:

- `OpenRemoteShouter-portable-net8.0`
- `OpenRemoteShouter-win-x64`
- `OpenRemoteShouter-win-x86`
- `OpenRemoteShouter-win-arm64`
- `OpenRemoteShouter-linux-x64`
- `OpenRemoteShouter-linux-arm64`
- `OpenRemoteShouter-linux-arm`
- `OpenRemoteShouter-linux-musl-x64`
- `OpenRemoteShouter-linux-musl-arm64`
- `OpenRemoteShouter-osx-x64`
- `OpenRemoteShouter-osx-arm64`
- `OpenRemoteShouter-linux-loongarch64-oldworld-abi1.0`
- `OpenRemoteShouter-all-platforms` (CI-only aggregate artifact)

GitHub Releases list each platform package as a separate downloadable asset, with the filename identifying the target platform. `SHA256SUMS.txt` is provided as an additional asset, and the same SHA256 values are shown in the release body. CI still keeps `OpenRemoteShouter-all-platforms` as an internal aggregate artifact for pipeline validation, but it is not attached to the public Release.

`OpenRemoteShouter-linux-loongarch64-oldworld-abi1.0.tar.gz` is only intended for LoongArch64 Old World ABI 1.0 systems.

## Local build

.NET 8 SDK is required.

```bash
dotnet restore
dotnet build RemoteShouter.sln -c Release
dotnet publish RemoteShouter.csproj -c Release -r win-x64 --self-contained true
```

Replace the Runtime Identifier after `-r` to build another platform, such as `linux-x64`, `linux-arm64`, or `osx-arm64`.

## Notes

- EdgeTTS requires network access to Microsoft's speech service.
- Full-screen topmost behavior depends on the Linux desktop environment and window manager.
- Linux speech playback depends on a system audio player.
- Apart from Windows and LoongArch64 Old World ABI 1.0, other platform builds have not yet been tested on physical hardware.
- macOS may require allowing the application in System Security settings on first launch.
- Windows may require a firewall rule; check that the network profile is set to Private.
