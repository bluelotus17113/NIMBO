# Informe — un canal de verdad para las explicaciones de bloqueo

Agente: bloqueos. Carpeta asignada: `Assets/_Project/Scripts/UI/Gates.cs` y creaciones
en `Scripts/UI/`. Ficheros nuevos: `Scripts/UI/GateNotice.cs`,
`Scripts/UI/GateNoticeHost.cs`, `Tests/PlayMode/AvisosEnLaIslaTests.cs`.
**`Gates.cs` no necesitó ni una línea**: sigue escribiendo donde siempre; lo que faltaba
era alguien que leyera.

## Resultado en una línea

Las explicaciones que nacen en `Gates.cs:36` y se cuelgan de `.tooltip` en siete sitios
(`SocialSection.cs:160/208/246/287`, `JobSection.cs:160`, `EventsPanel.cs:119`,
`CraftPanel.cs:158`) ahora **se leen en el juego**: una franja fija inferior las enseña
sin pedir ningún gesto al jugador. Probado sobre la escena `Isla` con un botón bloqueado
de verdad (`Halagar`, `SetEnabled(false)`, Convivencia 2 pedida al jugador que va por 1).

Números finales de mis corridas:

| Suite | Total | Pasadas | Rojas | Saltadas |
|---|---|---|---|---|
| PlayMode filtrada a mis pruebas | 2 | 2 | 0 | 0 |
| PlayMode `DiagnosticoTooltip` (sonda intacta) | 1 | 1 | 0 | 0 |
| PlayMode completa | 132 | 119 | **3 (ajenas, §6)** | 10 |
| EditMode completa | 565 | 563 | 0 | 2 |

Duraciones medidas de mis dos pruebas de juego: 2,62 s y 0,85 s
(`Informes/pruebas/bloqueos-PlayMode.xml`). Corrida de certificación sobre el estado
final del árbol (`bloqueos-final-PlayMode.xml`): 2/2 verdes de nuevo — segunda pasada
consecutiva en verde, que es la que acredita que la intermitencia del §4.3 quedó
arreglada y no corrida con suerte. La línea base de editor queda en su forma:
cero rojas. Las tres rojas de juego son de otros agentes, con sus nombres y mensajes en
la sección 6.

---

## 1. El canal elegido: franja fija. Por qué no los otros dos

**Elegida la franja fija** (una línea abajo-centro, sobre la zona que el jugador ya
mira: fila de acciones, fila de habitantes y barra). La justificación es estructural,
no de gusto:

- **Pegado al botón, descartado con medidas.** Exige hover o foco. El hover es puntero,
  y ese canal ya está medido como muerto: cero `TooltipEvent` en runtime, incluso con el
  elemento encendido (informe-tema §4). El foco es imposible sobre lo bloqueado: los
  siete sitios apagan con `SetEnabled(false)` (`UiTheme.cs:260`) y un elemento apagado
  no puede tomar el foco — lo midió `NiElFocoNiElHoverHacenFaltaParaLeerlo`, que pide
  `Focus()` al botón apagado real y afirma que el `focusController` no se lo concede.
- **Al intentar pulsarlo, descartado como canal único.** Un intento de activación que
  llegue a estos botones hoy solo existe con ratón: el proyecto va sobre el sistema de
  entrada antiguo y no hay navegación de foco por teclado ni mando hacia la interfaz
  (las únicas teclas de UI son Tab/M/B y los dígitos de la barra). Un canal de intento
  habría sido otra vez solo-puntero: la enfermedad original del tooltip.
- **La franja se alcanza por existir.** Vale igual con ratón, teclado, mando o sin tocar
  nada, que es justo lo que el tooltip no cumplía. Es además el único de los tres que
  llega a los siete sitios con la misma pieza y sin tocar un panel.

## 2. Cómo está hecho

**`GateNotice`** (la vista). Tarjeta en posición absoluta a 250 px del fondo, centrada,
fondo `CreamDeep` con filete izquierdo `Rose` — el rosa que ya usan los chips de
carencia; sin etiqueta de «error» porque por este mismo canal viajan también los dos
tooltips informativos que quedaron mudos (`DecorPanel.cs:345`, `MapPanel.cs:203`) y una
etiqueta les mentiría. `pickingMode.Ignore`, mismo porqué que `AchievementToast.cs:58`.

Cada 0,25 s recorre el árbol vivo y enseña la **primera explicación visible**, con
«· N más» si hay varias; cuando el contexto cierra, se apaga sola. Coste medido con
trazas temporales: 135 nodos visitados por pase con la ficha abierta (535 nodos el
documento entero), cuatro pases por segundo. Es el mismo orden de latido que el
refresco lento de `UiRoot` (0,4 s), donde el proyecto ya estableció que nadie nota ese
ritmo. Sin animación a propósito: es información ambiental, no un acontecimiento.

**`GateNoticeHost`** (el anfitrión). Se crea solo una vez por entrada en play
(`RuntimeInitializeOnLoadMethod` idempotente), mantiene **una franja por `UIDocument`**
— la escena Isla trae dos, §4 — y se recuela solo: `UiRoot.Mount` vacía la raíz
(`UiRoot.cs:100`) y al fotograma siguiente la franja vuelve a colgarse. Poda los
documentos que mueren con la escena. Ni `UiRoot` ni ningún panel saben que existe.

## 3. Lo que no se rompió, medido sobre el elemento real

`LaExplicacionDeUnBotonBloqueadoSeLeeSinPuntero` carga `Isla`, abre la ficha de un
vecino con **un solo clic sintético — sobre un botón encendido** — y entonces:

1. encuentra dentro un botón bloqueado de verdad: `enabledSelf == false`,
   clase `nimbo-btn-disabled`, explicación escrita;
2. espera **sin enviar ni un evento más** a que la franja diga esa explicación;
3. afirma que el bloqueado **sigue** apagado y con su clase, y que nunca recibió hover
   (`hasHoverPseudoState == false`).

`NiElFocoNiElHoverHacenFaltaParaLeerlo` repite la escena con el puntero aparcado en una
esquina y foco pedido al apagado: ni con eso deja de llegar la explicación.

## 4. Tres fallos míos que la prueba cazó antes que el jugador

Quedan escritos porque son exactamente el tipo de costura que este proyecto persigue:

1. **CS0191**: asignaba un campo `readonly` desde un método llamado por el constructor.
   Trivial, pero fue el primer compile del proyecto entero en rojo por mi culpa.
2. **El enchufe en un agujero que no existe, versión mía**: la primera versión se
   colgaba al *primer* `UIDocument` de `FindObjectsByType` — orden no garantizado — y
   la escena Isla trae **dos** documentos (juego y menú). La franja escaneaba el árbol
   del menú, donde nunca habrá una explicación: dos pruebas en rojo con la franja viva
   pero sorda. Arreglo: una franja por documento, y la que sobra se apaga sola.
3. **Intermitencia medida**: mis primeras esperas contaban *fotogramas* (300) contra un
   mecanismo *temporizado* (0,25 s reales). En batchmode los fotogramas corren casi
   gratis: las trazas mostraron pases separados por décimas de segundo reales mientras
   los 300 fotogramas pasaban en menos tiempo del que tarda un pase. Un día verde,
   dos rojos, misma code. Las esperas ahora son en tiempo real (8 s de presupuesto),
   el mismo cuidado que `DiagnosticoTooltip` con sus cinco segundos de quietud.

## 5. Qué prueba qué

| Prueba | Qué mide |
|---|---|
| `LaExplicacionDeUnBotonBloqueadoSeLeeSinPuntero` | Con un único gesto sobre un botón encendido, la explicación de un botón bloqueado de verdad llega a la franja; el bloqueado sigue apagado, con su clase y sin hover. |
| `NiElFocoNiElHoverHacenFaltaParaLeerlo` | Puntero aparcado + `Focus()` rechazado al apagado (documenta por qué el canal no puede ser pegado-al-botón): la explicación llega igual. |

## 6. Costuras: errores ajenos vistos durante mi turno (anotados, no tocados)

Al compilar en paralelo con otros tres agentes vi y **no arreglé**:

- `IslandCamera.cs(164,681)` CS1519/CS1022 — nimbo-camara, en edición; desapareció en
  mi segundo reintento sin que yo tocara nada.
- `ArbolNimboTests.cs(243)` CS0738, `AcabadosDeViviendaTests.cs(270)` CS0535,
  `CamaraEnLaIslaTests.cs` CS0246→CS0104, `RopaEnLaIslaTests.cs` CS0103/CS0246/CS1503 —
  de árbol, acabados, cámara y ropa respectivamente; todos desaparecidos en corridas
  posteriores.

En la **suite completa de juego** quedan 3 rojas, ninguna mía (mensajes literales del
XML): `ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle` («Falta el enganche D
del informe-arbol.md en PlayerInteractor.cs») y `CamaraEnLaIslaTests` ×2 («no registró
ni un bulto de cámara»). Sus dueños tienen el trabajo en marcha.

**Nota para el orquestador**: si prefieres la franja montada desde `UiRoot.Mount`
(junto al cartel de logros, `UiRoot.cs:189-191`, con `Tick` junto a `_toast.Tick`,
`UiRoot.cs:644`), el anfitrión autónomo sobra: son ~8 líneas en tu fichero y yo no
tocarlas fue el encargo. La vista (`GateNotice`) no cambia nada.

## 7. Límites dichos de frente

- Que la franja **se sienta bien** donde está (250 px fijos sobre el fondo, ancho máximo
  480) lo decide quien lo juega: yo medí que aparece, que dice lo que toca y que se va
  al cerrar el contexto, no que quede bonita.
- El pase lee `.tooltip`; si mañana alguien escribe una explicación por otro camino, no
  la verá. Los siete sitios de hoy pasan por `.tooltip`, y `Gates` sigue siendo el
  único sitio donde se redactan.
- La partida del jugador no se tocó: el guardado siguió desviado por
  `AislarGuardadoEnPruebasDeJuego`, que ya cubría todo el ensamblado de juego.

## 8. Huella

Sin cambios en `ProjectSettings`, sin cambios en asmdef, sin referencias nuevas entre
ensamblados, sin tocar paneles ni `UiRoot` ni `UiTheme`. Ficheros tocados:
`GateNotice.cs` y `GateNoticeHost.cs` (nuevos, `Nimbo.UI`),
`Tests/PlayMode/AvisosEnLaIslaTests.cs` (nuevo, `Nimbo.PlayTests`). La sonda
`DiagnosticoTooltip` sigue en la suite y en verde: su aserción espera el día en que
existan tooltips de verdad. Árbol sucio a propósito, sin commits.
