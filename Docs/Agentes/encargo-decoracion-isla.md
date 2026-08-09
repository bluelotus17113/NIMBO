# Encargo: decorar la isla (C#)

La isla se puede recorrer pero no se puede tocar. El jugador tiene un editor de
interiores para su casa y, en cambio, la plaza y el parque son exactamente iguales
en la partida de todo el mundo. Esto lo arregla: bancos, farolas, estatuas,
arbustos y fuentes que el jugador compra y coloca por las zonas comunes.

Es el punto «Personalización de la isla» del `[IMPORTANTE]` del GDD.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` — manda sobre todo lo demás.
- `Assets/_Project/Scripts/Core/Services/Contracts/IDecorService.cs` — **el
  contrato ya está escrito y cerrado**. Tú lo implementas tal cual. Si crees que
  una firma está mal, dilo antes de cambiarla; no la cambies por tu cuenta,
  porque yo estoy escribiendo la interfaz de usuario contra ella al mismo tiempo.
- `Assets/_Project/Scripts/Economy/Items/ItemCatalog.cs` — copia de ahí la forma
  de cargar un catálogo. Ya está resuelto y quiero que se parezcan.
- `Assets/_Project/Scripts/Housing/HousingService.cs` — el servicio hermano.
  Mismo aire, mismo estilo.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Scripts/Data/World/DecorPlacement.cs`
2. `Assets/_Project/Resources/Config/catalogo_decoracion.json`
3. `Assets/_Project/Scripts/Island/Decor/DecorCatalog.cs`
4. `Assets/_Project/Scripts/Island/Decor/DecorService.cs`
5. `Assets/_Project/Scripts/Tests/DecorTests.cs`

No toques nada más. Ni `IDecorService.cs`, ni `IslandState.cs`, ni
`GameEvents.cs`, ni `GameBootstrap.cs`, ni ningún `.asmdef`, ni `Docs/`. Hay otro
agente y yo trabajando en paralelo sobre este mismo repositorio.

Del montaje me encargo yo: yo registro tu servicio en `GameBootstrap`, yo dibujo
los adornos y yo hago el panel para colocarlos. Tú entregas la pieza.

## Lo que ya existe y no tienes que hacer

En `IslandState` (`Assets/_Project/Scripts/Data/World/IslandState.cs`) ya está
puesto el hueco donde se guarda tu trabajo, y es donde tienes que escribir:

```csharp
public List<DecorPlacement> Decor = new List<DecorPlacement>();
```

En `Nimbo.Core.Events` ya están, escritos y listos para publicar:

```csharp
public readonly struct DecorPlaced  { string PlacementId, CatalogId, ZoneId; }
public readonly struct DecorRemoved { string PlacementId; }
public readonly struct DecorMoved   { string PlacementId; }
```

`Nimbo.Island` ya referencia `Nimbo.Core` y `Nimbo.Data`. No hace falta tocar el asmdef.

## Fichero 1 — `DecorPlacement.cs`

Un `[Serializable]` con campos públicos, en `Nimbo.Data.World`. Dato muerto: sin
lógica, sin propiedades calculadas, sin validación. Va dentro del guardado, así
que **cambiarle un nombre de campo rompe las partidas**; piénsalo una vez y déjalo.

Lleva: `PlacementId` (identificador único de esta pieza puesta), `CatalogId`,
`ZoneId`, la posición local dentro de la zona y el giro en grados.

Guarda la posición como **tres `float`** (`X`, `Y`, `Z`), no como `Vector3`.
`Vector3` serializa con Newtonsoft arrastrando `magnitude`, `normalized` y demás
propiedades calculadas: engorda el fichero y, en algunos casos, revienta al leerlo
por recursión. El servicio convierte a `Vector3` en el borde y ya está.

## Fichero 2 — `catalogo_decoracion.json`

**32 adornos.** Misma forma que los otros catálogos: `{ "version": 1, "items": [...] }`.

Cada uno con `catalogId` (prefijo `deco_`, minúsculas, sin tildes ni eñes),
`displayName`, `description`, `kind`, `price`, `unlockLevel`, `footprint` y `charm`.

`kind` es uno de: `Seat`, `Light`, `Statue`, `Plant`, `Sign`, `Fence`, `Water`.

Reparto que quiero: 6 asientos, 5 luces, 4 estatuas, 8 plantas, 3 carteles,
3 vallas, 3 fuentes. Precios de 40 a 1200 nimbos, subiendo con `charm` y con
`unlockLevel` (de 1 a 10). Una estatua cara es un objetivo a medio plazo; un
arbusto es una compra de martes.

`footprint` en metros: un arbusto 0,6; un banco 1,4; una fuente 3,5. `charm` de
0,02 a 0,25 — la suma de una zona entera no debería pasar de 1, y el servicio lo
recorta de todos modos.

Las descripciones, en castellano y con gracia. Es lo que el jugador lee en la
tienda y es medio juego. «Banco de nube prensada. Incómodo, pero es el sitio
donde todo el mundo acaba sentándose a hablar» sí. «Un banco para sentarse» no.

## Fichero 3 — `DecorCatalog.cs`

En `Nimbo.Island.Decor`. Igual que `ItemCatalog`: carga el JSON en el constructor,
lo deja de solo lectura, y **tiene un segundo constructor que recibe el JSON en
crudo** para poder probarlo sin `Resources`. Ese segundo constructor no es opcional:
es lo que hace que las pruebas del fichero 5 se puedan escribir.

Si el JSON no carga, un `Debug.LogError` con el nombre del fichero y catálogo
vacío. Nunca una excepción: un catálogo roto no puede impedir que arranque la
partida.

## Fichero 4 — `DecorService.cs`

En `Nimbo.Island.Decor`, implementa `IDecorService`. Recibe por el constructor el
`DecorCatalog`, el `SaveGame` y el `IIslandService`.

Las reglas de colocación, en el orden en que hay que comprobarlas:

1. `UnknownItem` — ese `catalogId` no está en el catálogo.
2. `ZoneLocked` — la zona no está abierta (`IIslandService.IsUnlocked`). No se
   decora lo que todavía no existe.
3. `OutsideZone` — la posición local se sale del radio de la zona. Usa **12 metros**
   de radio por zona; es una constante pública del servicio para que yo pueda
   dibujar el círculo en la interfaz con el mismo número.
4. `Overlaps` — se solapa con otro adorno ya puesto. Dos adornos se solapan si la
   distancia entre sus centros es menor que la suma de sus dos `footprint`.
   **Compara distancias al cuadrado**, no llames a `Vector3.Distance` en un bucle
   que se ejecuta mientras el jugador arrastra.
5. `TooMany` — la zona ya tiene `CapacityPerZone` adornos. Ponlo en **20**.

Al mover una pieza, compruébalo todo otra vez, pero **sin contarse a sí misma**
en el solape. Es el fallo típico: mueves un banco un centímetro y el servicio te
dice que choca consigo mismo.

`CharmOf` suma el `charm` de los adornos de la zona y lo recorta a 1. Adornos
repetidos rinden menos: el segundo igual cuenta la mitad, el tercero un cuarto, y
del cuarto en adelante nada. Veinte farolas idénticas no pueden dar el mismo
encanto que veinte cosas distintas, o la respuesta óptima es comprar veinte veces
lo más barato y ya no hay decisión que tomar.

`PlacementId`: `$"{zoneId}_{catalogId}_{n}"` con `n` el primer número libre. Tiene
que ser estable entre sesiones, así que **nada de `Guid.NewGuid()`** ni de índices
de lista, que cambian al borrar del medio.

El servicio escribe directamente en `save.Island.Decor` y no guarda en disco:
de eso ya se encarga el autoguardado. Publica el evento que toque **después** de
haber cambiado el estado, nunca antes.

## Fichero 5 — `DecorTests.cs`

Espacio de nombres `Nimbo.Tests`, con `NUnit.Framework`. Un `SaveGame` de mentira
y un `IIslandService` de mentira que hagas tú en el propio fichero; **no cargues
`Resources` en una prueba**, para eso está el segundo constructor del catálogo.

Como mínimo, y son las que voy a mirar:

1. Poner un adorno válido lo mete en `save.Island.Decor` y devuelve un identificador.
2. Ponerlo en una zona cerrada devuelve `ZoneLocked` y **no** toca el guardado.
3. Dos adornos que se pisan: el segundo devuelve `Overlaps`.
4. Dos adornos que se rozan sin llegar a tocarse: ambos entran.
5. Mover una pieza a un sitio libre funciona, y moverla un centímetro **también**
   (la de no chocar consigo misma).
6. Pasar de `CapacityPerZone` devuelve `TooMany`.
7. Quitar una pieza la saca del guardado y deja libre su hueco.
8. `CharmOf` de una zona vacía es 0, y nunca pasa de 1 por muchos que pongas.
9. Veinte adornos iguales dan menos encanto que veinte distintos.
10. Los identificadores son estables: poner, quitar el del medio, y volver a poner
    no reutiliza un identificador que ya estaba en uso.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Decor" -testResults /tmp/decor.xml \
  -logFile /tmp/decor.log
```

Sin `-quit`: con `-quit` Unity se cierra **antes** de correr las pruebas y te
deja un log verde que no ha probado nada. El resultado sale en el `.xml`.

Si no tienes el envoltorio `unity` en el `PATH`, dilo y te paso la ruta; **no te
inventes una ruta a un Unity que no existe ni te saltes las pruebas**.

## Lo que NO haces

- No dibujas nada. Ni una malla, ni un `GameObject`, ni un `MonoBehaviour`.
- No cobras. El servicio coloca; quien cobra es `IEconomyService` y lo llamo yo.
- No haces el panel de la tienda de adornos. Lo hago yo.
- No metes el encanto en la simulación. Tú expones `CharmOf`; quien lo lea es
  cosa mía.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**. `// al mover, la pieza no
cuenta contra sí misma o no se puede mover nunca` vale; `// comprueba el solape`
no vale y lo borro.
