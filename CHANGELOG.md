# Changelog

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
