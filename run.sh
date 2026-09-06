#!/usr/bin/env sh
set -eu

# For trusted FRP/reverse-proxy setup, uncomment all three lines and replace
# the token with at least 32 random bytes. These variables belong to the app.
# To allow LAN access, including direct access through the computer's IP,
# uncomment this line. Leave it commented for local-only use.
# For plaintext HTTP this is an explicit exposure opt-in; HTTPS is preferred.
# export OPEN_REMOTE_SHOUTER_ALLOW_LAN=1
# export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_IPS=127.0.0.1
# export OPEN_REMOTE_SHOUTER_ALLOW_TRUSTED_PROXY_SETUP=1
# export OPEN_REMOTE_SHOUTER_TRUSTED_PROXY_SETUP_TOKEN='REPLACE_WITH_AT_LEAST_32_RANDOM_BYTES'

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [ -x "$SCRIPT_DIR/OpenRemoteShouter" ]; then
  exec "$SCRIPT_DIR/OpenRemoteShouter" "$@"
elif [ -f "$SCRIPT_DIR/OpenRemoteShouter.dll" ]; then
  exec dotnet "$SCRIPT_DIR/OpenRemoteShouter.dll" "$@"
else
  echo "OpenRemoteShouter or OpenRemoteShouter.dll was not found beside this script." >&2
  exit 1
fi
