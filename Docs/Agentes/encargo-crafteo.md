# Encargo: el crafteo (C#)

Convertir lo que se recoge por la isla en algo que sirva. Es el puente entre lo nuevo
y todo lo que ya existe: de aquí salen **muebles del catálogo de vivienda, adornos del
de decoración, herramientas y regalos** — nada inventado. Por eso el crafteo da valor
a la isla entera en vez de ser un sistema aparte.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` y `Docs/04_ALDEA.md`.
- `Assets/_Project/Scripts/Core/Services/Contracts/ICraftingService.cs` — **contrato
  cerrado**, lo implementas tal cual.
- `Assets/_Project/Scripts/Core/Services/Contracts/IInventoryService.cs`
- `Assets/_Project/Scripts/Economy/Items/ItemCatalog.cs` — cómo se carga un catálogo.

**Y lee los catálogos que ya existen**, porque tus resultados salen de ahí:
`Assets/_Project/Resources/Config/catalogo_muebles.json` y `catalogo_decoracion.json`.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Resources/Config/catalogo_recetas.json`
2. `Assets/_Project/Scripts/Crafting/RecipeCatalog.cs`
3. `Assets/_Project/Scripts/Crafting/CraftingService.cs`
4. `Assets/_Project/Tests/CraftingTests.cs`

Pruebas en `Assets/_Project/Tests/`, no en `Scripts/Tests/`.

No toques nada más. Ni contratos, ni `SaveGame.cs`, ni `GameEvents.cs`, ni los otros
catálogos, ni `.asmdef` (el de `Nimbo.Crafting` ya está). Tres agentes más en paralelo.

## Un aviso de dependencia

Los materiales (`mat_madera`, `mat_piedra`…) los está definiendo **otro agente ahora
mismo** en `catalogo_recursos.json`. Si ya existe cuando empieces, **usa esos
identificadores tal cual**. Si no existe todavía, escribe tus recetas con estos siete
y dilo en el informe para que yo lo cuadre:

`mat_madera` · `mat_piedra` · `mat_fibra` · `mat_resina` · `mat_cristal` ·
`mat_nube` · `mat_hierba`

## Lo que ya existe

```csharp
// Nimbo.Core.Services.Contracts
enum CraftStation { Hand, Bench, Kitchen }
struct CraftIngredient { string CatalogId; int Quantity; }
struct Recipe { string RecipeId, DisplayName, Description; CraftStation Station;
                IReadOnlyList<CraftIngredient> Ingredients;
                string OutputId; int OutputQuantity; int UnlockLevel; }
enum CraftError { Ok, UnknownRecipe, MissingIngredients, WrongStation, Locked, InventoryFull }

// Nimbo.Core.Events
struct ItemCrafted { string RecipeId; string OutputId; int Quantity; }
```

## Fichero 1 — `catalogo_recetas.json`

**36 recetas.** Forma `{ "version": 1, "items": [...] }`, con `recipeId` (prefijo
`recipe_`), `displayName`, `description`, `station`, `ingredients` (lista de
`{catalogId, quantity}`), `outputId`, `outputQuantity` y `unlockLevel`.

Reparto por estación: 10 de `Hand`, 20 de `Bench`, 6 de `Kitchen`.

**Los `outputId` tienen que existir ya** en `catalogo_muebles.json`,
`catalogo_decoracion.json` o `catalogo_comida.json`, salvo las herramientas, que son
nuevas y usan estos identificadores exactos:

`tool_azada` · `tool_regadera` · `tool_hacha` · `tool_pico` · `tool_guadana`

Ábrelos y coge identificadores de verdad. Una receta que fabrica algo que no existe es
un objeto fantasma y lo voy a mirar con un script.

De 1 a 4 ingredientes por receta. Las de `unlockLevel` 1 con dos ingredientes fáciles;
lo caro pide materiales de los que tardan en reponerse. Descripciones en castellano y
con gracia.

## Fichero 2 — `RecipeCatalog.cs`

En `Nimbo.Crafting`. Como `ItemCatalog`: carga en el constructor, solo lectura, **y un
segundo constructor con el JSON en crudo** para probarlo sin `Resources`. No es
opcional. JSON roto: `Debug.LogError` y catálogo vacío, nunca excepción.

## Fichero 3 — `CraftingService.cs`

En `Nimbo.Crafting`, implementa `ICraftingService`. Constructor: `RecipeCatalog`,
`IInventoryService` y `IIslandService` (de ahí sale el nivel de isla:
`State.Level`).

Orden de comprobación en `CanCraft`:

1. `UnknownRecipe`
2. `WrongStation` — pedirla en otro sitio.
3. `Locked` — el nivel de isla no llega.
4. `MissingIngredients`
5. `InventoryFull` — no habría dónde meter el resultado.

Y en `Craft`, **el orden importa y es donde se falla**: comprueba **todo** primero,
incluido que quepa el resultado, y solo entonces gasta. Si gastas los ingredientes y
luego descubres que no cabe la salida, el jugador se queda sin materiales y sin
objeto. Es un error que no se puede deshacer y que no se ve en un test que solo pruebe
el camino bueno.

Publica `ItemCrafted` **después** de que todo haya salido bien.

`AvailableAt(station, islandLevel)` devuelve las de esa estación con nivel suficiente,
**aunque no tenga los materiales**: ver lo que podrías hacer es la mitad del interés
de una lista de recetas.

## Fichero 4 — `CraftingTests.cs`

`Nimbo.Tests`, NUnit, dobles tuyos. **Comprueba con grep que el nombre de tu doble no
esté ya cogido** en el ensamblado de pruebas: todo comparte espacio de nombres y a
otro agente le costó media hora.

Como mínimo:

1. Craftear con todo a favor gasta los ingredientes y mete el resultado.
2. Sin materiales suficientes: `MissingIngredients` y **no gasta nada**.
3. En la estación equivocada: `WrongStation`.
4. Con el nivel de isla corto: `Locked`.
5. **La cara**: con la mochila llena devuelve `InventoryFull` y **los ingredientes
   siguen ahí**. Si esta falla, el juego se come materiales.
6. Una receta de varios ingredientes los gasta todos, en la cantidad justa.
7. `outputQuantity` mayor que 1 mete esa cantidad.
8. `AvailableAt` lista las de la estación aunque falten materiales, y **no** lista las
   de nivel superior.
9. Una receta desconocida devuelve `UnknownRecipe` sin romper nada.
10. `Craft` publica `ItemCrafted` una sola vez, y **no** lo publica si falló.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Crafting" -testResults /tmp/crafteo.xml -logFile /tmp/crafteo.log
```

Sin `-quit` (cierra Unity antes de probar y deja un log verde falso). Lee el XML:
`grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' /tmp/crafteo.xml

**Coordinación**: cuatro agentes y yo sobre el mismo proyecto; Unity coge
`Temp/UnityLockfile` en exclusiva. No lances `unity` hasta tener los cuatro ficheros,
y ante un error de bloqueo **espera y reintenta**, no mates procesos de Unity.

## Lo que NO haces

- No haces la pantalla de crafteo. La hago yo.
- No inventas objetos nuevos que no estén en los catálogos, salvo las cinco
  herramientas de la lista de arriba.
- No pones la mesa de trabajo en el mundo.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**. `// se comprueba que quepa antes
de gastar, o te quedas sin materiales y sin objeto` vale; `// comprueba el hueco` no.
