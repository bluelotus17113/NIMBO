#!/bin/bash
# Lanza una ola de agentes, cada uno en su panel de herdr con su nombre puesto.
#
# Existe porque lanzarlos a mano se hacía de tres en tres con un `for` distinto cada
# vez, y a la tercera ola ya nadie sabía qué panel era de quién. Aquí el panel se
# renombra con el nombre del agente antes de arrancarlo: en el herdr se ve la flota.
#
# **El tope de tres no es una preferencia.** Son ~500 MB por agente y 1,9 GB Unity con
# su worker en una máquina de 7,6 GB. Con cinco a la vez quedaban 200 MB libres y Unity
# se quedaba callado — que se parece exactamente a un cuelgue del código y costó una
# tarde de diagnóstico equivocado. Se lanzan en tandas y se espera a que la tanda
# termine.
#
# Uso: ola.sh <tab> <agente> [agente...]
#   tab      el tab de herdr donde están los paneles (p.ej. w2:t8)
#   agente   nombre sin el prefijo nimbo- (teclas, hud, seguridad…)

tab="$1"; shift
proyecto=/home/vaknadesu/Proyectos/isla-nimbo
tanda=3

mapfile -t paneles < <(herdr pane list 2>/dev/null \
  | python3 -c 'import json,sys; d=json.load(sys.stdin)
print("\n".join(p["pane_id"] for p in d["result"]["panes"] if p.get("tab_id")==sys.argv[1]))' "$tab")

if [ "${#paneles[@]}" -lt "$#" ]; then
  echo "✗ hacen falta $# paneles en $tab y hay ${#paneles[@]}."
  echo "  Divide el tab o lanza menos agentes de una vez."
  exit 1
fi

lanzados=0
i=0
for agente in "$@"; do
  # Se espera a que baje de la tanda antes de meter otro. `wait -n` no vale: los
  # opencode los arranca herdr en su panel, no este script, así que no son hijos
  # nuestros y hay que contarlos por fuera.
  #
  # Se cuenta con `pgrep | wc -l` y no con `pgrep -fc`: cuando no hay ninguno, `-fc`
  # imprime 0 **y sale con 1**, así que el `|| echo 0` de respaldo añadía un segundo
  # cero y `[` recibía «0\n0». No llegó a romper nada —la comparación fallaba y el
  # agente entraba igual, que es el lado bueno del error— pero es la clase de fallo
  # que espera a que haya prisa.
  while [ "$(pgrep -f 'opencode run' | wc -l)" -ge "$tanda" ]; do
    sleep 30
  done

  pane="${paneles[$i]}"; i=$((i + 1))
  herdr pane rename "$pane" "$agente" >/dev/null 2>&1
  herdr pane run "$pane" bash "$proyecto/Tools/agentes/lanza.sh" \
    "nimbo-$agente" "Informes/informe-$agente.md" \
    "Haz tu encargo entero. Está en Informes/encargos/nimbo-$agente.md y las reglas de la casa van dentro. Escribe el informe final en Informes/informe-$agente.md" \
    >/dev/null 2>&1

  echo "  ● $agente → $pane"
  lanzados=$((lanzados + 1))
  sleep 8   # que no arranquen tres a la vez contra el mismo endpoint
done

echo
echo "$lanzados agentes en el aire. Panel: Tools/agentes/panel.sh"
