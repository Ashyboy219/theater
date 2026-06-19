#!/bin/sh
# Convenience launcher for the THEATER work on this machine.
# Fixes the two things that stop `./launch-game.sh` from working here:
#   1. .NET 8 is keg-only (not on PATH) — export it.
#   2. defaults to launching Red Alert so no mod picker / zenity is needed on macOS.
# Usage: ./theater-play.sh            (launches Red Alert)
#        ./theater-play.sh Game.Mod=cnc   (or any extra OpenRA args)
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
cd "$(dirname "$0")" || exit 1

# Default to Red Alert unless the caller already specified a mod.
case "$*" in
	*Game.Mod=*) exec ./launch-game.sh "$@" ;;
	*) exec ./launch-game.sh Game.Mod=ra "$@" ;;
esac
