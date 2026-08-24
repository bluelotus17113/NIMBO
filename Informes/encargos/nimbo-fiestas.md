---
description: Haz que un festival de Isla Nimbo cambie la isla: hoy el evento empieza, la música cambia, y el mundo sigue exactamente igual.
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

# El fallo, ya medido

`VillageEventStarted` se publica en el bus y llega **a la música y a los avisos**. No llega
al mundo. Durante un festival la isla se ve idéntica a un martes cualquiera: ni decorado,
ni vecinos concentrados donde pasa la cosa.

El calendario funciona, las fiestas se pueden organizar y pagar (`VillageEvents.cs:78-98`),
los flechazos nacen en ellas (`EventSparks.cs:74-114`) y tocar bien anima a la aldea
(`MinigameService.cs:237-252`). **La simulación del festival está entera. Lo que falta es
que se vea.**

En Story of Seasons el festival es el día que recuerdas de la semana, y lo es porque el
pueblo cambia de cara.

# Tu carpeta

`Assets/_Project/Scripts/Art/World/` — pero **solo ficheros nuevos que crees tú**.
`WorldView.cs` es uno de los tres monolitos y **no es tuyo**: describe el enganche que
necesites —qué evento escuchas, qué llama, en qué línea— y lo aplica el orquestador.

Mira cómo lo hizo el agente del Árbol Nimbo (`Informes/informe-arbol.md`, sección 3): una
especificación por partes, cada una con la línea exacta y el texto propuesto. Ese es el
formato que quiero.

# Lo que quiero

Que al empezar un festival **la isla se entere**. Dos ideas, elige y justifica:

- **Decorado**: farolillos, banderines, algo que aparezca en la zona del evento y se vaya
  al acabar. Todo por código, como el resto del arte — mira `DecorMeshBuilder` para las
  piezas que ya existen.
- **Los vecinos van**: que durante el evento tiendan a estar donde pasa. Eso lo decide
  `IslanderBrain`, que no es tuyo, así que aquí seguramente solo puedas describir el
  enganche. Dilo igual: vale más un enganche descrito que una idea callada.

**No hagas las dos a medias.** Una bien cerrada vale más.

# Cuidado con

- Los eventos tienen zona (`zona_escenario`, `zona_plaza`). Los ids **existen y son esos**
  — hubo un fallo histórico en que se pedían con otro nombre y por eso tres eventos no
  ocurrían nunca. Sácalos de `IslandLayout`, no los escribas a mano.
- El decorado tiene que **desaparecer** al acabar. `VillageEventEnded` ya se publica.

# Comprobar

Una prueba de juego que cargue `Isla`, publique `VillageEventStarted` y compruebe que
**algo apareció en el mundo** — y otra que al publicar `VillageEventEnded` se fue. Un
decorado que se queda puesto para siempre es peor que no tenerlo.

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
