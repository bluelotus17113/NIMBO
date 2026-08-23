#!/bin/bash
# Lanza un agente y lo reintenta hasta que entregue su informe.
#
# El endpoint gratuito de ox-alpha se cae a ratos ("Upstream request failed"), así que un
# agente que muere a mitad y nadie relanza es una casilla vacía que no se detecta hasta el
# día siguiente. Alterna las dos rutas: x-preview-f-free y big-pickle responden las dos
# ox-alpha, y rara vez están caídas a la vez.
#
# La condición de éxito es que el fichero de informe exista y no esté vacío: un agente que
# muere justo antes de escribir sale con cero y parece que fue bien.
#
# Uso: lanza.sh <agente> <informe-relativo-al-proyecto> <prompt...>

agente="$1"; informe="$2"; shift 2; prompt="$*"
proyecto=/home/vaknadesu/Proyectos/isla-nimbo
case "$informe" in /*) ;; *) informe="$proyecto/$informe" ;; esac
modelos=(opencode/x-preview-f-free opencode/big-pickle)
maximo=10

cd "$proyecto" || exit 1
rm -f "$informe"

for intento in $(seq 1 $maximo); do
  modelo=${modelos[$(( (intento - 1) % 2 ))]}
  printf '\n\033[1m── %s · intento %d/%d · %s ──\033[0m\n' "$agente" "$intento" "$maximo" "${modelo#opencode/}"
  echo "$(date '+%H:%M:%S') intento $intento $modelo" >> "$proyecto/Informes/intentos-$agente.log"

  opencode run --agent "$agente" --model "$modelo" "$prompt"

  if [ -s "$informe" ]; then
    printf '\n\033[32m✓ %s entregó (%s líneas)\033[0m\n' "$agente" "$(wc -l < "$informe")"
    echo "$(date '+%H:%M:%S') OK $(wc -l < "$informe") lineas" >> "$proyecto/Informes/intentos-$agente.log"
    exit 0
  fi
  espera=$(( intento * 20 ))
  printf '\033[33m✗ sin informe. Reintento en %ds con la otra ruta.\033[0m\n' "$espera"
  sleep "$espera"
done
printf '\n\033[31m✗✗ %s agotó los %d intentos.\033[0m\n' "$agente" "$maximo"
exit 1
