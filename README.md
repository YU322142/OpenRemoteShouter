# OpenRemoteShouter

OpenRemoteShouter 是一个局域网远程喊话工具。它在电脑上启动一个本地网页服务，其他设备可以通过浏览器或 HTTP API 发送文字，让目标电脑弹出全屏/窗口提示并使用 EdgeTTS 语音播报。

## 重要提醒

- 本项目主要由 AI 辅助编写，可能存在未发现的问题。请在实际使用前自行测试，重要场景不要直接依赖未验证版本。
- 除 Windows 和 LoongArch64 Old World ABI 1.0 构建外，其他平台构建目前仅确认能够在 GitHub Actions 中完成打包，尚未经过实机运行测试。

## 功能

- 局域网网页喊话，默认监听 `21212` 端口。
- 支持全屏置顶显示和普通弹窗显示。
- 支持自动关闭倒计时，`0` 表示手动关闭。
- 支持 EdgeTTS 中文语音播报。
- 支持网页表单、JSON API 和表单 POST。
- 支持 Windows、Linux、macOS 的多架构构建。
- 提供 LoongArch64 Old World ABI 1.0 专用构建包。

## 使用

1. 下载适合当前系统的构建包。
2. 解压后运行：
   - Windows：运行 `run.bat` 或 `OpenRemoteShouter.exe`
   - Linux/macOS：运行 `./run.sh`
   - Portable 包：需要先安装 .NET 8 Runtime，再运行 `run.sh` 或 `run.bat`
3. 打开控制台窗口或托盘菜单，复制访问地址。
4. 在同一局域网设备的浏览器中访问该地址并发送喊话。

如果局域网设备无法访问，请检查防火墙是否放行 `21212` 端口。

## Linux 语音依赖

EdgeTTS 生成的是 WAV 音频。Linux 下程序会按顺序寻找以下播放器：

- `paplay`
- `aplay`
- `pw-play`
- `ffplay`
- `mpv`
- `cvlc`
- `vlc`

常见安装命令：

```bash
# Debian/Ubuntu
sudo apt install pulseaudio-utils alsa-utils ffmpeg

# Fedora
sudo dnf install pulseaudio-utils alsa-utils ffmpeg

# Arch Linux
sudo pacman -S libpulse alsa-utils ffmpeg
```

如果没有可用播放器，控制台会显示语音后端错误，但文字喊话仍可使用。

## HTTP API

服务启动后可访问：

- `GET /`：网页喊话表单
- `GET /api/status`：服务状态
- `GET /api/voices`：可用语音列表
- `POST /api/shout`：发送喊话
- `POST /api/close`：关闭当前显示

JSON 示例：

```bash
curl -X POST http://127.0.0.1:21212/api/shout \
  -H "Content-Type: application/json" \
  -d '{
    "title": "通知",
    "message": "这是一条远程喊话。",
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

字段说明：

| 字段 | 说明 |
| --- | --- |
| `title` | 显示标题，留空时使用默认标题 |
| `message` | 喊话内容，必填 |
| `mode` | `fullscreen` 或 `popup` |
| `durationSeconds` | 自动关闭秒数，范围 `0` 到 `3600` |
| `topmost` | 是否置顶 |
| `speechEnabled` | 是否语音播报 |
| `voiceName` | EdgeTTS 语音，如 `zh-CN-XiaoyiNeural` |
| `speechRate` | 语速，范围 `-100` 到 `100` |
| `speechVolume` | 音量，范围 `0.0` 到 `1.0` |
| `theme` | `cyan`、`blue`、`green`、`amber`、`rose`、`violet` |

## 构建产物

GitHub Actions 的 `Build OpenRemoteShouter` workflow 会构建并上传：

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
- `OpenRemoteShouter-all-platforms`

`OpenRemoteShouter-all-platforms` 是发布用总包，里面包含所有平台包和校验文件。

`OpenRemoteShouter-linux-loongarch64-oldworld-abi1.0.tar.gz` 仅用于 LoongArch64 Old World ABI 1.0 系统。

## 本地构建

需要安装 .NET 8 SDK。

```bash
dotnet restore
dotnet build RemoteShouter.sln -c Release
dotnet publish RemoteShouter.csproj -c Release -r win-x64 --self-contained true
```

替换 `-r` 后的 Runtime Identifier 可以构建其他平台，例如 `linux-x64`、`linux-arm64`、`osx-arm64`。

## 注意事项

- EdgeTTS 需要联网访问微软语音服务。
- 全屏置顶效果受 Linux 桌面环境和窗口管理器影响。
- Linux 下语音播放依赖系统播放器。
- 除 Windows 和 LoongArch64 Old World ABI 1.0 外，其他平台构建尚未实机测试。
- macOS 首次运行可能需要在系统安全设置中允许该程序运行。
