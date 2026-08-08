# Encargo: el módulo de eventos y espectáculo (C#)

Implementas lo que hace que la isla tenga días distintos entre sí: conciertos, sueños,
viajes, el tablón de noticias y los sucesos aleatorios. Sin esto el juego es una tabla
de necesidades que baja; con esto, un sitio donde pasan cosas.

## Lee esto antes de escribir una línea

1. `Docs/00_ARQUITECTURA.md` — el contrato. Manda sobre todo.
2. `Docs/01_GDD.md`, sección 9 («Eventos y espectáculo») y sección 10 («Estructura de
   una semana de juego») — de ahí sale QUÉ eventos hay.
3. `Assets/_Project/Scripts/Core/Events/GameEvents.cs` — los avisos que ya existen.
   **No añadas ninguno**: si necesitas uno nuevo, lo pides en el informe.
4. `Assets/_Project/Scripts/Core/Services/Contracts/` — los servicios que puedes usar.
   Se piden con `ServiceRegistry.Get<IX>()`, nunca se construyen.
5. `Assets/_Project/Scripts/Simulation/Requests/RequestConfig.cs` — el patrón de
   ScriptableObject de configuración que tienes que seguir.

## Ficheros que escribes — y ningún otro

Todos bajo `Assets/_Project/Scripts/Events/`:

- `Scheduling/EventDefinition.cs` — un evento: id, nombre, cuándo puede salir, requisitos
- `Scheduling/EventCalendar.cs` — qué eventos hay y cuál toca; el catálogo en código
- `Scheduling/EventScheduler.cs` — se suscribe a `HourPassed` y `DayPassed` y dispara
- `Scheduling/EventsConfig.cs` — ScriptableObject con las probabilidades y los aforos
- `Shows/ConcertEvent.cs` — el concierto en el escenario
- `Dreams/DreamEvent.cs` — el sueño nocturno de un habitante
- `News/NewsBoard.cs` — el tablón: titulares generados de lo que ha pasado de verdad
- `EventsService.cs` — la fachada que monta y expone lo anterior

Y los tests, en `Assets/_Project/Tests/EventsTests.cs`.

**No toques nada fuera de esas rutas.** Ni `Core/`, ni `Data/`, ni `Docs/`, ni los otros
módulos. Hay más agentes trabajando a la vez.

## Qué tiene que hacer cada pieza

**EventCalendar** — al menos 12 eventos distintos. Como mínimo estos, y los que se te
ocurran del GDD: concierto, festival de la isla, cumpleaños de un habitante, día de
lluvia, tormenta, aurora, mercadillo, competición de talentos, noche de estrellas,
sueño compartido, llegada de un visitante, y el «puente aparece» que abre el embarcadero.

**EventScheduler** — decide cuál toca. Reglas:
- Cada evento tiene una ventana horaria y un día de la semana o «cualquiera».
- Un evento con requisitos que no se cumplen no se sortea (zona cerrada, pocos
  habitantes, nivel insuficiente).
- **Nunca dos eventos a la vez.** Si uno está en marcha, no empieza otro.
- Los cumpleaños no se sortean: se comprueban contra `IslanderIdentity.Birthday`,
  que está en formato `"MM-DD"`.

**ConcertEvent** — elige al habitante con más nivel entre los que estén libres, reúne
público en la zona del escenario, y al terminar reparte ánimo y afinidad entre los
asistentes. Quien más disfruta es quien tenga el eje `Expression` alto.

**DreamEvent** — le pasa a un habitante dormido. El sueño va sobre alguien de su agenda
(su flechazo, su mejor amigo o con quien está reñido) y al despertar mueve la afinidad
con esa persona. Los soñadores (eje `Outlook` alto) sueñan más a menudo.

**NewsBoard** — se suscribe a los eventos del juego (`RomanceStageChanged`,
`IslanderLeveledUp`, `BabyBorn`, `BuildingUnlocked`, `ConflictStageChanged`) y guarda
titulares en español con el nombre real de quien salga. Guarda los **20 últimos** y
tira los viejos. Los titulares se escriben con plantillas: da dos o tres redacciones
distintas por tipo de suceso para que no cansen.

## Las trampas

1. **`EventBus.Subscribe` obliga a `Unsubscribe`.** Todo lo que se suscriba implementa
   `IDisposable` y se da de baja ahí. Un suscriptor zombi que sobrevive a una recarga
   de partida es un fallo que luego cuesta días encontrar.
2. Las cuatro necesidades son `Hunger`, `Energy`, `Social`, `Hygiene`. **`Mood` no es
   una necesidad**, es `MoodState` y se toca con `ISimulationService.ApplyHappiness`.
3. `Rng` es un `struct` con estado mutable: pásalo con `ref` o guárdalo en un campo.
   Por valor te devolverá siempre lo mismo.
4. Para el azar reproducible usa `Rng.FromSeed(...)`. Para lo que debe cambiar en cada
   partida, `Rng.FromTime()`.
5. **No escribas en `IslanderData` directamente.** Ni necesidades, ni ánimo, ni
   afinidad. Todo por `ISimulationService` y `ISocialService`, o los avisos de cambio
   de umbral no salen y la cara del habitante no se entera.
6. Los nombres de habitante para los titulares salen de `Identity.ShortName`.

## Los tests

`EventsTests.cs`, con NUnit. Como mínimo:

- el calendario tiene al menos 12 eventos y ningún id repetido
- un evento cuya zona está cerrada no se sortea nunca
- con un evento en marcha, el planificador no arranca otro
- el cumpleaños salta el día que toca y no el anterior
- el tablón guarda como mucho 20 titulares y tira el más viejo
- un titular contiene el nombre del habitante al que se refiere

No hace falta que los ejecutes: no tienes Unity. Escríbelos bien y yo los corro.

## Formato de respuesta al terminar

```
FICHEROS: <lista con líneas de cada uno>
LOS EVENTOS: <id y una línea de qué hace cada uno>
EVENTOS QUE PEDIRÍAS: <avisos nuevos que te habrían hecho falta en GameEvents.cs>
DECISIONES: <lo que tuviste que decidir tú>
```
