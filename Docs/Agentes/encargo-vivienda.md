# Encargo: el módulo de vivienda (C#)

Implementas el editor de interiores tipo *Los Sims* de Isla Nimbo. Es trabajo de
reglas de rejilla: determinista, sin gráficos y comprobable con tests.

## Lee esto antes de escribir una línea

1. `Docs/00_ARQUITECTURA.md` — el contrato. Manda sobre todo.
2. `Docs/Contratos/vivienda.md` — tu especificación, ya escrita y cerrada.
3. `Assets/_Project/Scripts/Core/Services/Contracts/IHousingService.cs` — **la interfaz
   que tienes que implementar, tal cual está**. No la cambies.
4. `Assets/_Project/Scripts/Data/Housing/RoomLayout.cs` — los datos. Tampoco se tocan.
5. `Docs/Contratos/catalogo_muebles.json` — los 80 muebles, con `layer`, `footprintX`,
   `footprintY`, `function` y `needBonus`.

## Ficheros que escribes — y ningún otro

Todos bajo `Assets/_Project/Scripts/Housing/`:

- `Catalog/FurnitureEntry.cs` — una entrada del catálogo de muebles, solo lectura
- `Catalog/FurnitureCatalog.cs` — carga `catalogo_muebles.json` y `catalogo_acabados.json`
  desde `Resources/Config/` y los indexa por `catalogId`
- `Grid/RoomGrid.cs` — la ocupación por capas: qué casillas usa cada objeto colocado
- `Grid/Footprint.cs` — huella de un mueble y cómo gira con `Facing`
- `Grid/PlacementRules.cs` — las reglas de si cabe o no, devolviendo `PlacementError`
- `HousingService.cs` — implementa `IHousingService` usando los anteriores

Y los tests, en `Assets/_Project/Tests/`:

- `HousingPlacementTests.cs`

**No toques nada fuera de esas rutas.** Ni `Core/`, ni `Data/`, ni `Docs/`, ni los
otros módulos. Hay más agentes trabajando a la vez.

## Las reglas, en orden

1. **Una capa, una casilla.** Dos objetos de la misma `PlacementLayer` no comparten
   casilla. De capas distintas, sí: una alfombra debajo de una mesa es legal.
2. **La huella gira con el objeto.** Un mueble de 2×1 mirando al norte ocupa dos
   casillas en X; mirando al este, dos en Y. `Footprint` resuelve eso y nadie más.
3. `WallMounted` solo cabe en la fila `y == Height-1` (pared norte) o en la columna
   `x == 0` (pared oeste) — son las dos que se ven en cámara. Si no, `NeedsWall`.
4. `Surface` necesita que **todas** sus casillas caigan sobre un objeto de capa
   `Furniture` cuya `function` sea `table`, `storage` o `kitchen`. Si no, `NeedsSurface`.
5. Fuera de la rejilla → `OutOfBounds`. `catalogId` que no está en el catálogo →
   `UnknownCatalogId`.
6. `CanPlace` **no muta nada**. La interfaz la llama en cada movimiento del ratón, así
   que además tiene que ser barata: nada de reservar listas dentro.
7. `Place` devuelve el mismo error que habría devuelto `CanPlace`, y si el error no es
   `None` **no toca la habitación en absoluto**. Ni a medias.
8. `ComfortScore` suma el `needBonus` de los muebles colocados más un extra por los de
   `function: "decor"`, y devuelve de 0 a 100. Documenta la fórmula que elijas.

## Las trampas

- `RoomLayout.Objects` guarda solo `Origin`, `Facing` y `Layer`. El tamaño está en el
  catálogo. Si lo duplicas en el guardado, tarde o temprano discreparán.
- Cada `PlacedObject` necesita un `InstanceId` único: sin él, `Remove` no sabe cuál
  quitar cuando hay dos sillas iguales. Genéralo tú al colocar.
- El `needBonus` del JSON usa las claves `hunger`, `energy`, `social`, `hygiene`.
  **No existe `mood`**: el ánimo no es una necesidad. Si lo ves, ignóralo y dilo.
- `FootprintOf` devuelve `IEnumerable<GridCoord>` y la llama la interfaz al dibujar la
  previsualización: que no reserve una lista nueva por llamada si puedes evitarlo.

## Los tests

`HousingPlacementTests.cs`, con NUnit (`Assets/_Project/Tests/`). Como mínimo:

- colocar un mueble de 1×1 en una habitación vacía funciona
- el mismo sitio dos veces da `Occupied`
- una alfombra y una mesa en la misma casilla funcionan (capas distintas)
- un mueble de 2×1 girado al este ocupa las casillas de Y, no las de X
- un cuadro en mitad de la habitación da `NeedsWall`, y pegado a la pared norte funciona
- una lámpara de mesa sobre el suelo da `NeedsSurface`, y sobre una mesa funciona
- un `Place` que falla deja `room.Objects.Count` exactamente como estaba
- colocar cerca del borde con una huella que se sale da `OutOfBounds`

No hace falta que ejecutes los tests: no tienes Unity. Escríbelos bien y yo los corro.

## Formato de respuesta al terminar

```
FICHEROS: <lista con líneas de cada uno>
FÓRMULA DE COMODIDAD: <la que elegiste y por qué>
DECISIONES: <lo que tuviste que decidir tú>
DUDAS: <lo que te chocó del contrato o del catálogo, o "ninguna">
```
