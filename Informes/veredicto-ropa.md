# Veredicto — nimbo-ropa

Verificador: nimbo-verificador · 2026-08-23

```
VEREDICTO: RECHAZADO

DIFF REAL:
  Su carpeta (Wardrobe/**, Art/Chibi/**):
    WardrobeService.cs        +53/-1   (suscripción DayPassed, OnDayPassed, Dispose, comentario de FavouriteOf)
    ChibiMeshBuilder.cs       +189     (ChibiBodyMeshes, Build(appearance,look), piernas piel→ropa, falda, piezas Extra)
    IslanderView.cs           +135     (lee EquippedOutfit, RefreshOutfitIfChanged, sondeo 0,5 s, SwapMesh, OnDestroy)
    OutfitLook.cs             nuevo (117 líneas) + .meta
  Declarados fuera de carpeta:
    Core/Services/Contracts/IEconomyService.cs  +14 (Slot/Style/Palette en IItemDefinition)
    Tests/InventoryTests.cs                     +3  (FakeItemDef implementa los 3 nuevos)
    Tests/PlayMode/Nimbo.PlayTests.asmdef       +2  («+1 referencia» según el informe; añade DOS)
  Pruebas nuevas:
    Tests/RopaLookTests.cs (8), Tests/PlayMode/RopaEnLaIslaTests.cs (4) + metas
  El resto del árbol sucio (IslandCamera, NimboTree, Meadow, fog de Isla.unity,
  GateNotice*, CapturaEstilo, Capturas/*, informes y logs) es de los otros agentes
  en paralelo; nada de ello lleva huella de ropa.

¿CUADRA CON SU INFORME?: Sí en todo, salvo dos desviaciones menores:
  - asmdef: dice «+1 referencia a Nimbo.Simulation»; el diff añade también
    «Nimbo.Island», que no declara y que parece sobrante (CronicaEnLaIslaTests
    usa Nimbo.Core/Nimbo.Data con el asmdef viejo sin referencias directas, así
    que la resolución transitiva ya cubre lo que la prueba toca).
  - PlayMode completa: informa 117 pasadas / 5 fallos; ola34 dice 121 / 1.
    Explicado, no inventado: su corrida es anterior y los otros agentes
    arreglaron lo suyo después. El informe además lista esos 5 fallos como
    ajenos y nombrados, y ola34 confirma que el único rojo restante es
    DelanteDelArbolElCartelOfreceHablarle —el excusado por el orquestador—.

PRUEBAS: EditMode 565 / 563 pasadas / 0 rojas / 2 saltadas (línea base 514/2 ✓).
  PlayMode 132 / 121 / 1 roja / 10 saltadas (base 92/9 ✓ en total; la roja es la
  del Árbol, ajena y declarada por el propio agente; la saltada de más es
  SondaDelPuente.MideElPaso, de otro agente). Las 12 pruebas de ropa pasan en
  ola34: 8 de RopaLookTests y las 4 de RopaEnLaIslaTests.

FUERA DE SU CARPETA: IEconomyService.cs, InventoryTests.cs y el asmdef —los tres
  declarados—. Vía correcta: contrato compartido en Nimbo.Core, no referencia
  nueva desde Nimbo.Art. Ningún fichero indebido.

¿ENCHUFADO?: Sí para la mitad principal del encargo. RopaEnLaIslaTests.cs carga
  la escena Isla, equipa ids reales (verifiqué los cuatro en
  Resources/Config/catalogo_ropa.json) y certifica la MALLA: PonerleUnPantalon…
  mide vértices piel−/ropa+ y _BaseColor #8B7355; ElCambioDeRopaSeVeSolo…
  demuestra el sondeo sin aviso externo. NO enchufado en cambio la otra mitad:
  OnDayPassed (WardrobeService.cs:66-84) no tiene ni una prueba que publique
  DayPassed — defecto 2.

DEFECTOS:
  1. Un sombrero tiñe de su color TODA la ropa del cuerpo. OutfitLook.From caso
     "hat" (OutfitLook.cs:69-72) devuelve HasGarment=true con Garment=paleta[0],
     e IslanderView pinta la malla «ropa» con look.Garment cuando HasGarment
     (IslanderView.cs:74 y :126). Resultado: con una gorra azul, el torso y la
     cadera del vecino se vuelven azules. Contradice el comentario del propio
     agente en ChibiMeshBuilder.cs:132 («quien solo lleva gorra sigue con su
     ropa») y ninguna prueba lo fija por ningún lado — CadaFamiliaDePrenda… solo
     mira Piece. Corrección: que "hat" devuelva HasGarment=false (la pieza Extra
     ya lleva el color en PieceColor; el switch de ChibiMeshBuilder no depende de
     HasGarment) o guardar el color de «ropa» cuando la pieza sea sombrero, y una
     línea de prueba que lo pinne.
  2. El cambio automático diario va sin certificado. OnDayPassed es código nuevo
     manejando un evento y no existe prueba que publique DayPassed y compruebe
     que un vecino con ≥2 prendas se cambia (o no, con semilla conocida). Diez
     ficheros de prueba de este repo prueban así sus manejadores de DayPassed
     (CoreTests, EconomyTests, BodasTests…); es la casa. Y es exactamente la
     enfermedad histórica del proyecto: manejador escrito, verde por ausencia,
     nadie lo dispara. Es determinista (semilla «{id}|cambio-ropa|{día}»), así
     que una prueba de editor con falsos sale en veinte líneas.
  3. Menor, de informe: «+1 referencia» al asmdef cuando son dos, y la segunda
     (Nimbo.Island) probablemente sobra. O la quita o la declara con motivo.

AFIRMACIONES SIN RESPALDO: Ninguna sustantiva. Todas las cifras del informe se
  verifican al dígito: 60 prendas y reparto 30/15/15, estilos 18/12/11/11/6/2,
  nivel 1 = gorra/diadema/pulsera/silbato, ItemDefinition ya tenía Slot/Style/
  Palette («cero cambios en Economía» cierto), GetItem registra error
  (ItemCatalog.cs:105), cadencia 0,5 s de destinos en WorldView.cs:686-689,
  patrón DayPassed de SocialService.cs:72, FNV de DecorMeshBuilder (su línea
  citada :41 se movió a :122 por ediciones paralelas de otro agente; el patrón
  es real), y el hueco de Dispose en GameBootstrap.OnDestroy (:567-583) existe
  tal como lo describe y lo delega correctamente en el orquestador en vez de
  tocar el fichero.

LO QUE ESTÁ BIEN (que no lo deshaga la siguiente vuelta):
  - La costura por contrato: 3 propiedades aditivas en IItemDefinition en vez de
    una referencia nueva Nimbo.Art→Economía. Cero cambios en Economía real.
  - La prueba de juego certifica malla, no dato: vértices que migran de piel a
    ropa y color de material. Es justo lo que pedía el encargo y lo que evita
    recertificar el fallo original.
  - Determinismo con semilla que incluye el día: cargar una partida no rebaraja
    el armario de nadie.
  - Higiene de memoria consciente: SwapMesh destruye la malla anterior y
    OnDestroy suelta las tres; ToonPalette.Solid cachea materiales, así que no
    hay fuga por cambio de outfit.
  - Fallbacks con criterio: paleta rota → hash FNV estable en vez de magenta;
    ItemsOfCategory en vez de GetItem para no llenar el registro de rojo con ids
    viejos de partidas guardadas; accesorios en «sin prenda» antes que pintar mal.
  - Comentarios /// que explican el porqué (por qué 1/3, por qué el día en la
    semilla, por qué la campana dobla al radio del torso, por qué el sondeo y no
    un evento). Español en comentarios y pruebas, identificadores en inglés.
  - El DESCARTADO con costes estimados es honesto y útil para el orquestador.
