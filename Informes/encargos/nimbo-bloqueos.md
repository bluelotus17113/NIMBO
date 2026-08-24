---
description: Dale un canal de verdad a las explicaciones de bloqueo de Isla Nimbo: hoy están en tooltips que en runtime no se muestran nunca.
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

# El hallazgo, medido con un experimento A/B

Otro agente montó una sonda sobre un botón real de la barra de acciones y midió esto:
**UI Toolkit no despacha ni un `TooltipEvent` en runtime — tampoco con el botón
encendido.** En el IL del módulo nadie despacha `TooltipEvent` fuera del editor. La sonda
está en `Tests/PlayMode/DiagnosticoTooltip.cs` y el detalle en `Informes/informe-tema.md`.

Consecuencia: **todas las explicaciones de por qué algo está bloqueado están escritas para
nadie.** Se escriben en `Gates.cs:36` y se aplican en `SocialSection.cs:160,208,246,287`,
`JobSection.cs:160`, `EventsPanel.cs:119`, `CraftPanel.cs:158`.

Y esto duele especialmente porque el auditor de interfaz llamó a ese patrón **«la decisión
de diseño más AAA del proyecto»**: el botón que no se puede pulsar **explica qué falta** en
vez de esconderse. La idea es correcta; el canal no existe.

# Tu carpeta

`Assets/_Project/Scripts/UI/Gates.cs` y lo que necesites crear en `Scripts/UI/`.

**No edites los paneles.** Igual que con el tema: tu solución tiene que llegar a los siete
sitios que usan `Gates` **sin tocarlos uno por uno**. Si alguno necesita cambiar, dilo en
el informe.

# Lo que quiero

Un canal que **se vea de verdad**. Piensa en qué hace un juego del género cuando no puedes
hacer algo: enseñarlo pegado al botón, o en una franja fija donde el jugador ya mira, o al
intentar pulsarlo. **Elige uno y justifícalo**; no hagas tres.

Dos cosas que no puedes romper:
- El botón bloqueado **sigue viéndose bloqueado**. No lo escondas y no lo hagas parecer
  pulsable.
- Debe funcionar **con teclado y mando**, no solo con puntero. Ese era justo el fallo del
  tooltip: exigía hover.

Y mira antes qué hay: este proyecto ya tiene un `AchievementToast` con cola
(`UI/Achievements/AchievementToast.cs`) y estados de vacío con voz en todas las listas.
Reutiliza el patrón que ya funciona en vez de inventar otro.

# Comprobar

Una prueba de juego que cargue `Isla`, encuentre un botón bloqueado de verdad y compruebe
que **la explicación es alcanzable sin puntero**. Y no borres la sonda del tooltip: su
aserción está escrita para seguir pasando el día que existan tooltips de verdad.

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
