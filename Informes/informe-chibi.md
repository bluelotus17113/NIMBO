# Informe — nimbo-chibi

Los dos defectos del muñeco que el paso a tercera persona destapó
(`Informes/nimbo-informe-camara.md`, puntos 3 y «lo demás»): el casquete del pelo era un
cacillo abierto y la cara un parche con curvatura, offset y normales propios. Todo el arte
sigue generado por código; no entró ni un asset.

## Defecto 1 — el cacillo abierto: CERRADO

`MeshShapes.SphericalCap` (`Assets/_Project/Scripts/Art/Chibi/MeshShapes.cs:82`) generaba
la campana y se quedaba ahí: desde abajo se veía el borde en canto, como papel, porque la
malla no tiene nada al otro lado (`Nimbo/Toon` hace `Cull Back`).

Cambio: un disco de cierre reutilizando el último anillo como borde —un vértice central y
un abanico, `MeshShapes.cs:121-137`, mismo orden de winding que la tapa inferior de
`Cylinder`, que es la que ya se sabía mirar hacia abajo—.

**Coste por cabeza** (lo que pedía el encargo): `segments` triángulos y 1 vértice por
casquete. Pelo 20 (`ChibiMeshBuilder.cs:363`), capucha 20 (`:260`), gorro 18 (`:302`). Peor
caso por vecino, pelo más prenda: **+40 triángulos** sobre los ~2.000 que ya lleva un
muñeco — menos de un 2 %—. Las tres llamadas son prendas de la cabeza y las tres quieren
cierre; por eso la función cierra siempre y no tiene parámetro.

## Defecto 2 — la cara parche: CONFORME AL CRÁNEO

`ChibiMeshBuilder.BuildFaceQuad` (`Assets/_Project/Scripts/Art/Chibi/ChibiMeshBuilder.cs:520`)
tenía tres pecados que a 5 m y ángulos rasantes se ven a cada paso:

1. **Superficie inventada**: `sqrt(R² − px² − py²·0,6)` aproxima el elipsoide de la cabeza
   comprimiendo el alto a ojo; cerca del borde el parche flota o se pasa de la silueta.
2. **Offset plano +4 mm**: constante hacia fuera, así que el contorno del parche queda
   siempre a la vista en los ángulos rasantes.
3. **`RecalculateNormals()`**: normales promediadas propias, distintas a las del cráneo
   que tienen debajo — plano y mejilla sombrean distinto y el rectángulo se marca.

Cambio (`ChibiMeshBuilder.cs:520-590`), sin añadir ni un triángulo (mismos 128/81):

- La superficie es ahora **el elipsoide de la cabeza de verdad**: semiejes
  `(headWidth·0,5, headHeight·0,5, headWidth·0,47)`, que son exactamente los de la esfera
  escalada del cuerpo (`ChibiMeshBuilder.cs:141-143`).
- Cada vértice se separa **a lo largo de la normal del elipsoide**, con una holgura que se
  invierte en el borde: `+4 mm` en el centro → `−6 mm` en el contorno
  (`SmoothStep(0,5,1,radio)`). El borde se hunde bajo la piel y desaparece desde cualquier
  ángulo, como una decal. El hundimiento (6 mm) es mayor que la holgura (4 mm) a propósito.
- Las normales son **las del elipsoide** (gradiente de la cuártica), escritas y no
  recalculadas: cara y mejilla sombreadan como una sola superficie.
- La ventana se estrechó de `0,40×0,34` a `0,37×0,31` cabezas: así el rectángulo completo
  cabe dentro de la huella elíptica —`(0,74)² + (0,62)² ≈ 0,93 < 1`— y hasta las esquinas
  del borde tienen superficie donde esconderse. Consecuencia honesta: los rasgos van un
  ~9 % más juntos al centro (las UV no cambian). Es lo que hay que mirar en la captura
  frontal antes de dar por bueno el arreglo.

## Costura con nimbo-ropa

Leído `OutfitLook.cs`, `IslanderView.cs` y su parte de `ChibiMeshBuilder` antes de tocar.
No hay choque: ellos viven en el mapeo catálogo→look y en el swap de mallas; mis cambios
no tocan ninguna línea suya. Sus pruebas (`RopaLookTests`, 9) siguen en verde en la
corrida completa de esta noche. El casquete cerrado afecta también a su capucha y su gorro
(piezas Extra): mejor, que era otro cacillo abierto colgando de la cabeza.

## Pruebas

Nuevas, editor: `Assets/_Project/Tests/ChibiCercaTests.cs` (**fuera de mi carpeta, se
declara**: fichero nuevo en `Tests/`, ensamblado `Nimbo.Tests` que ya referenciaba
`Nimbo.Art`; nadie más toca ese fichero).

- `ElCasqueteEstaCerradoPorAbajo`: cuenta aristas por posición — toda arista con 2 caras;
  una con 1 sería un borde abierto. Descuenta las astillas del polo de la rejilla
  (dos vértices coincidentes no son cara). Fija también el coste: 20·10·2+20 triángulos,
  y que el disco trae normales hacia abajo. **Nota honesta: la primera versión de ESTA
  prueba falló contra la malla buena** — mi contador contaba la arista viva de cada astilla
  como borde (salía 4 donde había 2). El fallo era del contador, no del cierre; quedó
  corregido y documentado en el propio fichero.
- `LaCaraAbrazaLaCabezaYEscondeElBorde`: para cada vértice de la cara, valor de la cuártica
  del elipsoide reconstruido desde `AppearanceData.Default`. Borde (UV en 0/1):
  `f < 0,99` —enterrado—. Interior: `0,85 ≤ f ≤ 1,06` —abraza sin hundirse ni flotar—.

Resultados medidos con el envoltorio (`Informes/pruebas/nimbo-chibi-*.xml`):

- **EditMode completo**: 588 pruebas · 585 pasadas · 1 fallo · 2 saltadas. El único fallo
  era el de mi contador descrito arriba, ya corregido; segunda corrida filtrada a mis
  pruebas: **2 · 2 · 0 · 0**. Línea base 514 sobrada — la suben los seis agentes de esta
  ola; las 16 de ropa y las 6 de peinados, que son las que podían notar mi cambio, en verde.
- **PlayMode completo**: [PENDIENTE_DE_COLA]
- **Captura de estilo**: [PENDIENTE_DE_COLA]

## Encuadre nuevo en CapturaEstilo.cs (concedido por el encargo)

`RetrataVecinoDeEspaldas` (`Tests/PlayMode/CapturaEstilo.cs`): saca un vecino real del
censo, lo planta con `PlaceAt` en un sitio escrito del prado (50, −11 — el rodal de
«flores_a_ras»; la altura la da un raycast contra el prado de verdad, porque la isla curva)
y lo fotografía **desde 35 cm del suelo y 1,7 m por detrás, mirando a la cabeza**. Los
siete encuadres anteriores no se han borrado ni tocado.

**Qué hay que mirar**: `Capturas/estilo_vecino_de_espaldas_<sufijo>.png`.

- Antes del arreglo: el pelo se veía como un sombrero de papel — el borde del cacillo en
  canto, y a través de él, el cielo o el cráneo recortado.
- Después: la nuca con el pelo sólido y el interior del pelo leyéndose como superficie, no
  como hueco. Si además se relanza `CapturaPersonajes`, en `personajes_frente.png` la cara
  no debe marcar rectángulo sobre la mejilla y los ojos deben seguir donde estaban
  (con el ~9 % de estrechamiento dicho arriba, que juzga quien mira).

## Para el orquestador (fuera de mi carpeta, anotado y no tocado)

1. **22:12–22:35 el proyecto entero no compilaba**: `FiestaDecoradaEnLaIslaTests.cs`
   (nimbo-fiestas) pedía `using UnityEngine.TestTools;` — 24 errores CS0246 que bloqueaban
   cualquier corrida. Anotado y no tocado, según las reglas; lo arregló su autor a las 22:35.
2. **Tres atascos de Unity con la misma firma** — registro congelado >20 min justo tras
   «Loading mode Default / Unloading 67 Unused Serialized files», CPU ~1%, ningún XML:
   memoria-EditMode (74 min, 22:38), creador-PlayMode (56 min, 00:00) y creador-PlayMode
   otra vez (44 min, 01:28). La guarda `limpia_huerfanos` de `unity.sh` no evalúa a quien
   YA sujeta el flock (`flock -n … || return 0`), así que un titular colgado bloquea la
   cola de seis para siempre. En los tres casos apliqué la política del propio envoltorio
   (kill al proceso >20 min sin trabajar, limpiar `Temp/UnityLockfile`); los bucles de
   reintento de sus autores relanzaron solos y sus corridas salieron después en verde.
   Sugerencia: que `limpia_huerfanos` valore también al titular del cerrojo con la misma
   regla de los 20 minutos.
3. La carrera documentada del lockfile me mordió dos veces como víctima: wrappers míos
   que quedaron huérfanos al morir mi shell entraron, vieron el `Temp/UnityLockfile` de
   otro y salieron «con éxito» sin ejecutar nada (XML ausente). El envoltorio ya lo
   detecta; solo dejar constancia de que sigue pasando cuando hay teardowns lentos.
