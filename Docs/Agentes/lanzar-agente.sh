#!/usr/bin/env bash
# lanzar-agente.sh <nombre> <modelo> — abre una pestaña de herdr con Claude Code sobre DeepSeek.
#
# Cada agente vive en su propia pestaña para que no se peleen por el panel y para
# poder leerlos por separado. El nombre es el que luego usas en `herdr agent prompt`.
set -euo pipefail

NAME="${1:?uso: lanzar-agente.sh <nombre> [modelo]}"
MODEL="${2:-deepseek-v4-pro}"
REPO="${REPO:-/home/vaknadesu/Proyectos/isla-nimbo}"
KEY="$(cat "$HOME/.config/deepseek/key")"

if herdr agent list 2>/dev/null | grep -q "\"$NAME\""; then
  echo "[$NAME] ya existe, lo reutilizo"
  exit 0
fi

if herdr tab list | grep -q "\"$NAME\""; then
  echo "[$NAME] la pestaña ya existe, reutilizo"
  OUT="$(herdr tab list)"
else
OUT="$(herdr tab create \
  --cwd "$REPO" \
  --label "$NAME" \
  --env ANTHROPIC_BASE_URL=https://api.deepseek.com/anthropic \
  --env ANTHROPIC_AUTH_TOKEN="$KEY" \
  --env ANTHROPIC_MODEL="$MODEL" \
  --env ANTHROPIC_SMALL_FAST_MODEL=deepseek-chat \
  --env ANTHROPIC_DEFAULT_HAIKU_MODEL=deepseek-chat \
  --env ANTHROPIC_DEFAULT_OPUS_MODEL="$MODEL" \
  --env ANTHROPIC_DEFAULT_SONNET_MODEL="$MODEL" \
  --no-focus)"
fi

# `tab create` no devuelve el panel: hay que buscarlo por la pestaña que acaba de abrir.
TAB="$(printf '%s' "$OUT" | LABEL="$NAME" python3 -c '
import json, os, sys
r = json.load(sys.stdin)["result"]
tabs = r.get("tabs") or [r.get("tab", r)]
print(next(t["tab_id"] for t in tabs if t.get("label") == os.environ["LABEL"]))')"

PANE="$(herdr pane list | TAB="$TAB" python3 -c '
import json, os, sys
panes = json.load(sys.stdin)["result"]["panes"]
print(next(p["pane_id"] for p in panes if p.get("tab_id") == os.environ["TAB"]))')"
echo "[$NAME] pestaña $TAB, panel $PANE"

herdr agent start "$NAME" --kind claude --pane "$PANE" --timeout 90000 -- --permission-mode acceptEdits
sleep 3
# El diálogo de confianza de carpeta sale la primera vez en un cwd nuevo.
herdr agent send-keys "$NAME" enter 2>/dev/null || true
echo "[$NAME] listo"
