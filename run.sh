#!/usr/bin/env sh
set -eu

# Configure these values for a trusted FRP/reverse-proxy deployment.
: "${OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS:=127.0.0.1}"
: "${OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP:=1}"
: "${OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN:=REPLACE_WITH_AT_LEAST_32_RANDOM_BYTES}"
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS
export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP
export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [ -x "$SCRIPT_DIR/OpenRemoteShouter" ]; then
  exec "$SCRIPT_DIR/OpenRemoteShouter" "$@"
elif [ -f "$SCRIPT_DIR/OpenRemoteShouter.dll" ]; then
  exec dotnet "$SCRIPT_DIR/OpenRemoteShouter.dll" "$@"
else
  echo "OpenRemoteShouter or OpenRemoteShouter.dll was not found beside this script." >&2
  exit 1
fi
