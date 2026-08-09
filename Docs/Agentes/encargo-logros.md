# Encargo: los logros (C#)

La isla no se acuerda de nada de lo que haces. Llevas cuarenta días, has casado a
dos vecinos y has abierto media isla, y el juego no te lo reconoce en ningún
sitio. Esto lo arregla: cuarenta y cuatro logros que la isla se va apuntando, con
recompensa en nimbos.

Es el punto «Sistema de logros» del `[IMPORTANTE]` del GDD.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` — manda sobre todo lo demás. Las tres reglas duras.
- `Assets/_Project/Scripts/Core/Services/Contracts/IAchievementService.cs` — **el
  contrato ya está escrito y cerrado**. Lo implementas tal cual. Si crees que una
  firma está mal, dilo antes de cambiarla; no la cambies por tu cuenta, porque yo
  estoy escribiendo la pantalla de logros contra ella al mismo tiempo.
- `Assets/_Project/Scripts/Economy/Items/ItemCatalog.cs` — copia de ahí la forma
  de cargar un catálogo desde `Resources`. Ya está resuelto y quiero que se parezcan.
- `Assets/_Project/Scripts/Simulation/Progression/WeeklyRhythm.cs` — vecino de
  carpeta. Mismo aire, mismo estilo.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Resources/Config/catalogo_logros.json`
2. `Assets/_Project/Scripts/Simulation/Progression/AchievementCatalog.cs`
3. `Assets/_Project/Scripts/Simulation/Progression/AchievementService.cs`
4. `Assets/_Project/Tests/AchievementTests.cs`

Ojo con la ruta del cuarto: las pruebas van en `Assets/_Project/Tests/`, **no**
en `Assets/_Project/Scripts/Tests/`, que no existe.

No toques nada más. Ni `IAchievementService.cs`, ni `AchievementRecord.cs`, ni
`SaveGame.cs`, ni `GameEvents.cs`, ni `GameBootstrap.cs`, ni ningún `.asmdef`, ni
`Docs/`. Yo registro tu servicio en el arranque y yo hago la pantalla. Tú entregas
la pieza.

## Lo que ya existe y no tienes que hacer

El hueco del guardado, en `SaveGame`:

```csharp
public List<AchievementRecord> Achievements = new List<AchievementRecord>();
```

Y el dato, en `Nimbo.Data.Save`:

```csharp
class AchievementRecord { string AchievementId; int Current; bool Unlocked; int UnlockedOnDay; }
```

El evento, en `Nimbo.Core.Events`, listo para publicar:

```csharp
public readonly struct AchievementUnlocked { string AchievementId; long Reward; }
```

`Nimbo.Simulation` ya referencia `Nimbo.Core` y `Nimbo.Data`. No hace falta tocar
el asmdef.

## Fichero 1 — `catalogo_logros.json`

**44 logros.** Misma forma que los otros catálogos: `{ "version": 1, "items": [...] }`.

Campos: `achievementId` (prefijo `logro_`, minúsculas, sin tildes ni eñes),
`displayName`, `description`, `kind`, `goal`, `reward`, `hidden`.

`kind` es uno de: `Life`, `Social`, `Home`, `Money`, `Play`, `Island`, `Odd`.

Reparto: 8 `Life`, 9 `Social`, 6 `Home`, 6 `Money`, 6 `Play`, 6 `Island`, 3 `Odd`.

`reward` de 50 a 2000 nimbos, en proporción a lo que cuesta. `goal` es 1 para los
de «pasó o no pasó» y el número a acumular para los demás.

Tres reglas sobre el contenido, y son las que separan una lista de logros buena de
un checklist:

1. **Nada de logros por dejar el juego abierto.** «Juega 100 horas» no es un logro,
   es una factura. Que midan cosas que el jugador decide.
2. **Escalones, no muros.** Si hay uno de «haz veinte amigos», que haya antes uno
   de tres y otro de diez. Que el primero de cada familia se consiga sin buscarlo.
3. Los `Odd` son los que le dan carácter a la lista: cosas raras que solo pasan si
   la isla está viva. Que uno se duerma en el parque, que dos vecinos con
   personalidades opuestas acaben siendo íntimos, que alguien rechace tres regalos
   seguidos. Esos tres van con `hidden: true`.

Las descripciones, en castellano y con gracia. «Se te ha dormido en el parque y no
has tenido corazón para despertarlo» sí. «Consigue que un habitante duerma fuera»
no, eso es la condición, no la descripción.

## Fichero 2 — `AchievementCatalog.cs`

En `Nimbo.Simulation.Progression`. Igual que `ItemCatalog`: carga el JSON en el
constructor, queda de solo lectura, y **tiene un segundo constructor que recibe el
JSON en crudo** para poder probarlo sin `Resources`. Ese segundo constructor no es
opcional: es lo que hace que las pruebas del fichero 4 se puedan escribir.

Si el JSON no carga, `Debug.LogError` y catálogo vacío. Nunca una excepción: una
lista de logros rota no puede impedir que arranque la partida.

## Fichero 3 — `AchievementService.cs`

En `Nimbo.Simulation.Progression`, implementa `IAchievementService`. Recibe por el
constructor el `AchievementCatalog`, el `SaveGame` y el `GameClock`.

Cómo cuenta: **se suscribe a los eventos que ya existen**. No le pide a nadie que
le avise, y nadie sabe que existe. Estos son los eventos disponibles, y no hay más:

```
IslanderCreated  IslanderMovedIn  IslanderLeft  NeedBandChanged  EmotionShown
HappinessChanged  RequestRaised  RequestResolved  RequestExpired
IslanderLeveledUp  AffinityChanged  FriendshipStageChanged  ConflictStageChanged
RomanceStageChanged  BabyBorn  CoinsChanged  ItemAcquired  ItemGifted
HourPassed  DayPassed  BuildingUnlocked  DecorPlaced  DecorRemoved  RoomEdited
```

Si un logro que se te ocurre no se puede contar con ninguno de esos, **no lo
pongas en el catálogo**. Un logro que nunca se consigue es peor que no tenerlo, y
no vas a añadir eventos: ese fichero no es tuyo.

Reglas del contador:

- `Advance` suma. `Record` deja el valor si es **mayor** que el que había; es para
  los que miden un máximo y no una suma.
- Los dos devuelven cierto **solo si el logro se ha desbloqueado justo en esa
  llamada**. Volver a llamar sobre uno ya desbloqueado devuelve falso y no vuelve a
  publicar el evento ni a pagar. Este es el fallo que hay que evitar: un logro que
  paga cada vez que sube el contador es dinero infinito.
- Al desbloquear se anota `UnlockedOnDay` con el día del reloj y se publica
  `AchievementUnlocked` con su recompensa. **El servicio no ingresa el dinero**: lo
  hace quien escuche el evento, que soy yo. Tú no tocas la economía.
- Un logro que no está en el guardado vale cero. **No siembres los 44 registros al
  arrancar**: solo se guarda lo que se ha tocado, para que añadir un logro nuevo no
  obligue a migrar las partidas.
- Un identificador que no está en el catálogo se ignora en silencio y no crea
  registro. Puede pasar si se quita un logro y una partida vieja lo tenía.

Y `Dispose`: quítate de todos los eventos a los que te hayas suscrito. El
`EventBus` guarda delegados, y un servicio muerto que sigue suscrito revienta al
volver al menú y recargar la escena.

## Fichero 4 — `AchievementTests.cs`

Espacio de nombres `Nimbo.Tests`, con `NUnit.Framework`. Un `SaveGame` de mentira
y el catálogo por el constructor de JSON crudo. **No cargues `Resources` en una
prueba.**

Como mínimo, y son las que voy a mirar:

1. Un logro nuevo empieza a cero y sin registro en el guardado.
2. `Advance` sube el contador y lo guarda.
3. Llegar al objetivo lo desbloquea, publica `AchievementUnlocked` una vez y
   devuelve cierto.
4. Seguir llamando a `Advance` después devuelve **falso** y **no** vuelve a
   publicar. (La del dinero infinito.)
5. `Record` con un valor menor que el que había no baja el contador.
6. `Record` con un valor mayor sí lo sube, y desbloquea si llega.
7. Un identificador desconocido no rompe nada y no crea registro.
8. `UnlockedCount` cuadra con lo desbloqueado.
9. Un logro sigue desbloqueado aunque le cambien el objetivo a uno mayor. (Por eso
   `Unlocked` se guarda aparte del contador.)
10. Después de `Dispose`, publicar un evento no cambia ningún contador.

## Cómo se comprueba que has terminado

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Achievement" -testResults /tmp/logros.xml \
  -logFile /tmp/logros.log
```

Sin `-quit`: con `-quit` Unity se cierra **antes** de correr las pruebas y te deja
un log verde que no ha probado nada. El resultado sale en el XML:

```
grep -oE 'total="[0-9]+" passed="[0-9]+" failed="[0-9]+"' /tmp/logros.xml
```

No des por buenas unas pruebas sin haber visto ese número.

**Aviso de coordinación:** puede haber otro proceso trabajando sobre este mismo
proyecto, y Unity coge un bloqueo exclusivo (`Temp/UnityLockfile`). No lances
`unity` hasta tener los cuatro ficheros escritos, y si te da un error de bloqueo,
espera y reintenta en vez de matar procesos de Unity.

## Lo que NO haces

- No pagas. Publicas el evento con la cifra y yo la ingreso.
- No haces la pantalla de logros. La hago yo.
- No añades eventos nuevos ni tocas otros módulos para poder contar algo.
- No siembras el guardado con los 44 logros a cero.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**. `// solo paga la primera vez o
sería dinero infinito` vale; `// comprueba si está desbloqueado` no vale y lo borro.
