---
description: Enciende el Árbol Nimbo: está entero, registrado, y nadie lo llama nunca.
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

`NimboTree.TryTalk` (`Island/NimboTree.cs:74`) **no tiene un solo llamador**: ni interfaz,
ni mundo, ni pruebas. Un grep de `TryTalk|CanTalkToday|GrowthStage|GrowthScale` fuera del
propio fichero da **cero resultados**. El servicio está construido y registrado
(`GameBootstrap.cs:335,367`) para nada.

Dentro hay regalo diario, pistas hacia el vecino más triste (`NimboTree.cs:136-152`) y
chistes. Todo escrito y apagado.

Y el árbol que se dibuja es una malla fija de 26 m (`WorldView.cs:134`) que **no lee
`GrowthScale`** (`NimboTree.cs:66`): tampoco crece nunca.

El GDD lo marca `[x] El Árbol Nimbo funcional` en §17.1. No lo es.

# Tu carpeta

`Assets/_Project/Scripts/Island/NimboTree.cs` y lo que necesites en `Scripts/Island/`.

**`PlayerInteractor.cs` y `WorldView.cs` NO son tuyos** — son dos de los tres monolitos
del proyecto, donde un cambio rompe cuatro cosas a la vez. Describe los dos enganches con
línea exacta y texto propuesto, y los aplica el orquestador:

1. Un `TargetKind.Tree` en `PlayerInteractor` que llame a `TryTalk`. **El patrón ya
   existe**: mira `TryTargetBoard` (`PlayerInteractor.cs:459`) y cópialo, incluido el
   cartel que nombra la tecla.
2. Que `WorldView.cs:134` escale la copa por `GrowthScale`.

# Lo que quiero de verdad

Que hablar con el Árbol sea **el ritual de apertura del día**. El analista lo dijo: el
ciclo diario está casi entero —día de 24 min, dormir, semana con bonos— y lo que falta es
que el día empiece con algo. El ritual previsto es justo este sistema.

Así que además del enganche: ¿el árbol tiene algo que decir **hoy** que no dijera ayer?
Mira qué sabe ya (el vecino más triste, el regalo, los chistes) y asegúrate de que el
jugador tiene un motivo para volver mañana.

# Comprobar

La prueba que hace falta es de juego: cargar `Isla`, ponerse delante del Árbol y
comprobar que **existe la forma de hablarle**. Con eso este sistema deja de estar apagado.

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
