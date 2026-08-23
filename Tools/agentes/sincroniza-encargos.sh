#!/bin/bash
# Los encargos viven en ~/.config/opencode/agent/, que los agentes NO pueden leer:
# opencode bloquea todo lo que esté fuera del proyecto. Los verificadores necesitan el
# encargo del agente al que revisan —es contra eso contra lo que juzgan—, así que aquí
# se copian dentro. Correr esto cada vez que se cambie un encargo.
cp ~/.config/opencode/agent/nimbo-*.md /home/vaknadesu/Proyectos/isla-nimbo/Informes/encargos/
cp ~/.config/opencode/nimbo-reglas.md /home/vaknadesu/Proyectos/isla-nimbo/Informes/encargos/_reglas.md
echo "encargos sincronizados: $(ls /home/vaknadesu/Proyectos/isla-nimbo/Informes/encargos/*.md | wc -l)"
