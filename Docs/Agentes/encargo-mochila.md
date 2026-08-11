# Encargo: la mochila (C#)

El protagonista ahora tiene cuerpo y anda por la isla. Necesita dónde meter lo que
recoge: una mochila con huecos contados, pilas, y un objeto en la mano.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` — manda sobre todo. Las tres reglas duras.
- `Docs/04_ALDEA.md` — de qué va el juego ahora. **§2 es contrato**: nada de castigar.
- `Assets/_Project/Scripts/Core/Services/Contracts/IInventoryService.cs` — **el
  contrato ya está escrito y cerrado**. Lo implementas tal cual. Si crees que una
  firma está mal, dilo; no la cambies, que yo escribo la interfaz contra ella a la vez.
- `Assets/_Project/Scripts/Data/Economy/Inventory.cs` — la clase que guarda las pilas.
- `Assets/_Project/Scripts/Data/Player/PlayerState.cs` — dónde vive tu estado.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Scripts/Items/InventoryService.cs`
2. `Assets/_Project/Tests/InventoryTests.cs`

Las pruebas van en `Assets/_Project/Tests/`, **no** en `Scripts/Tests/`, que no existe
y donde el runner descubre cero pruebas.

No toques nada más. Ni el contrato, ni `Inventory.cs`, ni `PlayerState.cs`, ni
`GameEvents.cs`, ni `SaveGame.cs`, ni ningún `.asmdef` (el de `Nimbo.Items` ya está
hecho). Hay tres agentes más trabajando en paralelo sobre este repositorio.

## Lo que ya existe

```csharp
// Nimbo.Data.Economy
struct ItemStack { string CatalogId; int Quantity; }
class Inventory  { List<ItemStack> Stacks; int CountOf(id); void Add(id,n); bool Remove(id,n); }

// Nimbo.Data.Player — tu estado vive aquí
class PlayerState { Inventory Bag; int SelectedSlot; ... }

// Nimbo.Core.Events — para publicar
struct InventoryChanged { int Slot; }   // -1 si cambió más de uno
struct SlotSelected     { int Slot; }
```

`ToolKind` y las categorías nuevas (`Tool`, `Material`, `Seed`, `Crop`) están en
`IEconomyService.cs`, ya escritas.

## `InventoryService.cs`

En `Nimbo.Items`, implementa `IInventoryService`. Recibe por el constructor el
`PlayerState` y el `IEconomyService` (para consultar el catálogo).

**24 huecos**, **10 en la barra de abajo** (`HotbarSize`).

La clave de todo, y es donde se falla: **los huecos son posicionales**. `Bag.Stacks`
es la lista de huecos en orden, así que tiene que tener siempre exactamente
`SlotCount` entradas, y un hueco vacío es una `ItemStack` de cantidad 0, **no un
elemento que se quita de la lista**. Si compactas la lista, todo lo que hay a la
derecha se desplaza cada vez que gastas algo, y la mochila se reordena sola delante
del jugador. Es de las cosas que más molestan de un juego.

Normaliza la lista en el constructor: si viene más corta (partida vieja), rellena con
huecos vacíos hasta `SlotCount`; si viene más larga, recorta.

Límite de pila: **99** para todo lo apilable, y **1** para `ItemCategory.Tool`. Dos
azadas ocupan dos huecos, que es lo que hace que llevar todas las herramientas cueste
sitio y haya que decidir.

`TryStore` llena primero las pilas que ya haya de ese objeto y luego huecos vacíos.
Devuelve `Ok` si entró todo, `Partial` si entró parte —con el resto en `leftover`— y
`Full` si no entró nada. Un objeto que no está en el catálogo es `UnknownItem` y no
entra.

`Select` recorta al rango de la barra, no al de la mochila: seleccionar el hueco 20 no
tiene sentido porque no se ve. Publica `SlotSelected` solo si cambió de verdad.

Publica `InventoryChanged` **después** de cambiar el estado, nunca antes, y con el
hueco concreto cuando sea uno solo.

## `InventoryTests.cs`

Espacio de nombres `Nimbo.Tests`, con `NUnit.Framework`. Un `PlayerState` de mentira y
un doble de `IEconomyService` que hagas tú.

**Aviso**: ya existe un `TestIslanderRegistry` en `EconomyTests.cs` y todo el
ensamblado de pruebas comparte el espacio de nombres `Nimbo.Tests`. Comprueba con
grep que el nombre de tu doble no esté cogido antes de darlo por bueno; a otro agente
le costó media hora esto.

Como mínimo, y son las que voy a mirar:

1. Una mochila nueva tiene 24 huecos y todos vacíos.
2. Guardar algo lo deja en el primer hueco libre.
3. Guardar más de lo mismo llena la pila que ya había antes de abrir otra.
4. Pasado 99 abre una pila nueva en otro hueco.
5. Con la mochila llena, `TryStore` devuelve `Full` y `leftover` trae todo.
6. Media mochila: devuelve `Partial` y `leftover` trae lo que no cupo.
7. **La de los huecos posicionales**: pon algo en el hueco 0, 1 y 2; vacía el 1; el
   objeto del 2 **sigue en el 2** y el 1 queda vacío. Si esta falla, todo lo demás da
   igual.
8. Las herramientas no se apilan: dos azadas ocupan dos huecos.
9. `Swap` intercambia, y con un hueco vacío también.
10. `ToolInHand` da la herramienta del hueco seleccionado, y `None` si no lo es.
11. `TryTake` de algo que no lleva devuelve falso y no toca nada.
12. Una mochila guardada con menos huecos de los que toca se normaliza a 24 al cargar.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Inventory" -testResults /tmp/mochila.xml -logFile /tmp/mochila.log
```

Sin `-quit`: con `-quit` Unity se cierra **antes** de correr las pruebas y deja un log
verde que no ha probado nada. El resultado sale en el XML:

```
grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' /tmp/mochila.xml
```

**Coordinación**: somos cuatro agentes y yo sobre el mismo proyecto, y Unity coge
`Temp/UnityLockfile` en exclusiva. No lances `unity` hasta tener los dos ficheros
escritos, y si da error de bloqueo, **espera y reintenta**; no mates procesos de Unity.

## Lo que NO haces

- No dibujas la mochila. La pinto yo.
- No tocas la despensa vieja (`IEconomyService.Inventory`): esa se queda para regalar
  y colocar muebles, y es otra cosa.
- No cobras ni vendes.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**. `// un hueco vacío es cantidad 0,
no un elemento menos, o la mochila se reordena sola` vale; `// comprueba el hueco` no.
