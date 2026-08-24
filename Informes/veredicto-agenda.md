# Veredicto: nimbo-agenda

Verificador: ox-alpha · Fecha: 2026-08-24 00:00 · Árbol revisado con cinco agentes aún activos

```
VEREDICTO: APROBADO
DIFF REAL: AgendaProjection.cs (nuevo, 109 líns), AgendaSection.cs (nuevo, 110),
  IslanderPanel.cs (+9: campo :37, alta en constructor :108-112, refresco :176),
  Tests/AgendaDeLosVecinosTests.cs (nuevo, 141), Tests/PlayMode/AgendaEnLaFichaTests.cs
  (nuevo, 134), más informe e intentos en Informes/. Los demás ficheros del diff
  (Chibi*, Creator*, SocialService, GDD, unity.sh…) son de otros agentes de la ola.
¿CUADRA CON SU INFORME?: Sí. Verifiqué una a una sus rutas: IslanderBrain.cs:58-95,
  :112 y :121-139 con umbral 0,3 en :126-128 ✓; GameClock.cs:59 ✓; constructor de
  PersonalityProfile (energy, expression, attitude, outlook) en PersonalityProfile.cs:34
  ✓ — los cuatro perfiles de sus pruebas caen en la rama que su nombre dice;
  IIslanderRegistry.All/Count/TryGet ✓; GameBootstrap.cs:55 (_starterIslanders = 3) ✓;
  GameBootstrap.cs:342 (Register<GameClock>) ✓; Nimbo.UI.asmdef sin referencia a
  Simulation ✓; NeedState.cs:43 ✓. Dos derivas de línea menores, ver DEFECTOS 2.
PRUEBAS: Editor 7/7 pasadas MEDIDO (agenda-EditMode.xml, 22:38). Juego 2/2 pasadas
  MEDIDO (agenda-PlayMode.xml, 23:54): LaFichaAbiertaEnseniaElDiaDelVecino y
  CambiarElCaracterCambiaLaAgendaEnElMismoRefresco, ambas en verde, log sin un solo
  «error CS» — o sea, el proyecto entero compiló dos veces con sus ficheros dentro.
  Línea base del orquestador esta mañana: 529 editor / 117 juego / 0 rojas (los
  «514/92» de _reglas.md están desfasados; el invariante que importa —cero en rojo—
  se cumple). No borró ninguna prueba.
FUERA DE SU CARPETA: Ninguno. IslanderPanel.cs está dentro de su carpeta. Sus pruebas
  viven en Assets/_Project/Tests/, como las de los seis agentes de esta ola, y su
  encargo las exige explícitamente («Comprobar: una prueba de juego que cargue Isla»).
¿ENCHUFADO?: Sí, y demostrado en juego. IslanderPanel.cs:112 lo cuelga en el scroll y
  :176 lo refresca; la prueba carga Isla, abre la ficha del primer vecino del censo y
  exige que el texto enseñado sea Haunt(vecino.Personality) —la personalidad REAL, no
  una constante—, y la segunda prueba siembra otro carácter y exige que la tarjeta
  cambie en el siguiente Refresh(). Es exactamente el antídoto contra el «texto fijo
  disfrazado» que pide la regla de la casa. El arnés es idéntico al de
  FichaEnLaIslaTests (7/7 en retomo-PlayMode.xml), mismo SetUp, misma carga de escena.
DEFECTOS:
  1. Menor, no bloqueante — AgendaEnLaFichaTests.cs:77: Has.Some.Contains("Duerme")
     con mayúscula solo casa con el texto de quien tiene casa («Duerme en su casa.»);
     sin casa el texto es «duerme donde le pille» y el assert fallaría aunque la
     tarjeta esté bien. Hoy no puede dispararse (isla nueva: 3 vecinos e
     IslandLayout.cs:36 da 4 viviendas por zona), pero acopla la prueba al orden del
     censo y a la vivienda libre. Bastaría buscar "uerme".
  2. Cosmético — informe-agenda.md §2.3 dice «líneas 37, 108-113 y 172»; reales:
     37, 108-112 y 176. La sustancia cuadra; los números no.
AFIRMACIONES SIN RESPALDO:
  - §1 «rg … solo encontraba RelationshipBook.cs:7»: el estado previo del árbol no es
    reconstruible ahora; plausible (hoy solo AgendaSection contiene «Su día») y sin
    consecuencia.
  - §5 las tres corridas abortadas con errores ajenos: los logs se sobreescriben por
    corrida y no son auditables; consistente con que los tres ficheros citados son de
    memoria/creador/fiestas y con que a las 22:38 todo compiló. Sin consecuencia.
  - Cero afirmaciones sobre cómo se ve o se siente. §6 son instrucciones de comprobación
    manual, no invenciones.
LO QUE ESTÁ BIEN:
  - La honestidad de §5: declara «no puedo decir que las pruebas pasaron, y no lo digo»
    mientras la cola estaba bloqueada, y deja por escrito que el XML caería en
    Informes/pruebas/. Cayó, y en verde. Es lo contrario de la enfermedad típica.
  - Proyectar en vez de copiar los ritmos de NeedsConfig: decisión correcta, razonada
    en los remarks de AgendaProjection.cs, con la costura IAgendaService descrita con
    contrato completo y firmas (§4 A-E) tal como pedía el encargo cuando el sistema
    no es alcanzable desde tu ensamblado.
  - La firma de refresco con tramo activo (AgendaSection.cs:71-74): el refresco de
    0,4 s no repinta salvo cambio de persona, carácter o tramo día/noche.
  - Las pruebas de frontera (0,3 exacto, envolvimiento 23→7) clavan la copia al
    original: si alguien toca el cerebro, la prueba lo delata.
  - Cero toques fuera de carpeta en plena ola de seis.
```

## Nota para el orquestador

Entre las 22:38 y las 23:54 la cola estuvo bloqueada por un Unity de **nimbo-memoria**
(pid 67682, EditMode filtrado) que pasó ~75 min sin escribir una línea al log: colgado
dentro del cerrojo, así que la limpieza de huérfanos del envoltorio no podía actuar
(ve alguien dentro del flock y se retira). Se descolgó solo a las ~23:54. No es defecto
de nadie en concreto esta vez, pero el envoltorio no tiene salida para ese caso: un
Unity vivo dentro del lock y mudo durante N minutos merecería vigilancia.

Mi propia corrida de verificación (verif-agenda) murió con el timeout esperando turno
detrás de ese cuelgue; la evidencia de juego la dio la corrida que el propio agente
tenía encolada (agenda-PlayMode.xml, 23:54, 2/2).
