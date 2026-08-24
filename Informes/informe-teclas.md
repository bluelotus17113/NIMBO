# Informe: teclas — ESC que cierra, y una lista de controles que no puede mentir

Agente: nimbo-teclas · Carpeta asignada: `Assets/_Project/Scripts/UI/Menu/` · Fecha: 2026-08-24

---

## 1. Lo que había (medido)

- **ESC solo existía en el menú.** `MainMenuView.cs:207-213` lo atendía para sus cuatro
  pantallas (título, pausa, ajustes, creador) y publicaba `GamePaused(true)` cuando
  estaba oculto — sin mirar si había paneles de juego abiertos: pausaba con la mochila
  delante. `UiRoot.Update` (`UiRoot.cs:648-712`) escucha Tab (`:669`), M (`:676`) y
  B (`:683`) y **nunca** Escape.
- **Cerrar un panel exigía localizar el botón «Cerrar», que está en cuatro sitios
  distintos**: abajo a lo ancho en la tienda, arriba a la derecha en diez paneles,
  «Dejarlo» en el minijuego (`MinigamePanel.cs:56`) y en la ficha de habitante **no hay
  nada** — se cierra volviendo a pulsar al vecino (`UiRoot.cs:603-615`, el `Toggle(id)`).
- **Tab, M y B no salían escritas en ninguna parte del juego.** Las teclas de acción sí:
  F en el cartel del mundo («F para su ficha», `PlayerInteractor.cs:289` y `:292`) y el
  botón derecho en el panel de construir (`BuildPanel.cs:39-40`). Los números 1-0 van
  pintados en cada hueco del hotbar (`HotbarView.cs:157`). Las tres de interfaz, solo en
  el código. `OptionsPanel.cs:9-12` decía «no hay más que ajustar»: tres volúmenes y
  nada de controles.

## 2. Lo que he aplicado yo (mi carpeta)

Todo compilable hoy, sin depender de ningún enganche. Sin él, el juego se comporta
exactamente como antes: ESC abre la pausa cuando el menú está apartado.

| Cambio | Dónde | Por qué |
|---|---|---|
| `GameKeys.cs` nuevo: las teclas como datos (`Pause`, `Bag`, `Map`, `Furnish`) + `Listed` + `Name(KeyCode)` | `Scripts/UI/Menu/GameKeys.cs` | La fuente única. Quien atiende la pulsación y quien pinta la lista leen al mismo sitio; cambiar una tecla cambia las dos cosas a la vez. Solo se apuntan las teclas cuyo consumidor lee de aquí — apuntar E/F/R sin mover sus lecturas sería recrear la lista que miente que esto evita. |
| `IEscapeCloser.cs` nuevo: el contrato `bool CloseTopPanel()` | `Scripts/UI/Menu/IEscapeCloser.cs` | Para que `MainMenuView` no conozca a `UiRoot` a pelo. Si nadie está registrado, ESC abre la pausa directamente: el comportamiento de siempre. |
| La tecla sale de `GameKeys.Pause` y la decisión se extrae a `HandleEscape()`, público | `MainMenuView.cs:208` y `:240-262` | Público porque la prueba de juego debe «pulsarlo»: con el sistema antiguo (`activeInputHandler: 0`, comentario en `MainMenuView.cs:205-207`) no se puede inyectar una tecla, y lo que se prueba es la decisión, no el cable. |
| Sección «Teclas» en ajustes, pintada desde `GameKeys.Listed` | `OptionsPanel.cs:42` y `:82-115` | Ajustes ya se abre desde dos sitios —título y pausa—, que son justo los dos momentos en que uno se pregunta «¿cómo era esto?». Cada fila lleva clase `fila-tecla` para que la prueba pueda contarlas. |

### La regla de Escape, y por qué este orden

De capas, de arriba a abajo. Una pulsación hace **una** cosa:

1. **Si el menú está enseñando algo, la tecla es suya** (volver de ajustes, salir del
   creador, reanudar). Justificación: es la capa que se ve. El velo de pausa pinta la
   isla con alfa 0,82 (`MainMenuView.cs:127-129`): los paneles que quedan detrás están
   tapados, y gastar el Escape en cerrar algo que no se ve es pausar por debajo sin
   decirlo. Este es el «sin robarle el ESC a la pausa» pedido.
2. **Si hay un modo activo (amueblar o construir), ESC sale del modo**, publicando el
   mismo evento que ya usan B y «Terminar». Justificación: al entrar, esos modos apagan
   barra, fila de habitantes y mochila (`UiRoot.cs:469-472` y `:498-501`) — no queda
   nada más arriba que ellos.
3. **Si hay paneles, cierra UNO: el pintado más arriba**, que es el último montado en
   `Mount()` (`UiRoot.cs:114-172`; en UI Toolkit los últimos hermanos pinta encima).
   Una pulsación, un cierre: la mochila y el mapa son ventanas independientes que
   conviven, y ESC no significa «limpiar todo» sino «sacarme de donde estoy». Es también
   la convención del género: Stardew y Rune Factory desmontan sus menús de uno en uno.
4. **La ficha de habitante, además de cerrarse, suelta la cámara**: publica
   `IslanderFocused("")`, el mismo camino que usa el re-pulsado (`UiRoot.cs:605-609`).
   Sin eso, ESC cerraría la ficha y la cámara se quedaría mirando a nadie.
5. **Sin nada abierto, abre la pausa** (`GamePaused(true)`), como hacía ya
   `MainMenuView.cs:209`.

### El botón «Cerrar» en cuatro sitios (defecto aparte, no lo toco)

Queda igual —son treinta y tres paneles—, pero **el enganche lo vuelve prescindible**:
con H aplicado, todo panel tiene una salida uniforme (ESC), incluida la ficha de
habitante, que era la que no tenía ninguna. La inconsistencia pasa de defecto funcional
a cuestión cosmética; arreglarla sigue siendo decisión del orquestador.

## 3. La costura — la aplica el orquestador

Cinco pasos sobre `Scripts/UI/UiRoot.cs`, todos con ancla exacta. H.1-H.4 enchufan el
cierre; H.5 es lo que convierte `GameKeys` en fuente única de verdad de verdad: mientras
no se aplique, la lista enseña los valores correctos pero una tecla cambiada solo en
`GameKeys` haría que la lista mintiera. Los cinco van juntos.

### H.1 — Línea 26

```diff
-    public sealed class UiRoot : MonoBehaviour
+    public sealed class UiRoot : MonoBehaviour, Menu.IEscapeCloser
```

### H.2 — En `Mount()`, justo después de `_mounted = true;` (línea 219)

```csharp
            ServiceRegistry.Register<Menu.IEscapeCloser>(this);
```

### H.3 — En `OnDisable()`, junto a `_shop?.Dispose();` (línea 81)

```csharp
            ServiceRegistry.Unregister<Menu.IEscapeCloser>();
```

### H.4 — Método nuevo, justo después de `AnyPanelOpen` (que acaba en la línea 623)

```csharp
        /// <summary>
        /// Cierra una cosa de las que estén abiertas: primero el modo activo, que ha
        /// apagado toda la demás interfaz; después el panel pintado más arriba, que
        /// es el último que se montó en Mount. False si no había nada.
        /// </summary>
        /// <remarks>
        /// Una pulsación, un cierre: la mochila y el mapa pueden estar abiertos a la
        /// vez porque son ventanas independientes, y ESC no es «limpiar todo» sino
        /// «sacarme de donde estoy». Nunca toca la tecla él mismo: la lee MainMenuView,
        /// y si aquí no queda nada es él quien abre la pausa. Así hay un solo dueño
        /// de Escape y este método no puede robársela a nadie.
        /// </remarks>
        public bool CloseTopPanel()
        {
            if (_furnishMode)
            {
                EventBus.Publish(new FurnishModeChanged(false));
                return true;
            }

            if (_buildMode)
            {
                EventBus.Publish(new BuildModeChanged(false));
                return true;
            }

            // Orden inverso al montaje: el último hermano pinta encima, así que el
            // último abierto de esta lista es el que el jugador ve delante.
            if (_furnish.IsShowing) { _furnish.Hide(); return true; }
            if (_build.IsShowing) { _build.Hide(); return true; }
            if (_map.IsShowing) { _map.Hide(); return true; }
            if (_shipping.IsShowing) { _shipping.Hide(); return true; }
            if (_craft.IsShowing) { _craft.Hide(); return true; }
            if (_bag.IsShowing) { _bag.Hide(); return true; }
            if (_events.IsShowing) { _events.Hide(); return true; }
            if (_skills.IsShowing) { _skills.Hide(); return true; }
            if (_minigame.IsShowing) { _minigame.Hide(); return true; }
            if (_board.IsShowing) { _board.Hide(); return true; }
            if (_chronicle.IsShowing) { _chronicle.Hide(); return true; }
            if (_achievements.IsShowing) { _achievements.Hide(); return true; }
            if (_decor.IsShowing) { _decor.Hide(); return true; }
            if (_shop.IsShowing) { _shop.Hide(); return true; }
            if (_creator.IsShowing) { _creator.Hide(); return true; }

            // La ficha va la última (montó la primera) y su cierre avisa a la cámara,
            // igual que hace Toggle: sin esto ESC la cierra y la cámara se queda
            // mirando a nadie.
            if (_panel.IsShowing)
            {
                _panel.Hide();
                EventBus.Publish(new IslanderFocused(""));
                return true;
            }

            return false;
        }
```

### H.5 — Que las teclas se lean de `GameKeys` (líneas 669, 676 y 683)

```diff
-            if (Input.GetKeyDown(KeyCode.Tab))
+            if (Input.GetKeyDown(Menu.GameKeys.Bag))
```
```diff
-            if (Input.GetKeyDown(KeyCode.M))
+            if (Input.GetKeyDown(Menu.GameKeys.Map))
```
```diff
-            if (Input.GetKeyDown(KeyCode.B) && (_indoors || _furnishMode))
+            if (Input.GetKeyDown(Menu.GameKeys.Furnish) && (_indoors || _furnishMode))
```

### Notas de la costura

- **Dónde vive la interfaz**: la convención del registro pide contratos en
  `Core/Services/Contracts`, pero esa carpeta no es mía. `IEscapeCloser` está en
  `Nimbo.UI.Menu` y ambos consumidores viven en `Nimbo.UI`, así que compila y no abre
  ningún ciclo. Si prefieres la letra de la convención, muévela a Contracts y ajusta los
  dos `using` (`UiRoot.cs` y `MainMenuView.cs`); nada más cambia.
- **Hay un worktree paralelo tocando este mismo monolito**:
  `.claude/worktrees/ui-aaa/Assets/_Project/Scripts/UI/UiRoot.cs:624-645` tiene su propio
  `EscapeGuard` y unos `_hub.Toggle`. Si esa versión aterriza primero, los anclajes de
  H.1-H.5 se mueven pero el contrato no cambia: implementar `IEscapeCloser`, registrarse
  en `Mount`, darse de baja en `OnDisable`, y que `CloseTopPanel` devuelva false sin
  nada que cerrar. El dueño de Escape sigue siendo uno solo: quien lee la tecla.
- **El minijuego entra en la lista a propósito**: ESC durante el ritmo equivale a
  «Dejarlo» (`MinigamePanel.Leave` → `Hide()`, `MinigamePanel.cs:95-103`), que es un
  cierre limpio. Si algún día el minijuego tuviera progreso que defender, es ahí donde
  toca pedir confirmación, no en el router.

## 4. Estado de las pruebas — VERDAD POR DELANTE

Pruebas nuevas: `Tests/TeclasTests.cs` (4 de editor) y
`Tests/PlayMode/TeclasEnLaIslaTests.cs` (3 de juego, cargando `Isla` de verdad con el
mismo cuidado que `CronicaEnLaIslaTests`: barre el bootstrap superviviente y publica
`ProtagonistCreated`).

### Resultado medido (XML en `Informes/pruebas/teclas-*.xml`)

| Suite | Resultado |
|---|---|
| Editor, filtro `TeclasTests` | **4 pruebas · 4 pasadas · 0 fallos** |
| Juego, filtro `TeclasEnLaIslaTests` | **3 pruebas · 2 pasadas · 1 fallo** |
| Editor completo | **599 · 597 pasadas · 0 fallos · 2 saltadas** |
| Juego completo | **161 · 147 pasadas · 4 fallos · 10 saltadas** |

- **El fallo de juego es exactamente el esperado: es el verificador del enchufe.**

  ```
  ESC no cerró la mochila. Falta aplicar el enganche del informe-teclas.md §3:
  UiRoot : IEscapeCloser, el registro en Mount() y el método CloseTopPanel.
  ```

  No falla por un accidente: la mochila abrió (la aserción previa pasó), el menú estaba
  oculto y `HandleEscape` corrió. Falla porque nadie responde aún a `IEscapeCloser` —
  que es precisamente lo que esta prueba tiene que delatar. Verde en cuanto se apliquen
  H.1-H.4, sin tocar nada más; su mensaje señala el paso exacto.

- **Las otras dos de juego están verdes hoy** y siguen siéndolo tras el enganche:
  `ConElMenuDePausaDelanteEscapeSigueSiendoDelMenu` comprueba que con la pausa delante y
  una mochila abierta detrás, ESC reanuda (`Time.timeScale` vuelve a 1) y la mochila
  sigue ahí; `SinNadaAbiertoEscapeAbreLaPausa` comprueba el camino de siempre.

- **Editor completo**: la línea base documentada era 588+2 saltadas; ahora 597+2 con
  cero fallos. Mi aporte son 4; las otras 7 son de agentes en paralelo. Nadie baja.

- **Juego completo, los otros 3 fallos no son míos**: dos de `AvisosEnLaIslaTests`
  (explicaciones de botones bloqueadas sin puntero: tooltips) y uno de
  `TiendasEnLaIslaTests` (un botón «Comprar» ocluido). Ninguno toca fichero mío ni
  sistema mío; sus autores estaban editando durante mis corridas (los errores de
  compilación de `CajonSeguroTests.cs` y `PanelesAlDiaTests.cs` que bloquearon mis dos
  primeras corridas desaparecieron solos entre intento y intento).

- Lo que **no** queda probado: el cable físico `Input.GetKeyDown(GameKeys.Pause)`
  (`MainMenuView.cs:208`) y el mismo cable en H.5. Con el sistema de entrada antiguo no
  hay inyección de teclas; es una línea sin lógica propia cada uno. Y que la lista
  «se sienta bien» en pantalla: eso lo decides mirándola (ajustes, desde el título o
  desde la pausa).

## 5. Cómo se comprueba a mano (tras aplicar §3)

1. Cargar `Isla`, pulsar Tab: mochila abierta. Pulsar Esc: mochila cerrada, **sin**
   pausa (el reloj no se para).
2. Pulsar Esc de nuevo: pausa. Pulsar Esc: se sigue jugando.
3. Abrir mochila, abrir mapa (M): dos ventanas. Esc: se va el mapa. Esc: se va la
   mochila. Esc: pausa.
4. Pulsar F delante de un vecino y Esc con la ficha delante: la ficha se cierra y la
   cámara vuelve al plano general.
5. Entrar en casa, B para amueblar, Esc: se sale del modo y vuelven barra y fila.
6. Ajustes (título o pausa): debajo de los volúmenes, la lista «Teclas» con las cuatro
   filas. Cambiar una constante en `GameKeys.cs` y relanzar: la lista y el juego cambian
   juntos.
