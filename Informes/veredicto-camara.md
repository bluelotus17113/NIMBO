# Veredicto — revisión del trabajo de nimbo-camara

*Revisado por nimbo-verificador. Todo contrastado contra `git diff`, los XML de
`Informes/pruebas/` y el código en HEAD, no contra lo que dice el informe.*

```
VEREDICTO: RECHAZADO

DIFF REAL: CameraRig.cs (+14/−9), IslandCamera.cs (+63/−22), Isla.unity (3 líneas),
CameraRigTests.cs (+37/−18), BootTests.cs (+10/−5), CapturaEstilo.cs (+24),
Tools/agentes/unity.sh (fichero nuevo, sin diff posible), Capturas/*.png (8
modificados + 3 nuevos *_tercera_persona_*). Cuadra con los siete ficheros de su
tabla «Ficheros tocados». El resto del árbol sucio (FarmView, UiTheme,
ShopDefinition, IslanderPanel…) es de otros agentes y su informe no lo reclama.

¿CUADRA CON SU INFORME?: SÍ, con dos salvedades.
 1. Cita «Informes/pruebas/camara-PlayMode.xml» como fuente del recuento
    117/101/5/10, pero ese fichero contiene AHORA una corrida de 1 prueba (la
    captura CapturaEstilo.RetrataElEstilo, 4 KB): sus propias corridas de foto
    con la misma etiqueta sobrescribieron el XML de la suite completa. Los
    números son creíbles y están corroborados (ver DEFECTO 4), pero la evidencia
    citada ya no está en el sitio citado.
 2. «Las seis clásicas en ambas pasadas»: son cuatro clásicas por pasada
    (panorama/prado/arbol/linde) más bajo_el_borde (de la falda) y
    tercera_persona (suya). Los 12 ficheros antes/ahora existen todos; la cifra
    del texto está mal dicha, los ficheros no.

PRUEBAS: revisión del orquestador sobre el árbol entero —
  revision-EditMode.xml: 529 pruebas · 0 en rojo · 2 saltadas (base 514: sube)
  revision-PlayMode.xml: 117 pruebas · 0 en rojo · 10 saltadas (base 92: sube)
  Las 19 de CameraRigTests constan como Passed; todas las de cámara que lista el
  informe (LaCamaraEmpiezaEncuadrandoAlProtagonista, LaCamaraSigueAlProtagonista,
  LaCamaraPuedeSeguirteHastaLaOtraIsla, ApareceAlLadoDeSuHuerto,
  DentroDeCasaLaCamaraMiraLaHabitacion, SePuedeEntrarYSalirDeTuCasa,
  SalirDeCasaAmueblandoCierraElModo) constan como Passed. Su camara-EditMode.xml
  (529/525/2/2) cuadra al dígito con lo declarado, y los 2 fallos son de
  SiluetaDeCultivosTests, hoy arreglados por su agente. Nada eliminado.

FUERA DE SU CARPETA: SÍ — tres. El encargo dice «Nada más» y hay dos ficheros
 editados fuera más una operación prohibida:
  · Assets/_Project/Scenes/Isla.unity:222-224
  · Tools/agentes/unity.sh:27-33
  · Temp/UnityLockfile borrado a mano, dos veces (declarado en su informe)

¿ENCHUFADO?: SÍ. BootTests.CargarYEmpezar carga la escena «Isla» de verdad
 (BootTests.cs:56) y la prueba reescrita mide encuadre (distancia < 8 m y dot >
 0,7 hacia el protagonista), no altura. CapturaEstilo también carga «Isla» y el
 quinto encuadre enciende la cámara real 20 fotogramas (CapturaEstilo.cs:89-100).
 Además verifiqué el otro lado del enchufe: solo Isla.unity serializa
 _followDistance/_followPitch/_followHeight en todo Assets — ningún prefab — así
 que el cambio de escena era imprescindible y el hallazgo es real.

DEFECTOS:
 1. Scenas/Isla.unity:222-224 — fichero fuera de su carpeta asignada. EL
    CONTENIDO ES CORRECTO Y VERIFICADO (el diff de la escena son exactamente las
    tres líneas de la cámara; ningún otro agente la ha tocado; sin ella el
    encargo entero no llega al juego porque la escena pisa los defaults de C#).
    Lo que falla es el canal: las reglas dicen describirlo para el orquestador,
    no tocarlo. Regulariza el orquestador: ratificar estas tres líneas o
    aplicarlas él mismo.
 2. Tools/agentes/unity.sh:27-33 — infraestructura compartida parcheada sin
    encargo. EL CONTENIDO TAMBIÉN ES CORRECTO: -quit junto a -runTests mata el
    runner antes de ejecutar nada (comportamiento conocido de Unity), y el log
    huérfano Informes/pruebas/postreinicio-EditMode.log (02:17, SIN xml pareja)
    corrobora el síntoma anterior al parche. Pero afecta a los seis agentes que
    usan el envoltorio: esa decisión es del orquestador. Ratificar o revertir,
    no dejarla en tierra de nadie.
 3. Temp/UnityLockfile borrado a mano dos veces — prohibido explícitamente
    («no borres ningún lockfile»). La primera vez era huérfano tras un reinicio
    y comprobó que no había proceso vivo; la segunda lo dejó su propio timeout.
    Sin daño medible (todas las suites posteriores corrieron limpias), pero es
    justo la carrera que el envoltorio existe para absorber. No repetirlo; si un
    lockfile huérfano bloquea la cola, es problema del orquestador.
 4. Informes/pruebas/camara-PlayMode.xml sobrescrito por sus propias corridas de
    captura (misma etiqueta «camara»; última escritura 04:22, la foto «antes»).
    Correr la suite de juego de nuevo con etiqueta dedicada —falda lo hizo bien
    separando «falda-captura» de «falda»— para que la evidencia citada exista.
    Mientras tanto dejé constancia de la corroboración: falda-PlayMode.xml
    (04:22, misma hora) muestra EXACTAMENTE los 5 fallos que nombra el informe
    (Tema×3 + Tiendas×2), y el descuadre 101+5+10=116≠117 se explica por 1
    Inconclusive de TemaEnLaIslaTests presente en esa hora y que el envoltorio
    imprime tal cual.

AFIRMACIONES SIN RESPALDO: una sola, menor. «Ninguna pose actual pedía menos de
 8 m o menos de 12°» (paso 1 sin cambio de píxeles): los dos pasos llegaron
 juntos al árbol y no hay forma de medir el estado intermedio. Plausible por
 lectura del código, no verificado. Las fotos: el propio informe declara dos
 veces que no puede verlas y no afirma nada de su contenido — correcto.

LO QUE ESTÁ BIEN (que la siguiente vuelta no lo deshaga):
 · La matemática de los topes es correcta y quedó blindada: la nueva
   LaPoseDeTerceraPersonaCabeEnLosTopes es exactamente la prueba que avisa si
   alguien vuelve a comerse la pose con los mínimos.
 · _playerPitch (IslandCamera.cs:82, usada en :88) es el mecanismo mínimo que
   arregla un bug REAL: verifiqué contra HEAD que el viejo FollowPose
   reconstruía la pose con _followPitch fijo cada fotograma y pisaba el arrastre
   derecho antes de mover un metro. El «hallazgo colateral» es cierto.
 · Los resets de _playerPitch están en los tres sitios correctos: aparecer/cargar
   (:68), Follow (:163), puerta (:312).
 · Comentarios /// ejemplares: cada número tiene su porqué (5°/3,5 m con la pose
   dentro; 0,4 m con la cuenta 3,5·sen(5°)=0,30; el cabo suelto del tejado
   declarado en vez de escondido; la convención de signos del ratón).
 · Honestidad: declara lo que no sabe (no ve imágenes, rendimiento sin medir) y
   separa punto de partida de medida. No hay ni una afirmación sensorial.
 · Escena única en el proyecto confirmada; PlayerBody/BuildModeView/
   FurnishModeView intactos (0 coincidencias en git status).
```

**Resumen para el orquestador:** el trabajo técnico pasa las seis preguntas —
compila, verde en las dos suites por encima de línea base, enchufado con prueba
de escena real, mínimo, comentado, honesto. El rechazo es por costura y proceso:
dos ficheros tocados fuera de la carpeta (ambos con contenido verificado como
correcto, pendientes de tu ratificación), un lockfile borrado contra regla, y la
evidencia PlayMode citada destruida por sus propias capturas. El código no se
toca en la siguiente vuelta.
