# Informe — Acabados de vivienda encendidos

Agente: nimbo-acabados · Fecha: 2026-08-23

## Lo que había (medido)

- `SetWallpaper` en el contrato (`IHousingService.cs:55`) y en la implementación
  (`HousingService.cs:149`) con **cero llamadores** en todo `Assets/_Project`. `SetFloor`
  (`IHousingService.cs:54`, `HousingService.cs:138`), igual.
- `InteriorView` pintaba suelo y paredes con colores escritos a mano
  (`InteriorView.cs:192,200` del árbol anterior) y nunca leía el id guardado.
- Los ids por defecto no existían en el catálogo: `wall_liso_crema`
  (`RoomLayout.cs:68-69`) y `floor_madera_clara` (`RoomLayout.cs:96`,
  `HomeUpgradeService.cs:274`, `InteriorView.cs:127`) frente a los 20+20 acabados reales
  de `Resources/Config/catalogo_acabados.json`.
- `FurnishPanel` excluía los acabados a propósito (`FurnishPanel.cs:104` del árbol
  anterior, vía `IsPlaceable` → `TryGetFurniture`).

## Lo que hay ahora

### 1. Los ids por defecto existen (`Nimbo.Data`, sancionado por el encargo)

- `RoomLayout.cs:70` — `DefaultWallpaper = "wall_nube_blanca"` (5⭐, nivel 1,
  «la pared de toda la vida», `#F5F0E8`: casi el mismo color que el fijo antiguo).
- `RoomLayout.cs:77` — `DefaultFloor = "floor_suelo_de_vinilo_imitacion_madera"`
  (el más barato del catálogo, 6⭐, nivel 1, `#C8A882`: el color madera clara que
  siempre tuvo la cabaña, para que una partida vieja no cambie de aspecto).
- Usados en los inicializadores de campo (`RoomLayout.cs:86-87`), en `Starter()`
  (`RoomLayout.cs:114`), en `HomeUpgradeService.RemapFloor` (`HomeUpgradeService.cs:277`)
  y en el relleno de estreno de la cabaña (`InteriorView.cs:147`). Un solo punto de
  definición; nadie repite el literal.

### 2. `InteriorView` lee lo guardado y lo pinta (`Nimbo.Art`)

- `BuildFloor` pinta la losa con el color del **acabado dominante** de `FloorTiles`
  (`InteriorView.cs:212`, `FloorColour` en `:230`). Dominante y no primero: al ampliar,
  las casillas nuevas traen el acabado de serie y «el primero» haría pasar un suelo
  recién pintado por uno de serie. Desempate estable por segunda pasada sobre la lista
  (`DominantFinish`, `:238`), porque el orden de un `Dictionary` no está garantizado.
- `BuildWalls` pinta norte/sur con `WallpaperNorth` y oeste/este con `WallpaperWest`
  (`InteriorView.cs:270-294`). Sur y este heredan de su pareja: no los mira la cámara,
  pero pintarlos distinto se colaría por los cantos.
- Ids que ya no existen (partidas viejas guardadas con `wall_liso_crema`): caen al color
  del acabado por defecto (`WallColour :265`, `FloorColour :234`). Sin excepción, sin
  pared blanca de error.
- **Repintado en vivo**: `InteriorView` escucha `RoomEdited` (`:67`, manejador `:87`) y
  rehace la habitación si es la que se está mirando. Va por evento porque `Nimbo.UI` no
  ve `Nimbo.Art`.

### 3. Se eligen desde `FurnishPanel` (`Nimbo.UI`)

- `Rebuild` separa los acabados del inventario por categoría
  (`ItemCategory.Wallpaper/Flooring`, `FurnishPanel.cs:118`) y les da su propia sección
  con rótulo «Acabados — pínchalos para aplicarlos» (`:129-137`). Los muebles siguen el
  camino de siempre.
- Pinchar una fila **aplica y gasta** en el momento (`FinishRow :196`): un bote de
  pintura no se coloca casilla a casilla. La pastilla dice superficie y unidades
  («pared ×2»).
- `TryApplyFinish(catalogId)` (`:238`) es el verbo, público para las pruebas:
  - papel → `SetWallpaper` norte + oeste; suelo → `SetFloor` en todas las casillas;
  - gasta 1 unidad **solo después** de comprobar que el servicio aplicó de verdad
    (`:257`, `:271`) — el servicio rechaza en silencio ids desconocidos;
  - repintar lo ya puesto entero devuelve `false` sin gastar (`:250`, `:265`);
  - publica `RoomEdited("", 0)` (`:275`), que es lo que repinta la vista y lo que
    alimenta los logros de decoración (`AchievementService.cs:248-253`, que hoy ignora
    el identificador).

## Pruebas

Nuevas, 9 en total:

- **Editor** — `Tests/AcabadosDeViviendaTests.cs` (7):
  `LosAcabadosPorDefectoExistenEnElCatalogo`, `LaCasaDeEstrenoEstrenaAcabadosQueExisten`,
  `AmpliarLaCasaRellenaLoNuevoConUnAcabadoQueExiste`,
  `ElPanelAplicaUnPapelGuardadoYGastaLaUnidad`, `ElPanelAplicaUnSueloEnteroYGastaLaUnidad`,
  `AplicarSinUnidadesOYaPuestoNoGastaNiCambiaNada`, `LoQueNoEsUnAcabadoNoSeAplicaComoTal`.
  La primera es la que habría cazado la enfermedad: carga el catálogo real de Resources
  y exige que los dos ids por defecto existan con su superficie correcta.
- **Juego** — `Tests/PlayMode/AcabadosEnLaIslaTests.cs` (2):
  `CambiarElPapelDeLaParedSeVeAlMomentoYAlVolverAEntrar` y `CambiarElSueloSeVeYSeQueda`.
  Cargan la escena `Isla` de verdad, entran en casa, comprueban que de serie se pinta el
  color del catálogo (`Nimbo_F5F0E8` / `Nimbo_C8A882`), aplican otro acabado vía
  `FurnishPanel.TryApplyFinish` y miden que el material de `pared_n`/`suelo` cambia
  (`Nimbo_B8D8F0` / `Nimbo_FFFFFF`), que se gasta la unidad, y que al salir y volver a
  entrar sigue pintado — el id se lee del guardado, no de un color fijo.

Recuentos con `Tools/agentes/unity.sh` (XML en `Informes/pruebas/`):

| Corrida | Filtro | Resultado |
|---|---|---|
| EditMode | `AcabadosDeViviendaTests` | 7 pruebas · 7 pasadas · 0 fallos |
| EditMode | `HomeUpgradeTests` | 16 pruebas · 16 pasadas · 0 fallos |
| EditMode | `HousingPlacementTests` | 20 pruebas · 20 pasadas · 0 fallos |
| PlayMode | `AcabadosEnLaIslaTests` | 2 pruebas · 2 pasadas · 0 fallos |
| PlayMode | `BootTests` | 31 pruebas · 31 pasadas · 0 fallos |

`HomeUpgradeTests` y `HousingPlacementTests` son vecinos que consumen lo que toqué:
16+20 en verde confirma que la línea base de housing no bajó. `BootTests` es el mayor
consumidor de `InteriorView` (entra, amuebla, recoge): 31 en verde.

## Ajuste en un test ajeno (declarado)

`HomeUpgradeTests.Upgrade_RemapsFloor_OriginalTilesStayAtSameCoordinates` rellenaba con
`floor_madera_clara` y asumía que las casillas nuevas salían con ese mismo id — lo
cumplía porque el default era precisamente ese id fantasma. Con el default real, la
aserción de las columnas nuevas compara ahora contra `RoomLayout.DefaultFloor`
(`HomeUpgradeTests.cs:190-196`): misma intención («lo nuevo trae el de serie, no hereda
lo pintado»), distinto valor esperado. El resto del test queda intacto.

## Costuras y límites, para el orquestador

1. **Tercer lector del JSON de acabados.** `IItemDefinition` expone la categoría de un
   acabado pero no su color (`IEconomyService.cs:84-90` no tiene `BaseColor`), y
   `Nimbo.Art` no ve `Nimbo.Housing`. `InteriorView.FinishColours` (clase privada al
   final de `InteriorView.cs`) lee `Resources/Config/catalogo_acabados` por su cuenta.
   Es el tercer parser del mismo fichero (economía e vivienda tienen los suyos). Si el
   contrato de housing gana un `TryGetFinish(id)`, esto se sustituye y sobra.
2. **Hoy no se pueden comprar.** Las únicas tiendas con `Wallpaper`/`Flooring` son
   `AntiguedadesNimbo` y `MercadoFlotante` (`ShopDefinition.cs:64-72`), y según el
   comentario de ese mismo fichero (`:61-63`) **no tienen zona todavía**: en la primera
   isla no hay puerta que lleve a ellas. Añadir las dos categorías a `MueblesNimbo`
   (`ShopDefinition.cs:53-55`) sería una línea en `Scripts/Economy/Shops/`, que no es mi
   carpeta. Mientras tanto, el panel solo enseña acabados que estén en la mochila.
3. **Solo redecoras tu cabaña.** `TryApplyFinish` aplica sobre
   `PlayerService.State.Home`. Desde la UI no se puede saber de qué casa es el interior
   actual (`CurrentRoom` vive en `Nimbo.Art`, inaccesible desde `Nimbo.UI`); aplicar en
   casa ajena desde tu propio menú habría sido una sorpresa cara. Queda apuntado por si
   algún día se quiere redecorar la de un vecino.
4. **`RoomEdited` sigue sin publicarse al colocar muebles.** `FurnishModeView` coloca y
   recoge (`FurnishModeView.cs:236-272`) sin publicar `RoomEdited`, así que los logros
   `logro_primer_toque`/`logro_decorador`/`logro_arquitecto` siguen dormidos para los
   muebles; con los acabados ya se encienden. Es una línea en un fichero que no es mío.
5. **Patrones sin dibujar.** El catálogo trae `pattern` (rayas, cuadros, flores…); se
   pinta solo el `baseColor` liso. Dibujar patrones necesita texturas procedurales por
   patrón — trabajo aparte, no enchufe.

## Errores ajenos vistos al compilar (transitorios)

Dos corridas mías rebotaron por errores en ficheros de otros agentes, que fueron
arreglados por sus autores mientras tanto: `IslandCamera.cs(164)` CS1519 y
`GateNotice.cs(100)` CS0191 en la primera; `CamaraEnLaIslaTests.cs` (Debug ambiguo),
`RopaEnLaIslaTests.cs` (usings) y `ArbolNimboTests.cs` (doble desactualizado frente a
`IIslanderRegistry.InZone/Remove`) en la segunda. No toqué ninguno. Las corridas
finales de arriba compilaron el proyecto entero, así que al momento de escribirlas el
árbol estaba sano.

## Árbol sucio, tal cual se pide

Sin commits. Tocado: `Scripts/Data/Housing/RoomLayout.cs`,
`Scripts/Housing/HomeUpgradeService.cs`, `Scripts/Art/World/InteriorView.cs`,
`Scripts/UI/Player/FurnishPanel.cs`, `Tests/HomeUpgradeTests.cs` (2 líneas declaradas
arriba), y los dos ficheros de pruebas nuevos con sus `.meta` generados por Unity.
