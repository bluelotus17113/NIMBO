# Isla Nimbo — mapa real del proyecto (analista)
Fecha: 2026-08-23 · HEAD: fc47a00 («La isla deja de ser de plastico»)

**Línea base medida hoy** (no la de memoria): EditMode **516 pruebas / 514 pasadas / 0 fallos / 2 saltadas**; PlayMode **101 / 92 / 0 / 9**. La línea base está viva y en verde. Lo relevante: **las 2 saltadas de editor son los dos tests que prueban el rechazo de trabajos** (`GestionAldeaTests.cs:118,128` → `TryPeorOficio` devuelve falso en `GestionAldeaTests.cs:273` cuando el peor encaje no baja de 0,30 — y nunca baja: el mínimo producible es 0,35). El `[~]` del GDD no es una nota al pie: lleva su propio arnés de tests saltándose a sí mismos dos veces por corrida.

Código: **37.757 líneas** en `Assets/_Project/Scripts` (18 carpetas), **16.279 líneas** de pruebas (51 ficheros editor + 21 PlayMode).

---

## ESTADO POR SISTEMA

| Sistema | Servicio registrado | Se ve desde el juego | Pruebas | Líneas | Veredicto |
|---|---|---|---|---|---|
| **Core** | es la base (EventBus, SaveSystem, GameClock, Rng, 25 contratos) | todo cuelga de él | CoreTests 271 + GuardadoAislado | 3.546 | Completo |
| **Data** | n/a (datos muertos de guardado) | n/a | indirecta en todos | 2.451 | Completo |
| **Game** | GameBootstrap registra **28 servicios** (`GameBootstrap.cs:342-369`) | arranca con menú (`FlowController`) | BootTests 877 (PlayMode) | 691 | Completo; monolito orden-crítico |
| **Farming** | IFarmingService ✓ | 0 clics: E sobre la casilla (`PlayerInteractor.cs:546`) | FarmingTests 436 + EconomiaHuerto 204 + BootTests huerto entero | 417 | Funciona; **visual genérico** (ver HM-2) |
| **Gathering** | IGatheringService ✓ | 0 clics: E ante el nodo; 120 nodos (`GatheringService.cs:38`), 16 tipos | GatheringTests 582 + Materiales 125 + RecursosVisibles (PM) | 648 | Completo |
| **Crafting** | ICraftingService ✓ | banco/fogón (StationUsed) o «Hacer»; 44 recetas (GDD dice 36) | CraftingTests 467 + Herramientas 203 | 301 | Completo |
| **Economy** | IEconomyService ✓ | botones Comida/Muebles/Ropa → **siempre vacíos** (ver hallazgo 1) | EconomyTests 501 + EconomiaHuerto | 572 | **Escrito y roto en la costura**: no se puede comprar nada |
| **Items** | IInventoryService ✓ | Tab / «Mochila» / hotbar | InventoryTests 477 | 308 | Completo |
| **Social** | ISocialService ✓ | E habla/regala; F ficha → SocialSection con 9 gestos | Cortejo 431, Bodas 379, Triangulos 330, BodaProtagonista 347, Convivencia 248 + PM | 2.044 | El más completo y mejor probado del repo |
| **Simulation** | ISimulationService, IRequestService, IJobService, IAchievementService, IGiftService, WardrobeService ✓ | muñecos con rutina horaria, tablón, logros | IslandSim 269, JobsAndRhythm 163, Encargos 405, Achievement 304, GestionAldea 305 | 2.742 | Completo salvo umbral de trabajos muerto ([~] honesto) y armario a medias |
| **Personality** | IPersonalityService ✓ | caras y reacciones por tipo | PersonalityTests 145 | 2.046 | Completo (16 tipos, un fichero por tipo) |
| **Events** | IChronicleService, IMinigameService, IVillageEvents ✓ | Crónica/Fiestas/Encargos (1 clic), minijuegos con sitio en el mundo | Events 461, Cronica 358, Fiestas 358, Minigame 409, MinijuegosEnchufados 361 + 6 ficheros PM | 2.534 | Completo tras los enchufes de §18 |
| **Housing** | IHousingService, IHomeUpgradeService ✓ | Amueblar/Ampliar/Construir (contextuales) | HousingPlacement 301 + HomeUpgrade 424 | 1.075 | Completo; **acabados (papel/suelo) escritos y apagados** |
| **Island** | IIslandService, IDecorService, IBuildService, NimboTree ✓ | zonas, construir, DecorPanel… **el Árbol no tiene puerta** | DecorTests 488 + BuildGrid 153 | 1.252 | Completo salvo **NimboTree: escrito y apagado** |
| **Player** | PlayerService, IPlayerProgression, IConductService ✓ | «Vías» (1 clic) con las cinco vías + «cómo te ve la aldea» | Progresion 253, Conducta 252 + ViasEnLaIsla, CortejoEnLaIsla (PM) | 516 | Completo (23 puertas en `PlayerProgressionService.cs:47-72`) |
| **CharacterCreator** | IIslanderFactory ✓ | arranque + «Nuevo habitante» | BootTests (creador) | 481 | Funciona; **sin voz ni ropa** pese al `[x]` |
| **UI** | vistas | es la vista: 16 botones + fila de vecinos + teclas Tab/M/B/E/F | visibilidad PM: Crónica, Tablón, Fiestas, Vías, Encargos, Minijuegos, Boda | 7.037 | Completo; costuras por reflexión frágiles; ShopPanel roto por ids |
| **Art** | vistas | es el mundo | Estilo 343, Musica 147, CameraRig 470, WalkAround 103, MeshShape 63 + 8 capturas PM | 9.096 | Completo; árbol fijo, ropa sin renderizar |

Ningún servicio escrito dejó de registrarse: los 28 registros cubren todos los contratos con implementación (`IPersonalityBehaviour` se resuelve por roster, no por registro). Los tres construidos sin registro — `IslanderBrain`, `WeddingPlanner`, `EventSparks` (`GameBootstrap.cs:333,310,331`) — son oyentes de eventos por diseño.

---

## LO QUE EL GDD DICE Y NO ES CIERTO

1. **«[x] Economía con monedas, tiendas…» y «[x] …y las semillas se compran» (§17.1) — FALLO POR LA COSTURA.** Los tres botones de tienda pasan ids de zona: `UiRoot.cs:235-237` (`"tienda_comida"`, `"tienda_muebles"`, `"tienda_ropa"`). El único resolutor, `ShopDefinition.Get` (`ShopDefinition.cs:65-70`), solo conoce `"NimboMart"`, `"Muebles Nimbo"`, `"Boutique Celeste"`, `"Antigüedades Nimbo"`, `"Mercado flotante"`. `EconomyService.StockOf` (`EconomyService.cs:132-133`) devuelve vacío y `ShopPanel` enseña «Hoy no queda nada. Vuelve mañana.» para siempre (`ShopPanel.cs:96-99`). `TryBuy` tiene **un único llamador en todo el juego** (`ShopPanel.cs:140`): la compra entera —semillas, muebles, ropa— es inalcanzable. Es la misma enfermedad de §15.3 (eventos que pedían `stage` en vez de `zona_escenario`), en las tiendas, y **ningún test la cruza**: `EconomiaHuertoTests.cs:103` pregunta por `"NimboMart"` a mano, y `EconomyTests.cs:368` hasta afirma que un id desconocido devuelve vacío *sin error* — el modo de fallo exacto, consagrado como comportamiento correcto.
2. **«[x] El Árbol Nimbo funcional» (§17.1) — APAGADO.** `NimboTree.TryTalk` (`NimboTree.cs:74`) no lo llama nadie: ni UI, ni mundo, ni tests (búsqueda de `TryTalk|CanTalkToday|GrowthStage|GrowthScale` fuera del fichero: cero resultados). Está construido y registrado (`GameBootstrap.cs:335,367`) para nada. Y el árbol dibujado es una malla fija de 26 m (`WorldView.cs:134`) que no lee `GrowthScale` (`NimboTree.cs:66`): tampoco crece.
3. **«[x] Creador de personajes completo (cuerpo, cara, voz, ropa, personalidad)» (§17.1) — A MEDIAS.** Hay cuerpo/cara/personalidad/nombre (`CreatorPanel.cs:238-266,163-166,61`). No hay pestaña de voz ni de ropa (búsqueda de `Voz|Timbre|ropa|outfit` en CreatorPanel: vacío). La voz existe como dato y sintetizador — `VoiceConfig` (`IslanderIdentity.cs:10`), sorteada por `IslanderFactory.cs:41`, hablada en `AudioDirector.cs:361` — pero el jugador no la elige nunca.
4. **«[x] Sistema de relaciones con los 10 niveles y 9 estados» (§17.1) — OTRO MODELO.** La amistad tiene **5** niveles (`RelationshipRecord.cs:6-13`, Stranger→BestFriend), no 10; los «estados» viven repartidos entre `RomanceStage` (7 valores, `:59-68`) y `ConflictStage` (5, `:23-32`). El sistema funciona y está probado, pero la tabla §8.2 describe un diseño que no es el código.
5. **Números de catálogo desfasados.** «40 muebles base + pool de 200»: hay **80** muebles, 3 a nivel 1 (`catalogo_muebles.json`). «8 prendas base + pool de 150»: hay **60**, 4 a nivel 1 (`catalogo_ropa.json`). «36 recetas»: hay **44** (`catalogo_recetas.json`, 6 de cocina). El código va por delante del documento en estos tres.
6. **Lo que sí se sostiene** (comprobado, no asumido): progresión de cinco vías con 23 puertas y pantalla (`SkillsPanel.cs:118`), cortejo con rechazo y rivales (`Courtship`, `LoveTriangles`, `SocialSection.cs:123-248`), boda contigo con anillo de cristal (`WeddingPlanner` + `HomeUpgradeService` + `mat_cristal_nimbo` en catálogo), bodas/bebés autónomos (`WeddingPlanner.cs:89-242`), Crónica guardada (`NewsBoard` + `ChroniclePanel`), fiestas organizables (`VillageEvents.cs:78-98`), encargos que cobran lo que piden (`RequestService.cs:146-198`), herramientas nivel 2 (`PlayerInteractor.cs:699-740`), 16 nodos→12 materiales, semana estructurada (`WeeklyRhythm`, usado en `GameBootstrap.cs:193-204` y `JobService.cs:174`), guardado atómico con aislamiento de pruebas (`SaveSystem.cs:14-19,76-80`). El `[~]` del rechazo de trabajos es exacto y sigue abierto: `MinJobAffinity = 0.3f` (`JobService.cs:115`) contra mínimo producible 0,35.

---

## LO QUE ESTÁ ESCRITO Y APAGADO

1. **Las tiendas** — el enchufe está en un agujero que no existe. Falta una línea de traducción: o los `ShopId` de `ShopDefinition.cs:38-58` pasan a ser `tienda_comida/tienda_muebles/tienda_ropa`, o `UiRoot.cs:235-237` pasa los ids reales. Y el test que falta: recorrer `ShopPanel` con los ids que usa la UI y exigir stock no vacío (cuatro líneas, la misma forma que el test de zonas de §15.3).
2. **El Árbol Nimbo** — falta el punto de interacción: un `TargetKind.Tree` en `PlayerInteractor` (el patrón ya está: `TryTargetBoard`, `:459`) que llame a `TryTalk`, y que `WorldView.cs:134` lea `GrowthScale` para escalar la copa. El servicio está entero: regalo diario, pistas hacia el vecino más triste (`NimboTree.cs:136-152`), chistes.
3. **Los acabados de vivienda** — `SetWallpaper` está en el contrato (`IHousingService.cs:55`) y en la implementación (`HousingService.cs:149`) **sin un solo llamador**; `InteriorView` pinta colores fijos (`InteriorView.cs:192,200`) y nunca consulta el id guardado. Encima, los ids por defecto **no existen en el catálogo**: `wall_liso_crema` (`RoomLayout.cs:68-69`) y `floor_madera_clara` (`RoomLayout.cs:96`, `HomeUpgradeService.cs:274`, `InteriorView.cs:127`) frente a los 20+20 acabados reales de `catalogo_acabados.json`. Hay 40 acabados con precio y categoría vendible que nadie puede comprar (tiendas rotas), elegir (FurnishPanel los excluye, `FurnishPanel.cs:104`) ni ver pintados.
4. **Media librería de armario** — `WardrobeService.Wear/FavouriteOf/WardrobeOf` (`WardrobeService.cs:95,146,105`) sin llamadores: los vecinos nunca se cambian solos («La usa la IA», dice el comentario de `:143` — la IA no lo hace) y no hay vista de armario. `Give/Receive/Liking` sí viven, vía regalos.
5. **La ropa equipada no se renderiza** — `EquippedOutfit` se escribe (`WardrobeService.cs:91,100`) y ninguna vista de `Art/Chibi` lo lee. Regalar ropa cambia datos invisibles.
6. **`RequestKind.IslandBuilding`** (`IslanderRequest.cs:19`) sin coste — reconocido en §15.4, pendiente de `IBuildService`.

**Enterrado (encendido, pero sin camino que alguien usaría dos veces):** nada nuevo con la gravedad del tablón viejo. Los gestos sociales viven a un clic por vecino en la ficha y es diseño declarado (§13.3). El armario (4) y el árbol (2) son los únicos sin ninguna puerta.

---

## DISTANCIA A HARVEST MOON

1. **Ciclo diario que se siente — CASI.** Día de 24 min acelerable, dormir adelanta hora a hora (`PlayerInteractor.cs:524-536`), puesta al día offline amable (`GameBootstrap.cs:514-533`), semana con bonos y sueldos por día (`WeeklyRhythm.cs:42-50`). Falta que el día tenga rituales de apertura visibles: el ritual previsto (hablar con el Árbol) es justo el sistema apagado.
2. **Cultivos con etapas visibles — DÉBIL.** Estados Wild/Tilled/Planted/Ready con tierra que se oscurece al regar (`FarmView.cs:92-99`) y fruto al madurar (`:125-130`), pero **un solo brote genérico escalado para los 12 cultivos** (`FarmView.cs:110-118`: mismo cilindro, mismo verde, mismo color de fruto `0xE87A64` para todos). En Story of Seasons cada cultivo se reconoce a tres metros. El arte es código: `MeshShapes` ya tiene las piezas para una malla y un color por especie.
3. **Vecinos con rutina y memoria — FUERTE.** Agenda por hora según necesidades+personalidad (`IslanderBrain.cs:58-95`), encuentros casuales que forjan amistades y riñas (`:174-194`), enfriamiento y reevaluación diaria (`SocialService.cs:99-140`), flechazos→parejas→bodas→bebés solos, y la Crónica que lo cuenta por días. Falta la agenda *visible* del habitante (§2.2 promete «ve la agenda del día»; no existe esa vista) y que te mencionen lo de ayer en conversación.
4. **Regalos que importan — MEDIO.** Gustos estables y aprendibles por semilla (`WardrobeService.Liking`, `GiftService.cs:143`), caras según opinión, tope diario. Se cae en la ropa: comprada no puede ser (tiendas), regalada no se ve (sin render).
5. **Festivales que importan — BIEN.** Calendario con sorteo + fiestas pagables (`VillageEvents`), flechazos nacidos en fiestas (`EventSparks.cs:74-114`), tocar bien anima a la aldea (`MinigameService.cs:237-252`). Falta que el mundo cambie de cara durante el evento: `VillageEventStarted` llega a la música y a avisos, no a decorado ni concentración visual de vecinos en el sitio del evento.
6. **Herramientas con progresión — HECHO.** Nivel 2 crafteable que se come la vieja, barrido lateral, arco de guadaña, regadera grande por dos caminos (`PlayerInteractor.cs:699-740`; `HerramientasTests` 203 líneas).
7. **Interfaz que no te haga buscar — LA MEJOR DEL PROYECTO, CON UNA EXCEPCIÓN GRAVE.** Carteles que nombran la tecla que falta (`PlayerInteractor.cs:573-594`), tablón que dice de lejos cuánto hay (`:469-474`), botones apagados que explican qué falta (`SocialSection.cs:176-184`). La excepción: la tienda, el mostrador más usado del género, enseña «hoy no queda nada» todos los días de todas las partidas.
8. **Economía de granja con decisión — MEDIDA PERO COLGANDO.** `EconomiaHuertoTests` fija la banda 21,5–29 nimbos/día por cultivo y garantiza una semilla por familia en tienda. La mitad de ese pilar (reponer semillas) depende de la costura rota de las tiendas.

---

## LOS TRES MONOLITOS MÁS PELIGROSOS

1. **`Art/Player/PlayerInteractor.cs` (980 líneas).** Inyecta 13 servicios, resuelve 13 `TargetKind` con prioridades mutuas (`FindTarget`, `:111-196`), y concentra la única tecla de acción. Cambiar el orden de objetivos o un radio rompe a la vez granja, recolección, regalos, minijuegos, puertas, comer y dormir. Es además el único sitio que toca el mundo (bien puesto), pero cualquier sistema nuevo con «sitio en el mundo» pasa por aquí.
2. **`Game/Bootstrap/GameBootstrap.cs` (587 líneas).** El orden de construcción manda y ya cobró dos veces (logros del primer minuto que no pagaban, `:290-296`; economía antes del primer fotograma, `:339-341`). Añadir un servicio en el sitio equivocado no falla al compilar: falla en partida, tarde.
3. **`UI/UiRoot.cs` (702 líneas).** Monta 18 paneles, gestiona los modos excluyentes (construir/amueblar/interior) y mantiene **dos costuras por reflexión con nombres de objetos**: `GameObject.Find("Protagonista")` (`UiRoot.cs:573` ↔ `PlayerBody.cs:64`) y `GameObject.Find("Mundo")` (`:551` ↔ `Scenes/Isla.unity:506`). Renombrar esos objetos rompe el cartel de la hotbar y la selección del modo construir sin un solo error de compilación — la variante silenciosa de la enfermedad nº2, dentro del propio repo.

*(Mención: `Social/SocialService.cs`, 650 líneas, es denso pero es el mejor probado del proyecto — 5 ficheros de tests + 2 PlayMode.)*

---

## NO MEDIDO

- **No vi el juego.** Nada de lo anterior dice si el día se siente bien o si la cámara estorba; para eso están `CapturaEstilo`/`CapturaAldea` y quien juegue.
- No ejecuté `CapturaMusica` (los WAV de los tres humos) ni audité sus formas de onda.
- No medí rendimiento por fotograma: `WorldView` reconstruye mallas por evento y `UiRoot` refresca listas a 0,4 s, pero no hay números de frame time.
- No verifiqué el total exacto de peinados (solo `BaseCount = 32`, `HairStyles.cs:111`; `HairStyleTests` pasa) ni el contenido de `personalidades.json`.
- No audité los 44 logros uno a uno contra condiciones alcanzables: podría haber un segundo «listón donde no llega nadie» escondido ahí.
- No leí a fondo `FlowController` (104 líneas) ni la interacción TitleScreen↔Bootstrap más allá de lo que BootTests cubre.
- Los 9 saltadas de PlayMode no los abrí uno a uno; el total cuadra con la línea base declarada (92 pasadas).
