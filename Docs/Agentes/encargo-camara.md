# Encargo: la cámara de la isla (C#)

Haces que la cámara deje de ser un trípode clavado en el suelo. Ahora mismo la
escena tiene una cámara fija mirando la isla desde arriba y no se mueve nunca;
tiene que poder orbitar, acercarse, apartarse, y sobre todo **acercarse a mirar a
un habitante cuando el jugador abre su ficha**.

Ese último punto es el que importa. El resto es el andamio para que funcione.

## Lee esto antes de escribir una línea

- `Docs/00_ARQUITECTURA.md` — manda sobre todo lo demás. Las tres reglas duras.
- `Assets/_Project/Scripts/Art/World/WorldView.cs` — de ahí sacas dónde está cada
  cosa. **No lo edites**, solo lee su API pública.
- `Assets/_Project/Scripts/Core/Events/GameEvents.cs` — los eventos. Tampoco lo edites.

## Ficheros que escribes — y ningún otro

1. `Assets/_Project/Scripts/Art/Camera/CameraRig.cs`
2. `Assets/_Project/Scripts/Art/Camera/IslandCamera.cs`
3. `Assets/_Project/Scripts/Tests/CameraRigTests.cs`

No toques nada más. Ni `WorldView.cs`, ni `UiRoot.cs`, ni ningún `.asmdef`, ni
`Assets/_Project/Editor/`, ni los `Docs/`. Hay otro agente y yo trabajando en
paralelo sobre este mismo repositorio y cualquier otro fichero que toques es un
conflicto.

## Lo que ya existe y no tienes que hacer

`Nimbo.Art` ya referencia `Nimbo.Core` y `Nimbo.Data`. No hace falta tocar el asmdef.

En `WorldView` tienes, ya escrito y garantizado:

```csharp
public float IslandRadius { get; }                                  // 100 por defecto
public bool TryGetIslander(string islanderId, out Transform body);  // dónde anda alguien
public bool TryGetZoneCentre(string zoneId, out Vector3 centre);    // el centro de una zona
```

En `Nimbo.Core.Events` tienes, ya escrito:

```csharp
public readonly struct IslanderFocused { public readonly string IslanderId; }
public readonly struct GameLoaded { }
public readonly struct GamePaused { public readonly bool Paused; }
```

`IslanderFocused` se publica **cuando el jugador abre la ficha de un habitante**, con
su identificador, y **cuando la cierra**, con la cadena vacía. Eso es tu disparador.

## Aviso importante sobre la entrada

Este proyecto tiene `activeInputHandler: 0`, o sea **solo el sistema de entrada
antiguo**. Usa `UnityEngine.Input` (`Input.GetAxis`, `Input.mousePosition`,
`Input.GetMouseButton`, `Input.mouseScrollDelta`). Si escribes
`UnityEngine.InputSystem.Keyboard.current` **el juego lanza una excepción en cuanto
alguien toque una tecla**, porque ese backend está apagado. No lo uses.

## Fichero 1 — `CameraRig.cs`

Una clase normal de C#, **sin `MonoBehaviour` y sin tocar la escena**. Es la que
tiene toda la matemática, y es la que se puede probar. Va en el espacio de nombres
`Nimbo.Art.CameraWork` (no `Nimbo.Art.Camera`: un espacio de nombres llamado
`Camera` choca con `UnityEngine.Camera` y te vas a pasar el día peleándote con
`global::`).

Guarda un estado y lo lleva suavemente hacia otro:

```csharp
public struct CameraPose
{
    public Vector3 Pivot;    // el punto que se mira
    public float Distance;   // a cuánto está la cámara de ese punto
    public float Yaw;        // giro horizontal, en grados
    public float Pitch;      // inclinación, en grados; positiva mira hacia abajo
}
```

Lo que tiene que ofrecer:

- `CameraPose Current { get; }` — dónde está de verdad ahora.
- `CameraPose Target { get; set; }` — a dónde va.
- `void Advance(float deltaSeconds)` — acerca `Current` a `Target`. Suavizado
  exponencial, **no lineal**: `t = 1 - Mathf.Exp(-sharpness * dt)`. Con lineal la
  cámara arranca de golpe y con `Mathf.Lerp(a, b, 0.1f)` a pelo la velocidad
  depende de los fotogramas por segundo, que es un error clásico.
- `void Frame(Vector3 point, float distance)` — pone el objetivo en ese punto.
- `void Orbit(float deltaYaw, float deltaPitch)` y `void Zoom(float delta)` —
  mueven el objetivo, respetando los topes.
- `void Pan(Vector3 worldDelta)` — desplaza el pivote por el plano del suelo.
- `Vector3 Position { get; }` y `Quaternion Rotation { get; }` — lo que se le pone
  a la cámara de Unity, calculado desde `Current`.
- `void SnapToTarget()` — sin transición. Hace falta al cargar la partida: la
  cámara no puede entrar volando desde el infinito en el primer fotograma.

Topes, y son duros:

- `Pitch` entre 12° y 78°. Por debajo se ve el borde del mundo, por encima es
  una vista cenital que marea.
- `Distance` entre 8 y 220.
- El pivote se queda dentro de un círculo de radio `IslandRadius * 1.1f` alrededor
  del origen. Si no, el jugador se va a pasear por la nada y no sabe volver.
- `Yaw` se normaliza a [0, 360) y **gira por el lado corto**: ir de 350° a 10° son
  20 grados, no 340. Si esto se te olvida, enfocar a alguien hace que la cámara dé
  una vuelta entera de más, y se nota muchísimo.

## Fichero 2 — `IslandCamera.cs`

El `MonoBehaviour`, en `Nimbo.Art.CameraWork`. Va en el mismo objeto que la
`Camera`, así que lleva `[RequireComponent(typeof(Camera))]`.

Se suscribe en `OnEnable` y **se da de baja en `OnDisable`**, sin excepción: el
`EventBus` guarda delegados y un suscriptor muerto que no se dio de baja peta al
recargar la escena.

Comportamiento:

**Al cargar (`GameLoaded`)** — plano general: pivote en el centro de la isla,
distancia 150, pitch 45°, yaw 0. Y `SnapToTarget()`.

**Al enfocar (`IslanderFocused` con identificador)** — busca el habitante con
`WorldView.TryGetIslander`. Si está, pivote en su cabeza (su posición + 1,1 en Y),
distancia 14, pitch 22°. El yaw **no se toca**: girar alrededor del muñeco además
de acercarse marea y no aporta nada.

Y aquí está el detalle que hay que hacer bien: mientras alguien esté enfocado, el
pivote **sigue al habitante fotograma a fotograma**, porque los habitantes andan.
Si solo lo apuntas una vez, el muñeco se sale del plano a los tres segundos.
Guarda el `Transform` y actualiza el objetivo en el `LateUpdate`, no en `Update`:
si lo haces en `Update` la cámara se coloca con la posición del fotograma anterior
y el muñeco tiembla.

Si el habitante desaparece (se ha ido de la isla), vuelve al plano general en vez
de quedarte apuntando a un `Transform` destruido. `if (target == null)` funciona
con los objetos de Unity destruidos; úsalo.

**Al dejar de enfocar (`IslanderFocused` con cadena vacía)** — vuelve al plano
general, con transición, no de golpe.

**Mandos**, y solo con el sistema antiguo:

- Botón derecho arrastrando, o botón central: orbita.
- Rueda del ratón: acerca y aleja. Multiplica el paso por la distancia actual, o
  de lejos va a paso de tortuga y de cerca se pega un salto.
- `WASD` o las flechas: desplaza el pivote. **En el plano de la cámara**, no en el
  del mundo: si el jugador ha girado la vista 90°, la W tiene que seguir yendo
  «hacia arriba en la pantalla». Es proyectar el `forward` de la cámara sobre el
  plano Y=0 y normalizarlo.
- Cualquiera de los tres cancela el seguimiento de un habitante: si el jugador
  mueve la cámara a mano, manda él. Sigue enfocado a efectos de la ficha, pero la
  cámara deja de perseguirlo.

**Con `GamePaused(true)` la cámara no responde a los mandos.** Usa
`Time.unscaledDeltaTime` para `Advance`, para que la transición que estuviera en
marcha termine con el juego pausado en vez de congelarse a medias.

## Fichero 3 — `CameraRigTests.cs`

Espacio de nombres `Nimbo.Tests`, con `NUnit.Framework`. Pruebas de la matemática
de `CameraRig`, que para eso está separada. **Ni una prueba de `IslandCamera`**:
necesitaría una escena y no es lo que pago aquí.

Como mínimo, y son las que voy a mirar:

1. `Advance` acerca `Current` a `Target` y nunca lo pasa de largo.
2. Con `deltaSeconds` grande (medio segundo) tampoco se pasa ni oscila.
3. Llamar a `Advance` sesenta veces con `dt = 1/60` deja la cámara **más o menos
   donde** la deja llamarlo seis veces con `dt = 1/6`. Esta es la prueba de que el
   suavizado es independiente de los fotogramas; con un `Lerp` a pelo falla.
4. El pitch se queda entre 12 y 78 aunque le pidas 500 o -90.
5. La distancia se queda entre 8 y 220.
6. El pivote no se sale del círculo permitido.
7. Yendo de yaw 350 a yaw 10, en ningún momento intermedio el yaw pasa por 180.
   Esta es la del camino corto y es la que se suele suspender.
8. `SnapToTarget` deja `Current` exactamente igual a `Target`.
9. `Position` y `Rotation` son coherentes: la cámara mira al pivote, o sea que
   `Rotation * Vector3.forward` apunta de `Position` a `Current.Pivot`, con un
   margen de una milésima.

## Cómo se comprueba que has terminado

Compila y pasan las pruebas:

```
unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testFilter "CameraRig" -testResults /tmp/camara.xml \
  -logFile /tmp/camara.log
```

Sin `-quit`: con `-quit` Unity se cierra **antes** de correr las pruebas y te
deja un log verde que no ha probado nada. El resultado sale en el `.xml`.

Si no tienes el envoltorio `unity` en el `PATH`, dilo y te paso la ruta; **no te
inventes una ruta a un Unity que no existe ni te saltes las pruebas**.

## Lo que NO haces

- No tocas `WorldView.cs` ni `UiRoot.cs`. La API que necesitas ya está puesta.
- No metes Cinemachine. Está instalado pero no se usa, y para esta cámara es un
  peso muerto: son cuarenta líneas de matemática y así se pueden probar.
- No haces detección de clic en el mundo (`Physics.Raycast` sobre los muñecos).
  Eso viene después y es de otro encargo; ahora mismo los habitantes ni siquiera
  tienen colisionador.
- No cambias la escena. La monto yo con `SceneBuilder`; tú entregas los
  componentes y yo los coloco.

## Cómo quiero el código

En castellano, comentando **por qué** y no **qué**. `// gira por el lado corto o
la cámara da la vuelta entera` vale; `// asigna yaw` no vale y lo borro.
