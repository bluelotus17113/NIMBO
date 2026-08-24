# Veredicto — nimbo-chibi

```
VEREDICTO: RECHAZADO
DIFF REAL: (commit 341d3d8, porción chibi)
  Assets/_Project/Scripts/Art/Chibi/MeshShapes.cs        +27 −1  (disco de cierre, :121-137)
  Assets/_Project/Scripts/Art/Chibi/ChibiMeshBuilder.cs  +62 −15 (BuildFaceQuad, :503-590)
  Assets/_Project/Tests/ChibiCercaTests.cs               +160 (nuevo) + .meta
  Assets/_Project/Tests/PlayMode/CapturaEstilo.cs        +48 −0 (RetrataVecinoDeEspaldas, :136-171)
¿CUADRA CON SU INFORME?: sí en código, no en resultados — el informe declara dos
  [PENDIENTE_DE_COLA] (PlayMode completo y captura) que nunca se rellenaron.
PRUEBAS: 588 editor / 142 juego / 0 rojas (todo-edit-EditMode.xml, todo-play-PlayMode.xml,
  contra línea base viva 588/142/0 — sin bajas). ChibiCercaTests: 2/2 verdes en la corrida
  completa. CapturaEstilo.RetrataElEstilo consta como Skipped por [Explicit]: es la
  herramienta de capturas de siempre, exención legítima.
FUERA DE SU CARPETA: ChibiCercaTests.cs (+meta) en Tests/ — declarado en el informe.
  CapturaEstilo.cs — concedido por el encargo. Nada más atribuible a chibi.
¿ENCHUFADO?: NO — es el motivo del rechazo (defecto 1).
DEFECTOS:
  1. La captura que el encargo exige no existe. El encargo dice «Esto se juzga mirándolo»
     y pide el encuadre nuevo en CapturaEstilo.cs; el código está (:136-171) y el informe
     dice qué mirar, pero `Capturas/estilo_vecino_de_espaldas_*.png` no existe — nadie lo
     ha visto jamás, autor incluido. El informe dejó «[PENDIENTE_DE_COLA]» en PlayMode y
     captura (informe-chibi.md:84-85) y no se tocó más (mtime 03:04); su propia corrida
     filtrada terminó a las 04:13, o sea que la cola estaba libre después del informe y
     no volvió a intentarlo. Acción: `NIMBO_ESTILO=<sufijo> Tools/agentes/unity.sh
     PlayMode nimbo-chibi "CapturaEstilo"`, mirar el PNG con lo que el propio informe
     promete (nuca con pelo sólido, interior del pelo como superficie, sin borde en
     canto), rellenar los dos huecos del informe y actualizarlo si lo visto contradice
     lo esperado.
  2. Comentario falso en la prueba nueva: ChibiCercaTests.cs:78-80 dice usar «los dos
     extremos de lo que se usa en ChibiMeshBuilder» con coberturas {0,44, 0,62}, pero las
     coberturas reales son {0,34 rapado (:355), 0,44 (:356), 0,52 (:357), 0,55 gorro
     (:302), 0,62 capucha (:260)}: el extremo bajo es 0,34 y no se prueba. La topología
     es idéntica para cualquier cobertura así que la aserción no pierde valor, pero el
     comentario miente sobre qué cubre. Acción: añadir 0,34 al bucle o corregir el
     comentario.
AFIRMACIONES SIN RESPALDO:
  - «cara y mejilla sombreadan como una sola superficie» (informe-chibi.md:46): el
    mecanismo es real (mismas normales analíticas del elipsoide, escritas y no
    recalculadas — verificado en ChibiMeshBuilder.cs:574-584), pero que el resultado se
    LEA como una sola superficie es un juicio visual que nadie ha hecho. Queda cubierto
    por el defecto 1: es exactamente lo que hay que mirar en la captura.
LO QUE ESTÁ BIEN (no deshacer):
  - El disco de cierre es correcto y mínimo: centro en y=0,5·cos(maxPhi) exactamente en
    el plano del último anillo (MeshShapes.cs:126), winding idéntico a la tapa inferior
    de Cylinder (:267), coste segments triángulos + 1 vértice. Los números del informe
    cuadran con el código: pelo 20 (:363), capucha 20 (:260), gorro 18 (:302), campana
    400 tris, +40 por vecino en el peor caso.
  - La cara usa los semiejes de verdad: la cabeza se escala (headWidth, headHeight,
    headWidth·0,94) sobre esfera de radio 0,5 (ChibiMeshBuilder.cs:141-143) → az =
    headWidth·0,47, exactamente lo que usa BuildFaceQuad. Las mates del informe verifican:
    (0,74)²+(0,62)²=0,932<1; sin triángulos nuevos (81 vértices/128 tris); offset +4 mm →
    −6 mm por SmoothStep(0,5,1,borde). El argumento convexidad→contorno enterrado es
    sólido y la prueba Lo afirma con f<0,99 en el borde.
  - El espejo de proporciones de ChibiCercaTests.cs:119-126 coincide línea a línea con
    HeightOf/HeadCentreOf (:92-105, HeadShare=0,42 en :62) y está declarado como copia a
    mantener.
  - Costura con nimbo-ropa respetada: RopaLookTests 9/9, RopaDiariaTests 3/3,
    RopaEnLaIslaTests 4/4, HairStyleTests 6/6 — los «16 de ropa y 6 de peinados» del
    informe son exactos.
  - Honestidad poco común: declara el fallo de su primera versión de la prueba, declara
    el estrechamiento del ~9 % de los rasgos como consecuencia asumida, y la debilidad
    de `limpia_huerfanos` que describe (unity.sh:39, `flock -n … || return 0` no evalúa
    al titular) es real y útil para el orquestador.
NOTA DE COSTURA (no es de chibi): el árbol tiene trabajo post-commit ajeno a este
  informe — CapturaPersonajes.cs +16 (encuadre personajes_desde_abajo, mtime 07:59),
  Capturas/personajes_desde_abajo.png y los *_antes.png, e ISocialService.cs/
  GameBootstrap.cs/SocialSection.cs (08:06). Ninguno aparece en informe-chibi.md ni es
  suyo por fechas; hay otro agente o el orquestador trabajando. No imputarlo a chibi,
  pero alguien debe reclamarlo.
