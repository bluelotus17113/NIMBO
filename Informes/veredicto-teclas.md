# Veredicto: nimbo-teclas

Verificador: nimbo-verificador · Fecha: 2026-08-24 10:46 · Encargo: `Informes/encargos/nimbo-teclas.md` · Informe revisado: `Informes/informe-teclas.md`

---

VEREDICTO: **APROBADO**

DIFF REAL (lo suyo, aislado del árbol compartido):
- `Assets/_Project/Scripts/UI/Menu/GameKeys.cs` — nuevo, 72 líneas
- `Assets/_Project/Scripts/UI/Menu/IEscapeCloser.cs` — nuevo, 24 líneas
- `Assets/_Project/Scripts/UI/Menu/MainMenuView.cs` — `HandleEscape()` extraído y público, tecla leída de `GameKeys.Pause` (`MainMenuView.cs:208`, `:236-256`)
- `Assets/_Project/Scripts/UI/Menu/OptionsPanel.cs` — sección «Teclas» pintada desde `GameKeys.Listed` (`KeysSection()`, `:82-115`); el resto del diff de este fichero es de nimbo-ajustes (`DisplaySection`, `DisplayPrefs`), misma carpeta asignada
- `Assets/_Project/Tests/TeclasTests.cs` — nuevo, 4 pruebas de editor
- `Assets/_Project/Tests/PlayMode/TeclasEnLaIslaTests.cs` — nuevo, 3 pruebas de juego que cargan `Isla`
- `UiRoot.cs` sin tocar: la costura §3 queda descrita para el orquestador, como manda el encargo

¿CUADRA CON SU INFORME?: **Sí, línea a línea.** Verifiqué una por una las referencias concretas: anclajes H.1–H.5 exactos (`UiRoot.cs:26`, `:81`, `:219`, `AnyPanelOpen` acaba en `:623`, Tab/M/B en `:669/:676/:683`), `PlayerInteractor.cs:289/:292`, `BuildPanel.cs:39-40`, `HotbarView.cs:157`, `MinigamePanel.cs:56`, velo de pausa alfa 0,82 (`MainMenuView.cs:129`). Los recuentos de pruebas del informe (599·597·0·2 editor / 161·147·4·10 juego) coinciden exactamente con los XML. El mensaje de fallo citado está literal en el XML.

PRUEBAS: **editor 599 (597 pasadas, 0 rojas, 2 saltadas) · juego 161 (147 pasadas, 4 rojas, 10 saltadas)** — contra línea base 588/142/0.
- Nota de método: los `todo-*.xml` del orquestador son de las 07:53 y **no contienen ninguna prueba de Teclas** — son anteriores a este trabajo. La evidencia usada son las corridas completas del propio agente con el envoltorio: `Informes/pruebas/teclas-todo-EditMode.xml` (09:35) y `teclas-todo-PlayMode.xml` (09:37), coherentes con sus corridas filtradas de las 09:27–09:29.
- Ninguna prueba de la línea base desapareció: comparé los `fullname` de ambas suites base contra las de teclas, cero bajas en editor y en juego.
- Las 4 rojas de juego: 1 es `TeclasEnLaIslaTests.EscapeCierraElPanelAbiertoYSinAbrirPausa`, **roja a propósito** (verificador del enchufe, mensaje que nombra el paso exacto de §3); las otras 3 (`AvisosEnLaIslaTests` ×2, `TiendasEnLaIslaTests` ×1) estaban verdes en la base de las 07:53 y pertenecen a sistemas de bloqueos y tiendas cuyos autores estaban editando en esa ventana. No hay mecanismo plausible por el que cambios solo en `Scripts/UI/Menu/` rompa tooltips ni oclusión de botones de tienda. Atribución correcta.

FUERA DE SU CARPETA: **ninguno que importe.** Las pruebas viven en `Assets/_Project/Tests/`, fuera de `Scripts/UI/Menu/**`, pero es la ubicación compartida que el propio encargo exige («una prueba de juego que cargue Isla») y donde escriben todos los agentes de esta ola. `UiRoot.cs` intacto: respetó la frontera más importante.

¿ENCHUFADO?: **A medias por diseño, y el diseño es del encargo**: `UiRoot.cs` no es suyo, así que el enchufe va descrito en §3 para el orquestador. Lo verificado:
- La prueba de juego carga `Isla` de verdad, barre el bootstrap superviviente, publica `ProtagonistCreated` y llega al sistema por el camino real (`MainMenuView.HandleEscape`, mismo método que llama el `Update`). Modelo `CronicaEnLaIslaTests` seguido correctamente.
- Hoy: 2 verdes sin enganche (pausa respeta su ESC; sin nada abierto abre pausa) y 1 roja que exige el enganche. Comprobé que la roja se pone verde aplicando solo H.1–H.4: los dieciséis campos que usa `CloseTopPanel` existen con `IsShowing`/`Hide`, `ServiceRegistry.Register/Unregister/TryGet` tienen esas firmas (`ServiceRegistry.cs:20/:34/:48`), `UiRoot.cs` ya tiene `using Nimbo.Core.Services` (línea 2), y los eventos `FurnishModeChanged(false)`/`BuildModeChanged(false)`/`IslanderFocused("")` ya se publican en ese mismo fichero. La costura es aplicable tal cual está escrita.
- En el árbol principal queda un único lector de Escape tras el cambio (`grep KeyCode.Escape` solo da `GameKeys.cs`): no hay doble consumo.

DEFECTOS: ninguno bloqueante. Dos observaciones menores, para la siguiente vuelta si acaso:
1. `GameKeys.Name` (`GameKeys.cs:58-70`) lleva ramas para `Alpha0-9` y `LeftShift/RightShift` que ningún binding actual usa. Es especulación pequeña en un helper de pintado — no la quitaría ya, pero tampoco crezca.
2. La suite de juego queda en rojo hasta que el orquestador aplique §3. Es intencional y el mensaje señala el remedio, pero mientras §3 no caiga, la invarianta «cero en rojo» está rota por diseño: aplicar §3 es lo primero que debe hacer el orquestador con este informe.

AFIRMACIONES SIN RESPALDO: ninguna relevante. El informe no describe sensaciones — deja explícitamente «que la lista se sienta bien» para quien la mire. La predicción «verde en cuanto se apliquen H.1-H.4» la he verificado yo contra el código actual y es correcta. El aviso sobre el worktree paralelo es cierto: `.claude/worktrees/ui-aaa/.../UiRoot.cs:624+` tiene su propio `EscapeGuard.Take()` leyendo Escape — el orquestador debe resolver quién es dueño de la tecla si esa versión aterriza.

LO QUE ESTÁ BIEN (no lo deshagas):
- `GameKeys` como datos y no documentación, con la prueba-editor que impide que la lista se despegue de la fuente (`TeclasTests.TodaTeclaDeclaradaEstaEnLaListaYNingunaMas` usa reflexión sobre las constantes). Es la vacuna correcta contra «la lista que miente».
- Honestidad total: declara qué prueba falla, por qué, y qué queda sin probar (el cable físico de `Input.GetKeyDown`, imposible de inyectar con `activeInputHandler: 0`).
- La regla de capas de Escape está justificada con razones de juego (el velo tapa lo de detrás; una pulsación, un cierre), no con gusto personal.
- Higiene de pruebas: `TearDown` restaura `Time.timeScale`, `SetUp` barre el bootstrap, mensajes de assert que dicen qué hacer cuando algo cambie.
