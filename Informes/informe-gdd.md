# Informe — nimbo-gdd · El GDD puesto al día contra el código

Fecha: 2026-08-23 · Carpeta asignada: **`Docs/01_GDD.md`, solo ese fichero** — ni una
línea tocada en `Assets/`. Versión del documento: 2.0 → **2.1**.

## Qué hice

Leí los doce veredictos y los trece informes de la ola, y por cada marca que cambié
verifiqué la ruta en el código yo mismo — ni el GDD viejo ni un informe bastan para
mover una marca. No corrí Unity: no escribí código, así que no ocupé la cola que otros
agentes estaban usando. Las cifras de pruebas que cito salen de los XML frescos ya
existentes: `arbolhook2-EditMode.xml` (**563 pruebas / 561 pasadas / 0 rojas / 2
saltadas**) y `retomo-PlayMode.xml` (**133 / 123 / 0 / 10**, corrida de las 01:50 del
24, posterior a la pausa). Línea base 514/92/0 superada en ambas.

## Las cinco marcas que el analista desmentía, hoy

| Marca del analista | Estado ahora | Qué sostiene el cambio |
|---|---|---|
| 1. Tiendas rotas por la costura de ids | **Arreglado** (veredicto APROBADO) | `ShopDefinition.cs:50-58` usa `tienda_comida/muebles/ropa` —los mismos ids que `IslandLayout.cs:44,60,68` declara y `UiRoot.cs:237-239` pasa—. `TiendasEnLaIslaTests` compra pulsando los botones reales sobre la escena cargada |
| 2. Árbol Nimbo apagado | **Enchufado** (el orquestador aplicó el enganche A–G; la prueba roja pasó a verde en `retomo`) | `PlayerInteractor.cs:478-490` cartel+tecla a 6 m; `:886-893` llama `TryTalk`; `WorldView.cs:151-153` escala la malla con el nivel; `ITreeService.cs` existe; `ArbolNimboTests.LaEscalaDelArteSigueALaDelServicio` ata las dos copias de la fórmula |
| 3. Creador sin voz ni ropa | **Sigue abierto** → `[~]` | `CreatorPanel.cs:235-276`: secciones de Cabeza a Detalles + personalidad + nombre. Cero pestañas de voz o ropa; la voz se sortea en `IslanderFactory.cs:41` y suena en `AudioDirector.cs:361`, pero nadie la elige |
| 4. Relaciones con otro modelo | **Sigue abierto** → `[~]` | `RelationshipRecord.cs:6-13`: `FriendshipStage` tiene 5 niveles (Stranger→BestFriend), no 10; los estados viven en `RomanceStage` y `ConflictStage` (`:23-32`) |
| 5. Números de catálogo desfasados | **Medidos y corregidos** | Conté los JSON: 80 muebles (`catalogo_muebles.json`), 60 prendas (`catalogo_ropa.json`), 44 recetas (`catalogo_recetas.json`), 40 acabados, 12 cultivos |

Cambios de marca en §17: dos `[x]` recuperados con nota de cómo estuvieron rotos
(economía/tiendas, Árbol), dos `[x]` bajados a `[~]` con lo que falta dicho (creador,
relaciones), tres números actualizados (muebles, prendas, recetas).

## Lo nuevo reconocido en el documento

- **§9.3** ganó su párrafo «Hecho», como ya tenían §9.2 y §13.2: racha ×1,0→×1,5
  (`NimboTree.cs:93`), semilla diaria (`:121`), pista hacia el vecino más triste
  (`:187-199`), reparto 45/20/20/15 (`:147-155`).
- **§17.2, acabados de vivienda `[x]`**: 40 acabados con precio, elegibles desde
  Amueblar (`FurnishPanel.TryApplyFinish`, `FurnishPanel.cs:238`) y pintados en el
  interior (`InteriorView`). Veredicto APROBADO con prueba de escena real
  (`AcabadosEnLaIslaTests`).
- **§17.2, armario `[~]`**: la ropa equipada se renderiza (`IslanderView.cs:63`,
  `:110-112` leen `EquippedOutfit`; veredicto APROBADO tras segunda vuelta) y los
  vecinos se cambian solos cada día (`WardrobeService.cs:81`), pero elegir tú qué se
  pone cada cual no existe: `Wear/FavouriteOf/WardrobeOf` sin un solo llamador fuera
  del servicio (grep propio, cero resultados).

## Lo que NO toqué, y por qué

- **§5.3 y §6.4 son diseño**: sus cifras («40 muebles base», «pool de 200», «150
  prendas», «32 peinados») describen lo que el juego quiere ser. Actualicé los números
  donde el GDD los da por existentes (§17), no donde los pide como objetivo. Si el
  diseño debe bajar al código o el código subir al diseño, lo decides tú.
- **§8.2 (tabla de 10 niveles)**: ídem. La dejé entera; la marca de §17.1 es la que
  dice la verdad ahora.
- **Trabajo de calidad sin impacto de marca**: cámara en tercera persona (ratificada
  por el orquestador, escena `Isla.unity` 5,5 m / 18° / 1,2), estados hover/pulsado/
  foco de botones (`NimboRuntimeTheme.tss:38-70`), ficha con scroll
  (`IslanderPanel.cs:63`), franja de explicaciones de bloqueo (`GateNoticeHost.cs`),
  siluetas por cultivo (`FarmView.cs:146-158`), falda del borde soldada
  (`IslandMeshBuilder.cs`), colisionadores de cámara (`CameraObstacles.cs`). Todo está
  probado en escena real y verde, pero ninguna línea del alcance decía lo contrario:
  no había marca que mentir ni que reconocer. Quedan en sus informes.
- **Agentes en vuelo sin informe todavía** (agenda, memoria, fiestas, chibi, creador):
  cuando entreguen habrá que revisar §2.2 («ve la agenda del día» — hoy no existe esa
  vista), §3.2 (memoria que te mencionen), §15.3 (el mundo cambia de cara en un
  evento) y la línea del creador. Hoy no he movido nada suyo porque no hay diff ni
  veredicto que citar.

## Decisiones que siguen abiertas (y sus marcas, honestas)

- **El rechazo de trabajos** (`[~]` de §17.1): `MinJobAffinity = 0.3f`
  (`JobService.cs:115`) contra mínimo producible 0,35. Sin decidir el valor; las dos
  saltadas de editor son las de siempre.
- **`RequestKind.IslandBuilding`** (§15.4 «lo que queda»): sigue sin coste — grep
  propio: solo pesos de personalidad lo nombran, nada lo cobra.
- **Relaciones 10 vs 5**: decisión tuya, no mía (ver arriba).

## Costuras que describo para el orquestador (no son mías, no las toqué)

1. **Los defectos 2 y 3 del veredicto-arbol siguen vivos**: `NimboTree.cs:88` aún dice
   «techo en ×1,5 al quinto» (la fórmula de `:93` da ×1,4 al quinto y ×1,5 al sexto) y
   `NimboTree.cs:222` aún dice «al tercer chiste ya es más probable repetir» (con seis
   opciones eso llega en la quinta). El defecto 1 sí quedó arreglado: la prueba
   prometida existe (`ArbolNimboTests.cs:41`). Los dos comentarios falsos los he
   llevado a §18 como enfermedad nueva, pero el arreglo es una línea de comentario en
   tu lado.
2. **`CreatorPanel.cs:14`** promete «aspecto, personalidad, voz y nombre» y la voz no
   tiene pestaña. Mismo family que §18: comentario que promete algo que no hay.
3. **`WardrobeService.Dispose()` sin llamador** en `GameBootstrap.OnDestroy`
   (pendiente declarado en `veredicto-ropa.md`): cada descarga de escena deja una
   suscripción viva al calendario.
4. **Las saltadas de juego van 10 y no 9**: `SondaDelPuente.MideElPaso` se añadió a la
   lista (nota 2 de `veredicto-acabados.md`). Nadie de esta ola tocó ese fichero;
   conviene mirarlo aparte.
5. **Atribución del asmdef**: la línea `Nimbo.UI` de `Nimbo.Tests.asmdef` la necesita
   `TemaBotonesTests` (agente tema), aunque la aplicó el agente tiendas
   (`veredicto-tiendas.md`, defecto 2). Ya está anotado en ambos veredictos; lo dejo
   constancia aquí para que no se pierda al integrar.

## §18 — lo añadido

Tres entradas nuevas, todas sacadas de veredictos de esta ola y con el mismo tono de
siempre (qué pasó, por qué ningún test lo veía, cómo se caza):

1. **El comentario que promete una prueba** (veredicto-arbol, defecto 1).
2. **El número despegado de su fórmula** (veredicto-arbol, defectos 2 y 3 — que siguen
   en el código).
3. **La evidencia citada, sobrescrita por quien la cita** (veredicto-camara, defecto 4).

## Compilación

No compila nada nuevo: markdown puro. Durante la sesión no lancé Unity ni vi errores
de otros agentes en mis logs; los cinco intentos registrados en
`Informes/intentos-nimbo-gdd.log` son de cola, no de ejecución.
