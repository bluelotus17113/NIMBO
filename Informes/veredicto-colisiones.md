# Veredicto — revisión de nimbo-colisiones

Verificador: `nimbo-verificador` · 2026-08-23

```
VEREDICTO: RECHAZADO
DIFF REAL: GatheringView.cs (+78), DecorMeshBuilder.cs (+81), IslandCamera.cs (+104/-8),
           CameraObstacles.cs (nuevo, 115 l.), ColisionesDeCamaraTests.cs (nuevo, 207 l.),
           CamaraEnLaIslaTests.cs (nuevo, 314 l.), CapturaEstilo.cs (+1 encuadre)
¿CUADRA CON SU INFORME?: sí — tabla completa abajo
PRUEBAS: 565 editor / 132 juego / 1 roja — contra base crecida 565/132; la roja es
         ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle (del agente del árbol,
         esperando enganche del orquestador, declarada por colisiones en su informe)
FUERA DE SU CARPETA: ninguno imputable (CapturaEstilo.cs estaba en su encargo; el cambio
         de Nimbo.PlayTests.asmdef es de arbol/ropa; la niebla de Isla.unity es rasdesuelo)
¿ENCHUFADO?: sí, con un agujero — ver DEFECTO 1
DEFECTOS: 1 (bloqueante)
AFIRMACIONES SIN RESPALDO: ninguna relevante
LO QUE ESTÁ BIEN: lista al final, para no deshacerlo
```

## Cuadre diff ↔ informe, punto por punto

| Afirmación del informe | Verificado | Resultado |
|---|---|---|
| `CameraObstacles.cs` nuevo, `TryHit` en :62 | `CameraObstacles.cs:62` | ✅ |
| `RegisterCameraBlocker` en `GatheringView.cs:201` | `GatheringView.cs:201` | ✅ |
| `TryKindOf`/`TryCameraSpheres` en `DecorMeshBuilder.cs:47`/:77 | exacto | ✅ |
| `Unobstructed` en `IslandCamera.cs:659`, handlers :266-268 | exacto | ✅ |
| «0 colisionadores añadidos» | cero `Collider` en el diff de sus ficheros | ✅ |
| 120 esferas registradas | log `[colisiones] esferas registradas tras cargar la isla: 120` en `Informes/pruebas/ola34-PlayMode.log:2788` | ✅ |
| 3,40–3,67 µs por consulta | 3,417 µs (ola34) y 3,390 µs (colisiones-full), mismos logs | ✅ (ver nota menor 3) |
| Pivote 1,2 m, topes 3,5 m a 5° | `IslandCamera.cs:40-42`, `CameraRig.cs:27,29` | ✅ |
| 12 editor + 3 juego, todas verdes en la corrida final | contadas en `ola34-*.xml` | ✅ |
| Los cinco encuadres previos intactos | panorama, prado, arbol, linde, bajo_el_borde intactos; `tras_el_arbol` añadido; `flores_a_ras` es de rasdesuelo, como declara | ✅ |
| «No he tocado PlayerInteractor.cs» | no aparece modificado en `git status` | ✅ |

Notas menores de cita (no defectos): «3,5 m a 5°, CameraRig.cs:29-30» — MinPitch está en
:27 y MinDistance en :29; el gancho del barrido citado como `:380` está en
`IslandCamera.cs:377`; una corrida dio 3,390 µs, un pelín bajo el rango «3,40–3,67»
declarado. Drift de líneas, los valores existen donde se dice.

## Atribución de lo que NO es suyo (para el orquestador)

- `Nimbo.PlayTests.asmdef` (+`Nimbo.Island`, +`Nimbo.Simulation`): lo necesitan
  `ArbolEnLaIslaTests` (usa `NimboTree`, ensamblado `Nimbo.Island`) y
  `RopaEnLaIslaTests` (usa `WardrobeService`, ensamblado `Nimbo.Simulation`). La prueba
  de colisiones usa `NodeKind` de `Nimbo.Data`, ya referenciado. No imputar a colisiones.
- `Isla.unity` (niebla 60/280→30/260): rasdesuelo.

## DEFECTO 1 (bloqueante): el barrido de adornos muere al entrar en modo construcción

Cadena medida, con línea:

1. `BuildModeView.cs:71` — al entrar en construir, `island.enabled = false`.
2. `IslandCamera.OnDisable` (`IslandCamera.cs:147-155`) — se desuscribe de
   `DecorPlaced/Removed/Moved` **y llama `SoltarAdornos()`**: todos los bultos de
   adornos fuera del registro.
3. `BuildModeView.cs:95` — al salir, `island.enabled = true`. `OnEnable`
   (`IslandCamera.cs:151-159`) solo resuscribe; **nadie vuelve a marcar el barrido**, y
   en mitad de sesión no hay `GameLoaded` nuevo.
4. Resultado: tras un ida-y-vuelta por modo construcción, ningún adorno vuelve a apartar
   la cámara hasta el próximo evento de adorno o recarga de escena. Sin excepción, sin
   aviso. Y peor: cualquier adorno colocado **durante** el modo construcción nunca llegó
   a registrarse — su `DecorPlaced` saltó con la cámara desuscrita.

Es la variante exacta de la enfermedad de la casa («enchufado con un agujero», silencioso):
las pruebas de juego cubren la carga fresca de la isla y ninguna entra y sale de construir.
Construir es bucle central de este juego, así que el fallo llega al usuario en la primera
sesión normal. Los árboles y rocas sobreviven (sus dueños son otros y `SoltarAdornos` no
los toca), pero la mitad de adornos del entregable se apaga sola.

**Qué debería pasar**: `MarcarBarridoDeAdornos()` en `OnEnable` — una línea. En el primer
arranque es inofensivo (`_world == null` corta el barrido y `GameLoaded` lo remarca); al
reencender a mitad de sesión, el barrido del fotograma siguiente repone todo lo vivo. Y
una prueba de juego que cargue Isla, entre y salga del modo construcción, y compruebe que
`CameraObstacles.Count` conserva los bultos de adornos.

## AFIRMACIONES SIN RESPALDO

Ninguna relevante. Todo lo que el informe afirma sobre comportamiento tiene prueba o log
detrás; no hay adjetivos sobre cómo se ve o se siente.

## LO QUE ESTÁ BIEN (no deshacer en la siguiente vuelta)

- **La decisión esferas-sobre-capa-física está bien tomada y mejor escrita**: respeta el
  «Sin colisionadores, a propósito» de `GatheringView.cs:28-33` sin deshacerlo, y el
  motivo queda documentado en `CameraObstacles.cs:9-24` con el coste comparado.
- **0 colisionadores añadidos**, verificado en el diff: la física de la escena queda
  intacta, jugador y vecinos incluidos.
- **`TryHit` expulsa a los dueños destruidos** (`CameraObstacles.cs:72-77`) y hay prueba
  de editor que lo exige (`UnDuenoDestruidoSeIgnoraYSaleDeLaLista`).
- **El barrido diferido un fotograma** con el motivo escrito en el sitio
  (`IslandCamera.cs:257-264`): el orden entre quien coloca y quien escucha no está
  garantizado, y lo resuelve sin acoplar nada a `WorldView`.
- **La prueba de juego mide el escenario antes de juzgar**: busca postura cuyo recorrido
  cruza de verdad un bulto antes de exigir el corte (`CamaraEnLaIslaTests.cs:125-139`),
  comprueba el enchufe antes que la geometría (:96-100) y espera convergencia en tiempo
  reloj, no en fotogramas (:150-151).
- **Informe honesto**: declaró la roja ajena, el cabo suelto de tejados
  (`CameraRig.cs:36-47`) y el error de compilación de otro agente que le abortó corridas.
