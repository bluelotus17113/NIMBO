# Veredicto: nimbo-fiestas

Verificador: ox-alpha · Fecha: 24/08/2026 · Encargo: `Informes/encargos/nimbo-fiestas.md`

```
VEREDICTO: APROBADO
DIFF REAL: El trabajo llegó dentro del commit compartido 341d3d8 («Ola 5»). La huella de
  fiestas en él es exactamente: Assets/_Project/Scripts/Art/World/FestivalDecor.cs (+157,
  nuevo), Assets/_Project/Scripts/Art/World/FestivalDecorBuilder.cs (+249, nuevo), más sus
  .meta; Assets/_Project/Tests/DecoradoDeFiestaTests.cs (+135, nuevo) y
  Assets/_Project/Tests/PlayMode/FiestaDecoradaEnLaIslaTests.cs (+201, nuevo), más sus
  .meta; e Informes/informe-fiestas.md. Nada más.
  El árbol sucio actual (ISocialService.cs, GameBootstrap.cs, SocialSection.cs,
  CapturaPersonajes.cs, Capturas/*.png, veredicto-memoria.md) es trabajo de otros agentes
  posteriores — ni una línea suya.
¿CUADRA CON SU INFORME?: Sí, con dos erratas numéricas menores (ver DEFECTOS 1 y 2).
  Verificado contra el código, no contra su prosa: los costes 300/1.200
  (VillageEvents.cs:28-29), la publicación de los avisos (EventScheduler.cs:80 y :162),
  los structs (GameEvents.cs:369-384), el único suscriptor previo AudioDirector
  (:100-101, :191-192, :206-216), plaza 18 m / escenario 16 m con nivel 10
  (IslandLayout.cs:27,89-90), amarillo de farola 0xF2C96B (DecorMeshBuilder.cs:147),
  ToonPalette.Flowers (:39), TryGetWorldCentre (BuildService.cs:76-89), IslandService
  consumiendo IslandLayout.FirstIsland() (:44) y usándolo para el centro (:174),
  RuntimeInitializeOnLoadMethod de GateNoticeHost (:87), FlowController escuchando
  ProtagonistCreated (:60-61), _startFromMenu: 1 (Isla.unity:698) y el fallo histórico
  «stage»/«plaza_central» documentado (FiestasTests.cs:205-224). Todas ciertas.
PRUEBAS: 588 editor / 142 juego / 0 rojas — contra la línea base 588/142/0. Exacto:
  586 pasadas + 2 saltadas y 132 pasadas + 10 saltadas (todo-play XML leído, no
  re-corrido). Las suyas están dentro y en verde: DecoradoDeFiestaTests 3/3 (editor),
  FiestaDecoradaEnLaIslaTests 4/4 (juego). No falta ninguna prueba de la base.
FUERA DE SU CARPETA: ninguno. Solo ficheros nuevos en Scripts/Art/World/ (su carpeta) y
  en Tests/ y Tests/PlayMode/, donde el propio encargo exige las pruebas. La costura del
  vecino NO fue aplicada: IslanderBrain.cs no tiene ninguna referencia a fiestas y
  IVillageEvents solo expone ActiveEventId (:57) — quedó especificada en su sección 4,
  como mandaba el encargo.
¿ENCHUFADO?: Sí, y por la puerta grande. FiestaDecoradaEnLaIslaTests.cs carga la escena
  Isla de verdad, arranca la partida publicando ProtagonistCreated (lo mismo que hace el
  menú vía FlowController.cs:60), publica por el bus justo la firma que publica
  EventScheduler.StartEvent, y exige: decorado a menos de 1 cm del centro real de la zona
  (IBuildService.TryGetWorldCentre) con ≥6 grupos de malla; recogida al publicar
  VillageEventEnded (y un segundo cierre no deja nada); concierto en el escenario —
  abriendo la zona cerrada por la puerta pública IIslandService.Unlock—; market_day e id
  inventado no cuelgan nada y la fiesta buena siguiente sí se pinta. Además el arranque
  automático ([RuntimeInitializeOnLoadMethod], FestivalDecor.cs:67) replica el camino de
  GateNoticeHost, y OnGameLoaded (:75-86) repone el decorado al cargar partida con la
  fiesta a media sesión. Prueba de aparición, no solo de ausencia — aprendió la lección
  de su propia 5.ª corrida y lo cuenta.
DEFECTOS:
  1. Menor, en el informe, no en el código: «6 mallas nuevas por fiesta»
     (informe-fiestas.md, sección 3.1). Son 5 colocadas: madera, farolillos y tres
     combinados de banderines (FestivalDecorBuilder.cs:92-103) — su propia enumeración
     entre paréntesis lista 5. Si contó el molde del banderín, ese se destruye dentro de
     Build (:126-127) antes de que la fiesta esté en pie. Corregir el número.
  2. Menor, contexto de la sección 1: «WorldView se suscribe a ocho eventos». En los
     rangos citados (WorldView.cs:59-72,103-110) hay 9 tipos de evento distintos
     (GameLoaded va aparte en OnEnable). La conclusión —ninguno era de fiesta— es cierta
     igualmente. Corregir el número.
  Ninguno de los dos afecta al código, a las pruebas ni al comportamiento.
AFIRMACIONES SIN RESPALDO: Ninguna relevante. La sección 6 declara expresamente lo que
  NO puede saber (si se ve bien de cerca, si la comba convence, si seis mástiles bastan)
  y no hay ni un «queda fluido» en todo el informe. Los números que spot-checké cuadran:
  48 banderines (8×6), 12 farolillos (2×6), comba 0,8 m, triángulo 0,60×0,62 m,
  radio 10 m, ±1 cm de la prueba (<0.01f).
LO QUE ESTÁ BIEN:
  - Los ids de zona no se escriben a mano: la tabla guarda el PROPÓSITO y lo resuelve
    contra el plano real (FestivalDecor.ZoneWithPurpose, :148-155). Y la duplicación que
    esa tabla implica está vigilada en las dos direcciones por
    DecoradoDeFiestaTests.LaTablaDelDecoradoSigueAlCalendario — evento nuevo sin línea =
    roja, clave zombi = roja. Es la respuesta correcta al fallo histórico del encargo.
  - Guardas con motivo escrito: zona cerrada no se decora (:106-110), sin vista no hay
    isla (:120-122), TearDown defensivo antes de poner (:90-93). Nada pinta en el vacío.
  - Reutiliza lo que existía: malla del catálogo para las macetas
    (DecorMeshBuilder.For(Plant)), paleta de flores y amarillo de farola, caché de
    ToonPalette. Cero abstracciones para casos que nadie pidió, cero banderas.
  - Comentarios /// que explican el porqué en cada decisión: por qué radio 10, por qué
    parábola y no catenaria, por qué dos triángulos con devanado opuesto y no un
    material de doble cara, por qué sin colisionador. Modelo de la convención de la casa.
  - Higiene de memoria pensada: mallas nuevas por fiesta y destruidas al recoger, barrido
    de referencias muertas tras cambio de escena (:55-57,136-138), y el molde destruido
    con Destroy/DestroyImmediate según modo (:122-127) porque sus pruebas de editor lo
    llaman fuera de play.
