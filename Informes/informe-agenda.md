# Informe: la agenda del día en la ficha de habitante

Agente: ox-alpha · Carpeta asignada: `Assets/_Project/Scripts/UI/Islander/` · Fecha: 2026-08-23

---

## 1. Lo que había (medido)

El GDD §2.2 promete que el jugador «ve la agenda del día» de un habitante.
**Esa vista no existía**: `rg "Su día|agenda" Assets/_Project/Scripts/UI` solo encontraba
usos de «agenda» como sinónimo del libro de relaciones (`RelationshipBook.cs:7`).

Y la información **está calculada**, pero no guardada:

- `IslanderBrain.Decide` (`Simulation/Behaviour/IslanderBrain.cs:58-95`) decide hora a
  hora con una escalera fija: sueño → energía crítica → hambre → aseo → compañía →
  paseo por personalidad. No escribe ninguna lista de planes; reacciona y borra.
- Las dos reglas estables y públicas son: la franja de sueño
  (`GameClock.IsSleepingHours`, `Core/Time/GameClock.cs:59` — de 23 a 7) y el paseo por
  personalidad (`IslanderBrain.WanderByPersonality`, `IslanderBrain.cs:121-139`):
  Actitud > 0,3 → zona social; si no, Visión > 0,3 → naturaleza; si no, Energía > 0,3 →
  ocio; si no, tiendas o casa al 50 %.
- Los ritmos de necesidad (cuándo saltará el hambre) viven en `NeedsConfig`
  (`Simulation/Needs/NeedsConfig.cs:31-34`), un ScriptableObject editable desde el
  inspector — inalcanzable desde Nimbo.UI y, sobre todo, no copiable sin poner números
  que un diseñador puede cambiar sin avisar.
- `Nimbo.UI.asmdef:4-8` referencia solo `Nimbo.Data`, `Nimbo.Core` y `Nimbo.Player`:
  **no ve** `Nimbo.Simulation`, donde vive el cerebro. Mismo motivo de ser que toda la
  carpeta `Core/Services/Contracts`.

## 2. Lo que he construido (mi carpeta)

### 2.1 `Scripts/UI/Islander/AgendaProjection.cs` — nuevo

La proyección pura del día, sin servicios ni escena:

| Pieza | Qué hace | De dónde sale |
|---|---|---|
| `Haunt(profile)` | dónde se le encuentra cuando no le urge nada (4 frases) | misma precedencia y umbral 0,3 que `WanderByPersonality` (`IslanderBrain.cs:126-129`) |
| `Intro(profile)` | la frase de carácter («La gente es su plan…») | derivada de la misma rama |
| `Day(profile, hasHome)` | dos tramos: `07–23` rato libre + `23–07` sueño | bordes de `GameClock.cs:59`; el texto de sueño distingue si tiene casa (`IslanderBrain.cs:112` solo lo manda a casa si tiene) |
| `Caveat` | «Si le entra hambre o mucho sueño, lo deja todo…» | honestidad: las interrupciones por necesidad NO se proyectan (ver §4) |

**Qué deliberadamente no proyecta**: horas de comida ni baños. Sus ritmos son datos de
inspector de otro ensamblado (§1); copiarlos era fabricar mentiras caducables. La
tarjeta lo dice en vez de fingirlo.

### 2.2 `Scripts/UI/Islander/AgendaSection.cs` — nuevo

La tarjeta «Su día», siguiendo los patrones de la ficha que dejó `nimbo-ficha`
(`TastesSection.cs`: tarjeta + título + refresco con firma; `SocialSection.cs:131`:
reloj vía `ServiceRegistry.TryGet<GameClock>`):

- Dos filas con columna de horas (`07–23`, `23–07`) — los momentos que importan, no 24
  filas.
- El tramo en curso va en tinta entera con «· ahora»: una agenda sin «ahora» no dice
  dónde estás dentro del día.
- Firma de reconstrucción = `islanderId|tramoActivo|introducción`: el refresco de 0,4 s
  no repinta nada salvo cambio de persona, de carácter o de tramo día/noche.
- Sin censo o sin vecino, la tarjeta se oculta (`DisplayStyle.None`), como hace
  `TastesSection`.

### 2.3 `Scripts/UI/Islander/IslanderPanel.cs` — tres líneas

Campo `_agenda`, alta en el constructor y llamada `_agenda.Refresh(_islanderId)` en
`Refresh()` (líneas 37, 108-113 y 172 tras el cambio).

### 2.4 Dónde puse la tarjeta, y por qué ahí

Entre **«Qué le gusta»** y **«Trabajo»**. El orden queda: peticiones → gestos sociales →
gustos → **su día** → trabajo → casa → con quién anda → mapa social.

- Arriba no: peticiones, gestos y gustos son **decisiones** que el jugador toma sobre el
  vecino, y eso iba arriba por diseño declarado en el propio panel
  (`IslanderPanel.cs:100-103`). La agenda es contexto, no decisión.
- Con el grupo de contexto vital, pero por delante de trabajo y casa: «qué hará hoy» se
  lee antes que su empleo.
- Coste en altura: una tarjeta más empuja las de abajo. La ficha lleva `ScrollView`
  desde la corrección del defecto nº 1 de la auditoría (`IslanderPanel.cs:21-24`), así
  que nada deja de ser alcanzable; el mapa social baja unas cinco filas. No toqué nada
  de lo que `nimbo-ficha` construyó.

### 2.5 Por qué dos vecinos se distinguen

La agenda sale de la personalidad por dos caminos a la vez: la frase de paseo (4 ramas)
y la introducción de carácter. Los dieciséis tipos canónicos reparten exactamente esas
cuatro ramas (`LosDieciseisTiposRepartenLosCuatroPlanes`), así que cualquier isla tiene
vecinos con días visiblemente distintos — y cuando dos comparten tipo, compartir día es
lo correcto: es lo que «tipo» significa.

## 3. Pruebas escritas

**Editor** — `Tests/AgendaDeLosVecinosTests.cs` (7 pruebas, lógica pura, sin escena):
cobertura de 24 h sin huecos (el sueño envuelve), cuatro caracteres → cuatro días,
los 16 tipos → las 4 ramas, frontera del umbral (0,3 exacto aún no es sociable, como en
el cerebro), determinismo (la misma personalidad da la misma agenda — sin esto, el
refresco de 0,4 s repintaría sin parar), el que no tiene casa duerme distinto, y el
envolvimiento del sueño marca bien las horas (23, 3, 6, 7).

**PlayMode** — `Tests/PlayMode/AgendaEnLaFichaTests.cs` (2 pruebas, cargan `Isla`):

1. `LaFichaAbiertaEnseniaElDiaDelVecino` — abre la ficha del primer vecino del censo y
   exige: título «Su día», la frase de paseo **de su personalidad real** (comparada con
   `AgendaProjection.Haunt(vecino.Personality)`), la franja de sueño y un «ahora».
2. `CambiarElCaracterCambiaLaAgendaEnElMismoRefresco` — siembra otra personalidad en el
   mismo vecino (precedente: `FichaEnLaIslaTests.Sembrar`) y exige que la tarjeta cambie
   en el siguiente `Refresh()`. Es la prueba de que lee la simulación de verdad y no
   enseña un texto fijo.

No dependen del azar del censo: la isla nueva trae solo 3 vecinos
(`GameBootstrap.cs:55`) y exigir dos caracteres distintos entre ellos fallaría ~1 de
cada 7 veces (P(de que los tres compartan rama) ≈ 0,5³+0,25³+2·0,125³ ≈ 14,5 %).

## 4. La costura — la aplica el orquestador

Lo que hay hoy funciona y está probado, pero la proyección vive en la interfaz y copia
dos constantes del cerebro (umbral 0,3 y bordes 23/7). El arreglo definitivo es mover la
proyección a donde está el cerebro y serviría por contrato:

**A. `Scripts/Core/Services/Contracts/IAgendaService.cs` — fichero nuevo**

```csharp
using System.Collections.Generic;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Un tramo del día de un vecino, ya con texto en español.</summary>
    public readonly struct AgendaBlock
    {
        public readonly int StartHour; // inclusiva
        public readonly int EndHour;   // exclusiva; puede envolver (23 → 7)
        public readonly string Text;

        public AgendaBlock(int startHour, int endHour, string text)
        { StartHour = startHour; EndHour = endHour; Text = text; }

        public bool Includes(int hour)
        {
            if (StartHour < EndHour) return hour >= StartHour && hour < EndHour;
            return hour >= StartHour || hour < EndHour;
        }
    }

    /// <summary>
    /// La agenda del día de cada vecino. Vive en Core porque quien la lee es la vista
    /// (Nimbo.UI) y Nimbo.UI no ve Nimbo.Simulation, donde se planifica el día.
    /// </summary>
    public interface IAgendaService
    {
        /// <summary>
        /// Su día en tramos, en orden y cubriendo las 24 horas. Proyección del hábito:
        /// las urgencias por necesidad pueden interrumpirla y no se prometen horas.
        /// </summary>
        IReadOnlyList<AgendaBlock> DayOf(string islanderId);
    }
}
```

**B. `Scripts/Simulation/Behaviour/AgendaPlanner.cs` — fichero nuevo (implementación)**

Mover ahí `AgendaProjection` (es puro, no tocará nada) y, ya dentro de Simulation,
**dejar de copiar**: leer `NeedsConfig.DecayPerHour` para proyectar cuándo el hambre
cruza `NeedBand.Low` (`NeedState.LowThreshold`, `Data/Islanders/NeedState.cs:43`) e
insertar los tramos de comida de verdad; y leer el turno real si el vecino tiene empleo
(hoy `JobService` conoce `entry.StartHour` y `entry.Hours`, `JobService.cs:75,200`, pero
no los expone — si se quieren turnos en la agenda, añadir a `IJobService`
`bool TryGetShift(string islanderId, out int startHour, out int hours)`).

**C. `Scripts/Game/Bootstrap/GameBootstrap.cs:333` — registro**

Tras crear el cerebro:

```csharp
            ServiceRegistry.Register<IAgendaService>(_agenda);
```

**D. `Scripts/UI/Islander/AgendaSection.cs` — el consumidor**

Primera línea de `Refresh`:

```csharp
            if (ServiceRegistry.TryGet<IAgendaService>(out var agenda))
            { /* pintar agenda.DayOf(islanderId) */ }
```

y borrar la llamada local a `AgendaProjection`. Mientras no exista el contrato, la
tarjeta ya funciona con la proyección local — por eso esta costura no bloquea a nadie.

**E. Prueba de la costura** (cuando A-D existan): que `DayOf` de dos vecinos de tipos
distintos dé textos distintos y cubra 24 h — los mismos asserts de mis pruebas de
editor, contra el servicio registrado.

## 5. Estado de las pruebas — resultado medido

XML en `Informes/pruebas/agenda-EditMode.xml` y `agenda-PlayMode.xml`.

| Suite | Resultado |
|---|---|
| Editor `AgendaDeLosVecinosTests` | **7 pruebas · 7 pasadas · 0 fallos · 0 saltadas** |
| PlayMode `AgendaEnLaFichaTests` | **2 pruebas · 2 pasadas · 0 fallos · 0 saltadas** |

Por nombre, las de juego contra la escena `Isla` real: `LaFichaAbiertaEnseniaElDiaDelVecino`
y `CambiarElCaracterCambiaLaAgendaEnElMismoRefresco` — ambas verdes. La segunda es la
que demuestra que la tarjeta lee los datos del vecino: sembrada otra personalidad, el
mismo `Refresh()` enseña otro día.

**Lo que costó llegar aquí (anotado, no arreglado):** cuatro corridas abortaron con
`Scripts have compiler errors` — **cero errores en ficheros míos** en todas
(`rg "error CS" Informes/pruebas/agenda-EditMode.log`). Los bloqueos fueron:
`ConversationRecall.cs` y `CreatorMannequin.cs` (nimbo-memoria y nimbo-creador; sus
autores los arreglaron y desaparecieron de la corrida siguiente) y
`FiestaDecoradaEnLaIslaTests.cs` sin `using UnityEngine.TestTools`
(nimbo-fiestas; arreglado igual). Unity compila el proyecto entero: mientras una prueba
de otro agente no compile, ninguna suite corre, ni la mía ni la de nadie. Una quinta
corrida se quedó 45 min en la cola del `flock` detrás de las suites de los demás y mi
espera se agotó antes de entrar; la sexta entró y dio los números de arriba.

La línea base (514 editor + 92 juego) no baja por mi causa: 9 pruebas nuevas, 9 verdes,
ninguna existente tocada salvo las tres líneas de enganche en `IslanderPanel.cs`.

## 6. Cómo se comprueba a mano

1. Cargar `Isla` y abrir la ficha de cualquier vecino (clic sobre él).
2. Tarjeta «Su día»: frase de carácter, fila `07–23` con su sitio favorito y fila
   `23–07` de sueño; el tramo que esté pasando va marcado «· ahora».
3. Abrir las fichas de dos vecinos de carácter distinto (un sociable y un casero, por
   ejemplo): los días tienen que notarse distintos.
4. Esperar a que el reloj cruce las 23:00 (o cargar partida de noche): el «· ahora»
   salta a la fila del sueño.
