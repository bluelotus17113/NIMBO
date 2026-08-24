#!/bin/bash
# La única forma correcta de correr Unity con varios agentes a la vez.
#
# Dos agentes reportaron el mismo síntoma: Unity salió con «Exiting batchmode
# successfully» SIN ejecutar la suite y SIN escribir el XML.
#
# Yo lo achaqué a una carrera del lockfile y me equivoqué. La causa real la encontró
# nimbo-camara y está abajo: **-quit junto a -runTests mata el runner antes de que
# arranque**. La espera del lockfile se queda igual porque hace falta de todas formas
# —dos Unity a la vez siguen sin poder— pero no era eso lo que vaciaba los XML.
# Ratificado por el orquestador tras comprobarlo.
#
# Un verificador que cree haber corrido las pruebas y no las corrió es peor que no tener
# verificador. Por eso la condición de éxito NO es el código de salida: es que el XML
# exista y traiga recuento.
#
# Uso: Tools/agentes/unity.sh <EditMode|PlayMode> <etiqueta> [filtro]

modo="${1:-EditMode}"; etiqueta="${2:-anon}"; filtro="$3"
proyecto=/home/vaknadesu/Proyectos/isla-nimbo
mkdir -p "$proyecto/Informes/pruebas"
xml="$proyecto/Informes/pruebas/${etiqueta}-${modo}.xml"
log="$proyecto/Informes/pruebas/${etiqueta}-${modo}.log"


# ── huerfanos ────────────────────────────────────────────────────────────────
#
# Un Unity que pierde a su padre —porque se cerro la terminal, o murio el
# envoltorio— no se entera y sigue vivo con el proyecto cogido. Paso una vez: se
# cerro herdr por accidente y quedo un batchmode colgado 43 minutos con tres
# AssetImportWorker detras, bloqueando la cola de seis agentes. El sintoma es
# reconocible: proceso durmiendo, ILPP.Runner arriba, y ningun XML nunca.
#
# Se limpia solo lo que lleva mas de 20 minutos: una corrida normal tarda cuatro,
# asi que a los veinte ya no esta trabajando, esta colgada. Y se comprueba que no
# haya nadie dentro del flock antes de tocar nada, para no matar a un compañero
# que si esta corriendo de verdad.
limpia_huerfanos() {
  flock -n /tmp/nimbo-unity.lock true 2>/dev/null || return 0   # hay alguien dentro: no es huerfano

  local mato=0
  while read -r pid segundos; do
    [ -z "$pid" ] && continue
    [ "$segundos" -gt 1200 ] 2>/dev/null || continue
    kill "$pid" 2>/dev/null; mato=1
  done < <(ps -eo pid=,etimes=,args= | grep -E "Unity -batchmode|AssetImportWorker" \
           | grep -v grep | awk '{print $1, $2}')

  if [ "$mato" = "1" ]; then
    sleep 6
    ps -eo pid=,etimes=,args= | grep -E "Unity -batchmode|AssetImportWorker" | grep -v grep \
      | awk '$2 > 1200 {print $1}' | xargs -r kill -9 2>/dev/null
    rm -f "$proyecto/Temp/UnityLockfile"
    echo "  ⚠ habia un Unity huerfano de mas de 20 min bloqueando la cola. Limpiado."
  fi
}

limpia_huerfanos

for intento in 1 2 3; do
  rm -f "$xml"
  # El cerrojo se coge por descriptor y NO envolviendo la orden, y esto importa:
  # `flock -w N fichero orden` devuelve el codigo de salida de la ORDEN, no el de coger
  # el cerrojo. Unity sale con codigo != 0 cuando fallan las pruebas, asi que la version
  # anterior decia «la cola esta ocupada» a un agente cuyas pruebas estaban en rojo. Se
  # paso una hora esperando turno de una cola vacia porque el envoltorio le mentia.
  # Cogiendolo por descriptor, un fallo de `flock` solo puede significar una cosa.
  #
  # **Veinte minutos de espera y no cuatro.** Con cuatro, en una ola de tres agentes
  # el tercero se rendía siempre: una corrida filtrada tarda cinco minutos, así que
  # el que llega tercero tiene por delante diez de cola legítima. Y rendirse le sale
  # carísimo — `lanza.sh` no reintenta el comando, reintenta el AGENTE ENTERO, que
  # vuelve a leerse el encargo desde cero. Esperar en la cola es gratis; volver a
  # empezar, no. El tope sigue existiendo porque una espera infinita ante un Unity
  # colgado no se distingue de un cuelgue propio.
  exec 9>/tmp/nimbo-unity.lock
  espera_desde=$SECONDS
  if ! flock -w 1200 9; then
    echo "  ⏳ La cola de Unity sigue ocupada tras 20 min de espera. No es un fallo tuyo:"
    echo "     vuelve a lanzarlo dentro de un rato. NO digas que las pruebas pasaron."
    exit 2
  fi
  esperado=$(( SECONDS - espera_desde ))
  [ "$esperado" -gt 30 ] && echo "  ⏳ Esperé ${esperado}s en la cola. Ahora sí corro."

  bash -c '
    proyecto="$1"; modo="$2"; xml="$3"; log="$4"; filtro="$5"
    for i in $(seq 1 60); do [ -f "$proyecto/Temp/UnityLockfile" ] || break; sleep 2; done
    # Sin -quit: junto con -runTests, el apagado por lotes se disparaba al terminar
    # la inicialización y ANTES de que el runner de pruebas arrancara — tres corridas
    # seguidas compilaron el proyecto entero (Tundra success) y salieron «successfully»
    # sin ejecutar una sola prueba ni escribir XML. El runner se cierra solo al acabar;
    # -quit sobra y estorba.
    args=(-batchmode -projectPath "$proyecto" -runTests
          -testPlatform "$modo" -testResults "$xml" -logFile "$log")
    [ -n "$filtro" ] && args+=(-testFilter "$filtro")
    unity "${args[@]}"
  ' _ "$proyecto" "$modo" "$xml" "$log" "$filtro"
  flock -u 9

  if [ -s "$xml" ] && grep -q "testcasecount" "$xml" 2>/dev/null; then
    python3 - "$xml" <<'PY'
import re, sys
x = open(sys.argv[1]).read()
m = re.search(r'<test-run [^>]*', x)
a = dict(re.findall(r'([\w-]+)="([^"]*)"', m.group(0))) if m else {}
print(f"  → {a.get('total','?')} pruebas · {a.get('passed','?')} pasadas · "
      f"{a.get('failed','?')} FALLOS · {a.get('skipped','?')} saltadas")
for c in re.finditer(r'<test-case[^>]*?name="([^"]*)"[^>]*?result="Failed"', x):
    print("  ✗", c.group(1))
PY
    exit 0
  fi
  echo "  ⚠ Unity salió sin escribir XML. Intento $intento/3."
  sleep 10
done
echo "  ✗ No se pudo correr $modo en tres intentos. NO digas que las pruebas pasaron."
exit 1
