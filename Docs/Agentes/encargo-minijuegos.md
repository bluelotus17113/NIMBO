# Encargo: los tres minijuegos

El GDD pide tres minijuegos en `[NÚCLEO]`: **cocina, pesca y ritmo**. Son la
recompensa activa del juego: lo único donde el jugador hace algo con las manos en vez
de decidir. Los tres son de lógica pura, sin gráficos: tú haces las reglas y el
marcador, y la parte visual va aparte.

## Lee esto antes de escribir una línea

1. `Docs/00_ARQUITECTURA.md` — el contrato. Manda sobre todo.
2. `Assets/_Project/Scripts/Core/Services/Contracts/` — los servicios que puedes usar.
   Se piden con `ServiceRegistry.Get<IX>()`, nunca se construyen.
3. `Assets/_Project/Scripts/Simulation/Requests/RequestConfig.cs` — el patrón de
   ScriptableObject de configuración que tienes que seguir.
4. `Assets/_Project/Scripts/Core/Util/Rng.cs` — el azar. Es un `struct` con estado:
   se pasa con `ref` o se guarda en un campo.

## Ficheros que escribes — y ningún otro

Todos bajo `Assets/_Project/Scripts/Events/Minigames/`:

- `MinigameResult.cs` — el resultado: puntos, aciertos, fallos, monedas y experiencia
- `IMinigame.cs` — la interfaz común: `Start`, `Step`, `Finish`, `IsOver`
- `CookingGame.cs` — cocina
- `FishingGame.cs` — pesca
- `RhythmGame.cs` — ritmo
- `MinigameConfig.cs` — ScriptableObject con los números de los tres

Y los tests, en `Assets/_Project/Tests/MinigameTests.cs`.

**No toques nada más.** Ni `Core/`, ni `Data/`, ni el resto de `Events/`, ni ningún
otro módulo. Hay más agentes trabajando a la vez.

## Cómo funciona cada uno

Los tres son **por turnos y deterministas dados una semilla**. Nada de corrutinas ni
de `Time.deltaTime`: el juego llama a `Step(entrada)` y el minijuego responde. Así se
pueden probar sin abrir Unity y la parte visual se puede cambiar sin tocarlos.

**CookingGame** — se sirven `N` pasos de una receta. En cada paso hay 3 acciones
posibles (`Chop`, `Stir`, `Season`) y solo una es la correcta; el minijuego dice cuál
toca con una pista, y el jugador elige. Acertar suma; fallar resta y gasta un intento.
Termina al completar la receta o al gastar 3 fallos. La dificultad sube el número de
pasos.

**FishingGame** — hay un pez que tira con una fuerza que cambia cada turno, y una
tensión de sedal de 0 a 100. El jugador elige `Reel` (recoger) o `Wait` (aguantar).
Recoger acerca el pez pero sube la tensión; aguantar la baja. Si la tensión llega a
100 el pez escapa. Se gana al acercar el pez a 0 de distancia.

**RhythmGame** — una secuencia de notas con su momento. El jugador manda `Hit` con un
desfase en milisegundos, y se puntúa según lo cerca que esté: `Perfect` (±50 ms),
`Good` (±120 ms), `Miss`. La puntuación final sale de la racha máxima y del número de
perfectos.

## Reglas comunes

1. Todos devuelven un `MinigameResult` al terminar, con `Coins` y `Experience` ya
   calculados a partir de la puntuación y de la configuración.
2. **El minijuego no toca nada del juego.** No da monedas, no sube experiencia, no
   publica eventos. Devuelve el resultado y quien lo llamó decide. Eso es lo que hace
   que se puedan probar solos.
3. La dificultad es un `int` de 1 a 5 y entra por `Start`. Documenta qué cambia en
   cada uno al subirla.
4. La semilla entra por `Start` también: la misma semilla y las mismas entradas tienen
   que dar exactamente el mismo resultado. Es lo que permite probarlos.
5. Nada de `UnityEngine.Random`: se usa `Nimbo.Core.Util.Rng`.

## Los tests

`MinigameTests.cs`, con NUnit. Como mínimo:

- los tres terminan siempre: un bucle de 500 pasos con entradas al azar nunca se
  queda colgado (`IsOver` acaba en true)
- la misma semilla y las mismas entradas dan el mismo resultado, dos veces seguidas
- en cocina, tres fallos terminan la partida
- en pesca, la tensión al máximo hace escapar al pez y el resultado es derrota
- en ritmo, un `Hit` con desfase 0 puntúa `Perfect`
- ningún resultado da monedas o experiencia negativas

No hace falta que los ejecutes: no tienes Unity. Escríbelos bien y yo los corro.

## Formato de respuesta al terminar

```
FICHEROS: <lista con líneas de cada uno>
LAS TRES REGLAS: <en una línea cada minijuego, cómo se gana>
DIFICULTAD: <qué cambia del nivel 1 al 5 en cada uno>
DECISIONES: <los números que inventaste y con qué criterio>
```
