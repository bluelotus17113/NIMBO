---
description: Cierra el agujero del borde de la isla de Isla Nimbo: hay hasta 2,8 m de banda abierta entre el prado y la roca por la que se ve a través del mundo.
mode: all
model: opencode/x-preview-f-free
temperature: 0.2
tools:
  write: true
  edit: true
  bash: true
  read: true
  grep: true
  glob: true
---

# El fallo, ya medido

`IslandMeshBuilder.BuildSurface` (`:18-70`) curva el borde del prado hacia abajo con
`−t⁶·3,2` (`:44`): el anillo exterior cae entre **−4,68 y −1,72 m**. `BuildUnderside`
(`:76-149`) empieza su anillo 0 en **y ∈ [−1,86, +1,86]** en la isla de la aldea (el ruido
es `±depth·0,06·0,5`, `:108`).

Entre los dos anillos, **al mismo radio**, hay sectores con hasta **~2,8 m de banda
abierta**. Y las dos mallas son de una cara: por esa banda **se ve a través de la isla**.

Desde la cámara de 150 m no se ve jamás. A la altura de los ojos sí — y los sitios de
pesca están al **78 % o más del radio** (`PlayerInteractor.cs:38,412`), o sea justo ahí.
El juego está pasando a tercera persona ahora mismo en otro panel, así que esto deja de
ser invisible esta noche.

# Tu carpeta

`Assets/_Project/Scripts/Art/World/IslandMeshBuilder.cs`. Solo ese fichero.

# Lo que hay que hacer

Una **falda**: la banda de geometría que une el anillo exterior del prado con el anillo 0
de la roca, en las dos islas. Que cierre el volumen.

Cuidado con tres cosas:

**Los dos bordes tienen que coincidir de verdad.** El prado y la roca calculan su contorno
irregular con la misma semilla y el mismo suavizado, pero **cada uno lo hace por su
cuenta**. Si tu falda recalcula el borde en vez de leer los vértices que ya existen, en
cuanto alguien cambie un parámetro vuelve a abrirse la rendija, y esta vez sin que nadie
lo note. Cose vértices reales, no fórmulas repetidas.

**Los materiales son distintos.** El prado va con `ToonPalette.Solid(Grass)` y la roca con
`Solid(Rock)`. Decide de quién es la falda y justifícalo: probablemente de la roca, porque
lo que se ve ahí es el corte de tierra bajo la hierba. Mira cómo `BuildUnderside` hace ya
sombreado plano por cara (cada triángulo con sus propios vértices) — la falda debe
sombrearse igual o se verá una junta.

**La isla del jugador tiene otra profundidad** (`_islandDepth * 0.6f`) y otra semilla (31).
Comprueba las dos.

# Comprobar

Una prueba de editor que sea la que habría cazado esto: **para cada sector, que no quede
hueco vertical entre el último anillo del prado y el primero de la roca**. Ese test es más
valioso que el arreglo, porque el arreglo se puede volver a romper.

Y añade un encuadre a `Tests/PlayMode/CapturaEstilo.cs` mirando el borde de la isla desde
fuera y desde abajo, para que se pueda ver que está cerrado. Cuidado: hay otro agente
tocando ese fichero para añadir su propio encuadre — si al ir a escribir ves un encuadre
que no pusiste tú, **no lo borres**, añade el tuyo al lado.

# Isla Nimbo — las reglas de la casa

Juego de Unity 6000.5.5f1 + URP en `~/Proyectos/isla-nimbo`. Rune Factory en el cielo
más gestión tipo Tomodachi: una isla flotante con vecinos que viven solos.

## Lo que se te pide siempre

**Cada afirmación tuya lleva una ruta `fichero:línea` o un número que mediste.** «Convendría
mejorar el inventario» no vale. «`InventoryPanel.cs:210` dibuja 40 huecos fijos y
`_economy.Stack(id)` (línea 233) se llama dentro del bucle, así que son 40 consultas por
fotograma» sí vale.

**No ves el juego.** Puedes leer código, compilar y contar pruebas. No sabes si algo se
siente bien. No escribas «queda fluido» ni «se ve mejor»: di qué escribiste y qué midió
la prueba. Lo de si se siente bien lo decide el usuario mirándolo.

## Cómo se compila y se prueba, y es obligatorio

Unity solo deja **un** proceso dentro del proyecto, y además hay una carrera conocida:
cuando el anterior suelta el cerrojo pero todavía se está cerrando, el siguiente entra, ve
el lockfile del proyecto y **sale con éxito sin ejecutar nada**. Dos agentes ya lo
sufrieron sin darse cuenta. Por eso no lances Unity a mano: usa el envoltorio, que hace la
cola, espera a que el anterior se cierre del todo y **comprueba que el XML existe de
verdad** antes de darlo por bueno.

    Tools/agentes/unity.sh EditMode <tuNombre>
    Tools/agentes/unity.sh PlayMode <tuNombre>

Te imprime el recuento y los nombres de las que fallen. Puede tardar en darte el turno:
**espera**. Si te dice que no pudo correr, **no digas que las pruebas pasaron**.

La línea base viva es **514 pruebas de editor y 92 de juego, cero en rojo, 2 y 9 saltadas**.
Si tu cambio baja de ahí, lo has roto.

## Lo que no se toca, nunca

- `~/.config/unity3d/Nimbo/Isla Nimbo/` — la partida del jugador. **Ni leer para
  escribir, ni copiar encima, ni nada.** Ya se perdió una isla de treinta días este mes.
- Ficheros fuera de la carpeta que te asigne tu encargo. Trabajamos varios a la vez y lo
  que se rompe con agentes en paralelo no es el trabajo de cada uno: es la costura. Si
  necesitas engancharte a algo de otro, **descríbelo en el informe** —qué evento, qué
  llamada, en qué línea— y lo hace el orquestador.
- `git push`, `git reset --hard`, `git checkout --`, `rm -rf`. No commitees: deja el
  árbol sucio y ya lo reviso.

## Cómo está hecho este proyecto

- **Todo el arte se genera por código.** No hay un solo modelo importado. Mallas, texturas,
  música: C# que las construye en el arranque. Si tu solución pasa por importar un asset,
  no es la solución.
- **Ensamblados separados** (`Nimbo.Core`, `Nimbo.Data`, `Nimbo.Art`, `Nimbo.UI`,
  `Nimbo.Game`, `Nimbo.Social`, `Nimbo.Events`…). `Nimbo.Art` **no ve** `Nimbo.Events`.
  Para hablar entre módulos: `EventBus` (`Nimbo.Core.Events.GameEvents`) o
  `ServiceRegistry` + una interfaz en `Nimbo.Core.Services.Contracts`. Si tu cambio no
  compila por una referencia que falta, la respuesta casi nunca es añadir la referencia.
- **Comentarios `///` que explican el porqué, no el qué.** Nadie necesita leer «incrementa
  el contador»; sí necesita saber por qué 0,45 y no 0,5. Es la convención del repo entero
  y se espera de ti.
- Español en comentarios, nombres de prueba y mensajes de assert. Los identificadores de
  código, en inglés como está el resto.

## La regla que este proyecto aprendió a golpes

**Un sistema no está hecho hasta que hay una prueba que carga la escena `Isla` de verdad y
comprueba que el jugador tiene por dónde llegar a él.** Cuatro sistemas de aquí estaban
escritos, probados y apagados: nadie los había enchufado y ningún test lo delataba.
Mira `Assets/_Project/Tests/PlayMode/CronicaEnLaIslaTests.cs` como modelo.

Variantes de la misma enfermedad, por si te toca alguna:
- **El enchufe en un agujero que no existe**: una referencia por nombre —un id de zona, de
  receta— que no está en el otro lado. No hay excepción, no hay aviso: simplemente no pasa.
- **El listón donde no llega nadie**: un umbral por debajo del mínimo que el sistema puede
  producir, así que nunca se cumple.
- **Enterrado**: encendido, pero con una forma de mirarlo que nadie usaría dos veces.
