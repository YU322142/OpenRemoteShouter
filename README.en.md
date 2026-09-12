# OpenRemoteShouter

[English](README.en.md) | [简体中文](README.md)

OpenRemoteShouter is a local-network remote announcement tool. It starts a local web service on a computer so that other devices can send text through a browser or HTTP API. The target computer then shows a Fluent-style animated full-screen notice and can read it aloud with EdgeTTS.

<div align="center">

> Warning: This project was written with assistance from artificial intelligence.

> Warning: Apart from Windows and the LoongArch64 Old World ABI 1.0 build, builds for other platforms have only been confirmed to package successfully in GitHub Actions and have not yet been tested on physical hardware.

</div>

## Features

- **Dedicated LoongArch64 Old World ABI 1.0 package.**
- Desktop console, system tray, and WebUI served on port `21212`.
- Loopback-only access by default; set `OPEN_REMOTE_SHOUTER_ALLOW_LAN=1` to allow direct access through a LAN IP address.
- Theme-driven animated full-screen display with nonlinear title and message entrance.
- Display time is derived from the actual TTS audio file duration, with a ten-second minimum.
- EdgeTTS speech playback with voice, rate, and volume controls.
- Cancel the current display after sending, or continue with another announcement.
- Account login, editable display names, 20 light/dark themes, administrator user management, and migration of themes from older account files.
- WebUI and JSON HTTP API with session and CSRF validation for state-changing operations.
- Multi-architecture builds for Windows, Linux, and macOS.
- The desktop uses FluentAvalonia and the WebUI bundles Fluent UI Web Components locally without a CDN dependency.

## Screenshots

#### Console

![1](screenshots/1.png)

#### Animated full-screen display

![2](screenshots/2.png)

#### Web announcement interface

![3](screenshots/3.png)
![4](screenshots/4.png)
![5](screenshots/5.png)

## Quick start

### Local initialization

1. Download and extract the Release package for your operating system.
2. Start the program:
   - Windows: run `run.bat`, or launch the packaged `OpenRemoteShouter.exe` directly.
   - Linux/macOS: run `./run.sh`; packaged scripts start in the background by default, while `./run.sh --foreground` keeps output in the terminal.
   - Portable package: install the .NET 8 Runtime first, then run the included `run.bat` or `run.sh`.
3. Left-click the tray icon to open the console and right-click it to open the native tray menu. The menu can also copy an access address or run a local display test.
4. Open `http://localhost:21212/` on the same computer. Create the first administrator account, then sign in.

By default, the service listens only on `localhost` / `127.0.0.1`, so phones and other computers cannot connect directly. First-time administrator creation is also local-only by default.

### Enable LAN access

To let a phone or computer on the same LAN connect through this computer's IP address, set the following value **before starting the program**:

```text
OPEN_REMOTE_SHOUTER_ALLOW_LAN=1
```

The packaged `run.bat` and `run.sh` files contain a commented configuration line that can be uncommented before restarting. Temporary launch examples:

```powershell
# Windows PowerShell
$env:OPEN_REMOTE_SHOUTER_ALLOW_LAN = "1"
.\run.bat
```

```bash
# Linux/macOS
OPEN_REMOTE_SHOUTER_ALLOW_LAN=1 ./run.sh --foreground
```

After enabling LAN access, the desktop console lists an `http://<lan-ip>:21212/` address, or its HTTPS equivalent. Complete the first administrator setup locally before signing in from another device. If the address is still unreachable, restart the program, allow TCP port `21212` through the firewall, and use an address shown in the console.

> Without a certificate, LAN mode transmits passwords, sessions, and announcement content over plaintext HTTP. Configure HTTPS or use the trusted relay design below outside a controlled network.

### WebUI and desktop controls

After login, the WebUI opens on the announcement page. Topmost behavior, speech, voice, rate, volume, closing the current display, and settings import/export are under Debug settings. Browser settings are stored per login name in a local Cookie. A successful send can be cancelled or followed by another announcement.

The desktop console shows service status and access addresses, copies addresses, runs a local display test, closes the current display, and starts or stops the web service. Left-clicking the tray icon opens the console; right-clicking opens the menu. Stopping the web service or exiting requires the password of any enabled administrator. Operating-system force termination, such as Task Manager, remains outside the application's control.

## Accounts and permissions

The first WebUI visit must create an administrator account locally. After initialization, both the WebUI and announcement APIs require authentication. Remote first-run setup is available only through an explicitly allowlisted trusted relay with HTTPS and a high-entropy token.

- Every enabled user can change their own display name, theme, and password.
- The display name appears in the client announcement title. The announcement theme comes from the authenticated account, so API callers cannot impersonate another display name or theme.
- The 20 light/dark themes are single-choice and unique across accounts. On conflict, the WebUI reports the error and refreshes theme availability.
- Administrators can create, edit, enable, disable, and delete other users. Ordinary users do not see user management.
- The current administrator cannot remove their own administrator role, disable their own account, or delete it. At least one enabled administrator is always retained.
- During upgrade, missing or duplicate themes are assigned deterministically to unused themes in `accounts.json` order and persisted. When there are more than 20 accounts, remaining conflicts require manual administrator cleanup.

If a fresh extraction shows “account service unavailable” or unexpectedly asks for login, check the data directory used by the actual process, the permissions and integrity of `accounts.json`, and the log. The Windows default is `%LOCALAPPDATA%\OpenRemoteShouter\accounts.json`. Upgrading or extracting a new package does not clear existing accounts; a corrupt or empty account database is rejected instead of reopening setup.

### Security design

Implemented protections include:

- Passwords are stored with PBKDF2-SHA256 and a random salt; plaintext passwords are never stored.
- Five consecutive failures for the same source address and username lock that pair for five minutes. An existing account that accumulates 20 failures enters a five-minute account-level soft throttle; during the protection period, at most one real password verification runs every 10 seconds. Failed attempts return `429`, while a correct password can log in during the next verification window and clear the account bucket. The source address also has a limit of 100 failures per five minutes. The account bucket is created only for existing accounts, so exposing the service directly to the Internet can make the `401`/`429` difference a username-existence side channel. Use a trusted gateway to normalize responses and apply centralized rate limiting. Limiter records are process-local and are not shared across restarts or instances, so they cannot replace gateway/WAF controls.
- Login sessions use an HttpOnly, SameSite=Strict cookie with an expiration time.
- All state-changing APIs require a CSRF token.
- Changing a password, disabling a user, or deleting a user invalidates the affected sessions.
- The current administrator cannot demote, disable, or delete their own account, and the system always keeps at least one enabled administrator.
- Initial administrator setup is local-only by default; remote setup requires a fixed relay IP, an explicit opt-in, HTTPS, and a high-entropy token.
- WebUI responses include baseline security headers and a Content Security Policy (CSP).
- Login verification limits failed attempts per source and bounds the number of in-memory limiter and session records.
- At startup, the account database is checked for file size, structure, user count, and password-hash parameters. A corrupt file is rejected instead of silently returning to setup mode.
- TTS cache and log files have size limits. Old files are removed or rotated when limits are reached, preventing repeated requests from filling the disk indefinitely.

## Network and deployment

| Mode | Listener | Intended use | Key configuration |
| --- | --- | --- | --- |
| Local mode (default) | `localhost` / `127.0.0.1` | Local setup, testing, or a same-host reverse proxy | Leave unset or set `OPEN_REMOTE_SHOUTER_ALLOW_LAN=0` |
| Direct LAN access | All interfaces | Phones and computers on the same controlled LAN | Set `OPEN_REMOTE_SHOUTER_ALLOW_LAN=1`; HTTPS is recommended |
| Trusted relay | Usually remains loopback-only | FRP, Nginx/Caddy, VPN, or another controlled ingress | Allowlist the fixed relay IP; remote first-run setup also needs an opt-in, HTTPS, and a one-time token |

An explicit `OPEN_REMOTE_SHOUTER_ALLOW_LAN` value takes precedence over the legacy `OPEN_REMOTE_SHOUTER_ALLOW_DIRECT_IP` and `OPEN_REMOTE_SHOUTER_ALLOW_INSECURE_HTTP` settings. New deployments should use `OPEN_REMOTE_SHOUTER_ALLOW_LAN`.

### HTTPS and transport security

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

Classroom application environment variables (these belong to OpenRemoteShouter, not `frpc`):

```bash
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=<actual-frpc-peer-ip-seen-by-the-app>
export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN='at-least-32-random-bytes'
```

Prefer editing `run.bat` (Windows) or `run.sh` (Linux/macOS) beside the published application: uncomment the three relay settings, replace `REPLACE_WITH_AT_LEAST_32_RANDOM_BYTES`, and launch the app through that script. They are commented by default so a normal local deployment cannot enable remote setup accidentally. `frpc` reads only `frpc.toml`; do not put `OPEN_REMOTE_SHOUTER_*` variables in the FRP config.

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

Put `frpc.exe` and `frpc.toml` in `C:\frp\`. Edit `run.bat` beside OpenRemoteShouter, set the three `OPEN_REMOTE_SHOUTER_*` values, and launch it by double-clicking or from PowerShell. A temporary PowerShell setup is also possible:

```powershell
[Environment]::SetEnvironmentVariable("OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS", "127.0.0.1", "User")
[Environment]::SetEnvironmentVariable("OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP", "1", "User")
[Environment]::SetEnvironmentVariable("OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN", "at-least-32-random-bytes", "User")
C:\path\to\OpenRemoteShouter\run.bat
```

In a separate PowerShell window, run FRP: `C:\frp\frpc.exe -c C:\frp\frpc.toml`.

Restart OpenRemoteShouter after changing environment variables. If the app and `frpc` are on different machines, replace `127.0.0.1` with the TCP peer IP shown in the app logs.

**Linux classroom computer**

Save the config as `/etc/frp/frpc.toml`. Edit `run.sh` beside the published app, set the token and relay IP, and run `./run.sh` for the app; in another terminal run FRP:

```bash
chmod +x ./run.sh
./run.sh
```

In another terminal, run FRP: `/opt/frp/frpc -c /etc/frp/frpc.toml`.

When the app runs under systemd, put these variables in its `Environment=` or `EnvironmentFile=` instead of only in an interactive shell.

**macOS classroom computer**

Save `frpc` and `frpc.toml` under `~/frp/`. Edit `run.sh` beside the published app, set the token and relay IP, and run `./run.sh` for the app; in another terminal run FRP:

```zsh
chmod +x ./run.sh
./run.sh
```

In another terminal, run FRP: `~/frp/frpc -c ~/frp/frpc.toml`.

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

## Configuration reference

All settings are read from environment variables when the application starts. Restart OpenRemoteShouter after changing them. Boolean switches accept `1`/`0`, `true`/`false`, `yes`/`no`, or `on`/`off`.

| Environment variable | Default | Purpose |
| --- | --- | --- |
| `OPEN_REMOTE_SHOUTER_ALLOW_LAN` | `0` | Listen on all interfaces and allow direct LAN IP access. An explicit value overrides both legacy switches. |
| `OPEN_REMOTE_SHOUTER_ALLOW_DIRECT_IP` | `0` | Legacy LAN switch retained for compatibility only. |
| `OPEN_REMOTE_SHOUTER_ALLOW_INSECURE_HTTP` | `0` | Legacy plaintext-HTTP LAN switch retained for compatibility only. |
| `OPEN_REMOTE_SHOUTER_HTTPS_CERT_PATH` | Unset | Path to the HTTPS PFX certificate. |
| `OPEN_REMOTE_SHOUTER_HTTPS_CERT_PASSWORD` | Unset | PFX password; inject it through a service manager or secret store when possible. |
| `OPEN_REMOTE_SHOUTER_REQUIRE_HTTPS` | `0` | Refuse startup without a certificate when set to `1`. |
| `OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS` | Unset | Comma- or semicolon-separated fixed relay IPs, up to 32 entries; wildcard addresses are rejected. |
| `OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP` | `0` | Allow remote first-administrator setup through a trusted relay. |
| `OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN` | Unset | Remote setup token containing 32 to 512 bytes. |
| `OPEN_REMOTE_SHOUTER_DATA_DIR` | System user-data directory | Directory for `accounts.json` and the default log. |
| `OPEN_REMOTE_SHOUTER_LOG_FILE` | Log file under the data directory | Custom log file path. |
| `OPEN_REMOTE_SHOUTER_LOG_CONSOLE` | Automatic | Force logs to be written to the terminal as well. |
| `OPEN_REMOTE_SHOUTER_LOG_MAX_BYTES` | `10485760` | Per-log-file limit, 10 MiB by default. |
| `OPEN_REMOTE_SHOUTER_LOG_MAX_FILES` | `3` | Number of rotated log files. |
| `OPEN_REMOTE_SHOUTER_TTS_CACHE_MAX_BYTES` | `268435456` | Total TTS cache limit, 256 MiB by default. |
| `OPEN_REMOTE_SHOUTER_TTS_CACHE_MAX_FILES` | `512` | Maximum number of cached TTS files. |
| `OPEN_REMOTE_SHOUTER_EDGE_TTS_FORMAT` | `mp3` | EdgeTTS output format; use `wav` for older environments. |
| `OPEN_REMOTE_SHOUTER_AUDIO_PLAYER` | Auto-detected | Linux audio-player command, for example `ffplay`. |
| `OPEN_REMOTE_SHOUTER_SOFTWARE_RENDERING` | `0` | Force Avalonia software rendering; the LoongArch Old World package sets `1`. |
| `OPEN_REMOTE_SHOUTER_X11_ENABLE_IME` | `1` | Control X11 input methods; the LoongArch Old World package uses `auto`. |

Account data is stored in `accounts.json` under the system user-data directory by default. A custom data directory and the parent directory of `OPEN_REMOTE_SHOUTER_LOG_FILE` must be private to the service account. Do not place them in a directory writable by other accounts. On Windows, the program does not enforce ACL changes; permissions depend on the directory security configuration.

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
- `POST /api/auth/setup`: create the first administrator account.
- `POST /api/auth/login`: log in and create a session.
- `POST /api/auth/logout`: log out the current session.
- `POST /api/auth/password`: change the current account password.
- `PUT /api/account/profile`: change the current account display name and theme.
- `GET /api/account/themes`: get the current, complete, and available theme lists.
- `GET /api/status`: service status.
- `GET /api/voices`: available voices.
- `POST /api/shout`: send an announcement.
- `POST /api/close`: close the current display.
- `GET /api/users` and `POST /api/users`: list or create users as an administrator.
- `PUT /api/users/{username}` and `DELETE /api/users/{username}`: update or delete a selected user as an administrator.

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
| `title` | Retained for old clients; the server displays “(display name) sent a message”. |
| `message` | Required announcement text. |
| `mode` | Retained for old clients; display is always rendered as `fullscreen`. |
| `durationSeconds` | Retained for old clients; actual display time follows TTS audio duration with a ten-second minimum. |
| `topmost` | Whether the window stays on top. |
| `speechEnabled` | Whether to read the announcement aloud. |
| `voiceName` | EdgeTTS voice, for example `zh-CN-XiaoyiNeural`. |
| `speechRate` | Speech rate, from `-100` to `100`. |
| `speechVolume` | Volume, from `0.0` to `1.0`. |
| `theme` | Retained for old clients; the server always uses the theme saved for the authenticated account. |

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
