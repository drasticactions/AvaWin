#!/bin/sh
# Visual-review helper: renders a gallery page to PNG.
#   tools/shoot.sh <Page> <Light|Dark> <Desktop|Phone> <out.png> [extra args]
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PAGE="$1"; VARIANT="$2"; PLATFORM="$3"; OUT="$4"; shift 4
SIZE=1200x800
[ "$PLATFORM" = "Phone" ] && SIZE=400x760
"$ROOT/samples/AvaWin.Gallery.Desktop/bin/Release/net10.0/AvaWin.Gallery.Desktop" --page "$PAGE" --variant "$VARIANT" --platform "$PLATFORM" --size "$SIZE" --screenshot "$OUT" "$@"
