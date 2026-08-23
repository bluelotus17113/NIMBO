#!/bin/bash
# Panel de la flota: quién trabaja, desde cuándo y qué ha entregado.
cd /home/vaknadesu/Proyectos/isla-nimbo 2>/dev/null
while true; do
  clear
  printf '\033[1m  ISLA NIMBO — flota ox-alpha\033[0m        %s\n' "$(date '+%H:%M:%S')"
  printf '  ─────────────────────────────────────────────────────────────────────\n\n'
  printf '\033[1m  AGENTES\033[0m\n'
  vivos=0
  while read -r pid etime args; do
    [ -z "$pid" ] && continue
    ag=$(echo "$args" | grep -oP '(?<=--agent )[a-z0-9-]+'); [ -z "$ag" ] && continue
    vivos=$((vivos+1)); printf '    \033[32m●\033[0m %-26s %8s\n' "$ag" "$etime"
  done < <(ps -eo pid=,etime=,args= | grep "opencode run" | grep -v grep)
  [ "$vivos" = "0" ] && printf '    \033[90m(ninguno corriendo)\033[0m\n'
  printf '\n\033[1m  INFORMES ENTREGADOS\033[0m\n'
  found=0
  for f in Informes/*.md; do
    [ -e "$f" ] || continue; found=1
    printf '    \033[36m✓\033[0m %-42s %5s líneas  %s\n' "$(basename "$f")" "$(wc -l < "$f")" "$(date -r "$f" '+%H:%M')"
  done
  [ "$found" = "0" ] && printf '    \033[90m(todavía ninguno)\033[0m\n'
  printf '\n\033[1m  CAÍDAS Y REINTENTOS\033[0m\n'
  hubo=0
  for l in Informes/intentos-*.log; do
    [ -e "$l" ] || continue
    ag=$(basename "$l" .log); ag=${ag#intentos-}
    n=$(grep -c "intento" "$l" 2>/dev/null)
    if [ "$n" -gt 1 ] 2>/dev/null; then hubo=1; printf '    \033[33m↻\033[0m %-26s %s intentos\n' "$ag" "$n"; fi
  done
  [ "$hubo" = "0" ] && printf '    \033[90m(ninguna)\033[0m\n'
  printf '\n\033[1m  ESTADO DEL REPO\033[0m\n'
  printf '    ficheros tocados : %s\n' "$(git status --porcelain 2>/dev/null | grep -v '^?? .claude' | wc -l)"
  printf '    líneas añadidas  : %s\n' "$(git diff --shortstat 2>/dev/null | grep -oP '\d+(?= insertion)' || echo 0)"
  [ -f Temp/UnityLockfile ] && printf '    cerrojo de Unity : \033[33mocupado\033[0m\n' || printf '    cerrojo de Unity : libre\n'
  printf '\n  \033[90mrefresca cada 5 s · Ctrl-C para salir\033[0m\n'
  sleep 5
done
