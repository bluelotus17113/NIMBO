# Veredicto — nimbo-acabados

Verificador: nimbo-verificador · Fecha: 2026-08-23 · Suite de referencia: `Informes/pruebas/ola34-*.xml`

```
VEREDICTO: APROBADO
DIFF REAL: Scripts/Data/Housing/RoomLayout.cs (+18/-2), Scripts/Housing/HomeUpgradeService.cs
  (+4/-1), Scripts/Art/World/InteriorView.cs (+153/-8 aprox.), Scripts/UI/Player/FurnishPanel.cs
  (+131), Tests/HomeUpgradeTests.cs (+6/-1), y los nuevos Tests/AcabadosDeViviendaTests.cs (298 l.)
  y Tests/PlayMode/AcabadosEnLaIslaTests.cs (160 l.) con sus .meta.
¿CUADRA CON SU INFORME?: Sí. Cada fichero declarado está en el diff y cada fichero del diff
  se atribuye: lo demás del árbol (IEconomyService.cs, InventoryTests.cs, WardrobeService,
  ChibiMeshBuilder, NimboTree, IslandCamera, GateNotice, el asmdef de PlayTests…) es trabajo
  de ropa/árbol/cámara/bloqueos/colisiones. El cambio del asmdef añade Nimbo.Island y
  Nimbo.Simulation, que las pruebas de acabados no usan: no es suyo.
PRUEBAS: Editor 565 totales / 563 verdes / 0 rojas / 2 saltadas (las dos de GestionAldea de
  siempre). Juego 132 totales / 121 verdes / 1 roja / 10 saltadas. La única roja es
  ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle, roja a propósito y de otro agente.
  Las 7 de AcabadosDeViviendaTests y las 2 de AcabadosEnLaIslaTests están en los XML y en
  verde. Vecinos citados en el informe verificados contra el mismo XML: HomeUpgradeTests 16/0,
  HousingPlacementTests 20/0, BootTests 31/0. Línea base 514/92: solo sube (+51/+40 con todos
  los agentes); ninguna suite preexistente bajó.
FUERA DE SU CARPETA: Scripts/Data/Housing/RoomLayout.cs no está en la lista literal de su
  carpeta, pero el paso 1 del propio encargo ordena cambiarlo («Que RoomLayout y
  HomeUpgradeService arranquen con acabados que existan») y lo declara en el informe:
  sancionado. Tests/HomeUpgradeTests.cs ídem, declarado como «ajuste en un test ajeno» con la
  intención conservada (HomeUpgradeTests.cs:188-196).
¿ENCHUFADO?: Sí. Assets/_Project/Tests/PlayMode/AcabadosEnLaIslaTests.cs carga la escena
  «Isla» de verdad, entra en casa por el mismo camino que BootTests.cs:395
  (InteriorEntered("", "Tu casa")), aplica vía FurnishPanel.TryApplyFinish —el verbo real de
  la interfaz— y mide que el material de «pared_n» pasa de Nimbo_F5F0E8 a Nimbo_B8D8F0 y el
  de «suelo» de Nimbo_C8A882 a Nimbo_FFFFFF, que se gasta la unidad y que al salir y volver
  a entrar sigue pintado. La prueba de editor LosAcabadosPorDefectoExistenEnElCatalogo
  (AcabadosDeViviendaTests.cs:50) carga el catálogo real de Resources y es exactamente la
  que pedía el encargo. Los cuatro colores afirmados coinciden con catalogo_acabados.json y
  el nombre de material con ToonPalette.cs:207.
DEFECTOS: Ninguno bloqueante. Dos notas menores, para constancia:
  1. Informe, línea 26: llama al vinilo «el más barato del catálogo» siendo 6⭐ cuando
     wall_nube_blanca cuesta 5⭐; es el más barato de los SUELOS, que es lo que importa para
     la elección. El número y el motivo son correctos; sobra la palabra «del catálogo».
  2. Para el orquestador, no para este agente: las saltadas de juego pasaron de 9 a 10
     (SondaDelPuente.MideElPaso). No es fichero suyo ni lo toca nadie de esta ola en ese
     fichero; conviene mirarlo aparte.
AFIRMACIONES SIN RESPALDO: Ninguna sustantiva. Todas las citas del informe se verificaron
  contra el código: ids y precios en catalogo_acabados.json (líneas 6 y 336),
  validación silenciosa en HousingService.cs:138-155, AchievementService ignorando el id
  (AchievementService.cs:248-253), tiendas con Wallpaper/Flooring sin zona
  (ShopDefinition.cs:61-72), FurnishModeView sin publicar RoomEdited (0 menciones), y los
  recuentos filtrados del informe coinciden uno a uno con el XML de la suite completa.
LO QUE ESTÁ BIEN: El gasto va DESPUÉS de comprobar que el servicio aplicó de verdad
  (FurnishPanel.cs:257,271) — el rechazo silencioso del servicio no puede comerse una
  unidad. El acabado dominante con desempate estable por segunda pasada sobre la lista
  (InteriorView.cs:238) resuelve de verdad el caso ampliar-casa-mezclando-series-y-pintado,
  y el comentario dice por qué. Los ids caídos caen al color por defecto y no a magenta,
  mientras que Get() sí pinta magenta con aviso para el fallo que la prueba debe cazar:
  gradación correcta. Las cinco costuras del informe son accionables y con línea; en
  especial la del tercer lector del JSON está declarada como deuda sustituible, no como
  arquitectura. Comentarios /// con el motivo en todo, en español.
```
