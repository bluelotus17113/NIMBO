---
description: Da a cada cultivo de Isla Nimbo su propia silueta y su propio color. Hoy los doce se dibujan con el mismo brote genérico escalado.
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

# El problema, ya medido

`FarmView.cs:110-118` dibuja **el mismo cilindro, del mismo verde, con el mismo color de
fruto `0xE87A64`, para los doce cultivos**. Solo cambia la escala. En Story of Seasons un
cultivo se reconoce a tres metros de distancia y eso es la mitad del placer de tener un
huerto: mirarlo y saber qué hay plantado sin acercarte.

Lo que sí está bien y no se toca: los cuatro estados (Wild/Tilled/Planted/Ready), la
tierra que se oscurece al regar (`FarmView.cs:92-99`) y el fruto que aparece al madurar
(`:125-130`). El armazón es correcto; lo que falta es que cada especie sea ella.

# Tu carpeta

`Assets/_Project/Scripts/Art/World/FarmView.cs`. Solo ese fichero.

Si necesitas piezas de malla nuevas, **añádelas a `Art/Chibi/MeshShapes.cs`** — que es de
uso común— y avisa en el informe de qué añadiste, para que el verificador lo mire aparte.
No toques nada más de `MeshShapes`.

# Cómo se hace aquí

**Todo el arte se genera por código.** No hay ni un modelo importado y no lo va a haber.
`MeshShapes` ya tiene esfera, cilindro, caja, cápsula y casquete; `FoliageMeshBuilder`
sabe tejer racimos de lóbulos con las normales unificadas y `Meadow.Weave` teje briznas
curvadas. Mira los tres antes de escribir geometría nueva: es muy probable que la forma
que necesitas sea una composición de lo que ya hay.

El shader de vegetación es `Nimbo/Foliage` (`ToonPalette.Foliage(color)`), que da viento y
dos caras, y pide que la malla traiga **el color del vértice escrito**: rojo oclusión,
verde máscara de viento, azul degradado raíz-punta, alfa semilla. Una malla sin eso sale
plana y quieta. Está documentado en la cabecera de `Assets/_Project/Shaders/NimboFoliage.shader`.

# Lo que quiero

Que los doce cultivos se distingan **por silueta antes que por color**, porque el color se
lo lleva la luz y la silueta no. Piensa en familias: hoja baja, mata alta, trepadora,
raíz con penacho, bulbo. Y que el fruto de cada uno tenga su color, no el salmón de todos.

Sácate los doce del catálogo de cultivos que haya en `Data` o `Resources/Config` — **no te
los inventes**, y si un cultivo del catálogo no tiene forma asignada, que caiga en un
genérico y dilo en el informe.

# Comprobar

Además de las pruebas, deja escrito **cómo se mira esto**. Ya existe `CapturaEstilo`
(`Tests/PlayMode/CapturaEstilo.cs`) que retrata cuatro encuadres fijos: añade una
herramienta `[Explicit]` igual que retrate el huerto con los doce cultivos maduros, para
que el usuario pueda verlos de un vistazo. Sin foto, esto no se puede juzgar.

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
    Tools/agentes/unity.sh PlayMode <tuNombre> "NombreDeLaPrueba"

Te imprime el recuento y los nombres de las que fallen. Puede tardar en darte el turno —
espera hasta veinte minutos y te avisa de cuánto esperó. **No lo saltes, no lances `unity`
a mano, no uses `-quit` (mata el runner antes de que arranque) y no borres ningún
lockfile.** Si te dice que no pudo correr, **no digas que las pruebas pasaron**.

La línea base viva es **588 pruebas de editor y 142 de juego, cero en rojo, 2 y 10 saltadas**.
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
