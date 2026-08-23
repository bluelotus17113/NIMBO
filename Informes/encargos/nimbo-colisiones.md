---
description: Da colisionadores de cámara a los árboles y adornos de Isla Nimbo: ahora la cámara los atraviesa, y desde que va a ras de suelo eso se ve constantemente.
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

# El problema, ya medido

El antiobstáculos de la cámara lanza su rayo contra todas las capas
(`IslandCamera.cs:539-540`): **choca con lo que tenga colisionador y atraviesa el resto.**

Sin colisionador hoy (`Informes/nimbo-informe-camara.md`):
- **Los 120 nodos de recolección**: árboles de 4,6 m, rocas, matas. Es el caso más
  frecuente de todos: talar implica ponerse al lado del árbol con la cámara justo detrás.
- **Todos los tejados** (zonas y cabaña). Por eso existía el suelo duro de 3 m en
  `CameraRig.cs:36`, argumento que se cayó al bajar la cámara a la altura de los ojos.
- **Adornos**: farolas de 3,35 m, estatuas de 2,72 m, bancos, vallas
  (`DecorMeshBuilder.cs:91-177`).

La cámara acaba de pasar a 5,5 m y 18°. Antes esto era invisible; ahora no.

# La decisión de diseño que no puedes ignorar

`GatheringView.cs:28-33` dice **«Sin colisionadores, a propósito»**, y el motivo está
escrito: los vecinos no van por física, y hacer sólidos los árboles solo para el jugador
significaría verle a un vecino cruzar por dentro del roble que a ti te frena.

**Ese motivo sigue siendo bueno y no lo vas a deshacer.** Lo que necesitas es distinto:
que la **cámara** choque sin que el **jugador** ni los vecinos choquen. Una capa propia y
una máscara en el antiobstáculos, o una lista de esferas como `_obstacles` pero para la
cámara. Elige y justifica.

Cuidado con el coste: son 120 nodos más los adornos. Si tu solución añade 120
colisionadores a la escena, di en el informe qué mides de coste.

# Tu carpeta

`Assets/_Project/Scripts/Art/World/GatheringView.cs`,
`Assets/_Project/Scripts/Art/World/DecorMeshBuilder.cs` y
`Assets/_Project/Scripts/Art/Camera/IslandCamera.cs`.

`WorldView.cs` **no es tuyo**: es uno de los tres monolitos. Describe el enganche que
necesites con línea y texto exacto y lo aplica el orquestador.

# Comprobar

Una prueba de juego que ponga al protagonista pegado a un árbol con la cámara detrás y
compruebe que **la cámara no está dentro del árbol**. Y añade un encuadre a
`Tests/PlayMode/CapturaEstilo.cs` — pero **no borres los ajenos**: ya hay cinco, dos
puestos por otros agentes esta noche.

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
