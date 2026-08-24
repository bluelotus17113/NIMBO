# Veredicto — nimbo-ajustes

Fecha: 2026-08-24 · Verificador: nimbo-verificador-ajustes · Encargo: `Informes/encargos/nimbo-ajustes.md`

```
VEREDICTO: APROBADO
DIFF REAL: (su trabajo entró en el commit de barrido 19c23c5; el árbol está limpio en sus ficheros)
  Assets/_Project/Scripts/UI/Menu/DisplayPrefs.cs        | nuevo, 178 líneas
  Assets/_Project/Scripts/UI/Menu/DisplayPrefs.cs.meta   | nuevo
  Assets/_Project/Scripts/UI/Menu/OptionsPanel.cs        | sección Pantalla, flush, Restaurar, PaintSlider extraído
  Assets/_Project/Tests/AjustesDePantallaTests.cs        | nuevo, 4 pruebas de editor
  Assets/_Project/Tests/PlayMode/EscalaEnElPanelTests.cs | nuevo, 2 pruebas de juego
¿CUADRA CON SU INFORME?: sí, línea a línea (detalle abajo)
PRUEBAS: 609 editor / 607 verdes / 0 rojas / 2 saltadas · 163 juego / 151 verdes / 2 rojas / 10 saltadas
  — contra la línea base viva 588/142/0. Las dos rojas de juego NO son suyas (detalle abajo).
FUERA DE SU CARPETA: ninguno. Ni `NimboPanelSettings.asset` ni `SceneBuilder.cs`
  aparecen en el commit; sus cuatro ficheros son exactamente los declarados.
¿ENCHUFADO?: sí, por el camino que el propio encargo define:
  - Persistencia: `UnAjusteCambiadoSobreviveAGuardarYVolverALeer`
    (`AjustesDePantallaTests.cs:69`) — set → Flush → releer, los cuatro valores.
  - Llegada real: `MoverElDeslizadorDeEscalaCambiaLaReferenciaDelPanel`
    (`EscalaEnElPanelTests.cs:97`) — UIDocument vivo, callback real del deslizador,
    y afirma sobre `PanelSettings.referenceResolution` (1536×864), no sobre el deslizador.
  - Arranque: `MainMenuView.Start()` → `Build()` → `new OptionsPanel(...)` →
    `DisplayPrefs.ApplySavedOnce()` (`MainMenuView.cs:55-59,107` → `OptionsPanel.cs:34`),
    con guardia de sesión (`DisplayPrefs.cs:100-101`). El panel ya era alcanzable
    desde título y pausa antes de este trabajo.
DEFECTOS: ninguno bloqueante. Dos observaciones menores al final.
AFIRMACIONES SIN RESPALDO: ninguna. Todas las que se podían comprobar, cuadran.
LO QUE ESTÁ BIEN: (abajo, para que no lo deshaga)
```

## Las seis preguntas

**1. Compila y pasan las pruebas.** Sí. Leí los XML del orquestador
(`Informes/pruebas/todo-edit-EditMode.xml`, `todo-play-PlayMode.xml`) y los suyos
(`nimbo-ajustes-{EditMode,PlayMode}.xml`). Sus 6 pruebas nuevas, verdes en las dos
corridas. Editor 609/0 rojas. Juego tiene 2 rojas en el árbol entero:

- `TeclasEnLaIslaTests.EscapeCierraElPanelAbiertoYSinAbrirPausa` — ya fallaba en la
  corrida de ajustes (10:30), antes de que el orquestador barriera el árbol. El informe
  lo diagnosticó correctamente: en HEAD (`19c23c5`) grep de `IEscapeCloser` en
  `UiRoot.cs` da **0 coincidencias**; solo existen la interfaz y su consumidor
  (`MainMenuView.cs:247`). Es el enganche pendiente de nimbo-teclas. De hecho, mientras
  verificaba ha aparecido sin commitear una implementación en `UiRoot.cs` (mtime 11:04):
  trabajo vivo de otro, no de ajustes.
- `MemoriaEnLaIslaTests.NoTeCuentaOtraHistoriaElMismoDia` — posterior a la corrida de
  ajustes (XML de las 11:00; el de ajustes es de las 10:30 y solo tenía esa una roja).
  El propio mensaje del commit `19c23c5` documenta esta prueba como inestable y su
  reescritura. Dominio ajeno: vecinos que cuentan historias, nada que ver con pantalla.

Ninguna prueba borrada: 588→609 y 142→163, todo sumas de la ola.

**2. Su carpeta.** `UI/Menu/` para código, `Tests/` para las pruebas que el encargo
exige explícitamente («Comprobar: una prueba de… y otra de…»), como hace toda la ola.
Los dos ficheros prohibidos (`NimboPanelSettings.asset`, `SceneBuilder.cs`): ausentes
del commit, verificado con `git show 19c23c5 --stat`.

**3. Enchufado.** Sí según lo que el encargo pide probar, que no es la escena `Isla`
(su sistema vive en la capa menú, no en la isla): persistencia de verdad y aplicación
de verdad al `PanelSettings` en runtime, ambas con prueba. La cadena de arranque está
en el código y vericada a mano: `MainMenuView.cs:55-59` llama `Build()`, línea 107
construye el panel, `OptionsPanel.cs:34` aplica lo guardado. Sin guardia sería un
no-op repetido; con ella, una vez por sesión (`DisplayPrefs.cs:100`).

**4. Lo mínimo.** Un fichero nuevo de 178 líneas que hace exactamente lo pedido y nada
más: tres ajustes, aplicar, persistir. Reutiliza en vez de reimplementar: extrajo
`PaintSlider` (`OptionsPanel.cs:166`) porque había dos llamadas, sigue el patrón
`AudioPrefs` clave a clave (`nimbo.pantalla.*` contra `nimbo.volumen.*`,
`AudioPrefs.cs:20-22`), y resuelve el `PanelSettings` compartido en vez de tocar el
asset — que es justo lo que el encargo prohibía. Sin banderas ni configuración
especulativa.

**5. Comentarios.** `///` con el porqué en todos los puntos donde alguien cambiaría un
número sin saber qué rompe: por qué se divide la referencia y no `PanelSettings.scale`
(`DisplayPrefs.cs:18-25`), por qué la base se captura antes de tocar
(`DisplayPrefs.cs:47-50`), por qué el disco va al salir y no en cada arrastre
(`OptionsPanel.cs:78-80`), por qué dos modos y no tres (`OptionsPanel.cs:204-208`),
por qué 0,75 de suelo (`DisplayPrefs.cs:33`). En español. Bien.

**6. ¿Miente?** Comprobadas todas las afirmaciones verificables:

| Afirmación del informe | Veredicto |
|---|---|
| «609 · 607 · 0 · 2» editor y «163 · 152 · 1 · 10» juego en su corrida | exacto, coincide con sus XML |
| `MainMenuView.cs:55-59,107` construye el panel en Start | correcto |
| Menú y juego comparten instancia de PanelSettings (`SceneBuilder.cs:209,217`) | correcto, mismo objeto asignado a los dos UIDocument |
| `match = 1`, 1920×1080 (`SceneBuilder.cs:78-80`) | correcto |
| Fuentes 9-11 px en `HotbarView.cs:143,151,158` | correcto (10, 11 y 9) |
| Patrón de búsqueda de UIDocument como `GateNoticeHost.cs:39` | correcto |
| «nadie implementa IEscapeCloser» | cierto EN SU MOMENTO (HEAD no lo tiene); la implementación apareció después, sin commitear, de otro agente |
| Límite honesto del batchmode con `Screen.SetResolution` | bien declarado, sin inventarse lo que no midió |

No hay una sola frase sobre cómo se ve o se siente; donde tocaba decirlo, remite al
usuario («lo decide el usuario mirándolo», informe §Lo del panel abierto).

## Observaciones menores (no bloquean)

1. **Nombre de prueba sin ñ**: `ElPanelEnseaLosTresControlesDePantalla`
   (`AjustesDePantallaTests.cs:130`). La casa escribe el verbo con ñ cuando puede —
   `ViasEnLaIslaTests.cs`: `LaPantallaEnseñaLasCincoViasYLoQueVieneDespues`. Cosmético.
2. **Costura latente, hoy contenida**: el constructor de `OptionsPanel` ahora tiene un
   efecto secundario global (`ApplySavedOnce`). Solo su prueba de juego lo construye en
   play mode, y con preferencias apuntadas y borradas antes, así que la suite actual
   está limpia (0 rojas atribuibles). Si mañana una prueba que cargue escena construye
   el panel ANTES de apuntar las claves, la escala guardada del desarrollador se aplicaría
   al `PanelSettings` real a mitad de suite. Una frase en la documentación de
   `ApplySavedOnce` advirtiéndolo lo dejaría cerrado. Para el orquestador, no para otra
   vuelta de ajustes.

## Estado del árbol durante la verificación

Aviso para el orquestador: mientras revisaba, el árbol de trabajo ha ganado cambios sin
commitear en `UiRoot.cs` (+72, el enganche IEscapeCloser), `MemoriaEnLaIslaTests.cs` y
`AchievementsPanel.cs` (mtimes ~11:04). Son de otros agentes; ninguno toca los ficheros
de ajustes, que están limpios respecto a `19c23c5`.
