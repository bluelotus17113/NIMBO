# Informe — nimbo-colisiones (segunda vuelta, tras veredicto RECHAZADO)

2026-08-23 · Corrige exclusivamente el DEFECTO 1 de `Informes/veredicto-colisiones.md`.
Nada de lo que el verificador aprobó se ha rehecho ni deshecho.

## Qué corregí

### DEFECTO 1 (bloqueante): el barrido de adornos moría al entrar en modo construcción

La cadena que midió el verificador era exacta y no la repito entera: `BuildModeView.Enter`
apaga `IslandCamera` (`BuildModeView.cs:71`), el `OnDisable` de la cámara suelta los bultos
de adornos (`IslandCamera.cs:167`), y al salir de construir nadie volvía a marcar el
barrido — los adornos quedaban fuera del registro hasta recargar escena, sin excepción y
sin aviso. Además, todo adorno colocado **durante** el apagón perdió su `DecorPlaced` con
la cámara desuscrita.

**El arreglo es la línea que pedía el veredicto**: `MarcarBarridoDeAdornos()` al final de
`OnEnable` (`IslandCamera.cs:142-149`), con el porqué escrito en el sitio. Por qué basta:

- En el primer arranque es inofensivo: `_world` aún es null, así que `BarridoDeAdornos`
  corta en su guarda (`IslandCamera.cs:309-310`) y `GameLoaded` remarca cuando hay partida.
- Al reencender a mitad de sesión, el barrido del fotograma siguiente repone **todo lo
  vivo** leyendo la jerarquía del mundo (`IslandCamera.cs:311-321`) — incluidos los
  adornos colocados durante el apagón, cuyo aviso se perdió: no hace falta replay de
  eventos porque el barrido no confía en ellos, mira lo que hay pintado.

### La prueba que faltaba

`CamaraEnLaIslaTests.EntrarYSalirDeConstruirConservaLosBultosDeAdornos`
(`CamaraEnLaIslaTests.cs:290-338`): carga Isla de verdad, coloca un adorno alto
garantizado (ayudante `ColocaUnAdornoAlto`, :340-384 — la isla recién estrenada puede
traer adornos o no según la partida), y hace el ida-y-vuelta por modo construcción
publicando `BuildModeChanged` por el `EventBus`, igual que hace el botón de la interfaz
(`UiRoot.cs:242`). Tres aserciones encadenadas para que ninguna pase vacía:

1. `antes > 0`: con adorno colocado el registro no puede estar vacío.
2. `durante < antes` (:313-317): entrar en construir tiene que soltar bultos — si no
   bajara, el ida-y-vuelta no estaría probando nada.
3. `después == antes` (:324-328): al salir, el registro vuelve a estar completo y sin
   duplicados (el barrido llama `SoltarAdornos` antes de reponer).

Resultado: **Passed**, 0,576 s (`Informes/pruebas/colisiones-PlayMode.xml`, caso
`EntrarYSalirDeConstruirConservaLosBultosDeAdornos`). Sin el arreglo de `OnEnable`, la
tercera aserción falla con «los adornos no volvieron al registro».

## Qué NO he tocado (lo aprobado, intacto)

- La decisión esferas-sobre-capa-física y su documentación: `CameraObstacles.cs:9-24`,
  sin cambios esta vuelta.
- **0 colisionadores añadidos**: mi diff sigue sin un solo `Collider`; la física de la
  escena queda como estaba.
- `TryHit` expulsa dueños destruidos: `CameraObstacles.cs:72-77`, y su prueba de editor
  sigue verde.
- El barrido diferido un fotograma: ahora en `IslandCamera.cs:384-390` (estaba en :377;
  el parche añade 9 líneas en `OnEnable` y desplaza todo lo posterior).
- Las tres pruebas de juego anteriores y las doce de editor: sin cambios.
- El encuadre `tras_el_arbol` de `CapturaEstilo.cs`: sin cambios esta vuelta.

## Correcciones de cita que señalaba el veredicto (notas menores)

- Topes del rig: MinPitch está en `CameraRig.cs:27` y MinDistance en `CameraRig.cs:29`
  (antes cité ambos como :29-30).
- El gancho del barrido diferido: citado como :380, estaba en :377 y tras este parche va
  en `IslandCamera.cs:386`.
- Coste por consulta: esta corrida mide **3,507 µs** con 120 esferas
  (`colisiones-PlayMode.log:481`); las corridas de la primera entrega dieron 3,417 y
  3,390 µs. Rango honesto observado: **3,39–3,67 µs por consulta**, no «3,40–3,67».

## Pruebas de esta entrega

    Tools/agentes/unity.sh EditMode colisiones
      → 569 pruebas · 567 pasadas · 0 FALLOS · 2 saltadas
    Tools/agentes/unity.sh PlayMode colisiones
      → 133 pruebas · 122 pasadas · 1 FALLO · 10 saltadas

- Resultados en `Informes/pruebas/colisiones-EditMode.xml` y
  `Informes/pruebas/colisiones-PlayMode.xml`.
- La base ha seguido creciendo por otros agentes (565→569 de editor desde el veredicto);
  lo relevante: cero rojas propias en ambos modos.
- **El único fallo de juego sigue siendo ajeno y declarado**:
  `ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle`, del agente del árbol,
  esperando enganche del orquestador. Ya lo declaré en la primera entrega y el
  verificador lo aceptó como no imputable.
- Medidas que se sostienen: 120 esferas registradas al cargar la isla
  (`colisiones-PlayMode.log:480`) y 3,507 µs por consulta (:481).

## Cabos sueltos que sigo declarando (no son míos)

- **Tejados sin bultos**: zonas y cabaña siguen sin esferas de cámara; el suelo duro de
  3 m de `CameraRig.cs:36-47` quedó sin sentido al bajar la cámara pero nadie lo ha
  retirado todavía. Si el orquestador quiere cerrarlo, el gancho sería el mismo barrido
  de `BarridoDeAdornos` (`IslandCamera.cs:306`) añadiendo una mirada a los tejados.
