# Veredicto: nimbo-memoria

Revisor: ox-alpha · Fecha: 2026-08-24 · Encargo: `Informes/encargos/nimbo-memoria.md`

```
VEREDICTO: APROBADO
DIFF REAL: commit 341d3d8 (punto de control del orquestador, mezcla varias olas).
  Del trabajo de memoria:
  - Assets/_Project/Scripts/Social/Memory/ConversationRecall.cs (nuevo, 378)
  - Assets/_Project/Scripts/Social/SocialService.cs (+30/-1: parámetro opcional
    memoryFlags, campo _recall, propiedad Recall, puerta RecallLine)
  - Assets/_Project/Tests/MemoriaConversacionalTests.cs (nuevo, 331)
  - Informes/informe-memoria.md (163)
¿CUADRA CON SU INFORME?: Sí. Cada fichero declarado existe con ese contenido y
  ninguna pieza más. Las citas a código ajeno verificadas una a una:
  SaveGame.cs:98 (Chronicle) ✓, HasFlag/SetFlag en SaveGame.cs:130/132 ✓,
  NewsBoard escribe ChronicleEntry(Today(), text) en NewsBoard.cs:344 (dentro del
  rango citado 335-347) ✓, IChronicleService en Core/Services/Contracts ✓,
  PersonalityProfile.Expression en [-1,1] ✓, ShortName ✓, SocialSection.cs:171-172
  con el Say("") mudo ✓, GameBootstrap.cs:245-246 construye SocialService sin
  banderas ✓, Nimbo.UI no referencia Nimbo.Social (asmdef: Data, Core, Player,
  TMP, InputSystem) ✓. La cita «SocialService.cs:555» para Rng.FromSeed hoy cae en
  583 porque su propio cambio añadió ~27 líneas encima; era correcta contra el
  árbol que editó. No es un defecto.
PRUEBAS: 588 editor / 586 pasadas / 0 rojas / 2 saltadas · 142 juego / 132
  pasadas / 0 rojas / 10 saltadas (Informes/pruebas/todo-edit-EditMode.xml y
  todo-play-PlayMode.xml). Línea base clavada. Las 13 de MemoriaConversacionalTests
  están dentro de las 588 y en verde, incluidas las dos que pedía el encargo:
  UnVecinoPuedeMencionarUnaRinaDeAyer y DejaDeMencionarloCuandoEnvejece (a 3 días
  cuenta, a 4 calla — la prueba anti-tablón-eterno está y pasa).
FUERA DE SU CARPETA: Ninguno que importe. Tocó Scripts/Social/**, su prueba en
  Assets/_Project/Tests/ — convención de toda la ola, ya asentada en
  veredicto-agenda.md — y su informe. No tocó UI, PlayerInteractor ni Bootstrap,
  que era exactamente su límite.
¿ENCHUFADO?: NO llega al jugador todavía, y es conforme al encargo: este decía
  literalmente que hablar pasa por PlayerInteractor o la ficha y «eso no es tuyo».
  Lo que sí hizo es lo segundo que le pedían: describir los enganches con línea y
  texto propuesto (informe §4):
  1. SocialSection.cs:171-172 → Say(social.RecallLine(_islanderId) ?? ""), más
     string RecallLine(string islanderId) en ISocialService (Nimbo.UI no ve
     Nimbo.Social; verificado en el asmdef).
  2. GameBootstrap.cs:245-246 → pasar _save.Flags como noveno argumento, sin lo
     cual el «ya te lo contó» no sobrevive a cerrar el juego.
  3. Tras aplicarlos, la prueba PlayMode de costura siguiendo
     CronicaEnLaIslaTests.cs. Hoy no existe y no podía existir: es de otro carril.
  PENDIENTE DEL ORQUESTADOR: ganchos 1 y 3 (el 2 del mundo es opcional y él mismo
  argumenta por qué no), y después la prueba de costura. Hasta entonces el sistema
  solo es alcanzable por pruebas.
DEFECTOS: Ninguno bloqueante. Dos notas menores, para que no se pierdan:
  - ConversationRecall.cs:141 detecta «lo propio» con Text.Contains(ShortName);
    un nombre corto dentro de otra palabra daría un falso propio. Consecuencia
    acotada —banco de voz o relación vetada equivocados, nunca frase rota ni
    excepción— y el comentario de StillTrue ya documenta que fallar del lado de
    permitir es el peor caso asumido. No pide vuelta.
  - El informe cita la suite entera en memoria-EditMode.xml; el canónico es
    todo-edit-EditMode.xml. Ambos dicen 588/586/0, así que es un despiste de
    cita, no de números.
AFIRMACIONES SIN RESPALDO: Ninguna relevante. Declaró «no he corrido PlayMode»
  en vez de presumir, y declaró «nadie lo llama desde la interfaz todavía» siendo
  cierto — las dos honestidades que más suelen faltar.
LO QUE ESTÁ BIEN:
  - No inventó memoria: lee IChronicleService vía ServiceRegistry, el mismo camino
    de PlayerAskFavour, y el «ya contado» vive en SaveGame.Flags con el pacto de
    siempre (parámetro opcional, sin partida en memoria de sesión).
  - Cada número tiene su porqué en ///: FreshDays=3, el rango 0,20–0,40 con el
    análisis tablón/invisible, el hash FNV porque el de string no es estable, la
    limpieza de banderas para que el guardado no engorde. Español, motivo no qué.
  - Dado determinista por día y vecino (Rng.FromSeed): que te cuente algo es un
    hecho del día, no del clic — y hay prueba que lo clava.
  - Las dos pruebas que el encargo marcaba como importantes existen y son las
    buenas: puede mencionar, y deja de mencionarlo al envejecer.
  - Se mantuvo en su carril en plena ola: cero toques en UI/Art/Game.
