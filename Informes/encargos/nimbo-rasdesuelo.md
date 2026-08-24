---
description: Ajusta el prado de Isla Nimbo a la altura de los ojos: las flores están tumbadas mirando al cielo y la niebla empieza donde ya no se ve.
mode: all
model: opencode/x-preview-f-free
temperature: 0.25
tools:
  write: true
  edit: true
  bash: true
  read: true
  grep: true
  glob: true
---

# Lo que se midió

**1. Las flores están tumbadas.** `Meadow.Bloom` las inclina solo 4–22° respecto a la
normal del suelo, y el comentario dice por qué: «Inclinarla más la hacía desaparecer de
canto desde la cámara del juego, que mira desde arriba» (`Meadow.cs:316-318`).

**Esa premisa acaba de dejar de ser cierta.** La cámara ya no mira desde arriba: va a
5,5 m y 18°. A esa altura una flor tumbada es un hilo invisible. Hay que darle la vuelta
al razonamiento y dejar el comentario contando la historia nueva, no la vieja.

**2. La niebla nunca entra.** `IslandLighting.cs:57-58` la arranca a 60 m y la cierra a
280, pensada para ver la isla desde 150. A 5 m de cámara **no se activa jamás**, así que
el juego perdió la bruma que hacía que una isla flotante se leyera como una isla flotante.

**3. La hierba alta no sufre** —briznas verticales y a dos caras— así que no la toques
salvo que midas algo.

# Tu carpeta

`Assets/_Project/Scripts/Art/World/Meadow.cs` y
`Assets/_Project/Scripts/Art/World/IslandLighting.cs`.

# Cuidado con dos cosas

**El presupuesto.** Son 9.000 matas por isla y la cámara ahora las mira de cerca: el
informe de cámara avisa de que a 5 m la hierba llena el encuadre como no lo hacía desde
150 y que **el overdraw no está medido**. Si subes densidad o tamaño de flor, mide y dilo.

**La niebla es un compromiso.** Bajarla mucho tapa la isla de enfrente y el puente, que
son referencias que el jugador usa para orientarse. Elige un número, justifícalo, y di qué
se pierde.

# Comprobar

Esto **no se puede juzgar sin verlo**. Añade a `Tests/PlayMode/CapturaEstilo.cs` un
encuadre a ras de suelo mirando un macizo de flores — y **no borres los cinco encuadres
que ya hay**, dos los pusieron otros agentes esta noche. Deja escrito en el informe qué
capturas hay que mirar y qué se debería ver en cada una.

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

La línea base viva es **588 pruebas de editor y 142 de juego, cero en rojo, 2 y 10 saltadas**.
Si tu cambio baja de ahí, lo has roto.

## Dónde se escribe

**Todo lo que escribas para que lo lea otro va en `Informes/` dentro del proyecto**, nunca
en `/tmp`. opencode bloquea leer ficheros fuera del proyecto —un agente se pasó nueve
intentos rebotando contra eso— así que `/tmp` es un agujero negro: se puede escribir y
nadie lo puede leer después. Los resultados de las pruebas caen solos en
`Informes/pruebas/`.

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
