# Encargo: el huerto (C#)

Una parcela pequeña junto a la casa del protagonista. Labrar, sembrar, regar, recoger.

**Pequeño a propósito.** Lee `Docs/04_ALDEA.md` §2 antes de nada: este juego no
castiga por no entrar. Sin estaciones, sin ventanas de siembra, y **una planta sin
regar no se muere** — deja de crecer ese día y ya está. Volver tras una semana fuera
tiene que ser encontrarse el huerto parado, no muerto. Si tu implementación mata
plantas, está mal aunque sea más "realista".

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` y `Docs/04_ALDEA.md`.
- `Assets/_Project/Scripts/Core/Services/Contracts/IFarmingService.cs` — **contrato
  cerrado**. Lo implementas tal cual.
- `Assets/_Project/Scripts/Data/Farming/FarmTile.cs` — tu dato, ya escrito.
- `Assets/_Project/Scripts/Core/Services/Contracts/IInventoryService.cs` — de ahí
  sacas y metes cosas.
- `Assets/_Project/Scripts/Economy/Items/ItemCatalog.cs` — copia de ahí cómo se carga
  un catálogo desde `Resources`.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Resources/Config/catalogo_cultivos.json`
2. `Assets/_Project/Scripts/Farming/CropCatalog.cs`
3. `Assets/_Project/Scripts/Farming/FarmingService.cs`
4. `Assets/_Project/Tests/FarmingTests.cs`

Las pruebas en `Assets/_Project/Tests/`, no en `Scripts/Tests/`.

No toques nada más. Ni contratos, ni `FarmTile.cs`, ni `SaveGame.cs`, ni
`GameEvents.cs`, ni `.asmdef` (el de `Nimbo.Farming` ya está). Hay tres agentes más
en paralelo.

## Lo que ya existe

```csharp
// Nimbo.Data.Farming
enum TileState { Wild, Tilled, Planted, Ready }
class FarmTile  { int X, Y; TileState State; string SeedId; int GrowthDays; bool Watered; }
class FarmState { int Width = 8; int Height = 6; List<FarmTile> Tiles; }

// Nimbo.Core.Events
struct TileChanged   { int X; int Y; }
struct CropHarvested { string CropId; int Quantity; }
```

## Fichero 1 — `catalogo_cultivos.json`

**12 cultivos.** Forma `{ "version": 1, "items": [...] }`, con `seedId` (prefijo
`seed_`), `cropId` (prefijo `crop_`), `displayName`, `daysToGrow`, `yield`, `regrows`
y `description`.

De 2 a 8 días de crecimiento. Tres de ellos con `regrows: true` (al recoger vuelven a
una fase anterior y siguen dando; esos tardan más la primera vez).

Son cultivos de **islas flotantes**, no un huerto de pueblo: que se note en los
nombres y en las descripciones. En castellano y con gracia — es lo que se lee en la
tienda de semillas.

## Fichero 2 — `CropCatalog.cs`

En `Nimbo.Farming`. Igual que `ItemCatalog`: carga en el constructor, solo lectura, y
**un segundo constructor que recibe el JSON en crudo** para poder probarlo sin
`Resources`. No es opcional: es lo que hace que las pruebas se puedan escribir.

JSON roto: `Debug.LogError` y catálogo vacío. Nunca una excepción.

## Fichero 3 — `FarmingService.cs`

En `Nimbo.Farming`, implementa `IFarmingService`. Constructor: `CropCatalog`,
`FarmState` (del `SaveGame`) y `IInventoryService`.

Siembra el `FarmState` si viene vacío: `Width × Height` casillas en `Wild`.

Las reglas:

- `Till` sobre `Wild` la deja `Tilled`. Sobre una ya labrada, no pasa nada y devuelve
  `Ok` — repasar la azada por donde ya labraste no puede ser un error.
- `Plant` pide `Tilled` (si no, `NotTilled`), que la semilla exista (`UnknownSeed`) y
  que la lleve encima (`NoSeed`). **Gasta la semilla de la mochila.** Deja `Planted`.
- `Water` pide algo sembrado. Marca `Watered`.
- `AdvanceDay`: a cada casilla `Planted` **regada**, `GrowthDays++`; si llega a
  `DaysToGrow`, pasa a `Ready`. Después, **a todas** se les quita el riego. Las no
  regadas no avanzan y **no se mueren**.
- `Harvest` pide `Ready` (si no, `NotReady`). Mete `Yield` unidades del `cropId` en la
  mochila. **Si no cabe, no recoge**: devuelve 0 con `error` a `InventoryFull` y deja
  la planta lista, para que el jugador vuelva a por ella tras hacer sitio. Recoger
  algo que se pierde porque no cabía es de las cosas que peor sientan.
- Tras recoger: si `Regrows`, vuelve a `Planted` con `GrowthDays` a la mitad; si no,
  la casilla queda `Tilled` y `SeedId` vacío.
- `Clear` arranca lo que haya y deja `Tilled`, sin devolver nada.

`AdvanceDay` **tiene que poder llamarse varias veces seguidas** sin romperse: al
volver de estar fuera, el juego adelanta varios días de golpe por ahí.

Publica `TileChanged` en cada cambio y `CropHarvested` al recoger, siempre **después**
de cambiar el estado.

## Fichero 4 — `FarmingTests.cs`

`Nimbo.Tests`, NUnit. Dobles tuyos para `IInventoryService`. **Comprueba con grep que
el nombre de tu doble no esté ya cogido** en el ensamblado de pruebas: todo comparte
espacio de nombres y a otro agente le costó media hora.

Como mínimo:

1. Un huerto nuevo tiene todas las casillas en `Wild`.
2. Labrar deja `Tilled`; labrar dos veces no rompe nada.
3. Sembrar sin labrar devuelve `NotTilled`.
4. Sembrar gasta una semilla de la mochila.
5. Sembrar sin llevar semillas devuelve `NoSeed` y no cambia la casilla.
6. **La importante**: siembra, riega y pasa los días justos → queda `Ready`.
7. **La otra importante**: siembra y pasa cinco días **sin regar** → sigue `Planted`,
   con `GrowthDays` a 0, y **no se ha muerto**.
8. Regar un día sí y otro no tarda el doble.
9. Recoger mete el cultivo en la mochila y devuelve el `Yield`.
10. Un cultivo con `Regrows` vuelve a `Planted` al recogerlo; uno sin él deja `Tilled`.
11. `AdvanceDay` diez veces seguidas no rompe nada ni pasa de `Ready`.
12. Fuera de la parcela devuelve `OutOfBounds` en las cuatro operaciones.
13. Con la mochila llena, recoger devuelve `InventoryFull` y la planta **sigue**
    `Ready`, no se pierde.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Farming" -testResults /tmp/huerto.xml -logFile /tmp/huerto.log
```

Sin `-quit` (cierra Unity antes de probar y deja un log verde falso). Lee el XML:
`grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' /tmp/huerto.xml`

**Coordinación**: cuatro agentes y yo sobre el mismo proyecto; Unity coge
`Temp/UnityLockfile` en exclusiva. No lances `unity` hasta tener los cuatro ficheros,
y ante un error de bloqueo **espera y reintenta**, no mates procesos de Unity.

## Lo que NO haces

- No dibujas la parcela ni las plantas. Lo hago yo.
- No pones la parcela en el mundo ni decides dónde está.
- No añades valores a `FarmError` ni a ningún enum del contrato.
- No haces que nada se muera.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**.
