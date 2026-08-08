# Encargo: el módulo de economía (C#)

Implementas el dinero, el catálogo y las tiendas de Isla Nimbo. Es la pieza que
convierte los 225 objetos que ya existen en algo que el jugador puede comprar y regalar.

## Lee esto antes de escribir una línea

1. `Docs/00_ARQUITECTURA.md` — el contrato. Manda sobre todo.
2. `Docs/Contratos/economia.md` — tu especificación, ya escrita y cerrada.
3. `Assets/_Project/Scripts/Core/Services/Contracts/IEconomyService.cs` — **la interfaz
   que implementas, y también `IItemDefinition` y `ItemCategory`**. No las cambies.
4. `Assets/_Project/Scripts/Data/Economy/Inventory.cs` — `Wallet` e `Inventory`, tal cual.
5. Los cuatro catálogos en `Assets/_Project/Resources/Config/catalogo_*.json`.

## Ficheros que escribes — y ningún otro

Todos bajo `Assets/_Project/Scripts/Economy/`:

- `Items/ItemDefinition.cs` — implementa `IItemDefinition`, solo lectura
- `Items/ItemCatalog.cs` — carga los cuatro JSON de `Resources/Config` y los indexa
- `Items/CatalogJson.cs` — las clases planas para deserializar con Newtonsoft
- `Shops/ShopDefinition.cs` — qué categorías vende cada tienda y cuánto stock saca
- `Shops/ShopStock.cs` — el stock del día y su rotación
- `EconomyService.cs` — implementa `IEconomyService`

Y los tests, en `Assets/_Project/Tests/`:

- `EconomyTests.cs`

**No toques nada fuera de esas rutas.** Ni `Core/`, ni `Data/`, ni `Docs/`, ni los
otros módulos. Hay más agentes trabajando a la vez.

## Cómo son los JSON

Los cuatro tienen la misma forma: `{ "version": 1, "category": "...", "items": [...] }`.
Los campos comunes de cada elemento son `catalogId`, `displayName`, `description`,
`price`, `unlockLevel` y `tags`. Luego cada fichero trae los suyos:

- comida: `hungerRestore`, `foodKind`
- ropa: `slot`, `style`, `palette`
- muebles: `layer`, `footprintX`, `footprintY`, `function`, `needBonus`
- acabados: `surface`, `baseColor`, `pattern`

Un solo `ItemDefinition` tiene que poder representar los cuatro. Los campos que no
apliquen se quedan a su valor por defecto; `FootprintX`/`FootprintY` valen 1 salvo en
muebles. La `ItemCategory` sale del fichero del que vino, no de adivinarla por el id.

## Las reglas, en orden

1. **El catálogo se carga una vez.** `ItemCatalog` es de solo lectura tras cargarse.
   Si te piden un `catalogId` que no existe, devuelves null y registras un error: nunca
   inventas una entrada por defecto que luego el jugador pueda comprar.
2. **`TrySpend` es la única puerta al monedero.** Si no llega el dinero, devuelve false
   y **no toca nada**. Nunca dejes el saldo en negativo.
3. Todo movimiento de monedas publica `CoinsChanged` con su motivo. Todo. Sin excepción:
   ese evento es lo que la interfaz usa para animar el contador.
4. `TryBuy` comprueba el dinero **y** el `unlockLevel` contra el nivel más alto de la
   isla. Un objeto bloqueado no se compra aunque sobre el dinero.
5. `GiveTo` gasta el objeto del inventario, mira los gustos del habitante
   (`IslanderData.Tastes.OpinionOf`) y publica `ItemGifted` con esa opinión (−1, 0, +1).
   **No toca el ánimo directamente**: para eso llama a `ISimulationService.ApplyHappiness`.
   El regalo que le encanta suma más que el que le da igual; los números los eliges tú
   y los anotas.
6. `StockOf` devuelve el stock del día de esa tienda y **es estable dentro del mismo
   día de juego**: si el jugador abre y cierra la tienda tres veces, ve lo mismo. Usa
   `Rng.FromSeed($"{shopId}:{dia}")` de `Nimbo.Core.Util` — es azar con semilla y por
   eso da lo mismo cada vez sin tener que guardarlo en la partida.

## Las trampas

- `Rng` es un `struct` y sus métodos mutan su estado: pásalo con `ref` o guárdalo en un
  campo. Si lo pasas por valor te devolverá siempre el mismo número y no lo verás venir.
- `Wallet` también es un `struct` dentro de `SaveGame`. Si haces
  `var w = save.Wallet; w.Coins += 10;` estás modificando una copia. Escribe de vuelta.
- Newtonsoft está disponible como `Unity.Nuget.Newtonsoft-Json` y el asmdef de
  `Nimbo.Economy` **aún no lo referencia**: añádelo tú a
  `Assets/_Project/Scripts/Economy/Nimbo.Economy.asmdef`. Es el único fichero de
  configuración que puedes tocar.
- `Resources.Load<TextAsset>("Config/catalogo_comida")` — sin la extensión `.json`.

## Los tests

`EconomyTests.cs`, con NUnit. Como mínimo:

- el catálogo carga los 225 objetos y los indexa sin repetidos
- `TrySpend` de más de lo que hay devuelve false y deja el saldo intacto
- `TryBuy` de un objeto bloqueado por nivel falla aunque sobre el dinero
- comprar descuenta el precio exacto y mete 1 unidad en el inventario
- `StockOf` devuelve lo mismo dos veces seguidas el mismo día, y algo distinto al
  día siguiente
- regalar algo que no se tiene devuelve false y no publica `ItemGifted`

No hace falta que los ejecutes: no tienes Unity. Escríbelos bien y yo los corro.

## Formato de respuesta al terminar

```
FICHEROS: <lista con líneas de cada uno>
NÚMEROS QUE ELEGISTE: <lo que suma cada tipo de regalo al ánimo, y por qué>
DECISIONES: <lo que tuviste que decidir tú>
DUDAS: <lo que te chocó, o "ninguna">
```
