# Changelog

## 0.1.1.1 - 2026-09-06

- Added deterministic migration for missing or duplicate themes in older `accounts.json` files, persisted on first launch after upgrade.
- Added visible WebUI feedback when a theme is already assigned to another account, with an availability refresh after the conflict.

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
