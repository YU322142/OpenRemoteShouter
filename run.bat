@echo off
setlocal

rem Configure these values for a trusted FRP/reverse-proxy deployment.
if not defined OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS set "OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=127.0.0.1"
if not defined OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP set "OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1"
if not defined OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN set "OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN=REPLACE_WITH_AT_LEAST_32_RANDOM_BYTES"

if exist "%~dp0OpenRemoteShouter.exe" (
  "%~dp0OpenRemoteShouter.exe"
) else if exist "%~dp0OpenRemoteShouter.dll" (
  dotnet "%~dp0OpenRemoteShouter.dll"
) else (
  echo OpenRemoteShouter.exe or OpenRemoteShouter.dll was not found beside this script.
  exit /b 1
)
