#!/bin/sh
set -eu

if [ -z "${API_URL:-}" ]; then
  echo "API_URL precisa apontar para a URL publica da API, incluindo /api." >&2
  exit 1
fi

case "$API_URL" in
  http://*|https://*) ;;
  *) echo "API_URL precisa usar http:// ou https://." >&2; exit 1 ;;
esac

case "$API_URL" in
  *[!A-Za-z0-9:/._-]*) echo "API_URL contem caracteres invalidos." >&2; exit 1 ;;
esac

printf "window.__SGE_CONFIG__ = { apiUrl: '%s' };\n" "${API_URL%/}" \
  > /usr/share/nginx/html/runtime-config.js
