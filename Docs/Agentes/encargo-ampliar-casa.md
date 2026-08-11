# Encargo: ampliar la casa (C#)

Las casas de Isla Nimbo son todas de 8×8 casillas y lo serán siempre. El jugador
gana nimbos, decora, y llega un punto en que no le cabe nada más y no tiene en qué
gastar. Esto lo arregla: dos ampliaciones que se compran y agrandan la habitación.

Es el punto «Ampliación de apartamento» del `[IMPORTANTE]` del GDD.

Es un encargo pequeño —dos ficheros— pero tiene una trampa de verdad, la del
remapeo del suelo. Está avisada más abajo.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` — manda sobre todo lo demás.
- `Assets/_Project/Scripts/Core/Services/Contracts/IHomeUpgradeService.cs` — **el
  contrato ya está escrito y cerrado**. Lo implementas tal cual.
- `Assets/_Project/Scripts/Data/Housing/RoomLayout.cs` — **léelo entero**. Ahí está
  la trampa.
- `Assets/_Project/Scripts/Housing/HousingService.cs` — el servicio hermano, para
  copiarle el estilo y ver cómo llega al guardado.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Scripts/Housing/HomeUpgradeService.cs`
2. `Assets/_Project/Tests/HomeUpgradeTests.cs`

Ojo con la ruta del segundo: las pruebas van en `Assets/_Project/Tests/`, **no** en
`Assets/_Project/Scripts/Tests/`, que no existe y donde el runner descubre cero
pruebas.

No toques nada más. Ni el contrato, ni `RoomLayout.cs`, ni `SaveGame.cs`, ni
`GameEvents.cs`, ni `HousingService.cs`, ni `GameBootstrap.cs`, ni ningún
`.asmdef`. Yo registro tu servicio y yo hago el botón.

## Lo que ya existe

En `HomeRecord` (dentro de `SaveGame.cs`) está el hueco donde guardas el nivel:

```csharp
public int UpgradeLevel;   // 0 es la casa de serie
```

El evento, listo para publicar:

```csharp
public readonly struct HomeUpgraded { string IslanderId; int Level; int Size; }
```

`Nimbo.Housing` ya referencia `Nimbo.Core` y `Nimbo.Data`.

## Los números

- Nivel 0: 8×8, la de serie.
- Nivel 1: 11×11, cuesta **1.200** nimbos.
- Nivel 2: 14×14, cuesta **3.500** nimbos.

`MaxLevel` es 2. `RoomLayout` admite de 6 a 16, así que 14 entra de sobra.

## `HomeUpgradeService.cs`

En `Nimbo.Housing`, implementa `IHomeUpgradeService`. Recibe por el constructor el
`SaveGame`, el `IIslanderRegistry` y el `IEconomyService`.

Orden de comprobación en `CanUpgrade`:

1. `UnknownIslander` — ese identificador no está en el censo.
2. `NoHome` — está en el censo pero no tiene casa asignada (no hay `HomeRecord`).
3. `MaxedOut` — ya está en `MaxLevel`.
4. `NotEnoughCoins` — no llega al precio del siguiente nivel.

`Upgrade` comprueba lo mismo, cobra con `IEconomyService.TrySpend`, agranda y
publica `HomeUpgraded`. Si `TrySpend` devuelve falso, **no toca la habitación**:
ni un cambio a medias.

### La trampa: remapear el suelo

`RoomLayout.FloorTiles` es una lista plana indexada por `y * Width + x`. Cambiar
`Width` **cambia el significado de todos los índices de golpe**. Si te limitas a
subir `Width` y `Height` y añadir casillas al final de la lista, el suelo del
jugador se desbarata: cada fila se desplaza un poco más que la anterior y lo que
era un salón con parqué queda a rayas en diagonal. Se ve al instante y es el fallo
que voy a mirar primero.

Lo que hay que hacer: construir una lista nueva del tamaño nuevo, copiar cada
casilla vieja a su sitio con los índices nuevos, y rellenar lo que sobra con el
acabado por defecto. Las casillas viejas van en la esquina (0,0) — la habitación
crece hacia el este y el sur, así que las coordenadas de lo que ya había **no
cambian** y los muebles se quedan donde estaban.

Ese último punto es el que hace que esto sea seguro: como solo se crece y se crece
por el lado contrario al origen, ningún `PlacedObject` se queda fuera y no hay que
tocar la lista de objetos. No la toques.

Si `FloorTiles` viene más corta de lo que dice `Width * Height` —pasa en casas
recién creadas—, trátalo como casillas vacías y no revientes.

## `HomeUpgradeTests.cs`

Espacio de nombres `Nimbo.Tests`, con `NUnit.Framework`. Un `SaveGame` de mentira
y dobles de `IIslanderRegistry` e `IEconomyService` que hagas tú en el fichero.

Como mínimo, y son las que voy a mirar:

1. Una casa nueva está en nivel 0 y mide 8×8.
2. Ampliar con dinero de sobra sube a nivel 1, deja la casa en 11×11 y cobra 1.200.
3. Ampliar sin dinero devuelve `NotEnoughCoins`, **no cobra** y deja la casa igual.
4. Dos ampliaciones seguidas llegan a 14×14 y la tercera devuelve `MaxedOut`.
5. Un habitante sin casa devuelve `NoHome`.
6. **La del suelo, y es la importante**: pinta un patrón reconocible en el suelo de
   la casa de 8×8 —por ejemplo, el acabado A en toda la fila y=0 y el B en el
   resto— amplía, y comprueba casilla a casilla que cada coordenada (x,y) de las
   originales sigue teniendo el acabado que tenía. Si haces esta prueba con un
   suelo de un solo color no prueba nada: el patrón tiene que distinguir filas.
7. Los muebles que había siguen en las mismas coordenadas después de ampliar.
8. Las casillas nuevas existen y tienen acabado, no cadena vacía ni null.
9. `Upgrade` publica `HomeUpgraded` una vez, con el nivel y el tamaño nuevos.
10. El nivel sobrevive: tras ampliar, `LevelOf` lo lee del `HomeRecord`, no del
    tamaño de la rejilla.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "HomeUpgrade" -testResults /tmp/casa.xml -logFile /tmp/casa.log
```

Sin `-quit`: con `-quit` Unity se cierra **antes** de correr las pruebas y deja un
log verde que no ha probado nada. El resultado sale en el XML:

```
grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' /tmp/casa.xml
```

**Aviso de coordinación:** puede haber otro proceso sobre este mismo proyecto y
Unity coge un bloqueo exclusivo (`Temp/UnityLockfile`). No lances `unity` hasta
tener los dos ficheros escritos, y si te da error de bloqueo, espera y reintenta en
vez de matar procesos de Unity.

## Lo que NO haces

- No encoges casas. Solo se crece.
- No tocas la lista de objetos colocados.
- No haces el botón ni la pantalla. Los hago yo.
- No añades logros por ampliar. Eso ya lo lleva otro módulo por su cuenta.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**. `// la habitación crece hacia
el este y el sur para que las coordenadas de lo que ya había no cambien` vale;
`// aumenta el ancho` no vale y lo borro.
