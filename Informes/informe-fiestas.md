# Informe: el decorado de las fiestas

Agente: ox-alpha · Carpeta asignada: `Assets/_Project/Scripts/Art/World/` · Fecha: 23-24/08/2026

---

## 1. Lo que había (medido)

- `VillageEventStarted` y `VillageEventEnded` se publican en el bus desde
  `EventScheduler.StartEvent` (`Scripts/Events/Scheduling/EventScheduler.cs:162`) y
  `FinishActiveEvent` (`:80`). Los structs viven en Core
  (`Scripts/Core/Events/GameEvents.cs:369-384`) justo para que los módulos lejanos
  puedan oírlos.
- **Quién los escuchaba**: `rg -l "Subscribe<VillageEvent"` daba un solo fichero,
  `AudioDirector.cs:100-101` — la música cambia de humor cuando hay fiesta
  (`AudioDirector.cs:206-216`). La interfaz no se suscribe a ninguno de los dos.
  `WorldView`, que es quien pinta el mundo, se suscribe a ocho eventos
  (`WorldView.cs:59-72,103-110`) y ninguno era de fiesta.
- La simulación del festival estaba entera: organizar y pagar
  (`VillageEvents.cs:78-98`), flechazos y afinidad por coincidir
  (`EventSparks.cs:70-114`) y el extra de ánimo por tocar bien en el concierto
  (`MinigameService.cs:237-252`).
- Montar un festival cuesta **1.200 monedas** frente a las 300 de una merienda
  (`VillageEvents.cs:28-29`). Ese pago no compraba ningún cambio visible en el mundo:
  durante el festival la isla era idéntica a un martes cualquiera.

## 2. Qué he elegido y por qué

**El decorado.** Dos razones medidas, no de gusto:

1. Es lo que puedo cerrar entero desde mi carpeta. «Los vecinos van» lo decide
   `IslanderBrain` (`Scripts/Simulation/Behaviour/IslanderBrain.cs`), que no es mío:
   hacerlo yo habría sido la costura sin aplicar, es decir, nada.
2. Es la mitad que faltaba del contrato implícito del pago: la música ya reacciona
   (`AudioDirector`), el mundo no. Un festival que solo se oye y no se ve es un cambio
   de banda sonora, no una fiesta.

La otra idea no la tiro: está especificada como costura en la sección 4, con línea y
texto propuesto, lista para que la aplique el orquestador.

## 3. Lo que he aplicado yo (mi carpeta)

Dos ficheros nuevos, cero líneas tocadas en ficheros ajenos:

### 3.1 `Scripts/Art/World/FestivalDecorBuilder.cs` — el decorado en sí

| Pieza | Números | Por qué así |
|---|---|---|
| Anillo de mástiles | radio **10 m**, 6 mástiles de 3,2 m (`FestivalDecorBuilder.cs:39`) | Tiene que caber dentro de las dos zonas donde hoy hay decorado: plaza 18 m y escenario 16 m de radio (`IslandLayout.cs:27,89`), con margen para el edificio central. `DecoradoDeFiestaTests.ElAnilloCabeEnLasZonasQueDecora` vigila que siga cabiendo si alguien retoca el plano. |
| Cuerdas | parábola con comba de 0,8 m, en 4 tramos rectos | Una cuerda tensa se lee como un error de colocación; una sola caja inclinada marcaría las puntas en el aire. |
| Banderines | 48 (8 por tramo), triángulo de 0,60×0,62 m, **3 colores = los de las flores del prado** (`ToonPalette.Flowers`) | Es la paleta festiva que la isla ya tiene; inventar colores nuevos es la forma segura de que lo nuevo parezca de otro juego. |
| Farolillos | 12, esfera de 0,34×0,40 m, amarillo cálido `0xF2C96B` | El mismo amarillo de la bombilla de las farolas (`DecorMeshBuilder.cs:147`): familia «luz» reconocible. |
| Macetas | 4, **reutilizando la malla del catálogo** (`DecorMeshBuilder.For(DecorKind.Plant)`) | El anillo vacío se lee como un corral desde la cámara alta; el color sale de `ColorOf("fiesta_maceta_i", Plant)`, determinista como el resto. |

Todo combinado en **6 mallas nuevas por fiesta** (madera, farolillos y uno por color de
banderín ≈ 2.000 vértices): se generan al empezar y **se destruyen al recoger**
(`FestivalDecor.TearDown`), así que no hay caché que mantener viva entre escenas. Los
materiales salen de la caché compartida de `ToonPalette`; sin colisionadores, igual que
todos los adornos pequeños (`WorldView.cs:577-583`).

### 3.2 `Scripts/Art/World/FestivalDecor.cs` — quien oye el bus y coloca/recoge

- **Se arranca solo**: `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`
  (`FestivalDecor.cs:69`), el mismo camino de `GateNoticeHost.cs:87`. Ni la escena ni
  el bootstrap saben que existe — no hizo falta tocar un fichero ajeno para tenerlo
  vivo. No tiene `Update`: colocar y recoger son operaciones secas, solo hay bus.
- **Evento → zona por propósito, no por identificador** (`FestivalDecor.cs:45`):
  `island_festival → Social`, `concert`/`talent_show → Leisure`. El aviso del bus lleva
  el id del evento pero no su zona, y la zona vive en `Nimbo.Events`, que Nimbo.Art no
  ve. Escribir `"zona_plaza"` a mano es exactamente cómo empezó el fallo histórico de
  «stage» y «plaza_central» (documentado en `FiestasTests.cs:205-224`): aquí el
  propósito se resuelve contra `IIslandService.ZoneIds`/`PurposeOf`, que trae el plano
  real (`IslandService.cs:44` consume `IslandLayout.FirstIsland()`). Que la tabla y el
  calendario no se separen lo vigila
  `DecoradoDeFiestaTests.LaTablaDelDecoradoSigueAlCalendario` en las dos direcciones:
  evento nuevo con zona de fiesta y sin línea en la tabla = prueba roja, y clave zombi
  de un evento borrado = prueba roja.
- **El centro sale de quien manda la colocación**: `IBuildService.TryGetWorldCentre`
  (`BuildService.cs:76-89`), la misma fuente que usan las agendas de los vecinos
  (`IslandService.cs:174`). Si el jugador movió el escenario, el anillo sigue al
  escenario.
- **Guardas**: evento desconocido, sin zona conocida, zona cerrada o sin vista de mundo
  → no decora nada (`FestivalDecor.SetUp`, `:100-131`). Recoger dos veces no rompe
  (lo prueba la PlayMode).
- **Cargar una partida guardada con la fiesta a media sesión**: `OnGameLoaded` lee
  `IVillageEvents.ActiveEventId` y decora sin esperar un aviso que ya no va a llegar —
  lo mismo que hace el sonido para elegir el humor (`AudioDirector.cs:191-192`).

## 4. La costura — la aplica el orquestador («los vecinos van»)

Que durante el festival los vecinos tiendan a estar donde pasa la cosa lo decide
`IslanderBrain`, y hoy no hay nada: `Decide` (`IslanderBrain.cs:58-95`) reparte por
necesidades y personalidad, y el sorteo de encuentros es por zona
(`RunChanceEncounters`, `:174-194`) — en una fiesta de plaza, la gente que va de paseo
por la tienda ni se cruza. Tres pasos, juntos:

### A. `Scripts/Core/Services/Contracts/IVillageEvents.cs` — exponer la zona

Tras `string ActiveEventId { get; }` (línea 57):

```csharp
        /// <summary>La zona donde pasa lo que hay puesto ahora mismo, o vacía.</summary>
        /// <remarks>
        /// La necesita quien quiere acercar gente o mirar hacia allí, y hoy esa
        /// respuesta solo existe dentro de Nimbo.Events (RequiredZone de la
        /// definición). Por contrato, como el resto de preguntas entre módulos.
        /// </remarks>
        string ActiveEventZoneId { get; }
```

### B. `Scripts/Events/Scheduling/VillageEvents.cs` — implementarla

Junto a `ActiveEventId` (línea 52):

```csharp
        public string ActiveEventZoneId => _scheduler.ActiveEvent?.RequiredZone ?? "";
```

### C. `Scripts/Simulation/Behaviour/IslanderBrain.cs` — el enganche

En `Decide`, **sustituyendo** la llamada final a `WanderByPersonality(islander);`
(línea 94) — después de las necesidades urgentes, que siguen mandando: nadie se queda
en casa con sueño porque haya fiesta:

```csharp
            // Durante una fiesta, quien no tiene nada más urgente va hacia donde pasa:
            // es lo que llena la plaza y lo que da público a las ondas sociales. La
            // zona ya llega validada por el planificador (el evento no habría
            // empezado con su zona cerrada).
            if (ServiceRegistry.TryGet<IVillageEvents>(out var fiestas))
            {
                var zonaDeFiesta = fiestas.ActiveEventZoneId;
                if (!string.IsNullOrEmpty(zonaDeFiesta))
                {
                    _island.SendTo(islander.Id, zonaDeFiesta);
                    islander.Activity = IslanderActivity.Socializing;
                    return;
                }
            }

            WanderByPersonality(islander);
```

Con A+B+C, mi tabla de propósitos podría jubilarse (el decorado leería
`ActiveEventZoneId` directamente); no lo propongo aún: la tabla funciona hoy para los
tres eventos y tiene prueba que la vigila. Jubilarla sería una simplificación, no una
corrección.

**Observación sin propuesta**: el mingling de `EventSparks` aplica afinidad a **todos**
los pares del censo sin mirar dónde están (`EventSparks.cs:61-70`). Con C aplicado, la
coincidencia física pasa a ser real y tendría sentido ceñir el mingling a quienes
comparten zona — es un cambio de diseño, no de costura, así que solo lo dejo escrito.

## 5. Estado de las pruebas

Verdad por delante, con la evolución real de las corridas:

1.ª y 2.ª (editor): abortadas por `CreatorMannequin.cs(78,96,97,187)` — CS1061/CS0029,
fichero nuevo de otro agente (creador de personajes). Sus autores lo arreglaron;
desaparecieron de la 3.ª.
3.ª (editor): errores míos — `[UnityTest]` sin `using UnityEngine.TestTools;` en mi
fichero de PlayMode. Corregido.
4.ª: editor en verde.
5.ª (juego): **1 de 4, y el fallo fue una lección de la casa**. Mis pruebas cargaban la
escena `Isla` y a correr: pero la escena trae `_startFromMenu: 1`
(`Isla.unity:698`) y sin nadie llamando a `StartGame` el bootstrap no construye nada —
en el log de mi corrida no hay ni un «Isla Nimbo lista» (`GameBootstrap.cs:163`),
frente a los 87 de una corrida verde ajena. Las demás pruebas de juego lo resuelven
publicando `ProtagonistCreated`, que es el aviso por el que `FlowController` llama a
`StartGame` (`FlowController.cs:60-61`): literalmente pulsar Continuar. Copiado ese
paso. Y con el juego ya vivo apareció el segundo detalle: el escenario está **cerrado**
en una isla recién empezada (`IslandLayout.cs:90`, pide nivel 10), y mi guardia se
niega a decorar zonas cerradas — así que la prueba del concierto lo abre por la puerta
pública (`IIslandService.Unlock`), igual que la partida hace cuando la isla se lo gana.
6.ª: juego en verde. 7.ª y 8.ª: repetición de las dos suites sobre el código final
exacto (entre medias quité una propiedad pública que nadie leía, `IsUp`, verificado
con grep, y retocé comentarios) — los XML de abajo son de esas dos.

### Resultado medido (XML en `Informes/pruebas/fiestas-*.xml`)

| Suite | Resultado |
|---|---|
| Editor `DecoradoDeFiestaTests` | **3 pruebas · 3 pasadas · 0 fallos** |
| PlayMode `FiestaDecoradaEnLaIslaTests` | **4 pruebas · 4 pasadas · 0 fallos** |

Las pruebas nuevas suman 7 (3 editor + 4 juego); no quito ninguna. Sin cambios en
ningún `.asmdef`: `Nimbo.PlayTests` ya referenciaba `Nimbo.Art`
(`Nimbo.PlayTests.asmdef:7`) y `Nimbo.Tests` ya traía `Nimbo.Art` y `Nimbo.Events`
(`Nimbo.Tests.asmdef:14-15`).

Lo que cada prueba exige, en corto:

- **Editor** — la tabla del decorado y `EventCalendar` dicen lo mismo en las dos
  direcciones (y ninguna clave zombi); el anillo cabe en toda zona Social/Leisure del
  plano; el conjunto levanta ≥6 grupos de malla, >1.000 vértices, material en todo, y
  sus bounds caben dentro del anillo (ancho/profundo ≤ 21 m, alto ≤ 3,7 m, nada bajo
  el suelo).
- **PlayMode** — cargando la escena `Isla` de verdad, arrancando la partida como el
  menú (`ProtagonistCreated`) y publicando por el bus justo lo que publica el
  planificador: el festival pinta el decorado **en la plaza** (posición exacta de
  `IBuildService.TryGetWorldCentre`, ±1 cm); `VillageEventEnded` lo recoge (y un
  segundo aviso de cierre no deja nada); el concierto decora **el escenario**;
  `market_day` (sin zona) y un id inventado no cuelgan nada — y la siguiente fiesta
  buena sí se pinta.

Confesión honesta sobre la 5.ª corrida: la única prueba que pasó fue precisamente la
de recoger, porque sus aserciones son de ausencia — y cuando nada funciona, «no hay
decorado» siempre es verdad. Es el motivo de que las pruebas de **aparición** sean las
que mandan aquí: sin ellas, un sistema sordo saca verde.

## 6. Cómo se comprueba a mano

1. Cargar `Isla` con una partida de Aldea 7 y 1.200 monedas; panel de eventos (F1),
   organizar **Festival de la isla**: la plaza amanece llena de mástiles con
   banderines blancos/amarillos/rosas y farolillos colgados.
2. Organizar un **concierto** (o esperar al sábado 17:00): el anillo sale en el
   escenario, no en la plaza.
3. Dejar pasar las horas hasta que acabe: el decorado desaparece entero y la plaza
   vuelve a ser la de siempre.
4. Guardar con la fiesta en marcha, salir y cargar: el decorado está puesto nada más
   cargar, sin esperar aviso alguno.

Lo que este informe **no** puede decir: si el decorado se siente bien de cerca, si la
comba de las cuerdas convence o si seis mástiles bastan para que la plaza lea como
fiesta. Eso se decide mirándolo — yo solo he medido que aparece, en su sitio, y que se
va.
