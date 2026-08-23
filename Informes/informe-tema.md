# Informe — estados de botón (hover, pulsado, foco) y la pregunta del tooltip

Agente: tema de interfaz. Carpeta: `Assets/_Project/Scripts/UI/UiTheme.cs`,
`Assets/_Project/Scripts/UI/Gates.cs`, `Assets/_Project/Settings/NimboRuntimeTheme.tss`
y las pruebas `TemaBotonesTests` / `TemaEnLaIslaTests` / `DiagnosticoTooltip`.

## Resultado en una línea

Los tres estados existen y se ven **en el juego**, medidos sobre un botón real de la
barra de acciones: hover a PeachDeep, pulsado a PeachDeep, foco con anillo Ink a 2 px,
y el apagado no promete lo que no puede cumplir. La pregunta del tooltip tiene respuesta
medida: **en runtime no se muestra ningún tooltip, ni siquiera en elementos encendidos**.

Números finales: **EditMode 529 pruebas, 527 verdes, 0 rojas, 2 saltadas. PlayMode 117
pruebas, 107 verdes, 0 rojas, 10 saltadas** (las 10 son sondas de captura que ya
estaban saltadas; ninguna mía). Línea base anterior: 514 editor / 92 juego, cero rojas.

---

## 1. Qué se conservó y qué se rehízo

Se conservó, como dijo el orquestador, `UiTheme.cs` (fábricas sin fondo ni borde inline)
y `NimboRuntimeTheme.tss`. Las dos pruebas apartadas en `Informes/borradores/` estaban
rotas por mezclar UGUI (`PointerEventData`) con UI Toolkit; se reescribieron desde cero
con la API correcta y viven en:

- `Assets/_Project/Tests/TemaBotonesTests.cs` — editor, estructura.
- `Assets/_Project/Tests/PlayMode/TemaEnLaIslaTests.cs` — juego, comportamiento.
- `Assets/_Project/Tests/PlayMode/DiagnosticoTooltip.cs` — sonda A/B del tooltip.

Antes de escribir una sola línea verifiqué contra el IL desensamblado del módulo
(`monodis` sobre `UnityEngine.UIElementsModule.dll` de 6000.5.5f1) cada API que iba a
usar. Tres hallazgos que cambiaron el diseño:

1. Los `Pointer*Event` **no tienen** ningún `GetPooled(posición,…)` público en esta
   versión: los sobrecargos con coordenadas son `assembly`. La única vía pública es
   `GetPooled(UnityEngine.Event)`, que copia `mousePosition`, botones e id de ratón tal
   cual (sin voltear Y). Todo evento sintético nace ahí.
2. Despachar un `PointerMoveEvent` sí alimenta el `:hover`: su `PreDispatch` guarda la
   posición en `PointerDeviceState` y el panel recalcula el elemento bajo el puntero
   dentro del propio despacho. No hace falta backend de entrada ninguno.
3. `Button.hasFocus` no existe; el foco se consulta en
   `elemento.focusController.focusedElement` y los pseudosestados se leen con
   `hasHoverPseudoState` / `hasActivePseudoState` / `hasFocusPseudoState`.

## 2. El fallo real no era solo el inline: el default gana los empates

Con el inline fuera, los estados seguían sin verse. Medido en la primera corrida de
juego (`/tmp/opencode/nimbo-tema-play.xml`):

- reposo: Peach ✓ (mi regla ganaba)
- hover: gris `RGBA(0.820, 0.820, 0.820)` = `#D1D1D1` del default ✗
- pulsado: gris `0.584` ✗
- anillo de foco: azul `RGB(0, 106, 166)` de Unity ✗

Causa documentada y confirmada: desde Unity 2023.2 el tema por defecto define sus
estados como `.unity-button:hover:enabled` — **dos pseudoclases**, especificidad mayor
que cualquier `.mi-clase:hover`. Ni añadir el tipo (`Button.nimbo-btn-action:hover`)
bastó: (0 clases+pseudo extra…) sigue perdiendo contra tres entradas de clase. Segunda
corrida medida: hover/pulsado/foco seguían siendo del default.

Arreglo (`NimboRuntimeTheme.tss:38-48`, y su espejo para el secundario en 56-70): dar
a mis reglas la misma forma que las suyas más el tipo delante:

```uss
Button.nimbo-btn-action:hover:enabled { background-color: #F0A47D; }
Button.nimbo-btn-action:active:enabled { background-color: #F0A47D; }
Button.nimbo-btn-action:focus:enabled { border-color: #5C4A42; }
```

Tercera corrida: hover, pulsado y foco ya resuelven al tema. El apagado necesitó lo
mismo por otra vía: el default pinta SUS botones apagados con `.unity-button:disabled`
y me ganaba el reposo (gris 0.584 en vez de CreamDeep); `tss:82-95` añade
`Button.nimbo-btn-disabled:disabled` y un cinturón `…:hover:disabled` para el día en
que Unity cuelgue pseudoclases de puntero sobre elementos apagados.

## 3. Dos trampas de las pruebas sintéticas, medidas y arregladas

- **El puntero sintético es global y sobrevive a la recarga de escena**
  (`PointerDeviceState` guarda posición por tipo de contexto, no por panel). En la
  segunda corrida, «Crónica» arrancaba YA con hover heredado de la prueba anterior y el
  color de reposo salía gris. Arreglo: `ApartarPuntero()` mueve el puntero fuera y
  espera `!hasHoverPseudoState` antes de leer reposo (`TemaEnLaIslaTests.cs:364-368`).
- **Un botón colgado de la raíz del panel layouta con ancho NaN**: la primera versión
  del apagado murió Inconclusive con `worldBound.width == NaN`. Arreglo: el botón de
  prueba va a la fila real de la barra (`boton.parent.Add`, línea 206), que es donde
  viven los botones de verdad, y `EsperarLayout` trata NaN como «todavía sin layout»
  (líneas 286-293).

La sonda del tooltip, además, no encontraba la barra porque no publicaba
`ProtagonistCreated`; ahora comparte arranque con `TemaEnLaIslaTests`.

## 4. Respuesta a la pregunta abierta: el tooltip NO se ve en runtime

Diseño A/B sobre el mismo botón real («Crónica»), puntero sintético quieto encima 5 s
por fase, contando `TooltipEvent` en la raíz (trickle y burbuja) y `PointerEnterEvent`
sobre el botón. Salida literal (`/tmp/opencode/sonda_tooltip.txt`):

```
habilitado: tooltips=0 (trickle=0 bubble=0) enters=1 | apagado: tooltips=0 (trickle=0 bubble=0) enters=1 | texto=''
```

- La maquinaria de puntero funciona (`enters=1` en ambas fases).
- **Cero `TooltipEvent` incluso con el botón ENCENDIDO.** No es cosa del
  `SetEnabled(false)`: es que en runtime nadie despacha tooltips.
- Corroborado en el IL: en `UnityEngine.UIElementsModule` no hay ninguna clase que
  despache `TooltipEvent` (el emisor vive solo en `UnityEditor.UIElementsModule`), y
  `PanelSettings` no expone propiedad de tooltips en esta versión.

**Es un defecto grave y no es mío de arreglar.** Las explicaciones de bloqueo están
escritas para nadie:

- `Gates.cs:36` escribe la frase («Te hace falta Oficio 5…»).
- Se cuelgan de `.tooltip` en `SocialSection.cs:160,208,246,287`, `JobSection.cs:160`,
  `EventsPanel.cs:119`, `CraftPanel.cs:158`.
- También quedan mudos los tooltips informativos de `DecorPanel.cs:345` y
  `MapPanel.cs:203`.

Para el orquestador: o se implementa un modo runtime de mostrar tooltips (un
manipulador propio que lea `elemento.tooltip` al hacer hover y pinte una tarjeta), o
esa información tiene que salir por otro camino (etiqueta visible, cartel). Mientras
tanto dejo `DiagnosticoTooltip.Sonda` en la suite: su aserción solo falla ante un
resultado ambiguo (nada en el control pero algo en el apagado), así que seguirá verde
el día que existan tooltips de verdad y servirá para verificarlos.

## 5. Qué prueba qué

| Prueba | Dónde | Qué mide |
|---|---|---|
| `ElBotonPrincipalLlevaSuClaseYNoPintaInline` (+2 hermanas) | editor | clase puesta y **cero** fondo/borde inline en las tres fábricas |
| `ElTemaImportaYTieneLosTresEstadosPorEscrito` | editor | el tss compila como `ThemeStyleSheet` y conserva `:hover/:active/:focus` |
| `LosBotonesDeLaBarraLlevanLaClaseDelTema` | juego | los N botones en pantalla pasan todos por el tema; «Crónica» lleva `nimbo-btn-action` |
| `ElBotonReaccionaAlPuntero` | juego | reposo Peach → hover PeachDeep → vuelve al reposo |
| `ElBotonSeHundeMientrasEstaPulsado` | juego | pulsado PeachDeep, al soltar vuelve, secuencia mover→pulsar→soltar como una mano real |
| `ElFocoDeTecladoSeVe` | juego | `Focus()` deja foco + pseudosestado, anillo Ink, borde siempre a 2 px (sin salto de texto) |
| `ElApagadoNoPrometeLoQueNoPuedeCumplir` | juego | apagado: reposo CreamDeep, sin hover bajo el puntero, sin cambiar de color, `enabledSelf == false` |
| `DiagnosticoTooltip.Sonda` | juego | mide si algún tooltip llega a verse; guarda la medida en `/tmp/opencode/sonda_tooltip.txt` |

Duraciones medidas de mis seis pruebas de juego: entre 0,50 y 0,54 s cada una; la sonda,
10,5 s (dos fases de 5 s de quietud).

## 6. Limitaciones dichas de frente

- Los eventos son sintéticos construidos desde IMGUI: prueban el pipeline completo de
  UI Toolkit (hit-test, pseudosestados, resolución de estilos) pero no hay driver ni
  sistema operativo de por medio. Que se sienta bien al jugarlo lo decide quien lo juega.
- Nota operativa: `-quit` junto con `-runTests` hace que Unity cierre **sin ejecutar
  las pruebas** (me pasó en la primera corrida de editor: exit 0 y sin XML). Las
  invocaciones que sí corren no llevan `-quit`.
- Una corrida completa de PlayMode murió con SIGABRT antes de empezar (agotamiento de
  descriptores de fichero en el servidor ILPP, aserción `fd < _SC_OPEN_MAX` de mono).
  Sin procesos zombis y con `ulimit -n` en 524288, el reintento salió limpio: fallo
  transitorio del entorno, no del proyecto.

## 7. Huella

Sin cambios en `ProjectSettings` (`activeInputHandler` sigue en 0), sin cambios en
asmdef, sin referencias nuevas entre ensamblados. Ficheros tocados: `NimboRuntimeTheme.tss`
(selectores de estado), `TemaBotonesTests.cs`, `PlayMode/TemaEnLaIslaTests.cs`,
`PlayMode/DiagnosticoTooltip.cs`. Árbol sucio a propósito, sin commits.
