# Encargo: recoger por la isla (C#)

La isla tiene que dar algo cuando la recorres: árboles que dan madera, piedras que
dan piedra, hierbas y flores que se cogen a mano, y restos de nube que aparecen en el
borde. Y **se repone solo con los días**, que es la razón de que este juego se pueda
dejar una semana sin que pase nada malo: la isla se rellena mientras no estás.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` y `Docs/04_ALDEA.md` (§2 es contrato: no se castiga).
- `Assets/_Project/Scripts/Core/Services/Contracts/IGatheringService.cs` — **contrato
  cerrado**, lo implementas tal cual.
- `Assets/_Project/Scripts/Data/World/ResourceNode.cs` — tu dato, ya escrito.
- `Assets/_Project/Scripts/Core/Services/Contracts/IInventoryService.cs`
- `Assets/_Project/Scripts/Island/Decor/DecorService.cs` — el servicio más parecido
  que hay. Mismo aire, mismo estilo, y ahí está resuelto lo de colocar sin solapar.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Resources/Config/catalogo_recursos.json`
2. `Assets/_Project/Scripts/Gathering/NodeCatalog.cs`
3. `Assets/_Project/Scripts/Gathering/GatheringService.cs`
4. `Assets/_Project/Tests/GatheringTests.cs`

Pruebas en `Assets/_Project/Tests/`, no en `Scripts/Tests/`.

No toques nada más. Ni contratos, ni `ResourceNode.cs`, ni `SaveGame.cs`, ni
`GameEvents.cs`, ni `.asmdef` (el de `Nimbo.Gathering` ya está). Tres agentes más en
paralelo.

## Lo que ya existe

```csharp
// Nimbo.Data.World
class ResourceNode   { string InstanceId, NodeId; float X,Y,Z,Yaw;
                       int HitsLeft; int RespawnOnDay; bool IsDepleted; }
class GatheringState { bool Seeded; List<ResourceNode> Nodes; }

// Nimbo.Core.Events
struct NodeGathered  { string InstanceId; string DropId; int Quantity; }
struct NodeRespawned { string InstanceId; }
```

`ToolKind` (`None`, `Hoe`, `WateringCan`, `Axe`, `Pickaxe`, `Scythe`) está en
`IEconomyService.cs`.

## Fichero 1 — `catalogo_recursos.json`

**16 tipos de nodo.** Forma `{ "version": 1, "items": [...] }` con `nodeId` (prefijo
`node_`), `displayName`, `kind`, `requiredTool`, `dropId`, `minDrop`, `maxDrop`,
`hits`, `respawnDays` y `description`.

`kind` es uno de: `Tree`, `Rock`, `Bush`, `Herb`, `Flower`, `Flotsam`.

Reparto: 4 `Tree`, 3 `Rock`, 2 `Bush`, 3 `Herb`, 2 `Flower`, 2 `Flotsam`.

Lo que se coge a mano (`requiredTool: "None"`) lleva `hits: 1` y se repone rápido
(1–2 días). Lo que pide herramienta aguanta de 3 a 6 golpes y tarda de 4 a 10 días.

Los `dropId` son materiales: `mat_madera`, `mat_piedra`, `mat_fibra`… con prefijo
`mat_`, minúsculas, sin tildes ni eñes. **Que no haya dieciséis materiales
distintos**: seis o siete bien repartidos, con varios nodos dando el mismo. Un
material por nodo convierte el crafteo en una lista de la compra.

Descripciones en castellano y con gracia; es una isla que flota, que se note.

## Fichero 2 — `NodeCatalog.cs`

En `Nimbo.Gathering`. Como `ItemCatalog`: carga en el constructor, solo lectura, **y
un segundo constructor con el JSON en crudo** para poder probarlo sin `Resources`. No
es opcional. JSON roto: `Debug.LogError` y catálogo vacío, nunca excepción.

## Fichero 3 — `GatheringService.cs`

En `Nimbo.Gathering`, implementa `IGatheringService`. Constructor: `NodeCatalog`,
`GatheringState` (del `SaveGame`), `IInventoryService` y `GameClock`.

**Sembrar la isla.** Si `state.Seeded` es falso, reparte los nodos por el mundo y lo
pone a cierto. Y aquí van las reglas que importan:

- Con `Nimbo.Core.Util.Rng` **sembrado con una semilla fija**, no con la hora: dos
  partidas nuevas del mismo jugador pueden diferir, pero la misma partida cargada dos
  veces tiene que dar la misma isla. Mira cómo lo hace el resto del proyecto.
- Dentro de un disco de **radio 95** alrededor del origen (la isla mide 100 y el borde
  no se pisa), y **fuera de un círculo de radio 30** en el centro, que ahí está la
  plaza y no puede amanecer llena de árboles.
- Unos **120 nodos**, con dos nodos nunca a menos de **2,5 m** uno de otro. Compara
  **distancias al cuadrado**, no `Vector3.Distance` en un bucle de 120×120.
- `Yaw` al azar, o saldrán todos los árboles calcados.
- `InstanceId` estable: `$"{nodeId}_{n}"` con `n` el primer número libre. **Nada de
  `Guid.NewGuid()`**, que cambia entre sesiones.

**Recoger.** `Gather(instanceId, tool, out dropped)`:

1. `UnknownNode` si no existe.
2. `Depleted` si ya estaba agotado esperando reponerse.
3. `WrongTool` si la herramienta no es la que pide. Ojo: `RequiredTool.None` se coge
   **a mano**, o sea con `ToolKind.None`; llevar un hacha en la mano no puede impedir
   coger una flor, así que con `None` **vale cualquier cosa**.
4. Resta un golpe. Si aún le quedan, devuelve `Hit` con `dropped` a 0.
5. Al llegar a 0: sortea entre `MinDrop` y `MaxDrop` y **lo mete en la mochila**. Si
   no cabe, devuelve `InventoryFull` y **deja el nodo con un golpe**, sin soltar nada:
   perder el árbol y el material por tener la mochila llena sienta fatal.
6. Anota `RespawnOnDay = díaActual + RespawnDays` y publica `NodeGathered`.
   Con `respawnDays: 0` no vuelve nunca.

**Reponer.** `AdvanceDay` recorre lo agotado y devuelve a la vida lo que ya cumplió
su espera, publicando `NodeRespawned`. **Tiene que poder llamarse varias veces
seguidas**: al volver de estar fuera se adelantan varios días por ahí. Usa el día del
reloj, no un contador propio, o al adelantar cinco días de golpe no se repondría nada.

## Fichero 4 — `GatheringTests.cs`

`Nimbo.Tests`, NUnit, dobles tuyos. **Comprueba con grep que el nombre de tu doble no
esté cogido** en el ensamblado de pruebas: todo comparte espacio de nombres.

Como mínimo:

1. Sembrar deja unos 120 nodos y pone `Seeded`.
2. Cargar una partida ya sembrada **no vuelve a sembrar**.
3. Ningún nodo cae dentro del círculo central de radio 30 ni fuera del de 95.
4. No hay dos nodos a menos de 2,5 m.
5. La misma semilla da la misma isla; dos semillas distintas dan islas distintas.
6. Darle con la herramienta que no es devuelve `WrongTool` y no gasta golpes.
7. Un nodo de mano se coge con `ToolKind.None` **y también llevando un hacha**.
8. Un nodo de 3 golpes devuelve `Hit` dos veces y `Ok` a la tercera, con el material
   en la mochila.
9. Un nodo agotado devuelve `Depleted`.
10. **Con la mochila llena**: devuelve `InventoryFull`, el nodo **no** queda agotado y
    no se pierde nada.
11. `AdvanceDay` repone justo el día que toca, ni antes ni después.
12. Adelantar diez días de golpe repone todo lo que tocaba, no solo lo del último día.
13. Un nodo con `respawnDays: 0` no vuelve nunca.
14. Los `InstanceId` no se repiten.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Gathering" -testResults /tmp/recoger.xml -logFile /tmp/recoger.log
```

Sin `-quit` (cierra Unity antes de probar y deja un log verde falso). Lee el XML:
`grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' /tmp/recoger.xml`

**Coordinación**: cuatro agentes y yo sobre el mismo proyecto; Unity coge
`Temp/UnityLockfile` en exclusiva. No lances `unity` hasta tener los cuatro ficheros,
y ante un error de bloqueo **espera y reintenta**, no mates procesos de Unity.

## Lo que NO haces

- No dibujas ni un árbol. Lo hago yo con las mallas del proyecto.
- No decides cómo se ven ni qué malla usa cada tipo.
- No tocas la cámara, el protagonista ni el mundo.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**.
