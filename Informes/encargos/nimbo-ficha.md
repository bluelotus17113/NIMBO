---
description: Arregla la ficha de habitante de Isla Nimbo: no cabe en pantalla, borra sus propios mensajes cada 0,4 s, y es el único sitio donde se lee la vida social.
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

# Los tres fallos, ya medidos

**1. No cabe y lo que se pierde es lo importante.** `IslanderPanel.cs:40-90` apila seis
tarjetas en un `VisualElement` pelado de 380 px. **Todos los demás paneles de lista usan
`ScrollView`** — Craft:55, Board:56, Chronicle:53, Achievements:51, Events:48, Decor:81,
Build:42, Furnish:43, Creator:55 — este no. La suma estimada es de 1.150-1.400 px contra
unos 780 disponibles a 1080p, y lo que se sale por abajo es justo la tarjeta de
relaciones. La prioridad declarada del proyecto es que **la vida social se lea al abrir el
menú**, y queda cortada.

**2. Todo el feedback se borra antes de leerse.** `UiRoot.cs:29` marca
`_refreshInterval = 0.4f` y `UiRoot.cs:681` llama a `_panel.Refresh()` en cada ciclo.
`SocialSection.Refresh()` (`:62`) empieza con `_hint.text = ""`, así que «Por hoy ya está
bien. Mañana más.» (`:106`), «Te ha traído X» (`:158`), «Has hablado con los dos» (`:172`)
y «Ya está dicho» (`:132,211`) viven **menos de medio segundo**. Además los botones se
reconstruyen bajo el cursor 2,5 veces por segundo.

**3. No hay forma de ver la vida social entera.** Para saber quién anda con quién hay que
abrir las fichas de uno en uno. Es exactamente la enfermedad que ya mató a las peticiones
y que el tablón curó: el propio `RequestBoardPanel.cs:15-19` lo tiene documentado.

# Tu carpeta

`Assets/_Project/Scripts/UI/Islander/**` — `IslanderPanel`, `SocialSection`, `JobSection`,
`HomeSection`. Nada más.

`UiRoot.cs` **no es tuyo**. Si necesitas que deje de refrescar a ciegas o que abra un
panel nuevo, descríbelo en el informe con línea y texto exacto, y lo aplica el
orquestador. Piensa si el arreglo del 2 va mejor en tu lado —que `Refresh` no destruya lo
que no cambió— que en el suyo.

# El tercero es el que más vale

Una tarjeta o un panel que conteste de un vistazo **quién anda con quién en la isla**:
parejas, prometidos, casados, riñas abiertas. La lógica ya existe y no hay que
reimplementarla — hay un `StatusOf` que da el estado entre dos, y `SocialService` guarda
las relaciones. Búscalo, reutilízalo, y **no calcules la compatibilidad otra vez**: en
este repo ya hay una fórmula duplicada por no poder verse entre ensamblados y no queremos
una tercera.

La Crónica lo cuenta en texto por días; esto es lo otro: el estado de hoy, de un vistazo.

# Y lo de regalar

Regalar funciona (`PlayerInteractor.cs:274-285`, `GiftService`), tiene opiniones por
personalidad, da XP y desbloquea logros — **y la interfaz no lo nombra en ningún sitio**.
`SocialSection` no tiene gesto de regalo y la ficha no dice qué le gusta a cada vecino.
Hay hasta un desbloqueo llamado «ExtraGift — un regalo más al día» (`SkillsPanel.cs:264`)
apuntando a un sistema invisible.

Si te cabe en el encargo, enseña **qué le gusta a este vecino** en su ficha. Si no te
cabe, dilo en DESCARTADO con lo que costaría.

# Cuidado con

- Los **estados de vacío con voz** son un patrón bueno de este repo («Hoy no queda nada.
  Vuelve mañana.»). Extiéndelo, no lo quites.
- Los **botones apagados que explican qué falta** (`Gates.cs:18-19`) son la mejor decisión
  de diseño del proyecto. No los escondas.
- La fila de petición es **compartida** con el tablón (`RequestRow.cs:14-19`): si la tocas,
  se mueve en dos sitios.

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

Unity solo deja **un** proceso dentro del proyecto. Si dos entran a la vez, uno se queda
con un lockfile huérfano y el otro cree que compiló. Por eso toda orden de Unity va
envuelta en `flock`, que hace la cola sola:

    flock /tmp/nimbo-unity.lock unity -batchmode -quit \
      -projectPath /home/vaknadesu/Proyectos/isla-nimbo \
      -runTests -testPlatform EditMode \
      -testResults /tmp/nimbo-<tuNombre>.xml -logFile /tmp/nimbo-<tuNombre>.log

Puede tardar en darte el turno. **Espera; no lo saltes, no uses `-nographics` ni borres
ningún lockfile.** Si algo se queda colgado, dilo en el informe y sigue.

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
