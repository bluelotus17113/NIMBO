# Auditoría de interfaz — Isla Nimbo
Fecha: 2026-08-23 · Alcance: `Assets/_Project/Scripts/UI/` (33 ficheros, leídos enteros) + rutas de entrada en `Art/Player/PlayerInteractor.cs` + escalado (`NimboPanelSettings.asset`, `Editor/SceneBuilder.cs`).
Línea base verificada sobre este árbol: EditMode 516 total / 514 passed / 0 failed / 2 skipped; PlayMode 101 / 92 / 0 / 9 (resultados frescos de otra sesión: `/tmp/nimbo-analista-edit.xml` 23:48, `/tmp/nimbo-analista-play.xml` 23:50).

## MAPA DE PANTALLAS

| Panel | Fichero | Cómo se abre | Qué muestra |
|---|---|---|---|
| HUD | `Hud/HudView.cs` | Siempre arriba | Reloj 24 px, día+fase, nimbos, chip «aldeano», badge «peticiones N». Nada es clicable. |
| Barra de acciones | `UiRoot.cs:223-309` | Siempre | 14 botones planos: Comida, Muebles, Ropa, Decorar, Construir/Amueblar/Ampliar (contextuales), Mapa, Mochila, Hacer, Fiestas, Vías, Encargos, Crónica, Logros, Nuevo habitante. |
| Tira de habitantes | `UiRoot.cs:176-184, 505-526` | Siempre, bajo la barra | Un botón por vecino → su ficha. |
| Hotbar | `Player/HotbarView.cs` | Siempre, abajo del todo | 10 huecos clicables + teclas 1-0, prompt de mundo, barra de vigor. |
| Ficha de habitante | `Islander/IslanderPanel.cs` (+ `SocialSection`, `JobSection`, `HomeSection`) | Clic en tira, o F delante del vecino (`PlayerInteractor.cs:107`) | Petición arriba, barras de necesidad, gestos sociales, trabajo, casa, relaciones AL FINAL. **Sin scroll.** |
| Tienda | `Shop/ShopPanel.cs` | Botones Comida/Muebles/Ropa | Catálogo del día, precio, botón comprar apagable. |
| Mochila | `Player/BagPanel.cs` | Botón o Tab | 24 huecos, intercambio a dos clics. |
| Crafteo | `Player/CraftPanel.cs` | Botón «Hacer», mesa o fogón (pestaña directa) | Pestañas A mano/Mesa/Cocina, recetas con ingredientes y faltas. |
| Cajón de envíos | `Player/ShippingPanel.cs` | Solo interacción con el cajón (`UiRoot.cs:379-381`) | Vender por fila o «Vender todo». |
| Mapa | `Player/MapPanel.cs` | Botón o M | Dos islas, puente, punto por vecino (tooltip), tú. |
| Tablón | `Requests/RequestBoardPanel.cs` + `Requests/RequestRow.cs` | Botón «Encargos» o tablón físico | Encargos ordenados por urgencia, filas compartidas con la ficha. |
| Crónica | `Chronicle/ChroniclePanel.cs` | Botón «Crónica» | Texto por días, más reciente arriba. |
| Logros | `Achievements/AchievementsPanel.cs` | Botón «Logros» | Lista con filtros, barras de progreso. |
| Toast | `Achievements/AchievementToast.cs` | Eventos | Cartel en cola, 3,2 s en pantalla. |
| Fiestas | `Village/EventsPanel.cs` | Botón «Fiestas» | Hostables con coste y motivo de bloqueo. |
| Vías | `Player/SkillsPanel.cs` | Botón «Vías» | Cinco vías + «Cómo te ve la aldea». |
| Decorar | `Decor/DecorPanel.cs` | Botón «Decorar» | Zonas, catálogo, plano cenital 300×300 para colocar/quitar. |
| Construir / Amueblar | `Player/BuildPanel.cs` / `FurnishPanel.cs` | Botones contextuales o B dentro | Listas de colocación con selección marcada. |
| Minijuego | `Minigames/MinigamePanel.cs` | Cocinar/pescar/tocar | Uno para los tres, cobra al terminar. |
| Creador | `Creator/CreatorPanel.cs` | «Nuevo habitante» o primera partida | Personalidad, 25 deslizadores, nombre. Con scroll. |
| Menú (propio UIDocument, sortingOrder 10) | `Menu/MainMenuView.cs`, `TitleScreen.cs`, `PausePanel.cs`, `OptionsPanel.cs`, `SaveSummary.cs` | Arranque / Escape | Título con resumen de partida, pausa con día/hora, tres volúmenes. |

## PROFUNDIDAD POR ACCIÓN

| Acción | Ruta de clics | nº | Veredicto |
|---|---|---|---|
| Comprar | HUD → tienda → «Comprar» | 2 | Bien |
| Vender | andar al cajón → E → «Vender todo» | 1 interacción + 1 | Bien (convención del género); no existe vía desde el menú |
| Cocinar desde el menú | HUD → «Hacer» → pestaña «Cocina» → «Hacer» → minijuego | 3 + juego | En el límite; desde el fogón es E → «Hacer» = 1+1 |
| Aceptar encargo | HUD → «Encargos» → «Ayudarle/Darle X» | 2 | Bien |
| Mirar la relación de alguien | tira → habitante → ficha (tarjeta final) | 1… pero inalcanzable | Roto: defecto 1 |
| Ver la vida social completa | abrir las fichas una a una | N | Defecto grave: defecto 4 |
| Regalar | tecla de hotbar → andar → E | 0 de UI | Invisible: defecto 5 |
| Dormir | andar a la hamaca → E («Dormir hasta mañana», `PlayerInteractor.cs:351`) | 1 interacción | Bien |
| Cambiar oficio | tira → habitante → «Ponerle aquí» | 2 | Bien |
| Ampliar casa ajena | tira → habitante → «Ampliársela» | 2 | Bien |
| Ampliar TU casa | entrar en casa → «Ampliar la casa» | 1 | Bien, pero solo existe dentro de casa |
| Decorar | «Decorar» → zona → adorno → plano | 3 | Límite, aceptable por ser colocación espacial |
| Cerrar cualquier panel | botón «Cerrar» (4 posiciones distintas) o repetir toggle | 1-2 | ESC debería hacerlo: defecto 7 |

## DEFECTOS, ORDENADOS POR DAÑO

**1. La ficha de habitante no tiene scroll y su contenido no cabe — `IslanderPanel.cs:40-90`.**
`Root` es un `VisualElement` pelado (380 px de ancho) al que se apilan seis tarjetas; los demás paneles de lista usan `ScrollView` (Craft:55, Board:56, Chronicle:53, Achievements:51, Events:48, Decor:81, Build:42, Furnish:43, Creator:55). El cuerpo de `UiRoot` estira los paneles a la altura disponible (`UiRoot.cs:107-114`) y lo que sobra se pinta debajo de las barras inferiores, que van después en el árbol y dibujan encima. Estimación por estilos: nombre+chips+ánimo+actividad+4 barras ≈ 250 px, petición ≈ 150-300, gestos sociales ≈ 290, trabajo ≈ 260, casa ≈ 140, «Con quién anda» ≈ 100-160 → **~1.150-1.400 px** contra ~780 disponibles a 1080p (HUD ~66 + acciones ~54 + tira ~52 + hotbar ~90 + márgenes). La tarjeta que se pierde es exactamente la de relaciones: la prioridad declarada del proyecto («la vida social se lee al abrir el menú») queda cortada por abajo. Coste: **medio**.

**2. Todo el feedback social se borra a los ≤0,4 segundos — `UiRoot.cs:29` + `UiRoot.cs:681` + `SocialSection.cs:62`.**
`_refreshInterval = 0.4f` y `_panel.Refresh()` en cada ciclo borran y reconstruyen la ficha abierta 2,5 veces por segundo. `SocialSection.Refresh()` empieza con `_hint.text = ""`, así que «Por hoy ya está bien. Mañana más.» (línea 106), «Te ha traído X» (158), «Has hablado con los dos» (172), «Ya está dicho» (132, 211) viven menos de un parpadeo. Además los botones se reconstruyen bajo el cursor cada 400 ms. El mismo refresh reconstruye peticiones, opciones de trabajo y relaciones sin que nada haya cambiado. Coste: **poco** (no limpiar el hint al refrescar; refrescar solo si cambió algo).

**3. Ningún botón del juego tiene hover, pulsado ni foco visible — `UiTheme.cs:131-141, 201-215, 221-236`.**
`Action`, `Secondary` y `Disabled` fijan `backgroundColor` y `borderWidth` **inline**, y en UI Toolkit el estilo inline gana a cualquier pseudoclase del tema. El tema global es el default (`NimboRuntimeTheme.tss` contiene solo `@import url("unity-theme://default")`), cuyo hover/foco vive justo en esas propiedades: queda anulado en el 100 % de los botones. Grep de `MouseEnter|cursor|focus` en `UI/`: cero resultados. Un listón AAA exige estado de foco visible para teclado; aquí ni eso ni feedback de puntero. Coste: **medio**.

**4. Regalar existe, funciona y la interfaz no lo nombra — `PlayerInteractor.cs:274-285`, `Core/Services/Contracts/IGiftService.cs`, `Simulation/Gifts/GiftService.cs`.**
Se regala lo llevado en mano delante del vecino (prompt «Darle X a Y — F para su ficha»). Hay opiniones loved/neutral/disliked por personalidad (12 tipos en `Personality/Types/*`), XP de Convivencia (`PlayerProgressionService.cs:290`) y logros asociados (`AchievementService.cs:282`). Pero: `SocialSection` no tiene gesto de regalo, la ficha no dice qué le gusta a cada quien (grep de `Opinion|Le gusta` en `UI/`: vacío), y el desbloqueo «ExtraGift — un regalo más al día» (`SkillsPanel.cs:264`) apunta a un sistema que no se ve por ningún lado. Coste: **medio**.

**5. No existe vista agregada de la vida social — `IslanderPanel.cs:85-89,169-204` es el único sitio con relaciones.**
Para contestar «¿quién con quién?» hay que abrir las fichas una a una. Es la misma enfermedad que ya mató a las peticiones y que el tablón curó — el propio `RequestBoardPanel.cs:15-19` lo documenta—. La crónica lo narra en texto pero no muestra el grafo. Contradice el objetivo declarado. Coste: **medio** (una tarjeta «La isla por dentro» con las parejas/riñas activas, reutilizando `StatusOf`).

**6. «Vender todo» es irreversible y no confirma — `ShippingPanel.cs:57,150-159`.**
Vende de golpe todos los materiales y cultivos de la mochila. Los encargos de material (`RequestKind.Material`, `RequestRow.cs:177`) pagan por entregar exactamente eso: un clic equivocado puede fundir lo que un vecino esperaba. Sin confirmación ni undo. El título ya aprendió a preguntar cuando algo se pierde (`TitleScreen.cs:147-175`). Coste: **poco**.

**7. ESC no cierra ningún panel de juego — `MainMenuView.cs:207-213` solo atiende menú/pausa/creador; `UiRoot.Update` (657-672) solo Tab/M/B.**
Cerrar exige localizar el botón «Cerrar», que está en cuatro sitios distintos (ver inconsistencias). Coste: **poco**.

**8. El HUD informa pero no lleva a ninguna parte — `HudView.cs:95-106`.**
El badge «peticiones N» cambia de color con Accent cuando hay algo (`RefreshBadge`, 161-168) y no es clicable; el chip «aldeano» no abre Vías; los nimbos no abren la tienda. Atajos gratis que no existen. Coste: **poco**.

**9. Paneles apilables sin exclusividad, con anchos fijos sumables — `UiRoot.cs:606-611` + anchos por panel.**
Nada impide tener abierta la ficha (380) + tienda (420) + decorar (720) = 1.520 px de 1.920. En proporciones estrechas el presupuesto horizontal de diseño baja (a 1024×768: ~1.440 px) y el flexbox comprime los paneles truncando texto. Solo construir/amueblar/minijuego cierran el resto (`UiRoot.cs:462-471,491-498,407`). Coste: **medio**.

**10. Filtros sin estado visible — `AchievementsPanel.cs:79-88` y `DecorPanel.cs:206-215`.**
El chip de filtro activo no se distingue; CraftPanel sí marca la pestaña (`CraftPanel.cs:109`) y DecorPanel marca la zona (`DecorPanel.cs:169`). Coste: **poco**.

**11. Frescura desigual de paneles abiertos — `UiRoot.cs:681-694`.**
Se refrescan solos: ficha, tienda, mochila, mapa, amueblar. No: encargos, logros, crónica, fiestas, vías. Un encargo caduca mientras lo miras y la pantalla sigue enseñándolo verde. Coste: **poco-medio**.

**12. Leer el tablón físico alterna el panel — `UiRoot.cs:412-416`.**
Con «Encargos» abierto, acercarse al tablón de la plaza lo CIERRA. El comentario de `BuildActionBar` (280-283) define el tablón como «mirar de lejos cuántas hay»: ese gesto rompe la lectura. Coste: **poco**.

**13. Copy roto con números y plurales.**
`SaveSummary.cs:66`: «hace 1 minutos». `RequestBoardPanel.cs:154-155`: `RoundToInt(left/60f)` con `hours <= 1` → quedando 61-89 minutos dice «Queda menos de una hora». Coste: **poco**.

**14. El mismo objeto se llama distinto según la pantalla.**
Hotbar recorta el identificador (`HotbarView.cs:167-174`: «regadera»), `ItemNames.Of` usa DisplayName en minúsculas, `BagPanel.NameOf` usa DisplayName tal cual («Regadera de madera»). Coste: **poco**.

**15. Ajustes sin nada de pantalla — `OptionsPanel.cs:9-12`.**
Solo tres volúmenes. Este proyecto ya sufrió el incidente de resolución; el jugador no tiene resolución, modo ventana ni escala UI para defenderse si algo no le encaja. Coste: **medio**.

**16. Jerarquía plana en la barra de acciones — `UiRoot.cs:235-299`.**
14 botones idénticos (Peach, 15 px bold, sin separadores). Los comentarios discuten el orden (Crónica antes de Logros, 288-290) pero el ojo no tiene punto de entrada ni grupos (tiendas / mundo / protagonista / aldea). Coste: **medio**.

**17. Doble título en el minijuego — `MinigamePanel.cs:54-59`.**
`_title` y `_headline` son ambos `UiTheme.Title` (20 px bold): dos títulos del mismo tamaño seguidos. Coste: **poco**.

**18. Las explicaciones de bloqueo viven solo en tooltips — `Gates.cs:36`, `SocialSection.cs:96,144,182,222`, `JobSection.cs:119`, `EventsPanel.cs:119`, `CraftPanel.cs:158`.**
Requieren hover sobre un botón apagado; invisibles con teclado/mando. Pendiente verificar en runtime si UITK muestra tooltip sobre elementos `SetEnabled(false)` — si no lo hace, estas explicaciones no se ven nunca. Coste: **medio**.

**19. Trabajo desperdiciado cada fotograma — `UiRoot.cs:569-589,652`.**
`RefreshPrompt()` hace `GameObject.Find("Protagonista")` cada fotograma hasta encontrarlo; `_panel.Refresh()` reconstruye ~50 elementos 2,5 veces/s aunque no cambie nada. No es UX directa, pero alimenta el defecto 2. Coste: **poco**.

## RESOLUCIÓN (el aviso histórico)

- `Assets/_Project/Settings/NimboPanelSettings.asset:21-29`: `ScaleWithScreenSize`, referencia **1920×1080**, `MatchWidthOrHeight` con `match = 1` (altura). A 1280×720 todo escala ×0,667 uniformemente: **el fallo del «HUD al 67 %» está corregido por el asset**, y `SceneBuilder.cs:78-80` reescribe estos valores en cada reconstrucción.
- Riesgo restante 1: `match = 1` solo-altura + anchos fijos sumables → defecto 9 (desborde en proporciones estrechas).
- Riesgo restante 2: `MainMenuView.cs:192` inicializa nubes hasta x=1920 fijo (el `Update` sí usa `resolvedStyle.width`, líneas 220-226, con fallback 1920 en la 221). Cosmético.
- Riesgo restante 3: fuentes base de 9-11 px en el hotbar (`HotbarView.cs:143,151,158`). El escalado proporcional no las agrava ni las arregla: son pequeñas por diseño a cualquier resolución.

## TABLA DE INCONSISTENCIAS

| Concepto | Cómo se ve/llama en cada sitio | Ruta |
|---|---|---|
| Botón «Cerrar» | Abajo full-width gris (tienda) · arriba-derecha Secondary (mochila, crafteo, cajón, mapa, vías, tablón, logros, crónica, fiestas, decorar) · «Dejarlo» (minijuego) · NO EXISTE (ficha de habitante: se cierra re-pulsando al vecino, `UiRoot.cs:591-603`) · «Listo/Este soy yo» (creador) | `ShopPanel.cs:57-59`; cabezas de cada panel; `MinigamePanel.cs:56` |
| Una pantalla, varias secciones | Tienda = 3 botones separados (Comida/Muebles/Ropa) · Crafteo = 1 panel con pestañas | `UiRoot.cs:235-237` vs `CraftPanel.cs:97-112` |
| Selección marcada | Sí en pestañas de crafteo y zonas de decorar · No en filtros de logros ni de adornos | `CraftPanel.cs:109`, `DecorPanel.cs:169` vs `AchievementsPanel.cs:79-88`, `DecorPanel.cs:206-215` |
| Canal de éxito | Hint local (social, crafteo, cajón, decorar) · Toast global (ampliar tu casa, logros, vías) · Silencio (ampliar casa ajena `HomeSection.cs:109-116`, asignar oficio `JobSection.cs:126-131`, comprar `ShopPanel.cs:138-141`) | — |
| Nombre de objeto | Id recortado (hotbar) · DisplayName en minúscula (`ItemNames.Of`) · DisplayName tal cual (mochila) | `HotbarView.cs:167-174`, `ItemNames.cs:20-32`, `BagPanel.cs:145-152` |
| Refresco abierto | Sí: ficha/tienda/mochila/mapa/amueblar · No: tablón/logros/crónica/fiestas/vías | `UiRoot.cs:681-694` |
| Anchuras de panel | 380, 420, 520, 600, 720, 492, 540, 560, 460, 480, 280, 260 — doce medidas sin escala común | constructores de cada panel |
| Título del panel | Un `Title` en todos · dos seguidos en minijuego | `MinigamePanel.cs:54-59` |
| Teclas documentadas en juego | F y R y botón derecho aparecen en prompts/textos · Tab, M y B solo existen en el código | `PlayerInteractor.cs:281`, `BuildPanel.cs:39-40`, `FurnishPanel.cs:40-41` vs `UiRoot.cs:657-672` |

## LO QUE NO SE PUEDE DESCUBRIR JUGANDO

1. **Regalar** (defecto 4): el sistema con más contenido social tras charlar, invisible fuera de un prompt condicionado a llevar algo regalable en la mano.
2. **Qué le gusta a cada vecino**: las opiniones existen (`GiftService`), ninguna pantalla las enseña.
3. **El mapa social completo** (defecto 5): solo recorriendo fichas.
4. **Tab, M y B**: sin ninguna lista de controles en el juego (las opciones no tienen pestaña de mandos).
5. **«Ampliar la casa»**: el botón solo existe dentro de tu casa (`UiRoot.cs:253-254`); si no entras, no sabes que puedes.
6. **El badge de peticiones** cuenta pero no invita a nada (defecto 8).
7. **Los favores y la mediación** (`SocialSection.cs:149-174`) están detrás de abrir la ficha de alguien; aceptable por diseño, pero nada sugiere que existan.

## LO QUE YA ESTÁ BIEN (no romperlo)

- **Estados de vacío escritos y con voz en todas las listas**: `ShopPanel.cs:98`, `RequestBoardPanel.cs:84-87`, `ShippingPanel.cs:111-113`, `CraftPanel.cs:126`, `FurnishPanel.cs:110`, `ChroniclePanel.cs:81-83`, `UiRoot.cs:525`. Patrón a extender.
- **Botón apagado que explica qué falta** en vez de esconderse: `Gates.cs:18-19` y sus usos. Es la decisión de diseño más AAA del proyecto; su único fallo es el canal (tooltip, defecto 18).
- **Confirmación destructiva que enseña qué pierdes**: `TitleScreen.cs:147-175`.
- **Fila de petición única compartida** entre tablón y ficha: `RequestRow.cs:14-19`.
- **Toast en cola, de uno en uno, con tiempo unscaled**: `AchievementToast.cs`.
- **`pickingMode.Ignore` en capas decorativas**: `MainMenuView.cs:174`, `AchievementToast.cs:58`, `DoorFade.cs:37`.
- **Pestañas con apertura contextual** (el fogón abre Crafteo ya en Cocina): `UiRoot.cs:386-388`.
- **Pausa sin «salir sin guardar»** y con día/hora en grande: `PausePanel.cs`.
- **Presupuesto de refresco consciente**: reloj por fotograma, listas a 0,4 s (`HudView.cs:14-17`, `UiRoot.cs:674-680`).
- **El escalado global del panel** está bien resuelto (ver sección RESOLUCIÓN).

## NO MEDIDO

- No vi el juego: nada de sensaciones. Legibilidad real de 9-11 px, peso visual del crema sobre el cielo y ritmo del toast los decide quien lo mira.
- Tooltips sobre botones `SetEnabled(false)` en runtime (defecto 18): requiere ejecutarlo.
- Altura renderizada real de la ficha: mi cifra (defecto 1) es estimación desde estilos, no layout resuelto; un test EditMode que mida `resolvedStyle.height` de `Root` vs su padre a 1920×1080 lo dejaría medido.
- Mis dos invocaciones batch de pruebas (`/tmp/nimbo-auditoria-ux.log`, `-ux2.log`) salieron con código 0 **sin generar XML ni ejecutar tests**; no quedó colgado nada. La línea base la confirmé con resultados frescos de otra sesión sobre este mismo árbol (514/92, cero en rojo). PlayMode no lo corrí yo.
