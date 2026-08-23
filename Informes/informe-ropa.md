# Informe — nimbo-ropa (segunda vuelta)

Corrección del veredicto RECHAZADO de nimbo-verificador (`Informes/veredicto-ropa.md`).
Solo se tocaron los tres defectos listados; lo aprobado no se rehizo.

## Defecto 1 — el sombrero tiñe toda la ropa del cuerpo: CORREGIDO

`OutfitLook.From` caso `"hat"` devolvía `HasGarment=true`, e `IslanderView` pinta la
malla «ropa» con `look.Garment` cuando `HasGarment` (`IslanderView.cs:76-79` y
`:125-126`): con una gorra azul, torso y cadera se volvían azules.

Cambio: el caso `"hat"` ahora devuelve `HasGarment=false` y su color solo en
`PieceColor` (`OutfitLook.cs:69-77`). La pieza Extra sigue creándose igual porque el
switch de `ChibiMeshBuilder.BuildBody` depende de `look.Piece`, no de `HasGarment`
(`ChibiMeshBuilder.cs:254`), y el guardia de pantalón ya blindaba el cuerpo con
`esSombrero` (`ChibiMeshBuilder.cs:135-137`). Quien solo lleva gorra vuelve a llevar
su ropa de siempre, que es lo que decía el comentario de `ChibiMeshBuilder.cs:132`.

Fijado por dos pruebas nuevas:
- Editor: `RopaLookTests.UnSombreroNoTineLaRopaDelCuerpo` (`RopaLookTests.cs:83`) —
  gorra y gorro con `HasGarment=false`, pieza intacta y color viajando en `PieceColor`.
- Juego: `RopaEnLaIslaTests.UnSombreroCuelgaUnaPiezaNuevaDelCuerpo`
  (`RopaEnLaIslaTests.cs:143`) ampliada: deja al vecino sin prenda, equipa la gorra
  `cloth_gorra_nimbo_clasica` y comprueba que el `_BaseColor` de la malla «ropa» del
  muñeco real **no** es el azul #3366AA de la gorra (`:173-175`). Con el código
  anterior esta aserción sale en rojo; no puede pasar por casualidad porque el color
  base del vecino tiene saturación ∈ [0.35, 0.62] y valor ∈ [0.78, 0.96]
  (`IslanderView.cs:98-102`) y la gorra es s=0.70, v=0.667.

## Defecto 2 — OnDayPassed sin certificado: CORREGIDO

Nuevo fichero `Assets/_Project/Tests/RopaDiariaTests.cs` (editor, 3 pruebas) que
publica `DayPassed` de verdad contra el manejador real (`WardrobeService.cs:66-84`),
con vecinos falsos pero sin doblar ninguna lógica:

- `AlPasarLosDiasQuienTieneVariasPrendasSeCambia` (`:66`): 60 días publicados, un
  vecino con 3 prendas cambia al menos una vez y nunca se pone algo fuera de su
  armario. El sorteo es determinista (semilla `{id}|cambio-ropa|{día}`), así que este
  resultado es estable, no probabilístico.
- `QuienSoloTieneUnaPrendaNoSeCambiaNunca` (`:87`): 60 días, armario de 1 prenda,
  cero cambios — el guardia de `WardrobeService.cs:72`.
- `TrasElDisposeElDiaPasaYElArmarioYaNoDecide` (`:100`): mismo vecino y mismos días
  que la primera prueba (que demuestra que hay cambios con la suscripción viva),
  tras `Dispose()` cero cambios — prueba también el desenganche del calendario.

## Defecto 3 — la referencia Nimbo.Island del asmdef: DECLARADA CON MOTIVO

El veredicto daba a elegir entre quitarla o declararla. **Se queda, declarada**: no
fue sobrante mía por azar — la necesita otro agente. `ArbolEnLaIslaTests.cs:7`
(trabajo en paralelo de nimbo-arbol, ya en el árbol) hace `using Nimbo.Island;`, y
con `overrideReferences: true` quitar la referencia de
`Nimbo.PlayTests.asmdef:10` rompería la compilación de toda la suite de juego, no
solo su prueba. Mi informe anterior la declaró mal («+1 referencia» cuando eran dos):
error de redacción mío, el diff siempre llevó las dos.

## Pruebas medidas (envoltorio `Tools/agentes/unity.sh`, XML en `Informes/pruebas/`)

- EditMode `nimbo-ropa-EditMode.xml`: **569 pruebas · 567 pasadas · 0 fallos · 2
  saltadas** (línea base 514 ✓; eran 565 antes de esta vuelta, +4 pruebas nuevas).
- PlayMode `nimbo-ropa-PlayMode.xml`: **133 · 122 pasadas · 1 fallo · 10 saltadas**.
  El único fallo es `DelanteDelArbolElCartelOfreceHablarle`, ajeno (nimbo-arbol) y
  excusado por el orquestador en el propio veredicto.
- Las 16 pruebas de ropa, todas en verde: 9 de `RopaLookTests`, 3 de
  `RopaDiariaTests`, 4 de `RopaEnLaIslaTests` (verificadas una a una en los XML).

## Lo que ya estaba aprobado y no se tocó

Costura por contrato en `IEconomyService` (Slot/Style/Palette), la prueba de juego
que certifica malla y no dato, la semilla con día, `SwapMesh`/`OnDestroy`, los
fallbacks (FNV, `ItemsOfCategory`, accesorios en «sin prenda») y los comentarios —
todo tal cual lo aprobó el veredicto. Esta vuelta añade 116 líneas entre cuatro
ficheros y no modifica ninguna línea de las ya revisadas salvo el caso `"hat"` de
`OutfitLook.cs`.

## Fuera de mi carpeta (acumulado, sin cambios esta vuelta)

- `Core/Services/Contracts/IEconomyService.cs` (+14: Slot/Style/Palette) — contrato
  compartido, vía correcta según el veredicto.
- `Tests/InventoryTests.cs` (+3: FakeItemDef implementa las 3 propiedades).
- `Tests/PlayMode/Nimbo.PlayTests.asmdef` (+2 referencias: `Nimbo.Simulation`, mía;
  `Nimbo.Island`, necesaria para `ArbolEnLaIslaTests.cs:7` de nimbo-arbol — ver
  defecto 3).

## PENDIENTE para el orquestador (igual que en la primera vuelta)

`WardrobeService.Dispose()` (`WardrobeService.cs:50`) nadie lo llama:
`GameBootstrap.OnDestroy` (:567-583) debería invocarlo al soltar el servicio. Es
fuera de mi carpeta; sin ello, cada descarga de la escena deja una suscripción viva
al calendario.
