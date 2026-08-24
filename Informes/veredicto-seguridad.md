# Veredicto — nimbo-seguridad

Fecha: 2026-08-24 · Verificador: nimbo-verificador · Encargo: `Informes/encargos/nimbo-seguridad.md`

```
VEREDICTO: RECHAZADO
DIFF REAL: 6 ficheros modificados, 448 inserciones / 22 borruras —
  UI/Player/ShippingPanel.cs (+108), UI/Player/SkillsPanel.cs (+66),
  UI/Requests/RequestBoardPanel.cs (+90/-15), UI/Achievements/AchievementsPanel.cs (+99),
  UI/Chronicle/ChroniclePanel.cs (+56), UI/Village/EventsPanel.cs (+51).
  Nuevos: Tests/PlayMode/CajonSeguroTests.cs (324 l.), Tests/PlayMode/PanelesAlDiaTests.cs (540 l.).
¿CUADRA CON SU INFORME?: Sí en lo esencial —ficheros, recuentos, atribución de fallos—.
  No declara una dependencia de compilación (defecto 1).
PRUEBAS: Sus corridas propias (`Informes/pruebas/nimbo-seguridad-*.xml`): EditMode
  599/597/0 fallos/2 saltadas · PlayMode 161/149/2 fallos/10 saltadas. Los 2 fallos son
  de otros agentes, verificado por fullname en su XML: TeclasEnLaIslaTests.EscapeCierra…
  y MemoriaEnLaIslaTests.NoTeCuentaOtraHistoriaElMismoDia. Sus 12 pruebas nuevas, todas
  Passed. Contra la línea base viva (588/142/0): nada ha bajado; los totales suben con
  pruebas nuevas de varios agentes de la ola. OJO: los XML del orquestador
  (todo-edit/todo-play, 07:53-07:55) son ANTERIORES a todo el trabajo de seguridad
  (08:43-09:59) y no lo cubren — me he apoyado en sus XML y en una corrida propia.
FUERA DE SU CARPETA: Ninguno. No tocó UiRoot.cs, como mandaba el encargo.
¿ENCHUFADO?: A medias, y es la parte que decide el orquestador ahora mismo.
  - «Vender todo»: ENCHUFADO Y PROBADO EN ESCENA. CajonSeguroTests carga la Isla real,
    abre el cajón por el camino del jugador (StationUsed Shipping) y pulsa botones reales:
    sin confirmar no vende ni mueve el monedero; confirmado cobra exactamente 5×precio.
    SellEverything solo queda alcanzable vía ConfirmEverything (ShippingPanel.cs:249).
  - Refresco de los cinco paneles: LOS CINCO Refresh() NO TIENEN NINGÚN CALLER en
    Scripts/. Grep de `.Refresh()` en Scripts: solo los tres de siempre en UiRoot.cs:693-702.
    El enganche descrito en el informe es correcto —ancla UiRoot.cs:706 verificada, los
    cinco nombres de campo (_board/_achievements/_chronicle/_events/_skills) existen en
    UiRoot.cs:36-42— pero hasta que el orquestador lo aplique, media entrega no llega al
    jugador. PanelesAlDiaTests llama a Refresh() a mano con dobles: prueba la unidad,
    no el camino del jugador.
DEFECTOS:
  1. Dependencia de compilación NO DECLARADA sobre otro agente. RequestBoardPanel.cs:135
     y :227 llaman a UiTheme.Plazo(), que NO existe en HEAD (grep sobre git show HEAD:
     UiTheme.cs = 0 resultados): lo añade nimbo-copy en su UiTheme.cs sin commitear. Si el
     trabajo de copy se rechaza o se revierte, este fichero no compila. El encargo exige
     «si necesitas engancharte a algo de otro, descríbelo en el informe —qué evento, qué
     llamada, en qué línea—» y el informe lo calla: habla de «tramo de horas… mismo
     redondeo que Deadline()» sin decir que Deadline() ahora vive en el fichero de otro.
     Arreglo mínimo: una frase en informe-seguridad.md declarando la dependencia
     (UiTheme.Plazo, añadido por nimbo-copy, usada en RequestBoardPanel.cs:135 y :227),
     o dejar Deadline() autocontenido y reservar Plazo para quien lo pida.
  2. (Menor) «Coste medido por construcción» (informe, §refresco): es una estimación
     razonada sobre el tamaño del StringBuilder, no una medición. Reformular como lo que
     es; «medido» reserva para lo que salió de una prueba o un profiler.
  3. (Menor) AchievementsPanel.SetFilter es público «por cualquier atajo futuro» —
     nadie lo pidió y nadie lo llama desde fuera. Los clics ya lo usan; basta privado.
AFIRMACIONES SIN RESPALDO: Ninguna sobre cómo se ve o se siente el juego. La del
  «coste medido» del defecto 2 es la única que estira la palabra medir.
LO QUE ESTÁ BIEN (no deshacer en la siguiente vuelta):
  - CajonSeguroTests es el modelo de cómo se prueba un clic: escena real, secuencia de
    puntero completa, Pick() para comprobar que el botón no está tapado, y el filtro de
    visibilidad documentado contra el «Mejor no» gemelo del menú (TitleScreen).
  - El patrón firma→rebuild está bien traído de JobSection, con el matiz correcto de
    sellar también al terminar Show(); las pruebas cubren LOS DOS LADOS (cambia→repinta,
    no cambia→no derriba instancias), que es justo lo que justifica meterlos en el bucle.
  - El tramo de horas en vez del minuto crudo en la firma del tablón es la decisión
    correcta: con el reloj a 1 min/s, el minuto crudo redibujaría cada segundo.
  - Show() cancelando la confirmación pendiente: reabrir sobre una mochila cambiada no
    puede prometer vender lo que ya no está.
  - Informe honesto: declara él mismo que falta el enganche, y la atribución de los 2
    fallos ajenos cuadra con su XML nombre por nombre.
PARA EL ORQUESTADOR (no es del agente): aplicar el enganche de 5 líneas tras
  UiRoot.cs:706 tal cual describe el informe —los nombres de campo ya los he verificado—,
  y resolver la costura del defecto 1 antes de aceptar el conjunto.
```

## Nota de verificación

Corrí un filtro propio (`Tools/agentes/unity.sh PlayMode verif-seguridad
"PanelesAlDiaTests"`, XML en `Informes/pruebas/verif-seguridad-PlayMode.xml`): **10/10
pasadas, árbol entero compilado**. Motivo: `RequestBoardPanel.cs` tiene mtime 09:59:53,
después de que terminaran las dos corridas de seguridad (EditMode 09:58:43, PlayMode
09:51:07), así que su estado final no estaba cubierto por nadie. Queda cubierto.
