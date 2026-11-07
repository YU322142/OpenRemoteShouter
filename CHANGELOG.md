# Changelog

## 0.1.2.1 - 2026-11-07

### 中文

- “启动设置 → 保存位置”新增当前用户登录后自启动，默认关闭。ClassIsland 仍可独立启动程序。已有实例时，重复启动会退出，避免同时启动多个实例。
- 可在同一选项卡选择账户、默认日志和 EdgeTTS 缓存的专用目录，便于把教室电脑的数据保存在 D 盘。旧配置缺少新字段时仍读取 `OPEN_REMOTE_SHOUTER_DATA_DIR`，显式指定的 `OPEN_REMOTE_SHOUTER_LOG_FILE` 继续优先。切换目录需要重启软件，已有账户不会自动迁移。
- 开启局域网访问后，可在主机浏览器中使用本机局域网 IP 创建首个管理员。远端首次初始化仍需可信中转。账户文件为空、损坏或不可读时，WebUI 提供更明确的排查和恢复提示。
- 切换到 Microsoft Edge Services 的 TTS WebSocket 接口。MP3 仍为默认格式。如果接口拒绝 WAV，程序会用 MP3 重试，Linux 需有支持 MP3 的播放器。语音不可用时，管理员可在 WebUI 的“服务状态”查看提示，文字喊话仍可使用。
- 修复全屏喊话窗口的关闭图标没有居中的问题。

### English

- Added optional per-user launch at login under Startup settings → Save location. It is off by default, and ClassIsland can still launch the app independently. A second launch exits if an instance is already running.
- Added a picker for a dedicated account, default log, and EdgeTTS cache directory, so classroom deployments can keep data on D. Older settings without the new field continue to use `OPEN_REMOTE_SHOUTER_DATA_DIR`. An explicit `OPEN_REMOTE_SHOUTER_LOG_FILE` still takes precedence. Restart the app after changing directories. Move existing accounts manually if needed.
- With LAN access enabled, a browser on the host can use its own LAN IP to create the first administrator. Remote first-time setup still requires a trusted relay. The WebUI gives clearer recovery guidance for empty, corrupt, or unreadable account files.
- Switched to the Microsoft Edge Services TTS WebSocket endpoint. MP3 remains the default. If the endpoint rejects WAV, the app retries with MP3, which requires an MP3-compatible player on Linux. Administrators can see speech errors under Service status in the WebUI. Text announcements remain available.
- Fixed the alignment of the close icon in the full-screen announcement window.

[完整改动 / Full comparison](https://github.com/YU322142/OpenRemoteShouter/compare/0.1.2.0...0.1.2.1)

## 0.1.2.0 - 2026-10-25

### 中文

- 新增跨平台桌面控制台和托盘“启动设置”入口，可配置局域网访问、可信中转、远程初始化令牌及 HTTPS 证书。
- 支持将 `startup-settings.json` 保存在程序目录或自定义文件夹，便于在 C 盘有冰点还原的教室中部署到 D 盘。自定义路径通过程序目录中的 `startup-settings-location.json` 记录，部署时需一并保留。
- 程序启动时自动读取配置，直接打开程序或由 CI 自动唤起均可生效，不修改系统环境变量；保存后需要完全退出并重新启动软件。
- 增加原子保存、覆盖备份和外部修改冲突检测，并校验可信中转 IP、初始化令牌及 PFX 证书，减少无效配置导致的启动失败。
- 修复首次创建管理员前无法正常退出的问题，便于保存首次部署设置后重启；创建管理员后仍需密码确认退出。

### English

- Added cross-platform desktop and tray Startup settings for LAN access, trusted relays, remote setup tokens, and HTTPS certificates.
- Added a choice of application directory or custom folder for `startup-settings.json`, supporting D-drive classroom deployments where Deep Freeze restores the C drive. Custom paths are recorded in `startup-settings-location.json` beside the application; retain this file when deploying.
- Load saved settings automatically at startup, including direct launches and CI auto-start, without changing system environment variables. Fully exit and restart the application after saving.
- Added atomic saves, overwrite backups, external-change conflict detection, and validation of relay IPs, setup tokens, and PFX certificates to reduce startup failures caused by invalid settings.
- Allowed exiting before the first administrator is created so initial deployment settings can be applied by restarting. Exiting still requires administrator confirmation after setup.

## 0.1.1.1 - 2026-09-12

### 中文

- 新增旧版 `accounts.json` 的主题色迁移：缺失或重复的主题色会在首次升级启动时，按账户文件中的稳定顺序分配到未使用的主题色并写回数据库。
- 新增主题色冲突提示：当主题色已被其他账户使用时，WebUI 会显示明确提示并刷新可用主题列表。
- 统一 WebUI、桌面窗口、任务栏、系统托盘和托盘菜单中的圆环标志，缩小桌面图标开口间距。
- 重整中英文使用文档，补充局域网开关、账户权限、配置参数和 HTTP API 说明。

### English

- Added deterministic migration for missing or duplicate themes in older `accounts.json` files, persisted on first launch after upgrade.
- Added visible WebUI feedback when a theme is already assigned to another account, with an availability refresh after the conflict.
- Unified the ring mark used by the WebUI, desktop window, taskbar, system tray, and tray menu, with narrower gaps in the desktop icon.
- Reorganized the Chinese and English documentation with clearer LAN access, account permission, configuration, and HTTP API guidance.

## 0.1.1.0 - 2026-09-06

Complete update since `0.1.0.0`, covering the trusted relay, mobile layout, client UI, account management, and release engineering:

- Added trusted relay controls, HTTPS enforcement, one-time remote setup tokens, and safe forwarding-header handling.
- Added FRP, reverse-proxy, systemd, launchd, and Task Scheduler deployment guidance for Windows, Linux, and macOS.
- Improved mobile layout and kept full-screen announcement motion visibly nonlinear while preserving the existing send, cancel, and resend effects.
- Updated the desktop console to use FluentAvalonia controls and theme resources, with branded window/tray icons and a Fluent-style context menu. The WebUI bundles Fluent UI Web Components locally.
- Added 20 light/dark account themes, editable display names, single-theme assignment, and administrator self-protection.
- Added administrator password confirmation before stopping the web service or exiting, plus the `OPEN_REMOTE_SHOUTER_ALLOW_LAN` opt-in for LAN IP access.
- Published separate platform packages and `SHA256SUMS.txt` checksums.

## 0.1.0.0

- Initial public release of OpenRemoteShouter.
