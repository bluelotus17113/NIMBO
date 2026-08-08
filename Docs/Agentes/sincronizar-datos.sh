#!/usr/bin/env bash
# sincronizar-datos.sh — lleva los JSON de diseño a Resources para que el juego los lea.
#
# Los ficheros se escriben en Docs/Contratos (ahí trabajan los agentes, y ahí no
# hay .meta de Unity que ensucien los diffs) y el juego los carga desde
# Assets/_Project/Resources/Config. Esta es la única copia autorizada entre los dos:
# si alguien edita el de Resources a mano, el siguiente pase se lo lleva por delante.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ORIGEN="$REPO/Docs/Contratos"
DESTINO="$REPO/Assets/_Project/Resources/Config"

mkdir -p "$DESTINO"

for fichero in "$ORIGEN"/catalogo_*.json "$ORIGEN"/personalidades.json; do
  nombre="$(basename "$fichero")"
  if ! cmp -s "$fichero" "$DESTINO/$nombre"; then
    cp "$fichero" "$DESTINO/$nombre"
    echo "actualizado  $nombre"
  else
    echo "sin cambios  $nombre"
  fi
done
