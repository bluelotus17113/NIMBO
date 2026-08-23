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

for intento in 1 2 3; do
  rm -f "$xml"
  flock /tmp/nimbo-unity.lock bash -c '
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
