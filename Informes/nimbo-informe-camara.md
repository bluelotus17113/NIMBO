# Isla Nimbo — medición del paso a tercera persona

*Pasada de medida, sin una línea escrita en `Assets/`. Todas las rutas son relativas a
`Assets/_Project/` y todos los números están leídos del código o calculados de él.*

## LA CÁMARA DE HOY

Dos ficheros, en `Scripts/Art/Camera/`:

**`CameraRig.cs`** — la matemática, sin MonoBehaviour (probable por eso).

| Qué | Valor | Dónde |
|---|---|---|
| Topes de pitch | 12° – 78° (positivo = mira hacia abajo) | `CameraRig.cs:23-24` |
| Topes de distancia | 8 – 220 m | `CameraRig.cs:25-26` |
| Suelo duro | pivote elevado a `GroundLevel + 3 m`; la casa mide 3 m y por debajo se entra en tejados | `CameraRig.cs:36`, comentario 31-35 |
| Pose inicial | pivote (0,0,0), **150 m**, pitch **45°**, yaw 0 | `CameraRig.cs:77-84` |
| Amortiguación | exponencial, `Sharpness = 8` (`t = 1−e^(−8·dt)`), yaw por el lado corto | `CameraRig.cs:63`, `146-160` |
| Recinto del pivote | disco de radio isla·1,1 + pasillo de ±14 m hasta la segunda isla | `CameraRig.cs:75`, `246-257` |
| Posición | esférica: `pivote + Euler(pitch,yaw)·back·distancia`, luego suelo duro | `CameraRig.cs:174-194` |

**`IslandCamera.cs`** — el MonoBehaviour que la mueve.

| Qué | Valor | Dónde |
|---|---|---|
| Seguimiento del protagonista | **15 m, pitch 48°**, pivote a +1,1 m sobre el muñeco | `IslandCamera.cs:35-37`, `67-73` |
| Mirar libre | solo eje horizontal del ratón, 2,2 °/unidad; el pitch va fijo a propósito («un picado de cuarenta y ocho grados…») | `IslandCamera.cs:41`, `380-388`, nota 371-377 |
| Foco de un vecino | 14 m, pitch 22°, pivote en la cabeza (+1,1) | `IslandCamera.cs:196-203` |
| Interior | pitch **62°**, distancia `max(ancho,largo)·1,6 + 4`, pivote al centro del cuarto +1,2; ve el cuarto entero por encima de las paredes (3,2 m) | `IslandCamera.cs:334-347`; paredes en `InteriorView.cs:199` |
| Antiobstáculos | raycast pivote→cámara contra **todo** (`~0`), acerca a `golpe − 0,6 m`, mínimo 1,5 m | `IslandCamera.cs:516`, `531-545` |
| Ratón | capturado mientras se mira libre; soltado por evento cuando cualquier panel lo pide | `IslandCamera.cs:222-257` |
| Puertas | al cruzar, `SnapToTarget()` — entrar/salir no pica 500 m | `IslandCamera.cs:286-315` |

Escena `Scenes/Isla.unity`: FOV **45°** (línea 311), near clip **0,3** (línea 309), fondo cielo liso que el interior cambia a cálido oscuro (`IslandCamera.cs:88`).

**Lectura:** ya NO es un mirador cenital puro. Es una cámara de seguimiento *alta*: detrás
del jugador a 15 m y 48°. La conversión a tercera persona es sobre todo **bajar el pitch
(48° → ~15-20°) y acortar la distancia (15 → ~5-6 m)**, más decidir quién manda el pitch.

## PLAN DE CONVERSIÓN, POR FICHERO

| Fichero:línea | Qué supone hoy | Qué tiene que pasar | Coste |
|---|---|---|---|
| `Art/Camera/CameraRig.cs:23-26` | pitch mínimo 12°, distancia mínima 8 m | bajar topes (~5°, ~3,5 m) para que exista la pose de tercera persona | Bajo. Rompe 5 pruebas editor fijas (ver abajo) |
| `Art/Camera/CameraRig.cs:36` | suelo duro a +3 m «por los tejados» | a altura de ojos el suelo duro debe bajar (~0,4 m); el antiobstáculos pasa a hacer el trabajo del tejado | Medio: hay que dar colisionador a tejados o atravesarlos |
| `Art/Camera/IslandCamera.cs:35-37` | follow 15 m / 48° / +1,1 | nueva pose: ~5,5 m / ~18° / +1,2 | Bajo (una terna) |
| `Art/Camera/IslandCamera.cs:380-388` | mirar libre solo horizontal; pitch clavado | decidir: ratón Y libre con topes, o auto-align tras X s sin input | Medio (diseño, no código) |
| `Art/Camera/IslandCamera.cs:531-545` | antiobstáculos solo contra lo que tiene colisionador | sigue valiendo; pero hoy atraviesa árboles/adornos (ver colisiones) | Alto si se quiere limpio: capa «ObstáculoCamara» |
| `Art/Camera/IslandCamera.cs:334-347` | interior: cuarto entero desde 62° | **no tocar** (ver recomendación) | Nulo |
| `Art/World/BuildModeView.cs:68-76` | impone su propia cenital a 175 m y apaga `IslandCamera` | **no tocar**: ya es un modo con cámara propia, inmune a la conversión | Nulo |
| `Art/World/FurnishModeView.cs:96-103` | no toca la cámara; usa la del interior | **no tocar**; el comentario ya documenta que la cenital pura se probó y era peor | Nulo |
| `Art/Player/PlayerBody.cs:342-347` | movimiento relativo a cámara (proyecta `forward` al plano) | **ya está hecho** — nada que cambiar | Nulo |
| `Art/Player/PlayerInteractor.cs:30-33,798-812` | objetivo por proximidad+cono alrededor de `Facing` del cuerpo, no de la cámara | **no depende del ángulo de cámara** — nada que cambiar | Nulo |
| `Art/World/IslandLighting.cs:57-58` | niebla lineal 60→280 m, pensada para ver desde 150 m | a 5 m nunca entra; bajar inicio (~25-40 m) si se quiere bruma a ras de suelo | Bajo |
| `Art/World/Meadow.cs:316-321` | flores inclinadas 4–22° respecto al cielo «porque la cámara mira desde arriba» | subir inclinación/densidad o desaparecen de canto a altura de ojos | Medio (retune visual) |
| `Art/World/IslandMeshBuilder.cs:18-70` | prado = disco de UNA cara, sin falda hasta la roca | añadir banda vertical borde↔roca (ver «feo de cerca») | Medio |
| `Scenes/Isla.unity:309` | near clip 0,3 | vale (mínimo antiobstáculos 1,5 m > 0,3) | Nulo |

No suponen ángulo y no cambian: sondas de suelo hacia abajo (`PlayerBody.cs:245-254`,
`GatheringView.cs:166-173`, `WorldView.cs:339-346`, `DebugPanel.cs:131-144`) — buscan
suelo, no pantalla; el mapa (`UI/Player/MapPanel.cs:71-87`) es esquema UI Toolkit desde
datos de `Archipelago`; el cartel de interacción es texto de la barra inferior
(`UI/Player/HotbarView.cs:106-113` vía `UiRoot.cs:569-588`) — **no hay ni un texto anclado
al mundo ni un billboard en todo el proyecto** (búsqueda de `TextMesh`/`LookAt(camera)`:
cero resultados).

## LA TRAMPA DEL MOVIMIENTO

**Ya está desactivada.** La traducción mando→mundo está en `PlayerBody.ReadMove()`
(`Art/Player/PlayerBody.cs:328-348`): toma `Horizontal`/`Vertical`, proyecta el
`forward` de `Camera.main` sobre el plano (`:342`), construye `right` con el producto
vectorial (`:346`) y compone. Con la cámara a 15 m y 48° funciona; con la cámara a 5 m y
18° sigue funcionando igual porque la proyección al plano no depende del pitch. El cuerpo
gira hacia donde anda a 720°/s (`:31`, `211-216`) — estilo Rune Factory de órbita libre,
coherente con la cámara actual.

Lo único que queda atado a la cámara vieja es el **pitch de seguimiento fijo**
(`IslandCamera.cs:371-377`): hoy el ratón no puede bajar la vista aunque quiera. En
tercera persona esa decisión hay que revertirla o el jugador no podrá mirar arriba/abajo,
y con ella aparece la pregunta de diseño que el código hoy resuelve por él.

## LO QUE SE VA A VER FEO DE CERCA

Recorridos los constructores de `Art/World/` y `Art/Chibi/`. Los shaders:
`Nimbo/Foliage` hace `Cull Off` (doble cara, `Shaders/NimboFoliage.shader:133`) y
`Nimbo/Toon` hace `Cull Back` (`Shaders/NimboToon.shader:141`).

1. **El borde de la isla tiene una rendija sin geometría.** El prado
   (`IslandMeshBuilder.BuildSurface`, `:18-70`) curva su borde hacia abajo
   (`−t⁶·3,2` en `:44`): el anillo exterior cae entre **−4,68 y −1,72 m** (calculado de la
   fórmula, 48 sectores). La roca de debajo (`BuildUnderside`, `:76-149`) empieza en su
   anillo 0 a **y ∈ [−1,86, +1,86]** en la aldea (ruido ±`depth·0,06·0,5`, `:108`). Entre
   ambos anillos, al mismo radio, hay sectores con **hasta ~2,8 m de banda abierta** donde
   se ve a través de la isla (las dos mallas son de una cara). Desde 150 m de cenital
   jamás se ve; pescando en el borde (los sitios de pesca están a ≥78 % del radio,
   `PlayerInteractor.cs:38,412`) con cámara a la altura de los ojos, sí.
2. **Las flores están tumbadas mirando al cielo.** `Meadow.Bloom` las inclina solo
   4–22° respecto a la normal del suelo y lo dice: «Inclinarla más la hacía desaparecer
   de canto desde la cámara del juego, que mira desde arriba» (`Meadow.cs:316-318`). A
   altura de ojos quedan de canto: hilos invisibles. La hierba alta no sufre (briznas
   verticales, doble cara).
3. **La cara del chibi es un parche de una sola cara al frente**
   (`ChibiMeshBuilder.BuildFaceQuad`, `:352-395`), pegado a la esfera cerrada de la
   cabeza. De frente y perfil se ve; de espaldas se ve la nuca (correcto), pero el casquete
   del pelo (`MeshShapes.SphericalCap`, `:74-114`) es un cacillo abierto: desde abajo el
   borde se ve en canto como papel. Con la cámara a 5 m detrás del personaje esto se ve
   constantemente en los vecinos de delante.
4. **Los interiores no tienen techo** (`InteriorView` monta suelo + 4 paredes, `:184-230`)
   y el fondo lo cierra un color plano (`IslandCamera.cs:82-90`). Hoy la cámara mira
   desde encima de las paredes y no se nota. Si el interior pasara a tercera persona, se
   vería el cielo dentro de casa. (Otro motivo para dejar los interiores como están.)
5. Lo demás cierra: cajas de 6 caras (`MeshShapes.Box:246-283`), cilindros con tapas
   arriba y abajo (`:212-213`), esferas, cápsulas, rocas con normales por cara
   (`RockMeshBuilder.cs:59-82`), copas de lóbulos completos (`FoliageMeshBuilder.cs:75-130`).
   Edificios, puente, cabaña, huerto, adornos: sólidos cerrados.

## COLISIONES QUE FALTAN

El antiobstáculos lanza su rayo contra capas todas (`~0`,
`IslandCamera.cs:539-540`): choca con lo que tenga colisionador y **atraviesa el resto**.
Inventario:

Con colisionador hoy (lo pone `WorldView.AddMesh` con `solid:true`, `WorldView.cs:613-640`):
prado (MeshCollider), muros de zonas (Box), tablero y barandas del puente (Box),
tronco del Árbol Nimbo (MeshCollider), cabaña (Box, `PlayerHomeView.cs:80-81`), base del
fogón (Box, `:57-58`), suelo y cuatro paredes interiores (Box, `InteriorView.cs:192-216`).

**Sin colisionador — la cámara los atraviesa a altura de ojos:**

- **Los 120 nodos de recolección**: árboles de 4,6 m, rocas, matas. Es decisión escrita:
  «Sin colisionadores, a propósito» (`GatheringView.cs:28-33`). Es el caso más frecuente:
  talar implica ponerse al lado del árbol y la cámara justo detrás.
- **Todos los tejados** (zonas `WorldView.cs:418-420`, cabaña `PlayerHomeView.cs:85-89`):
  por eso existe el suelo duro de 3 m (`CameraRig.cs:31-36`). Al bajar a ojos, ese
  argumento cae y hacen falta o colisionador o aceptar meterse en tejas.
- **Adornos**: farolas de 3,35 m, estatuas de 2,72 m, bancos, vallas
  (`DecorMeshBuilder.cs:91-177`, colocados sin `solid` en `WorldView.cs:461-463`).
- Valla del huerto («decorativa», `FarmView.cs:152-153`), hamaca/mesa/cajón
  (`PlayerHomeView.cs:159-207`), muebles interiores, postes del puente
  (`WorldView.cs:320-328`), cristales, chimeneas, nubes (`WorldView.cs:349-371`).

## PRUEBAS QUE SE CAEN

**Editor — `Tests/CameraRigTests.cs` (18 pruebas).** Fijan los topes actuales. Caen si se
bajan `MinPitch`/`MinDistance`:

- `Target_Pitch_ClampedBetween12And78` (:141) y `Orbit_ClampsPitch` (:162) — esperan 12/78 literales.
- `LaCamaraNoSeMeteDentroDelSuelo` (:391), `ElSueloSeMideDesdeElMundoYNoDesdeElPivote`
  (:407) y `SubirLaCamaraNoLeCambiaLoQueMira` (:453) — usan la combinación mínima actual
  8 m/12°; con topes nuevos siguen pasando sus afirmaciones (el suelo duro sigue ahí) pero
  hay que reescribir los números de escenario.
- `DentroDeUnaCasaElSueloBajaConLaHabitacion` (:428) usa 23 m/62° — sobrevive si los
  interiores no cambian.
- Las demás (suavizado, yaw corto, coherencia posición/rotación, recinto del pivote) no
  dependen de los topes.

**PlayMode — `Tests/PlayMode/BootTests.cs`:**

- **`LaCamaraEmpiezaEncuadrandoAlProtagonista` (:284-303) cae seguro**: exige
  `camera.y > body.y + 5`. A altura de ojos la cámara va a ~2-2,5 m sobre el muñeco.
  Hay que reescribirla (p. ej. distancia < 8 m y `dot > 0,7`).
- `ApareceAlLadoDeSuHuerto` (:365-374) exige `dot > 0,2` hacia el huerto al aparecer;
  depende del yaw inicial, no del pitch — probablemente sobrevive, revisar al tocar `FollowPose`.
- `LaCamaraSigueAlProtagonista` (:257-281, <30 m y dot>0,7), 
  `LaCamaraPuedeSeguirteHastaLaOtraIsla` (:466-487, <30 m) y la prueba de interior
  (:720-751, cámara bajo −400 y mirando al centro) **sobreviven** — sus topes son generosos
  a propósito («Un test que fija la distancia se rompe cada vez que se ajusta», :271-273).
- `SalirDeCasaAmueblandoCierraElModo` (:859-860) solo comprueba que `IslandCamera` queda
  encendida — sobrevive.

**Herramientas `[Explicit]`** (no corren en la suite, pero son la vara de medir del estilo):
`CapturaEstilo.cs:30-45` (4 encuadres fijos, uno ya es «a ras de prado»),
`CapturaAldea.cs:54-86` (delante de cada puerta), `CapturaInterior.cs:50-68`,
`CapturaRecursos.cs:34-35`. Todas apagan `IslandCamera` y colocan la cámara a mano, así
que **no se rompen**; pero si el juego pasa a tercera persona conviene añadir los
encuadres nuevos (detrás del protagonista, diálogo con vecino) para poder comparar antes/ahora.

## RECOMENDACIÓN: ¿SIEMPRE O SOLO FUERA?

**Tercera persona solo fuera; interiores, construcción y amueblar como están.** Motivos
medidos:

1. **Los interiores están diseñados para verse desde arriba.** Las cuatro paredes tienen
   BoxCollider (`InteriorView.cs:207-216`): una cámara a 5 m dentro de un cuarto de 12×10
   pasa la mitad del tiempo pegada a una pared, y el antiobstáculos pelearía con ella cada
   fotograma. La pose actual (62°, cuarto entero, `IslandCamera.cs:334-347`) es la de
   Animal Crossing y el propio modo amueblar documenta que la alternativa ya se probó:
   «mirando un mueble justo desde arriba solo se le ve la tapa» pero la cenital pura
   «era peor» (`FurnishModeView.cs:96-103`). Además no hay techo (punto 4 de arriba).
2. **El modo construcción ya es un modo con cámara propia** e inmune: impone cenital a
   175 m, apaga `IslandCamera` y la reenciende al salir (`BuildModeView.cs:68-95`). Su
   rejilla y su selección por ratón cruzan el rayo con el plano del suelo
   (`:167-174`), que geométricamente vale desde cualquier ángulo — pero a ras de suelo no
   se ve la rejilla entera ni se señala una casilla detrás de una casa. No tocar.
3. **El coste de tener las dos ya está pagado.** `IslandCamera` ya conmuta entre tres
   poses según contexto (seguimiento / foco de vecino / interior) en `LateUpdate`
   (`:279-318`) y `BuildModeView` añade la cuarta desde fuera. Tercera persona fuera +
   cenital dentro es **un caso más en ese conmutador**, no una arquitectura nueva. La
   transición ya tiene `SnapToTarget()` al cruzar puertas (`:300,315`).

Coste de «tercera persona siempre»: rehacer interiores (quitar/ocultar pared norte,
capa de colisión aparte para la cámara, techo o fondo nuevo) y rehacer amueblar contra una
vista que su propio código dice que fue peor. Coste de «solo fuera»: casi cero además del
propio cambio de cámara.

## ORDEN SUGERIDO

Cada paso deja el juego arrancando y la suite en verde (tocando su prueba):

1. **Topes de `CameraRig`** (`CameraRig.cs:23-26`) + actualizar los números de las 5
   pruebas editor listadas. Nadie pide aún los valores nuevos: el juego no cambia ni un
   píxel y la matemática nueva queda probada.
2. **`FollowPose` a tercera persona** (`IslandCamera.cs:35-37`) + decidir el pitch del
   ratón (`:380-388`) + reescribir `LaCamaraEmpiezaEncuadrandoAlProtagonista`. Aquí se
   ve por primera vez; es el paso grande.
3. **Colisionadores de cámara** para nodos y adornos (capa propia + máscara en
   `Unobstructed`, o lista de esferas como `_obstacles` pero para la cámara). Sin esto el
   paso 2 se ve sucio en cuanto hay un árbol cerca.
4. **Falda del borde de la isla** (`IslandMeshBuilder`): banda entre el anillo exterior
   del prado y el anillo 0 de la roca. Cierra la rendija de ~2,8 m.
5. **Flores y hierba a ras de suelo** (`Meadow.cs:316-321`): inclinación y densidad.
6. **Niebla** (`IslandLighting.cs:57-58`): bajar el inicio si se quiere bruma a escala humana.
7. Encuadres nuevos en `CapturaEstilo` para poder comparar el estilo antes/ahora.

## NO MEDIDO

- **La línea base de pruebas de esta sesión.** Lancé EditMode **tres veces** con
  `flock /tmp/nimbo-unity.lock` (logs `/tmp/nimbo-oxalpha{,2,3}.log`). Las tres salieron
  «Exiting batchmode successfully» sin ejecutar la suite ni escribir XML; la segunda llegó
  a compilar las 906 piezas («Tundra build success») y murió justo antes de la fase de
  pruebas. En cada intento había instancias de Unity de otros agentes vivas en el proyecto
  al mismo tiempo (`nimbo-ficha-editmode.log` 00:37, `nimbo-tiendas.log` 00:40,
  `nimbo-diag.log` 00:40) — la colisión de dos procesos que advierten las reglas de la
  casa, no un fallo del proyecto: la compilación completa pasa. Tomo como referencia la
  línea base viva declarada: **514 editor + 92 juego, 0 rojas, 2 y 9 saltadas**.
- **Si se siente bien.** Distancia y pitch propuestos (~5,5 m / ~18°) son punto de partida
  por comparación con el follow actual (15 m / 48°), no una medida: eso lo decide el
  usuario mirándolo.
- **Rendimiento a ras de suelo**: a 5 m la hierba de 9000 matas llena el encuadre como no
  lo hace desde 150 m (frustum culling por sectores ayuda, `Meadow.cs:381-395`, pero el
  overdraw real no lo he medido).
- **Sombras**: las copas no reciben sombra a propósito (`GatheringView.cs:282-294`) y el
  prado no la proyecta (`Meadow.cs:391-395`); cómo lee eso el ojo a altura de ojos no se
  puede saber sin verlo.
