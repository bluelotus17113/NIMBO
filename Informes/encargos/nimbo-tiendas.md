---
description: Arregla la costura rota de las tiendas de Isla Nimbo: comprar es imposible en todo el juego porque los ids que pasa la interfaz no existen en el catálogo de tiendas.
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

`UiRoot.cs:235-237` abre las tiendas con los ids `"tienda_comida"`, `"tienda_muebles"`,
`"tienda_ropa"`. El único resolutor, `ShopDefinition.Get` (`ShopDefinition.cs:65-70`),
solo conoce `"NimboMart"`, `"Muebles Nimbo"`, `"Boutique Celeste"`, `"Antigüedades
Nimbo"` y `"Mercado flotante"`.

Consecuencia: `EconomyService.StockOf` (`EconomyService.cs:132-133`) devuelve vacío
siempre, `ShopPanel` enseña «Hoy no queda nada. Vuelve mañana.» (`ShopPanel.cs:96-99`)
todos los días de todas las partidas, y `TryBuy` —que tiene **un único llamador** en todo
el juego, `ShopPanel.cs:140`— no se ejecuta jamás. **No se puede comprar nada: ni
semillas, ni muebles, ni ropa, ni los 40 acabados de pared y suelo.**

No hay excepción ni aviso. Es la misma enfermedad de §18: el enchufe puesto en un agujero
que no existe.

# Tu carpeta

`Assets/_Project/Scripts/Economy/**` y `Assets/_Project/Scripts/UI/Shop/**`. Nada más.

**`UiRoot.cs` no es tuyo** — lo comparten cuatro agentes. Si la solución que eliges pasa
por cambiar esas tres líneas, **descríbela en el informe** (línea exacta y texto nuevo) y
la aplica el orquestador. Piensa cuál de los dos lados debe ceder y justifícalo: puede
que lo correcto sea que el catálogo hable el idioma de las zonas, no al revés.

# Y el test que nadie escribió

Esto es la mitad del encargo, y la más importante. Hay dos tests que tocaban el tema y
**ninguno cruzó la costura**:

- `EconomiaHuertoTests.cs:103` pregunta por `"NimboMart"` **a mano**, así que nunca usó
  el id que usa la interfaz de verdad.
- `EconomyTests.cs:368` afirma que un id desconocido devuelve vacío **sin error**: el modo
  de fallo exacto, escrito como comportamiento correcto.

Escribe la prueba que sí lo habría cazado: **recorrer los ids que la interfaz usa de
verdad y exigir stock no vacío en cada uno.** Sácalos de donde estén declarados, no los
copies a mano — copiarlos a mano es repetir el fallo en el test. Y decide qué hacer con
`EconomyTests.cs:368`: si el contrato pasa a ser «un id desconocido es un error», ese test
tiene que cambiar, y el cambio va explicado.

Añade también una prueba de juego que cargue `Isla`, abra la tienda como la abre el
jugador y compruebe que hay algo comprable. Sin ella esto no está entregado.

# Cuidado con

- **Que las semillas vuelvan a estar en tienda.** `EconomiaHuertoTests` garantiza una
  semilla por familia y fija la banda de 21,5–29 nimbos/día por cultivo: media economía
  de la granja cuelga de esto. Si tu arreglo cambia el surtido, esos números se mueven.
- **El surtido rota por día.** Mira cómo se sortea antes de tocarlo.

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
