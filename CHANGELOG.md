# Changelog

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
