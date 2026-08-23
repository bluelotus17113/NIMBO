# Informe: encendido del Árbol Nimbo

Agente: ox-alpha · Carpeta asignada: `Assets/_Project/Scripts/Island/` · Fecha: 2026-08-23

---

## 1. Lo que había (medido)

- `NimboTree.TryTalk` (`Scripts/Island/NimboTree.cs:74`) tenía **cero llamadores**:
  `rg "TryTalk|CanTalkToday|GrowthScale|GrowthStage"` fuera del propio fichero daba
  vacío. El servicio estaba construido y registrado dos veces sin uso
  (`GameBootstrap.cs:335` lo construye, `:367` lo registra).
- El árbol dibujado era una malla fija: `WorldView.cs:134` llama
  `IslandMeshBuilder.BuildTree(26f, 9f)` y nadie leía `GrowthScale`
  (`NimboTree.cs:79`): no crecía nunca.
- **Ni una prueba propia**: `rg "NimboTree|Arbol|Árbol" Assets/_Project/Tests` solo
  encontraba la copa en `EstiloEnLaIslaTests.cs:137` y el rodeo en
  `BuildGridTests.cs:55`. Nada probaba su contenido ni su existencia jugable.
- El contenido no daba razón para volver: los chistes salían al azar de una lista de
  6 (`NimboTree.cs:156-165` versión anterior) — con 6 opciones, al tercer chiste
  repetir es más probable que estrenar —, el regalo salía de `Rng.FromTime()`
  (distinto cada partida) y no existía nada que contara días.

## 2. Lo que he aplicado yo (mi carpeta)

### 2.1 `Scripts/Island/NimboTree.cs` — el ritual diario

Todo compilable hoy, sin depender de ningún enganche:

| Cambio | Dónde | Por qué |
|---|---|---|
| Regalo con semilla del día: `Rng.FromSeed("arbol_" + Day)` | `NimboTree.cs:116` | El mismo día da lo mismo aunque se cierre y se abra el juego; las pruebas pueden afirmar sobre contenido estable. |
| Chistes por rotación diaria: `JokeOfTheDay(day)` público y puro | `NimboTree.cs:202` | No repite hasta agotar los 6; antes era azar puro y repetía pronto. |
| Racha: `Streak`, guardada como bandera `arbol_racha_<n>` | `NimboTree.cs:84,235-257` | `SaveGame.Flags` ya documenta «contadores sueltos… rachas» (`SaveGame.cs:113`); no serializo un campo nuevo. |
| Multiplicador `StreakMultiplier`: +10%/día, techo ×1,5, suelo ×1 | `NimboTree.cs:93` | Techo: sin él el ritual sería la mejor fuente de ingresos y faltar un día sería castigo económico. Suelo: racha 0 debe ser neutro (la primera versión daba ×0,9 — corregido antes de compilar). |
| `CleanOldFlags` conserva hoy **y ayer** | `NimboTree.cs:281-292` | La marca de ayer es la que dice si la racha continúa; la versión anterior la borraba y hacía la racha imposible. Quedan 2 banderas acotadas. |
| El árbol nombra la racha desde 3 días (`WithStreak`) y en el rechazo | `NimboTree.cs:211-230` | Se vuelve mañana para oír el número subir, no por el 10%. |
| Monedas escaladas por racha | `NimboTree.cs:137` | `low = round(20·etapa·multiplicador)`; el techo alto sigue siendo `×2`. |

### 2.2 Pruebas nuevas

**Editor** — `Tests/ArbolNimboTests.cs` (9 pruebas, todas de lógica pura, verdes sin
ningún enganche): apertura del primer día, cierre del día + rechazo que también habla,
reapertura al día siguiente, racha sube/se rompe, multiplicador con techo y suelo,
rotación de chistes sin repetir, determinismo por día, consejo nombra al vecino más
triste, entrega real de monedas a la cartera.

**PlayMode** — `Tests/PlayMode/ArbolEnLaIslaTests.cs` (3 pruebas):

1. `ElArbolEstaRegistradoYEnPieEnElCentro` — verde hoy.
2. `HablarleEnLaIslaCargadaCierraElDiaYEntrega` — verde hoy: carga `Isla`, habla por el
   servicio registrado, comprueba cartera y reapertura al dormir.
3. `DelanteDelArbolElCartelOfreceHablarle` — **la prueba de la costura**: teletransporta
   al protagonista junto al tronco (aleja antes a los vecinos de la plaza, que tienen
   prioridad de cartel), espera el refresco del objetivo y exige
   `Kind == Tree`, cartel «Hablar con el Árbol Nimbo», y simula la pulsación de E
   invocando `Act()` por reflexión (precedente: `CosturaDeLaIslaTests.cs:300`). Compara
   el enum **por nombre** (`Kind.ToString() == "Tree"`) para que el ensamblado compile
   antes de aplicar el enganche D y lo verifique en cuanto esté.

**Infraestructura de prueba**: añadí `"Nimbo.Island"` a las referencias de
`Tests/PlayMode/Nimbo.PlayTests.asmdef` (línea 10, aditiva de una línea): sin ella,
ninguna prueba puede tocar el tipo `NimboTree`. `Nimbo.Tests.asmdef` ya lo tenía
(`Nimbo.Tests.asmdef:13`).

## 3. La costura — la aplica el orquestador

`Nimbo.Art` solo referencia `Data`, `Core` y `Player` (`Nimbo.Art.asmdef:4-8`): **no ve**
`Nimbo.Island`, así que ni el interactuador ni la interfaz pueden tocar el tipo
`NimboTree`. Es la misma razón de ser de los contratos de `Nimbo.Core.Services.Contracts`.
Los siete pasos van juntos: A y B crean los tipos, G los usa desde mi fichero.

### A. `Scripts/Core/Events/GameEvents.cs` — nuevo evento

Tras el struct `IslanderFocused` (línea ~621):

```csharp
    /// <summary>
    /// El Árbol Nimbo ha hablado, haya dado lo suyo o ya lo hayan cobrado. El texto va
    /// compuesto porque quien lo publica ve tipos que la interfaz no conoce.
    /// </summary>
    public readonly struct TreeSpoke
    {
        public readonly string Text;
        public readonly int Coins;
        public readonly string ItemId;

        public TreeSpoke(string text, int coins, string itemId)
        { Text = text; Coins = coins; ItemId = itemId; }
    }
```

### B. `Scripts/Core/Services/Contracts/ITreeService.cs` — fichero nuevo

```csharp
using UnityEngine;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Lo que el Árbol Nimbo ofrece una vez al día.
    /// </summary>
    /// <remarks>
    /// Vive en Core y no el tipo Nimbo.Island porque quien lo llama es la vista
    /// (Nimbo.Art), y Nimbo.Art no ve Nimbo.Island: es el motivo de existencia del
    /// resto de contratos de esta carpeta.
    /// </remarks>
    public interface ITreeService
    {
        /// <summary>Queda por recoger lo de hoy.</summary>
        bool CanTalkToday { get; }

        /// <summary>Habla con el árbol. False si lo de hoy ya se recogió; el texto viene igual.</summary>
        bool TryTalk(out string text);
    }
}
```

(El `using UnityEngine` no hace falta; quítalo si prefieres. Sin dependencias nuevas.)

### C. `Scripts/Game/Bootstrap/GameBootstrap.cs:367` — registrar el contrato

Justo después de `ServiceRegistry.Register<NimboTree>(_tree);`:

```csharp
            ServiceRegistry.Register<ITreeService>(_tree);
```

### D. `Scripts/Art/Player/PlayerInteractor.cs` — el enganche principal

**D.1** Enum `TargetKind` (líneas 9-12), añadir tras `Stove = 13`:

```csharp
                             Tree = 14 }
```

**D.2** En `FindTarget()`, **después** de `if (TryTargetStage()) return;` (línea 173) y
antes de `if (TryTargetFishingSpot()) return;` (línea 174) — el orden importa: durante
el concierto el escenario ocupa el sitio del árbol (es `Vector3.zero`,
`PlayerInteractor.cs:435`) y subir a tocar tiene que ganar:

```csharp
            // El árbol antes que la caña y el huerto: está en mitad de la plaza, por
            // donde se pasa siempre, y su ritual es de las primeras cosas que hay que
            // poder hacer en una partida nueva.
            if (TryTargetTree()) return;
```

**D.3** Método nuevo, junto a `TryTargetBoard` (tras la línea 476):

```csharp
        /// <summary>
        /// ¿Está delante del Árbol Nimbo?
        /// </summary>
        /// <remarks>
        /// El radio cubre el bulto que rodea el tronco en WorldView (4,5 m) con margen
        /// para no pelearse con el colisionador del pie, que ensancha metro y medio.
        /// El cartel distingue si queda regalo hoy: saber que «ya ha hablado hoy» sin
        /// pulsar es lo que evita convertir el intento fallido en rabia.
        /// </remarks>
        private bool TryTargetTree()
        {
            if (!Near(transform.position, Vector3.zero, 6f)) return false;

            Kind = TargetKind.Tree;
            TargetId = "";

            bool queda = ServiceRegistry.TryGet<ITreeService>(out var arbol)
                         && arbol.CanTalkToday;
            Prompt = queda ? "Hablar con el Árbol Nimbo"
                           : "El Árbol Nimbo ya ha hablado hoy";
            return true;
        }
```

**D.4** En `Act()`, tras el caso `TargetKind.Board` (líneas 853-855):

```csharp
                case TargetKind.Tree:
                    {
                        // El texto que responde el árbol viaja por el bus (TreeSpoke) y
                        // lo pinta la interfaz; aquí no hay nada que enseñar.
                        if (ServiceRegistry.TryGet<ITreeService>(out var arbol))
                            arbol.TryTalk(out _);
                        break;
                    }
```

### E. `Scripts/Art/World/WorldView.cs:134` — que crezca

Tras `tree.SetParent(island, worldPositionStays: false);` (línea 136):

```csharp
            // Crece con el nivel de la isla. Misma fórmula que NimboTree.GrowthScale
            // (NimboTree.cs:79), que no se puede llamar desde aquí porque Nimbo.Art no
            // ve Nimbo.Island; ArbolNimboTests vigila que las dos partes no se separen.
            //
            // Se escala el árbol entero y no solo la copa: la malla de la copa lleva
            // los vértices cocidos alrededor de la base (IslandMeshBuilder.BuildTree la
            // teje a la altura que sale del tronco), así que escalar solo el hijo la
            // hundiría contra el tronco sin encoger este. Escalado desde el pie, el
            // conjunto lee como un árbol más joven, que es lo que la etapa cuenta.
            int nivel = Mathf.Clamp(_island != null ? _island.State.Level : 1,
                                    1, Data.World.IslandState.MaxLevel);
            tree.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, (nivel - 1) / 9f);
```

**Nota sobre el encargo**: pedía «escalar la copa»; propongo escalar el árbol entero
por lo dicho arriba (copa sola = masa flotando a media altura de un tronco de 17 m).
El colisionador del tronco es `MeshCollider` sobre la malla (`WorldView.cs:639`) y
sigue la escala del padre; el bulto de rodeo (4,5 m, `WorldView.cs:132`) se queda fijo
a propósito: es el espacio de paseo, no el tamaño del árbol.

### F. `Scripts/UI/UiRoot.cs` — que se lea

- Tras `EventBus.Subscribe<InteriorExited>(OnInteriorExited);` (línea 214):
  `EventBus.Subscribe<TreeSpoke>(OnTreeSpoke);`
- En `OnDisable`, junto a los demás (hay un bloque equivalente en líneas 61-72):
  `EventBus.Unsubscribe<TreeSpoke>(OnTreeSpoke);`
- Método, junto a `OnRequestBoardRead` (línea 412):

```csharp
        /// <summary>El Árbol Nimbo ha hablado: mismo cartelito que los logros.</summary>
        private void OnTreeSpoke(TreeSpoke evt) => _toast.Push("El Árbol Nimbo", evt.Text);
```

El toast va en cola y de uno en uno (`AchievementToast.cs:22`), que es justo lo que un
ritual de apertura necesita: una frase legible, no una pantalla.

### G. `Scripts/Island/NimboTree.cs` (mi fichero) — se aplica con A+B+C

1. Línea 50: `public sealed class NimboTree` → `public sealed class NimboTree : ITreeService`
2. Tras el `TryTalk(out TreeGift)` actual (línea ~121), el adaptador que ve la vista:

```csharp
        /// <summary>La forma que ve la vista: texto fuera, tipos de la isla dentro.</summary>
        public bool TryTalk(out string text)
        {
            bool spoke = TryTalk(out TreeGift gift);
            text = gift.Text;
            return spoke;
        }
```

3. Anunciar por el bus — en el rechazo (antes de `return false;`, línea ~104):

```csharp
                EventBus.Publish(new TreeSpoke(gift.Text, 0, null));
```

   y tras `Deliver(gift);` (línea ~119):

```csharp
            EventBus.Publish(new TreeSpoke(gift.Text, gift.Coins, gift.CatalogId));
```

## 4. Estado de las pruebas — VERDAD POR DELANTE

**No puedo decir todavía «las pruebas pasaron», y no lo digo.** Cuatro corridas de
`Tools/agentes/unity.sh EditMode arbol ArbolNimboTests` abortaron con
`Scripts have compiler errors`. Evolución real del bloqueo:

1.ª y 2.ª: `Scripts/Art/Camera/IslandCamera.cs(164)` CS1519 y `Scripts/UI/GateNotice.cs(100)`
CS0191 — ficheros de otros agentes; sus autores los arreglaron (desaparecieron de la 3.ª).
3.ª: uno mío — mi censo falso no cumplía `IIslanderRegistry` (`InZone` debía devolver
`IEnumerable<IslanderData>` y `Remove` ser `void`). Corregido en `ArbolNimboTests.cs`.
4.ª: **cero errores míos**; quedan solo pruebas de otros agentes, en sus ficheros:
   - `Tests/AcabadosDeViviendaTests.cs(255,260): CS1648` (readonly modificado)
   - `Tests/PlayMode/CamaraEnLaIslaTests.cs(185,197): CS0104` (`Debug` ambiguo)
   - `Tests/PlayMode/RopaEnLaIslaTests.cs(42,91,149,170): CS0103/CS0246/CS1503`

Unity compila el proyecto entero: mientras esas pruebas de terceros no compilen,
ninguna suite corre, ni la mía ni la de nadie. En la quinta corrida ya compiló y estas
son las cifras medidas; el filtro para mi suite es `ArbolNimboTests` (editor) y
`ArbolEnLaIslaTests` (juego).

### Resultado medido (XML en `Informes/pruebas/arbol-*.xml`)

| Suite | Resultado |
|---|---|
| Editor `ArbolNimboTests` | **9 pruebas · 9 pasadas · 0 fallos** |
| PlayMode `ArbolEnLaIslaTests` | **3 pruebas · 2 pasadas · 1 fallo** |

- Primera corrida del editor: 8/9 — mi prueba de entrega cobraba doble (el árbol
  candidato del bucle de búsqueda entregaba también en la economía compartida:
  «dijo 22, la cartera oyó 44»). Corregida la prueba, no el servicio: el servicio
  estaba bien.
- El fallo de PlayMode es **exactamente el esperado**: la costura sin aplicar.

  ```
  delante del árbol sale «None»: «». Falta el enganche D del informe-arbol.md en PlayerInteractor.cs
  Expected: "Tree" But was: "None"
  ```

  No falla por un accidente: el teletransporte funcionó (la aserción de caída pasó),
  los vecinos fueron apartados y el objetivo se refrescó. Falla porque
  `TargetKind.Tree` todavía no existe — que es precisamente lo que esta prueba tiene
  que delatar. Verde en cuanto se aplique D; su mensaje señala el paso exacto.

La línea base (514+92, cero rojos) no baja por mi causa salvo la prueba de costura 3,
que es deliberadamente el verificador del enchufe: es la prueba que este proyecto
aprendió a necesitar a golpes — sin ella, el árbol habría segundo «escrito, probado y
apagado», porque nada más delata un sistema sin llamadores.

## 5. Cómo se comprueba a mano

1. Cargar `Isla` y andar al centro de la plaza: cartel «Hablar con el Árbol Nimbo».
2. Pulsar E: toast del Árbol con su frase del día; si caen monedas, suben al momento.
3. Pulsar E otra vez: «El Árbol Nimbo ya ha hablado hoy» — y el toast lo dice también.
4. Dormir en la hamaca y volver: vuelve a dejar hablar. Tres días seguidos: el árbol
   nombra la racha («llevas 3 días sin faltar a la cita») y las monedas van subiendo
   hasta el techo del quinto día.
5. Subir el nivel de la isla (panel F1): el árbol entero —tronco y copa— se ve mayor.
