# Informe: silueta y color propias para los doce cultivos

Agente: nimbo-cultivos · 2026-08-23 · Carpeta asignada: `Assets/_Project/Scripts/Art/World/FarmView.cs` (+ `MeshShapes.cs` si hiciera falta malla nueva: **no hizo falta**).

## Qué encontré al reanudar (la máquina se reinició a mitad de pasada)

`git status` / `git diff` antes de escribir nada:

- `FarmView.cs` ya traía **el trabajo completo de la pasada anterior**: los doce cultivos con silueta y fruto propios (`LookFor`, `FarmView.cs:142`), el fallback genérico (`:503`) y la gestión de mallas compartidas por casilla (`Looks`, `:43`; limpieza en `OnDestroy`, `:640`). Mtimes: código terminado 00:29.
- `Tests/PlayMode/CapturaHuerto.cs` —la herramienta `[Explicit]` que retrata el huerto— estaba escrita (00:28) y **ya había corrido**: `Capturas/huerto_doce_ahora.png` y `huerto_cerca_ahora.png` son de 00:50, posteriores al código. Las capturas que juzgan este informe salen del estado actual del árbol.
- `MeshShapes.cs` sin modificar: todas las formas son composición de lo que ya había (esfera, cilindro, cápsula, caja de `MeshShapes`; racimos de `FoliageMeshBuilder.Weave`; briznas de `Meadow.Weave`). **Nada que revisar aparte ahí.**

Lo que faltaba al reinicio: prueba que fije la costura catálogo→forma, las dos suites corridas contra la base viva, y este informe.

## Lo que hice en esta pasada

1. **`Assets/_Project/Tests/SiluetaDeCultivosTests.cs` (nuevo, 2 pruebas de editor).** Es la prueba de la costura, la enfermedad de siempre: un id escrito a mano en un switch que nada comprueba. Si mañana se añade un cultivo al JSON sin darle forma, caería en la genérica sin excepción ni aviso; aquí se delata:
   - `TodoCultivoDelCatalogoTieneSuPropiaSilueta`: carga el catálogo real de producción (`new CropCatalog()`, `Resources/Config/catalogo_cultivos.json`) y para cada seedId llama a `FarmView.LookFor` por reflejo (convención de `TiendasEnLaIslaTests.cs:121`). Comprueba que (a) ninguno cae en la firma de la genérica, (b) madurar **se nota** —pieza nueva o material distinto—, y (c) las doce siluetas son distintas dos a dos, por nombres de pieza y por suma de vértices.
   - `UnIdDesconocidoCaeEnLaPlantaGenerica`: un id fuera del catálogo dibuja tallo+fruto en vez de petar o dejar un hueco (partidas viejas incluidas).
   - Dos correcciones durante la puesta a punto, ambas mías y no del arte: un `Part[]` es array de structs y no castea a `object[]` (va a `System.Array`), y la invariant correcta de madurar es «se nota» y no «añade pieza»: la nimbocalabaza es la misma malla con otra piel porque la planta *es* el fruto (`FarmView.cs:283`).
2. Corrí las dos suites (abajo). No toqué ningún fichero fuera de mi carpeta salvo este test nuevo y el informe.

## Verificación

Con `Tools/agentes/unity.sh` (XML en `Informes/pruebas/cultivos-*.xml`):

| Suite | Resultado | Base viva |
|---|---|---|
| EditMode | **529 pruebas · 527 pasadas · 0 fallos · 2 saltadas** | 514 · 0 · 2 |
| PlayMode | **117 pruebas · 101 pasadas · 5 fallos · 10 saltadas** | 92 · 0 · 9 |

- Editor: la base sube de 514 a 529 por pruebas nuevas de otros agentes (tiendas, tema) más mis 2. Las 2 saltadas son las mismas de siempre (`ElQueNoLePegaLoRechazaSiNoSoisAmigos`, `PeroLoHaceSiSeLoHasGanado`).
- Juego: las 10 saltadas son las 9 herramientas `[Explicit]` de captura previas + mi `RetrataLosDoce`.
- **Los 5 fallos de juego no son míos**, y lo digo con el mensaje del XML delante:
  - 3 del agente de tema (`ElBotonReaccionaAlPuntero`, `ElBotonSeHundeMientrasEstaPulsado`, `ElFocoDeTecladoSeVe`): «el tema pide Peach… o el inline volvió a pisar la clase».
  - 2 del agente de tiendas (`CadaBotonDeTiendaAbreUnaTiendaConSurtido`, `ComprarEnLaTiendaDeComidaDescuentaDelMonedero`): «en Muebles no hay ni una fila que se pueda comprar», «TryBuy no llegó a ejecutarse nunca».
  - Ninguno toca huerto, mallas ni farming; ambos agentes estaban con su pasada en marcha mientras yo corría (sus `unity.sh` encolados, visto en `pgrep`).

### Incidencias del entorno, para constancia

- La primera pasada tras el reinicio abortó con «Scripts have compiler errors» que luego desapareció sin cambiar código: la caché de compilación de `Library/` quedó corrupta por el reinicio; una pasada en limpio la reconstruyó.
- `Tools/agentes/unity.sh` **se editó en caliente por otro agente mientras lo estaba ejecutando** (bash lee el script por trozos y escupió errores de sintaxis fantasma; además un proceso `unity` mío apareció «killed»). La versión actual del script quita `-quit` y está bien; lo digo porque un verificador que corra en esa ventana puede ver fallos que no son de su código.

## Cómo se mira esto

Herramienta: `Tests/PlayMode/CapturaHuerto.cs` (`[Explicit]`). No monta un huerto falso: labra, siembra, riega y pasa días por `IFarmingService` dentro de la escena `Isla`, con las doce casillas usables (4×3 desde `FarmPlot.UsableSize`), y retrata dos encuadres:

    NIMBO_HUERTO=ahora Tools/agentes/unity.sh PlayMode cultivos CapturaHuerto

Fotos ya sacadas (00:50, con el código actual):

- `Capturas/huerto_doce_ahora.png` — plano general: bloque 4×3, una fila por línea de catálogo.
- `Capturas/huerto_cerca_ahora.png` — a la altura de juego, donde se ve si una seta parece seta.

Lo que muestran, como hechos y no como juicio: doce plantas presentes, cada una con silueta distinta de vecina — seta azul grande con pie claro (cetrella), corrito de tres setas blancas (esporaalba), prismas azul hielo (cristalovento), árbol con copa y frutos rosados colgados (nimbocereza), calabaza naranja flotando sobre su hueco (nimbocalabaza), espigas pálidas altas (trigoeter), cojín dorado bajo (musgolante), flor azul de corazón amarillo (azurflor), raíz blanca con penacho de briznas (raiznube), mata oscura con orbe brillante (sombraliquida), briznas bajas con bolas claras en el suelo (vidrioestelar), mata verde con vainas rojas erguidas (alasauce). Si eso **se lee bien o mal** a tres metros lo decide quien mira la foto; la distancia entre siluetas la mide la prueba de editor (nombres y vértices distintos dos a dos).

Una observación por si alguien la quiere recoger: en el encuadre cercano se ven sombras en cruz bajo raiznube y vidrioestelar —son las briznas de `Meadow.Weave`, dos quads cruzados, proyectando sombra—. No lo he tocado: es comportamiento del generador común, no de este encargo.

## Los doce, con ruta

Todos en `FarmView.cs`; cada uno tiene variante creciendo y variante madura:

| Cultivo | Forma | Fruto / señal de cosecha | Línea |
|---|---|---|---|
| nimbocereza | cerezo enano: tronco + copa tejida | cerezas rosado profundo colgadas | :169 |
| alasauce | mata alta, cuatro alas escalonadas | vainas rojo cobre | :200 |
| cristalovento | ramillete de prismas pentagonales | esquirla alta azul vivo | :234 |
| cetrella | seta única azul, sombrero ancho | luz propia bajo el sombrero | :259 |
| nimbocalabaza | el fruto ES la planta, flotando | piel verde → naranja | :283 |
| musgolante | cojín dorado bajo de cinco lóbulos | borlas crema encima | :306 |
| raisnube | raíz blanca asomando + penacho | bola de nube arriba | :336 |
| azurflor | capullo cerrado sobre tallo con hojas | roseta de seis pétalos + corazón dorado | :357 |
| trigoeter | dos matas de cañas pálidas altas | espigas doradas en las puntas | :398 |
| sombraliquida | mata oscura con lóbulo que gotea | orbe de tinta casi negro, brillante | :418 |
| esporaalba | corrito de tres setas desiguales | polvo luminoso sobre la alta | :446 |
| vidrioestelar | briznas bajas | esferas de vidrio apoyadas en tierra | :477 |

Los doce ids del switch coinciden uno a uno con `Resources/Config/catalogo_cultivos.json` (líneas 5–104), y la prueba de costura lo mantiene así: hoy **ningún cultivo del catálogo cae en el genérico**; el genérico (`:503`) queda solo para ids desconocidos de partidas viejas.

## Costuras para el orquestador

1. Fallos de tema (3) y tiendas (2) en PlayMode: de esos agentes, en plena revisión. Eventos y líneas en «Verificación».
2. `Tools/agentes/unity.sh` fue reescrito por otro agente en caliente (03:06); la versión actual funciona, pero conviene congelarla mientras haya agentes corriendo suites.
3. Nada más: no toqué `MeshShapes.cs`, ni eventos, ni servicios. El árbol queda sucio a propósito, sin commits.
