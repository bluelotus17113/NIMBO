# Informe — falda del borde de la isla

Agente: falda (`opencode/x-preview-f-free`) · 2026-08-23, madrugada
Carpeta asignada: `Assets/_Project/Scripts/Art/World/IslandMeshBuilder.cs` (+ pruebas y encuadre que el encargo me pide explícitamente)

## Qué encontré al volver (la máquina se reinició a mitad de pasada)

`git status` / `git diff` antes de escribir nada, como manda el encargo. La pasada anterior dejó:

- `IslandMeshBuilder.cs`: `BuildSurface` ya extraído a un método `FillSurface`, pero con un
  `return Build(...)` dentro de un método `void` — **el árbol no compilaba**. Lo arreglé como
  parte del trabajo (el fichero es mi carpeta).
- `Informes/intentos-nimbo-falda.log` con una sola línea: el intento registrado y nada más.
  No había pruebas ni capturas de la pasada anterior.

## El fallo, con números

El prado curva su borde hacia abajo con `−t⁶·3,2` (`IslandMeshBuilder.cs`, ahora línea 125 en
`MeadowHeight`): el labio cae entre **−4,68 y −1,72 m**. La roca arrancaba su anillo 0 en
`y = ruido ± depth·0,06·0,5` → **−1,86 a +1,86 m** en la isla de la aldea (depth 62). Entre los
dos anillos, a igual radio, quedaban sectores con hasta **~2,8 m de banda abierta** por la que
se ve a través de la isla. Los sitios de pesca están al 78 % del radio o más
(`PlayerInteractor.cs:38,412`), o sea justo ahí.

Agravante descubierto al leer las llamadas: **`WorldView.cs:258` construye la roca de la isla
del jugador sin pasar la semilla** — el prado se siembra con `seed: 31u`
(`WorldView.cs:255`) y la roca se llama con la 7 por defecto. En esa isla los dos bordes ni
siquiera compartían contorno. Cualquier arreglo que dependa de repetir fórmulas con «la misma
semilla» ya ha fallado una vez aquí mismo.

## Qué hice y por qué así

**Soldadura, no falda colgante.** El encargo proponía una banda entre los dos anillos. Medido
el código, hay sectores donde el anillo 0 de la roca sube por **encima** del labio del prado
(+1,86 > −1,72): una banda que una ambos anillos se plegaría sobre sí misma ahí. La única
topología que no puede plegarse es que **el anillo 0 de la roca sea el labio del prado**.
La falda degenera en costura soldada: cero triángulos nuevos, cero pregunta de material
(no hay cara nueva que colorear; la junta queda como borde duro hierba/tierra, que es lo que
se ve en un corte de tierra).

Tres piezas, todo dentro de `IslandMeshBuilder.cs`:

1. **Un solo sitio para el contorno.** `EdgeProfile(segments, seed)` (líneas 107-114) es la
   única copia del ruido del borde y su suavizado; `MeadowHeight(angle, t)` (121-126) la única
   copia de la altura del prado. Antes cada constructor tiraba la suya.
2. **Vértices reales, no fórmulas.** `FillSurface` deja su anillo exterior tal cual nació en
   `_labioDelPrado` (línea 99); `BuildUnderside` lo consume **de un solo uso** (156-158) y hace
   grid[0] = esos vértices exactos (180-183). El perfil radial de toda la roca se derive
   midiendo el radio del labio real (168-170), así la costura no depende de la semilla que le
   llegue por parámetro — que en la isla del jugador es incorrecta hoy.
3. **La soldadura se disuelve, no corta.** Anillos ≥ 1: la altura arranca en la del labio real
   de cada sector y se disuelve con `(1−t)²` sobre el perfil cuadrático de siempre (219); las
   estrías y el ruido vertical se estiran con `×t` (213, 221) para que nada mueva el anillo
   soldado. En la punta la roca es idéntica a la de antes.

Sin prado reciente del que cosecharse (capturas sueltas, pruebas aisladas), `BuildUnderside`
reconstruye el labio con las mismas funciones compartidas: determinista e idéntico mientras
prado y roca se pidan con los mismos parámetros.

Los llamadores no cambian: `WorldView.cs:119/122/255/258` y `Screenshotter.cs:62/64` compilan
igual. El depósito estático está documentado en el fichero con su porqué y su límite (pares de
llamadas inmediatos, que es como se llama hoy en los tres sitios).

## La prueba que habría cazado esto

`Assets/_Project/Tests/FaldaDelBordeTests.cs` — editor, NUnit:

- `LaRocaNacePegadaAlLabioDelPrado_EnLaIslaDeLaAldea`: `BuildSurface(100f)` + `BuildUnderside(100f, 62f)`,
  las llamadas exactas de `WorldView.BuildIsland`.
- `LaRocaNacePegadaAlLabioDelPrado_EnLaIslaDelJugador`: `BuildSurface(45f, seed: 31u)` +
  `BuildUnderside(45f, 62f*0.6f)` **sin semilla en la roca**, las llamadas exactas de
  `WorldView.BuildHomeIsland` — incluido el descuido, porque la costura no puede depender de
  que el llamador acierte.

`Cose(prado, roca)`: para cada uno de los 48 sectores, el vértice exterior del prado tiene que
existir en la malla de la roca a menos de 0,01 m. Contra el código de antes falla con metros de
hueco y dice sector y distancia; contra el actual pasa con separación 0,000 m (son copias).
No replica fórmulas: lee las dos mallas producidas por el mismo camino de producción.

## Encuadre nuevo

`CapturaEstilo.cs`, quinto encuadre `bajo_el_borde` (desde `(150, −18, 118)` mirando a
`(72, −6, 56)`): el borde desde fuera y por debajo del labio, el ángulo que enseña una rendija.
Generadas `Capturas/estilo_*_falda.png`; en `estilo_bajo_el_borde_falda.png` la roca baja
continua desde el labio en todo el arco visible, sin cielo entre hierba y roca. El encuadre
`tercera_persona` que otro agente dejó en el fichero está intacto y su captura también.

## Estado de las suites (envoltorio, XML verificado)

| Suite | Resultado | Notas |
|---|---|---|
| EditMode | **529 pruebas · 526 pasadas · 1 fallo · 2 saltadas** | Mis 2 pruebas nuevas pasan. |
| PlayMode | **117 pruebas · 100 pasadas · 6 fallos · 10 saltadas** | Ningún fallo es de geometría. |
| CapturaEstilo (filtro) | 1 prueba · 1 pasada | Las 6 fotos escritas. |

Los totales suben sobre la base viva (514/92) porque hay cinco agentes trabajando en paralelo
y sus pruebas ya están en el árbol. **Los 7 rojos son de otros paneles**, comprobado uno a uno
en el XML:

- `SiluetaDeCultivosTests.TodoCultivoDelCatalogoTieneSuPropiaSilueta` (`SiluetaDeCultivosTests.cs:82`)
  — cultivos.
- `BootTests.LaCamaraEmpiezaEncuadrandoAlProtagonista` («arranca a 16 m: eso no es tercera
  persona») — cámara; están en plena conversión a tercera persona.
- `TemaEnLaIslaTests` ×3 (hover/pulsado/foco) — tema de botones.
- `TiendasEnLaIslaTests` ×2 (surtido y monedero) — costura de tiendas.

Ninguno toca mallas de isla; el prado no cambió una fórmula (mismos `MeadowHeight` y perfil),
solo la roca de debajo.

## Para el orquestador (no es mi carpeta)

- **`WorldView.cs:258`**: `BuildUnderside(radius, _islandDepth * 0.6f)` debería llevar
  `seed: 31u` para emparejar con el `BuildSurface(radius, seed: 31u)` de la línea 255. Mi
  costura ya no lo necesita (lee el labio real), pero la llamada miente sobre qué semilla
  construye la roca y hoy solo funciona gracias al depósito. Una palabra, su fichero.
- Si algún día alguien llama a `BuildUnderside` sin `BuildSurface` inmediatamente antes,
  caerá en el camino de reconstrucción por fórmulas: correcto solo si los parámetros
  coinciden con los del prado pretendido. Está escrito en el `<remarks>` del método.

## Ficheros tocados

- `Assets/_Project/Scripts/Art/World/IslandMeshBuilder.cs` — soldadura + helpers compartidos + arreglo del `return` roto heredado.
- `Assets/_Project/Tests/FaldaDelBordeTests.cs` (+ `.meta` generado por Unity) — nuevo.
- `Assets/_Project/Tests/PlayMode/CapturaEstilo.cs` — un encuadre añadido, ninguno borrado.
- `Capturas/estilo_*_falda.png` — evidencia visual.

Árbol dejado sucio a propósito, sin commits.
