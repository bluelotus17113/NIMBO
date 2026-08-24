# Informe — nimbo-ajustes: resolución, modo ventana y escala de interfaz

Fecha: 2026-08-24 · Agente: nimbo-ajustes · Encargo: `Informes/encargos/nimbo-ajustes.md`

## Qué se entregó

Tres ajustes nuevos en el panel de opciones, persistentes, más las pruebas que
los sostienen. Ficheros tocados:

| Fichero | Qué |
|---|---|
| `Assets/_Project/Scripts/UI/Menu/DisplayPrefs.cs` | **Nuevo.** Persistencia y aplicación de los tres ajustes |
| `Assets/_Project/Scripts/UI/Menu/OptionsPanel.cs` | Sección «Pantalla» nueva; flush al salir; «Restaurar» ampliado |
| `Assets/_Project/Tests/AjustesDePantallaTests.cs` | **Nuevo.** 4 pruebas de editor |
| `Assets/_Project/Tests/PlayMode/EscalaEnElPanelTests.cs` | **Nuevo.** 2 pruebas de juego |

No se tocó `NimboPanelSettings.asset` ni `SceneBuilder.cs`. Tampoco ningún otro
fichero fuera del encargo.

## Diseño

### Persistencia — el camino de los volúmenes

`DisplayPrefs` sigue el patrón de `AudioPrefs`: claves propias en PlayerPrefs,
escritura en cada cambio (`DisplayPrefs.cs:67-79`, `81-87`) y disco al salir del
panel (`OptionsPanel.cs:82`, junto al `AudioPrefs.Flush()` que ya había). Las
claves son `nimbo.pantalla.modo/ancho/alto/escala` (`DisplayPrefs.cs:28-31`),
mismo convenio de nombres que `nimbo.volumen.*`.

Vive en `UI/Menu` y no en `Core` a propósito: los volúmenes los leen dos
sistemas (menú y sonido) y por eso están arriba; estos ajustes los aplica quien
los pinta. Está justificado en el propio fichero (`DisplayPrefs.cs:14-19`).

### Aplicación al arrancar sin tocar el arranque

El encargo prohibía tocar `SceneBuilder.cs` y `GameBootstrap` está fuera de mi
carpeta. El enchufe vive dentro del encargo: `OptionsPanel` llama a
`DisplayPrefs.ApplySavedOnce()` en su constructor (`OptionsPanel.cs:34`), y como
`MainMenuView.Build()` construye el panel en su `Start()`
(`MainMenuView.cs:55-59,107`), lo guardado manda desde el primer momento de cada
sesión sin que nadie del arranque tenga que saber de esto. Un guardia de sesión
(`_sessionApplied`, `DisplayPrefs.cs:38,98-107`) evita reaplicar si el panel se
reconstruye al volver al título.

### La escala: dividir la referencia, no `PanelSettings.scale`

Con `ScaleWithScreenSize`, `PanelSettings.scale` solo manda en
`ConstantPixelSize`, así que no sirve. El mecanismo es dividir la resolución de
referencia del panel: escala 1,25 sobre 1920×1080 deja la referencia en
1536×864 (`DisplayPrefs.cs:114-131`) y toda la interfaz —layout incluido— sale
un 25 % mayor. Es la respuesta de fondo a las fuentes de 9-11 px del hotbar
(`HotbarView.cs:143,151,158`): una palanca global en vez de rediseñar el hotbar.

La base original se captura del propio activo la primera vez que se toca cada
instancia y se recuerda por instancia (`ReferenceBase`,
`DisplayPrefs.cs:40-44,117-121`): aplicar dos veces no compone escalas, y si
mañana `SceneBuilder` cambia la referencia del activo, el código no lleva el
1920×1080 escrito en ningún sitio. Por eso no hizo falta tocar ni el asset ni
`SceneBuilder`.

Menú y juego comparten la misma instancia de `PanelSettings`
(`SceneBuilder.cs:209,217`), así que basta resolver cualquier `UIDocument` vivo
(`DisplayPrefs.cs:137-140`, mismo recurso que `GateNoticeHost.cs:39`) para
escalar las dos capas de golpe.

### Controles

`OptionsPanel.DisplaySection()` (`OptionsPanel.cs:211-285`): desplegable de modo
con dos opciones —«Pantalla completa» (= `FullScreenWindow` sin bordes) y
«Ventana»—, desplegable de resolución alimentado por `Screen.resolutions` sin
duplicados y de mayor a menor (`DisplayPrefs.cs:145-163`), y deslizador de
escala 0,75–2,0 con lectura en porcentaje, pintado con el mismo
`PaintSlider` que los volúmenes (extraído, `OptionsPanel.cs:166-188`). Los
cambios aplican al momento, como un volumen; el disco, al salir.

## Lo del panel abierto, medido

El encargo avisaba: cambiar resolución reconstruye el layout. Lo que mi código
controla es no tirar nada por encima: ni `Clear()` ni reconstrucción al cambiar;
solo `Screen.SetResolution` y la referencia del panel. La prueba PlayMode
`CambiarLaResolucionConElPanelAbiertoNoLoTira`
(`EscalaEnElPanelTests.cs:110-135`) aplica escala 1,25, cambia el modo de
ventana con el panel abierto, espera dos fotogramas y comprueba que el panel
sigue colgado de un panel vivo, visible (`display: flex`), con el modo guardado
y **la escala intacta** (1536×864 sin pisar).

Límite honesto: en batchmode `Screen.SetResolution` no llega a mover una ventana
real, así que el reescalado píxel a píxel del rebuild no es medible desde aquí.
Lo que se midió es que nada de mi lado desmonta el panel abierto. Que el rebuild
de Unity se vea bien, lo decide el usuario mirándolo.

## Pruebas — números de las corridas

Editor (`Informes/pruebas/nimbo-ajustes-EditMode.xml`):
**609 pruebas · 607 pasadas · 0 fallos · 2 saltadas.** Línea base 588 + trabajo
de otros agentes de la ola + mis 4 nuevas:

1. `UnAjusteCambiadoSobreviveAGuardarYVolverALeer` — la prueba que pedía el
   encargo: set → flush → releer da modo/ancho/alto/escala exactos.
2. `LaEscalaSeQuedaDentroDelRangoUtil` — 9→2,0 y 0,01→0,75.
3. `LaEscalaDivideLaReferenciaDeUnPanelDeVerdad` — PanelSettings real de
   laboratorio: 1920×1080 → 1536×864 con escala 1,25, y aplicar dos veces no
   compone.
4. `ElPanelEnseaLosTresControlesDePantalla` — los tres controles existen con
   sus nombres (`escala-interfaz`, `modo-ventana`, `resolucion`), para que la
   costura control↔prueba no se pudra en silencio.

Juego (`Informes/pruebas/nimbo-ajustes-PlayMode.xml`):
**163 pruebas · 152 pasadas · 1 fallo · 10 saltadas.** Mis 2 nuevas, ambas en
verde:

5. `MoverElDeslizadorDeEscalaCambiaLaReferenciaDelPanel` — la segunda prueba
   que pedía el encargo: UIDocument real, `slider.value = 1.25f` (el callback de
   verdad, no una llamada interna) y la referencia del `PanelSettings` pasa a
   1536×864.
6. `CambiarLaResolucionConElPanelAbiertoNoLoTira` — descrito arriba.

Las pruebas apuntan los valores previos de las cuatro claves y los devuelven en
el teardown: las preferencias del jugador quedan como estaban.

### El único rojo no es mío

`EscapeCierraElPanelAbiertoYSinAbrirPausa` falla con: «ESC no cerró la mochila.
Falta aplicar el enganche del informe-teclas.md §3: UiRoot : IEscapeCloser…».
Evidencia de que no es mi costura: en todo `Scripts` nadie implementa
`IEscapeCloser` — solo existe la interfaz (`IEscapeCloser.cs:14,22`) y su
consumidor (`MainMenuView.cs:247-249`). Es el enganche pendiente del agente
nimbo-teclas sobre `UiRoot.cs`, fichero que no puedo tocar. Sin mis cambios ese
test falla igual: nunca hubo implementador que romper.

## Para el orquestador

- **Nada obligatorio.** No hay enganche pendiente de mi parte: la aplicación al
  arranque cuelga del constructor de `OptionsPanel`, que ya se construía siempre.
- Opcional, si algún día se quiere aplicar antes del primer fotograma: llamar a
  `DisplayPrefs.ApplySavedOnce()` desde `GameBootstrap`; hoy no hace falta y no
  lo pedí.
- Riesgos de la auditoría que siguen vivos a propósito: `match = 1` por altura
  (`SceneBuilder.cs:80`) no se ha movido —es ajuste global y el encargo lo
  prohíbe—; las fuentes de 9-11 px del hotbar siguen pequeñas por defecto, pero
  ahora existe la palanca de escala para quien las no vea.
- Nota sobre preferencias: PlayerPrefs del editor vive en la misma carpeta de
  usuario que la partida; estas pruebas solo escriben/borran sus cuatro claves
  `nimbo.pantalla.*` y restauran las previas. Los ficheros de partida no se
  tocan.
