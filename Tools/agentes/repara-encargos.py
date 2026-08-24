#!/usr/bin/env python3
"""Pone al día el bloque de «cómo se prueba» que cada encargo lleva copiado dentro.

Cada fichero de agente arrastra su propia copia de las reglas de la casa. Las reglas
se corrigieron —`nimbo-reglas.md` ya manda usar el envoltorio— pero las copias no, y
nueve encargos seguían diciéndole al agente que lanzara Unity con `-batchmode -quit`.

Eso no es una errata: `-quit` junto a `-runTests` **mata al runner antes de que
arranque**, y Unity sale «successfully» sin ejecutar una prueba ni escribir XML. Un
verificador que siga esa línea cree que verificó, no verificó, y aprueba a ciegas —
que es exactamente el fallo silencioso que este repo lleva toda la noche cazando.

También se refresca la línea base, que estaba anclada en 514/92 de hace tres olas.
Una base vieja es igual de dañina en la otra dirección: hace pasar por «no lo has
roto» a un cambio que sí borró pruebas.
"""
import pathlib
import re
import sys

AGENTES = pathlib.Path.home() / ".config/opencode/agent"
REGLAS = pathlib.Path.home() / ".config/opencode/nimbo-reglas.md"

VIEJO = re.compile(
    r"Unity solo deja \*\*un\*\* proceso dentro del proyecto\. Si dos entran a la vez.*?"
    r"ningún lockfile\.\*\* Si algo se queda colgado, dilo en el informe y sigue\.",
    re.DOTALL,
)

NUEVO = """Unity solo deja **un** proceso dentro del proyecto, y además hay una carrera conocida:
cuando el anterior suelta el cerrojo pero todavía se está cerrando, el siguiente entra, ve
el lockfile del proyecto y **sale con éxito sin ejecutar nada**. Dos agentes ya lo
sufrieron sin darse cuenta. Por eso no lances Unity a mano: usa el envoltorio, que hace la
cola, espera a que el anterior se cierre del todo y **comprueba que el XML existe de
verdad** antes de darlo por bueno.

    Tools/agentes/unity.sh EditMode <tuNombre>
    Tools/agentes/unity.sh PlayMode <tuNombre>
    Tools/agentes/unity.sh PlayMode <tuNombre> "NombreDeLaPrueba"

Te imprime el recuento y los nombres de las que fallen. Puede tardar en darte el turno —
espera hasta veinte minutos y te avisa de cuánto esperó. **No lo saltes, no lances `unity`
a mano, no uses `-quit` (mata el runner antes de que arranque) y no borres ningún
lockfile.** Si te dice que no pudo correr, **no digas que las pruebas pasaron**."""


def main(edit: int, play: int, saltadas_edit: int, saltadas_play: int) -> int:
    base = (f"La línea base viva es **{edit} pruebas de editor y {play} de juego, cero en "
            f"rojo, {saltadas_edit} y {saltadas_play} saltadas**.")
    vieja_base = re.compile(r"La línea base viva es \*\*\d+ pruebas de editor y \d+ de "
                            r"juego, cero en rojo, \d+ y \d+ saltadas\*\*\.")

    tocados = 0
    for fichero in sorted(list(AGENTES.glob("nimbo-*.md")) + [REGLAS]):
        texto = fichero.read_text(encoding="utf-8")
        nuevo = VIEJO.sub(NUEVO, texto)
        nuevo = vieja_base.sub(base, nuevo)
        if nuevo != texto:
            fichero.write_text(nuevo, encoding="utf-8")
            tocados += 1
            print(f"  ✎ {fichero.name}")

    restantes = [f.name for f in AGENTES.glob("nimbo-*.md")
                 if "batchmode -quit" in f.read_text(encoding="utf-8")]
    print(f"\n{tocados} encargos puestos al día · base {edit}/{play}")
    if restantes:
        print(f"⚠ siguen con -quit: {', '.join(restantes)}")
        return 1
    print("✓ ningún encargo manda ya lanzar Unity con -quit")
    return 0


if __name__ == "__main__":
    sys.exit(main(*(int(a) for a in sys.argv[1:5])))
