# Veredicto — nimbo-hud

Fecha: 2026-08-24 · Revisado contra `Informes/informe-hud.md` y el árbol real.
Contexto: ola con varios agentes en paralelo; los XML que cuentan son los del
orquestador (`Informes/pruebas/todo-edit-EditMode.xml`, `todo-play-PlayMode.xml`,
15:58 UTC).

```
VEREDICTO: APROBADO
DIFF REAL: Assets/_Project/Scripts/UI/Hud/HudView.cs (+40/-4),
           Assets/_Project/Scripts/UI/Achievements/AchievementsPanel.cs (filtros:
           fila nombrada, SetFilter público, marca del activo — ver reparto abajo),
           Assets/_Project/Scripts/UI/Decor/DecorPanel.cs (+42),
           nuevos: Tests/FiltrosVisiblesTests.cs (5) y Tests/PlayMode/HudEnLaIslaTests.cs (2)
¿CUADRA CON SU INFORME?: sí, con un reparto que conviene conocer (ver nota 1)
PRUEBAS: editor 609 totales · 607 pasadas · 0 fallos · 2 saltadas (línea base 588/0/2)
         juego 163 totales · 151 pasadas · 2 fallos · 10 saltadas (línea base 142/0/10)
         Las 7 pruebas de nimbo-hud: todas Passed en el XML del orquestador.
FUERA DE SU CARPETA: ninguno atribuible a nimbo-hud. UiRoot.cs y GameEvents.cs
         intactos, como prometió el informe.
¿ENCHUFADO?: sí — HudEnLaIslaTests.PulsarElBadgeDePeticionesAbreElTablon carga la
         escena Isla, envía un ClickEvent sintético sobre `hud-peticiones` montado y
         comprueba que `tablon-encargos` pasa a Flex; la segunda prueba comprueba el
         cierre. Ambas en verde en todo-play-PlayMode.xml. El canal es el real:
         HudView.cs:128-129 publica RequestBoardRead y UiRoot.cs:424-428 ya lo
         convertía en abrir/cerrar.
DEFECTOS: ninguno bloqueante. Dos notas de costura para el orquestador (abajo).
AFIRMACIONES SIN RESPALDO: ninguna. Todas las citas fichero:línea del informe se
         verificaron contra el código y cuadran (±2 líneas).
LO QUE ESTÁ BIEN: reusó el aviso existente en vez de inventar un canal; copió el
         patrón CraftPanel.BuildStations (CraftPanel.cs:97-111) en vez de inventar
         otro; dejó los dos callbacks null-safe y declaró por escrito que están
         inertes hasta el enganche §2.A; midió y describió el bloqueo de puntero
         (§2.C) en vez de arreglarlo fuera de su carpeta; quitó los tooltips cuando
         chocaron con GateNotice y lo re-midió (AvisosEnLaIslaTests 2/2 en verde).
```

## Verificación de las seis preguntas

**1. Compila y pasan las pruebas.** Sí. Editor 609/607/0/2 contra línea base
588/0/2: no falta ninguna prueba. Juego 163/151/**2 fallos**/10 contra 142/0/10.
Los dos fallos no son suyos, con evidencia del propio XML:

- `TeclasEnLaIslaTests.EscapeCierraElPanelAbiertoYSinAbrirPausa` — su mensaje de
  fallo dice textualmente «Falta aplicar el enganche del informe-teclas.md §3:
  UiRoot : IEscapeCloser…». Es el enganche de otro agente en `UiRoot.cs`, que nadie
  ha aplicado todavía.
- `MemoriaEnLaIslaTests.NoTeCuentaOtraHistoriaElMismoDia` — simulación social,
  módulo ajeno al HUD; `informe-seguridad.md` ya la documentaba fallando de forma
  distinta entre corridas antes de esta revisión.

Las 5 de `FiltrosVisiblesTests` y las 2 de `HudEnLaIslaTests`: Passed, una a una,
en los XML del orquestador. `TiendasEnLaIslaTests`, que el informe declaró inestable
en mitad de la ola, pasó 2/2 en la corrida final.

**2. ¿Se ha salido de su carpeta?** No. Su delta atribuible cabe entero en
`UI/Hud/HudView.cs`, `UI/Achievements/AchievementsPanel.cs`, `UI/Decor/DecorPanel.cs`
y las dos pruebas nuevas que el encargo le pedía explícitamente («Comprobar»).

**3. ¿Está enchufado?** El badge, sí, con prueba de juego en escena real (ver arriba).
Los callbacks de nimbos y aldeano **no están enchufados y el informe lo dice** —no
vende como hecho lo que es pendiente—, y deja descrito el enganche exacto (§2.A):
verifiqué que `Toggle(System.Action, bool)` existe en `UiRoot.cs:354-357`, que los
botones de tienda usan exactamente esa forma (`UiRoot.cs:237-239`) y que `_shop` y
`_skills` se construyen después que el HUD (`UiRoot.cs:127` y `:145`), así que la
propuesta es aplicable tal cual.

**4. ¿Es lo mínimo?** Sí. ~40 líneas en el HUD, ~40 por panel de filtros, dos
ficheros de prueba acotados. Reuso de canal existente, patrón copiado del repo, ni
una bandera ni una abstracción de más.

**5. ¿Comentarios con el porqué?** Sí: por qué callback y no evento
(`HudView.cs:36-46`), por qué sin tooltip junto a GateNotice (`HudView.cs:89-91`),
por qué `SetFilter` es público, por qué el inline pisando el `:hover` del activo es
deliberado (`AchievementsPanel.cs`, remarks de `BuildFilters`). En español.

**6. ¿Miente?** La afirmación más fuerte del informe es §2.C («con el puntero
capturado, TODO el HUD es inclicable») y es cierta: `IslandCamera.cs:49` arranca con
`_freeLook = true`, `ShouldLook` (`:335-336`) solo se frena por pausa/panel/puntero
pedido, `CapturePointer` (`:350-353`) bloquea el cursor, y `UiRoot.RefreshPointer`
(`UiRoot.cs:639-646`) solo publica `PointerNeeded(true)` cuando hay panel abierto.
Es decir: durante el juego normal ningún botón del HUD es físicamente clicable —
incluidos los catorce de la barra, que ya tenían ese problema antes de esta ola.
Correctamente descrito y devuelto a su dueño (`IslandCamera.cs` es de Nimbo.Art).

## Notas de costura para el orquestador (no son defectos de nimbo-hud)

1. **`AchievementsPanel.cs` tiene dos autores en esta ola.** El diff del fichero
   mezcla los filtros de nimbo-hud (fila `filtros-logros`, `SetFilter`, marca del
   activo) con el refresco por firma de nimbo-seguridad (`Refresh()`/`Firma()`/
   `_signature`, líneas 110-146 actuales), que su propio informe reclama en su tabla
   (§2, fila «Logros»). Los dos encargos asignaban el mismo fichero: el solape es
   decisión de orquestación, y ninguno de los dos salió de su carpeta. El resultado
   combinado compila y pasa.
2. **Al aplicar ambos enganches, un clic de filtro reconstruye dos veces.**
   `SetFilter` (logros y adornos) repinta directamente pero no sella `_signature`;
   cuando esté el enganche de `informe-seguridad.md`, la siguiente pasada lenta
   (0,4 s) detectará la firma cambiada —el filtro entra en ella— y reconstruirá otra
   vez. Inofensivo (una reconstrucción extra por clic), pero si se quiere fino:
   sellar `_signature` al final de `SetFilter`.
3. **El §2.C es previo a este encargo y afecta a toda la barra.** Hasta que se decida
   el puntero, el badge «funciona hoy» solo en el sentido de cableado: la prueba lo
   demuestra con clic sintético, pero un jugador no puede llegar al clic. El informe
   no lo oculta; lo digo aquí porque cambia la prioridad real del §2.A frente al §2.C.
