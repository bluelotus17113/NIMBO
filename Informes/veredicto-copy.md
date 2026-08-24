# Veredicto — nimbo-copy

Revisado el 24/08/2026 contra `Informes/encargos/nimbo-copy.md` y `Informes/informe-copy.md`.
Ojo al marco: mientras revisaba, el orquestador commiteó la ola entera (`19c23c5`, 10:57),
incluida la reescritura de `NoTeCuentaOtraHistoriaElMismoDia`. Los diffs que citan líneas
«hoy» se midieron sobre ese estado.

```
VEREDICTO: RECHAZADO
```

**Un solo defecto, y es del informe, no del código.** Los cuatro arreglos están hechos,
verificados línea a línea, y el quinto correctamente delegado. Lo que no cuadra es la
reclamación de autoría sobre las pruebas de la corrida — y un informe que atribuye a otro
trabajo propio no pasa, aunque el código esté bien.

```
DIFF REAL (hoy en 19c23c5):
  Assets/_Project/Scripts/UI/ItemNames.cs              | 30 ±   Display/Of/Short; una sola fuente
  Assets/_Project/Scripts/UI/Menu/SaveSummary.cs       | 22 ±   Ago pública y pura + UiTheme.Plural
  Assets/_Project/Scripts/UI/Minigames/MinigamePanel.cs|  6 ±   _headline Title→Body negrita
  Assets/_Project/Scripts/UI/Player/BagPanel.cs        | 16 ±   ItemNames.Display; NameOf y _economy borrados
  Assets/_Project/Scripts/UI/Player/HotbarView.cs      | 15 ±   ItemNames.Short; ShortName borrado
  Assets/_Project/Scripts/UI/Requests/RequestBoardPanel.cs | 90 ± | ver nota de costura abajo
  Assets/_Project/Scripts/UI/UiTheme.cs                | 35 +   Plural y Plazo (sección «Texto»)
  Assets/_Project/Tests/TextoEnPantallaTests.cs        | 315 +  nuevo, 6 pruebas

¿CUADRA CON SU INFORME?: casi todo sí, una cosa NO.
  SÍ: los ocho ficheros declarados son exactamente los suyos; cada descripción de código
      coincide con el diff (Display manda y Of/Short son vistas; Ago pura; Deadline delega
      en Plazo comparando minutos; headline a Body). La búsqueda de ids crudos cuadra
      exacta: rg '\{evt\.[A-Za-z]*Id\}' devuelve tres sitios y son los que publica
      (NewsBoard.cs:284-286 defecto; NewsBoard.cs:203 clave interna; GameBootstrap.cs:186
      traza contable). La atribución de las tres pruebas rojas de PlayMode como ajenas
      cuadra: los mensajes del XML dicen literalmente lo que el informe cuenta.
  NO: «línea base 588 + 17 nuevas mías = 605» (informe-copy.md:10-12 y :181-183).
      Falso. Su aportación medida es 6: testcasecount="6" para TextoEnPantallaTests en su
      propio XML, sin un solo [TestCase] que lo multiplique. Los otros 11 los puso otra
      gente: +2 FiestasTests (commit bfda61c, 08:27), +4 TeclasTests (nimbo-teclas) y
      +5 FiltrosVisiblesTests (otro frente), todos ya presentes en el árbol cuando corrió.
      La explicación «(6 pruebas públicas, alguna con varios asserts)» confunde asserts
      con casos de prueba. Vio 605−588=17, sabía que había escrito 6, y no se preguntó
      de dónde salían los otros 11.

PRUEBAS: editor 605 / 603 pasadas / 0 rojas / 2 saltadas; juego 161 / 148 / 3 rojas
  (todas ajenas, verificado por mensaje) / 10 saltadas — contra la línea base viva
  588/142/0. Nada por debajo de la base. Nota para el orquestador: los XML todo-* son de
  las 07:53, ANTERIORES a los commits de 07:56–08:31, así que el «588» de referencia ya
  se queda corto para HEAD; la próxima línea base debería recalcularse.

FUERA DE SU CARPETA: estrictamente, UiTheme.cs y Tests/TextoEnPantallaTests.cs no están
  en la lista del encargo. No lo cobro: el propio encargo exige «un sitio para el plural,
  no un if en cada llamada» y «pruebas de editor», así que algún sitio había que elegir,
  no hay colisión material (ningún otro informe de la ola reclama UiTheme.cs, y las +35
  líneas son solo Plural/Plazo), y el fichero de pruebas es nuevo. Que el próximo encargo
  liste dónde vive el ayudante y dónde las pruebas.

¿ENCHUFADO?: no hay sistema nuevo que enchufar — son arreglos de texto sobre superficies
  ya cableadas. La costura en modo juego está demostrada: TablonVisibleTests (9 casos,
  PlayMode, escena Isla, ejercitan ItemNames vía las filas del tablón) pasaron los 9 en su
  corrida. La prueba nueva de las tres pantallas es de editor con dobles; suficiente para
  un arreglo de redacción.

DEFECTOS:
  1. Informe-copy.md:10-12 y :181-183 — «588 + 17 nuevas mías». Corregir la
     descomposición: 588 base → +2 FiestasTests (commit bfda61c) +4 TeclasTests
     +5 FiltrosVisiblesTests +6 TextoEnPantallaTests = 605. Es defecto de informe, no de
     código: no hace falta volver a correr Unity para arreglarlo, pero el registro tiene
     que decir de quién es cada prueba.

AFIRMACIONES SIN RESPALDO: ninguna dañina. Al contrario: el único cambio visual del lote
  (minijuego) va marcado como «hay que mirarlo en pantalla», y el defecto 1 admite por
  escrito que el literal «hace 1 minutos» no era reproducible en HEAD por la guarda de
  «un momento». Eso es honestidad, no invención.

NOTA DE COSTURA (para el orquestador, no es defecto de copy): el mecanismo
  Refresh/Firma/_signature de RequestBoardPanel.cs es de nimbo-seguridad (su informe lo
  documenta y pide el enganche). El comentario de `_signature` dice «el ciclo lento de
  UiRoot llama a Refresh cada 0,4 s» y HOY ES FALSO: nadie llama a _board.Refresh()
  (UiRoot.cs refresca _panel/_shop/_map/_bag/_furnish, no el tablón). El enganche sigue
  pendiente tal como describe informe-seguridad.md. Copy cambió encima de ese mecanismo
  legítimamente (Deadline y la firma usan ahora el texto pintado de UiTheme.Plazo), y su
  informe lo describe bien — pero quien lea el comentario debe saber que el cable no existe
  todavía.

VERIFICACIÓN DEL ANÁLISIS DEL DEFECTO 5 (lo ejecutará el orquestador): contrastado y
  correcto. Publicador único: IslandService.cs:210. Evento: GameEvents.cs:344-347.
  Suscriptores restantes: JobService, AudioDirector, AchievementService, WorldView —
  exactamente los cuatro ficheros que lista. NewsBoard.cs:284-286 sigue con el id crudo,
  intacto como mandaba el encargo. La recomendación (opción A, campo DisplayName con
  valor por defecto) está razonada sobre números reales.

LO QUE ESTÁ BIEN (para no deshacerlo en la siguiente vuelta):
  - Una fuente de verdad para el nombre (ItemNames.Display) y dos vistas; borró los dos
    recortadores privados y el campo _economy que solo los alimentaba. Menos código que
    antes, no más.
  - Ago pública y pura: cada tramo se defiende solo sin depender de la guarda anterior.
  - Los casos frontera que pedía el encargo, todos: 1, 59, 60, 61, 89, 90 minutos — 61 y
    89 son justo los que mentían — y la prueba de las tres pantallas a la vez con dobles
    mínimos, que habría cazado el defecto 3 el día que se introdujo.
  - SetUp/TearDown guardan y restauran el registro global: no contamina a las demás.
  - Comentarios /// que explican el porqué («al sexto sitio le sale bien y al séptimo se
    olvida»; «redondear antes de comparar hacía que…»). Ejemplares.
  - El defecto 5 delegado con análisis verificable en vez de parcheado fuera de carpeta:
    es exactamente lo que pedía el encargo.
```

**Qué hacer:** nimbo-copy corrige únicamente la descomposición numérica de su informe
(defecto 1). Sin tocar código, sin re-corridas obligatorias. Con esa enmienda, el trabajo
pasa: todo lo demás está verificado contra el árbol.
