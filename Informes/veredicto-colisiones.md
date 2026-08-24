# Veredicto — revisión de nimbo-colisiones (segunda vuelta)

Verificador: `nimbo-verificador` · 2026-08-24 · re-verificación tras RECHAZADO

```
VEREDICTO: APROBADO
DIFF REAL (2ª vuelta, dentro del commit de control 835cba2 junto a otros agentes):
           IslandCamera.cs (+9 en OnEnable: comentario+llamada; desplaza lo posterior),
           CamaraEnLaIslaTests.cs (+112: prueba EntrarYSalirDeConstruirConservaLosBultosDeAdornos
           y ayudante ColocaUnAdornoAlto)
¿CUADRA CON SU INFORME?: sí — línea a línea, tabla abajo
PRUEBAS: suites frescas del orquestador — editor 563/561/0/2 (arbolhook2-EditMode.xml),
         juego 133/123/0/10 resultado global Passed (retomo-PlayMode.xml). Cero rojas.
         Las 12 editor + 4 juego de colisiones, todas Passed en esas corridas frescas
FUERA DE SU CARPETA: ninguno imputable a esta vuelta (declara tocar solo sus dos ficheros,
         y es lo que hay)
¿ENCHUFADO?: sí — y la prueba nueva cierra el agujero exacto del rechazo
DEFECTOS: ninguno
AFIRMACIONES SIN RESPALDO: ninguna
LO QUE ESTÁ BIEN: lista al final, para no deshacerlo
```

## El DEFECTO 1, arreglado como se pidió

La corrección es literalmente la línea que exigía el veredicto anterior:
`MarcarBarridoDeAdornos()` al final de `OnEnable` (`IslandCamera.cs:149`), con el porqué
escrito en `:142-148`. Verificado por lectura que basta:

- Primer arranque inofensivo: `_world == null` corta el barrido (`IslandCamera.cs:309`)
  y `GameLoaded` vuelve a marcar cuando hay partida (`:189`).
- Reencendido a mitad de sesión: el gancho diferido de `LateUpdate`
  (`IslandCamera.cs:386-390`) repone **todo lo vivo** leyendo la jerarquía
  (`:311-321`) — incluidos los adornos colocados durante el apagón, cuyo aviso se
  perdió. No hace falta replay de eventos: el barrido no confía en ellos.
- Y `SoltarAdornos()` antes de reponer (`:308`) garantiza reposición sin duplicados.

## La prueba que faltaba, y por qué no puede pasar vacía

`EntrarYSalirDeConstruirConservaLosBultosDeAdornos`
(`CamaraEnLaIslaTests.cs:290-328`, Passed en la corrida fresca, 0,550 s):

1. `antes > 0` (:306) — con adorno colocado, el registro no puede estar vacío.
2. `durante < antes` (:315) — entrar en construir tiene que soltar bultos; si no bajara,
   el ida-y-vuelta no probaría nada. Esta aserción es la que impide el falso verde.
3. `después == antes` (:325) — sin el arreglo de `OnEnable`, `OnDisable`→`SoltarAdornos`
   (`:167`) deja el registro sin nadie que lo repita en mitad de sesión, y esta
   aserción cae. Cadena verificada por lectura.

El enchufe existe por los dos lados: la prueba publica `BuildModeChanged` por EventBus
(`:311`, `:321`), igual que el botón (`UiRoot.cs:244`; el informe citaba :242, drift de
dos líneas), y `BuildModeView` lo escucha y apaga/enciende `island.enabled`
(`BuildModeView.cs:53-56`, Enter y Leave). El ayudante `ColocaUnAdornoAlto`
(`:340-387`) coloca por el servicio de verdad y explica por qué alto (asiento, planta y
valla no llegan al recorrido pivote→cámara).

## Cuadre diff ↔ informe, punto por punto

| Afirmación del informe | Verificado | Resultado |
|---|---|---|
| Arreglo = llamada en `OnEnable` :142-149 | `IslandCamera.cs:149` | ✅ |
| Guarda `_world == null` en :309-310 | `:309` | ✅ |
| Barrido lee jerarquía :311-321 | exacto | ✅ |
| Gancho diferido :384-390 | `:386-390` | ✅ |
| Prueba :290-338, ayudante :340-384 | :290-328 / :340-387 | ✅ (drift ≤3 l.) |
| Tres aserciones encadenadas | :306 / :315 / :325 | ✅ |
| Publica como el botón (`UiRoot.cs:242`) | `UiRoot.cs:244`, mismo código | ✅ |
| «Lo aprobado intacto»: `CameraObstacles.cs` 115 l., sin cambios | 115 líneas, mismo contenido | ✅ |
| «0 colisionadores añadidos» | cero `Collider` en sus ficheros (rg) | ✅ |
| 12 editor + 3 juego anteriores sin cambios | las 15 en las corridas frescas, Passed | ✅ |
| Encuadre `tras_el_arbol` intacto | `CapturaEstilo.cs:72`; los cinco ajenos también | ✅ |
| Citas corregidas: MinPitch :27, MinDistance :29 | `CameraRig.cs:27` y `:29` | ✅ |
| Coste 3,507 µs / 120 esferas | `colisiones-PlayMode.log:2774,:2800` | ✅ |

Nota para el registro, no defecto: la corrida fresca mide **3,316 µs**
(`retomo-PlayMode.log:2813`), un pelo bajo el rango «3,39–3,67» que declara el informe —
escrito antes de esa corrida. Orden de magnitud estable y prueba de coste verde en ambas;
el rango honesto completo observado es 3,32–3,67 µs con 120 esferas.

Sobre los recuentos: su corrida dio 569 editor y la fresca 563; la diferencia es de la
base movida por otros agentes entre corridas, no de colisiones — sus 17 pruebas están
íntegras y verdes en la fresca. La roja ajena del árbol
(`ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle`) desapareció de la fresca:
el orquestador aplicó el enganche (commit 835cba2), como esperaba el informe.

## AFIRMACIONES SIN RESPALDO

Ninguna. Todo lo afirmado sobre comportamiento tiene prueba en XML fresco, log con línea,
o cadena verificable por lectura. No hay adjetivos sobre cómo se ve ni cómo se siente.

## LO QUE ESTÁ BIEN (no deshacer)

- **El arreglo es mínimo y va con el porqué**: nueve líneas contando el comentario, sin
  banderas ni abstracciones nuevas; reutiliza el barrido que ya existía en vez de añadir
  un replay de eventos.
- **La aserción intermedia (`durante < antes`)**: la diferencia entre una prueba que
  vigila y una que saluda. Que exija ver la caída antes de exigir la subida es lo que
  hace imposible el falso verde.
- **El ayudante coloca por el servicio y justifica «alto»**: nada de fabricar GameObjects
  de mentira; y el motivo de farola/estatua/cartel/fuente está escrito.
- **Informe honesto**: separa lo corregido de lo aprobado-intacto, corrige sus propias
  citas de la vuelta anterior sin que se le pidiera, y mantiene declarados los cabos
  ajenos (tejados sin bultos, suelo duro de 3 m de `CameraRig.cs:36-47` pendiente de
  decisión del orquestador).
