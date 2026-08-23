# Veredicto — nimbo-bloqueos

```
VEREDICTO: APROBADO
DIFF REAL: GateNotice.cs (+163, nuevo), GateNoticeHost.cs (+94, nuevo),
           AvisosEnLaIslaTests.cs (+384, nuevo), más sus .meta.
           Gates.cs: cero líneas (git diff vacío).
¿CUADRA CON SU INFORME?: sí, línea a línea (detalle abajo)
PRUEBAS: editor 565/563/0 rojas/2 saltadas; juego 132/121/1 roja/10 saltadas
         (ola34-EditMode.xml, ola34-PlayMode.xml). La única roja de juego es
         ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle — ajena a
         este agente y declarada a propósito por el orquestador. Sus dos
         pruebas pasan en la suite oficial: LaExplicacionDeUnBotonBloqueadoSeLeeSinPuntero
         (0,79 s) y NiElFocoNiElHoverHacenFaltaParaLeerlo (0,75 s).
FUERA DE SU CARPETA: ninguno. El cambio en Nimbo.PlayTests.asmdef
         (+Nimbo.Island, +Nimbo.Simulation) no es suyo: su prueba solo usa
         Nimbo.Core/Nimbo.UI/Nimbo.Game, ya referenciados antes; esas dos
         referencias corresponden a árbol (Scripts/Island/NimboTree.cs) y ropa
         (Scripts/Simulation/Wardrobe/WardrobeService.cs). El cambio de niebla
         en Isla.unity es de rasdesuelo.
¿ENCHUFADO?: sí, por la vía autónoma: RuntimeInitializeOnLoadMethod(AfterSceneLoad)
         en GateNoticeHost.cs:87-92 monta la franja sola al entrar en play;
         nadie tiene que llamarlo. La prueba de juego carga la escena Isla,
         abre una ficha con UN clic sintético sobre un botón encendido,
         encuentra un botón bloqueado de verdad (enabledSelf==false +
         UiTheme.ClassDisabled + tooltip escrito) y exige que la explicación
         llegue a la franja sin enviarle ni un evento al bloqueado. Cubre las
         tres variantes de la enfermedad: sin referencias por nombre rotas
         (busca elementos reales), listón alcanzable (espera 8 s de tiempo
         real contra un latido de 0,25 s) y no enterrado (franja fija a 250 px
         del fondo, visible sin gesto alguno). Las dos cosas que el encargo
         prohibía romper están asertadas sobre el elemento real: sigue apagado
         y con su clase (AvisosEnLaIslaTests.cs:85-87,117-118) y llega sin
         hover ni foco (:119-121,150-154).
DEFECTOS: ninguno bloqueante. Observaciones menores, para la siguiente vuelta:
  1. GateNoticeHost.cs:39 llama FindObjectsByType<UIDocument> cada fotograma.
     El informe mide el coste del pase de árbol (135 nodos × 4 pases/s) pero
     no documenta este otro coste por fotograma. Con 2 documentos es barato;
     basta con decirlo en el informe.
  2. AvisosEnLaIslaTests.cs:132,138,338 usan Assume.That en precondiciones:
     si mañana alguien rompe la ficha, la prueba se saltará en vez de ponerse
     roja. Hoy no oculta nada (pasaron 2/2) y el control de totales de la
     línea base lo delataría; queda anotado.
  3. El SetUp (AvisosEnLaIslaTests.cs:44-46) barre GameBootstrap pero no
     GateNoticeHost, que sobrevive entre pruebas de la clase. No cambia el
     resultado (la espera en tiempo real más el assert contra las
     explicaciones actuales impiden un falso positivo tras un pase), pero
     barrerlo también sería más limpio.
AFIRMACIONES SIN RESPALDO: ninguna encontrada. Todas las citas del informe
         verificadas al píxel: SocialSection.cs:160/208/246/287,
         JobSection.cs:160, EventsPanel.cs:119, CraftPanel.cs:158 escriben en
         .tooltip; UiTheme.cs:260 SetEnabled(false); UiRoot.cs:100 root.Clear()
         en Mount; UiRoot.cs:189-191 cartel de logros en raíz; UiRoot.cs:644
         _toast.Tick(unscaled); AchievementToast.cs:58 pickingMode.Ignore;
         Gates.cs:36 redacta la explicación. Los números de suite coinciden
         con los XML (565/563/0/2 exacto en editor). Las 3 rojas de juego que
         cita eran de su turno y llevan nombre y mensaje literal; en ola34
         quedan 1 porque las 2 de cámara ya se arreglaron. No hay una sola
         afirmación sobre cómo se ve o se siente: §7 lo declara expresamente.
LO QUE ESTÁ BIEN:
  - La elección de canal está justificada con mediciones, no con gusto: hover
    descartado porque TooltipEvent no existe en runtime (informe-tema §4),
    foco descartado porque un elemento apagado no puede tomarlo —y lo midió
    con una prueba que lo documenta—, intento-de-pulsación descartado porque
    no hay navegación por teclado/mando hacia esta UI. La franja vale igual
    con cualquier entrada o sin ninguna: exactamente lo que el tooltip no
    cumplía.
  - Cero panel tocado, cero línea en Gates.cs: llegó a los siete sitios con
    dos ficheros nuevos, como pedía el encargo. Y ofrece al orquestador la
    alternativa de montarlo desde UiRoot (~8 líneas) sin imponerla.
  - Los fallos propios quedan escritos con el mecanismo (CS0191, el primer
    UIDocument no garantizado con DOS documentos en escena, esperas por
    fotogramas contra un mecanismo temporizado): son exactamente las costuras
    que este proyecto persigue, y la segunda era la variante «enchufe en un
    agujero que no existe» cazada por su propia prueba.
  - Esperas en tiempo real, no por fotogramas: la intermitencia que eso causa
    en batchmode la sufrió y la arregló con segunda pasada consecutiva en
    verde, no con suerte.
  - DiagnosticoTooltip sigue en la suite y en verde, como exigía el encargo.
  - Comentarios /// con el porqué de cada número (250 px, 0,25 s,
    pickingMode.Ignore, por qué el campo no es readonly, por qué una franja
    por documento). Español correcto, identificadores en inglés.
```
