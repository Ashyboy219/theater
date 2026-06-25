#!/bin/sh
# Convenience launcher for the THEATER work on this machine.
# Fixes the two things that stop `./launch-game.sh` from working here:
#   1. .NET 8 is keg-only (not on PATH) — export it.
#   2. defaults to launching the THEATER mod so no mod picker / zenity is needed on macOS.
# Usage: ./theater-play.sh            (launches the THEATER mod)
#        ./theater-play.sh Game.Mod=ra    (stock Red Alert; or cnc/d2k, or any extra OpenRA args)
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
cd "$(dirname "$0")" || exit 1

# Default to the THEATER mod unless the caller already specified a mod.
case "$*" in
	*Game.Mod=*) exec ./launch-game.sh "$@" ;;
	*) exec ./launch-game.sh Game.Mod=theater "$@" ;;
esac
