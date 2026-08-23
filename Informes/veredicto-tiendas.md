# Veredicto — nimbo-tiendas · La costura rota de las tiendas

Revisor: `nimbo-verificador` · Fecha: 23/08/2026 · Contra: `Informes/informe-tiendas.md`

```
VEREDICTO: APROBADO
DIFF REAL: ShopDefinition.cs (+33/-12 aprox.), ShopPanel.cs (+16/-1),
           EconomiaHuertoTests.cs (+23/-2), EconomyTests.cs (+24/-6),
           Nimbo.Tests.asmdef (+1); nuevos: Tests/CosturaDeTiendasTests.cs(+.meta),
           Tests/PlayMode/TiendasEnLaIslaTests.cs(+.meta)
¿CUADRA CON SU INFORME?: sí — cada fichero que declara está en el diff, y cada
    fichero atribuible a él del diff está declarado. UiRoot.cs sin diff, como
    promete su §8 (verificado: git diff vacío sobre ese fichero).
PRUEBAS: revisión del orquestador — editor 529/527/0 rojas/2 saltadas;
    juego 117/107/0 rojas/10 saltadas. Línea base 514/92/0 superada en ambas.
    Sus 6 pruebas nuevas (4 edición + 2 juego): todas Passed en revision-*.xml.
FUERA DE SU CARPETA: ninguno atribuible. Nota para el orquestador: la línea
    «Nimbo.UI» de Nimbo.Tests.asmdef sirve a TemaBotonesTests (agente tema),
    no a sus pruebas — declarada en su §2 con justificación; sin ella el
    ensamblado entero de pruebas de edición no compila en este árbol compartido.
¿ENCHUFADO?: sí — Assets/_Project/Tests/PlayMode/TiendasEnLaIslaTests.cs carga
    la escena «Isla», publica ProtagonistCreated, pulsa «Comida/Muebles/Ropa»
    con secuencia real PointerMove→PointerDown→PointerUp, exige surtido sin
    «Hoy no queda nada», exige panel.Pick(centro)==botón Comprar, y afirma una
    compra por ItemAcquired + CoinsChanged negativo con motivo «Comprar …».
DEFECTOS: menores, no bloqueantes (ver abajo).
AFIRMACIONES SIN RESPALDO: ninguna sustantiva. Dos inverificables pero
    declaradas con detalle (§4 volcado de rects no conservado; §7 PIDs muertos).
LO QUE ESTÁ BIEN: decisión de lado correcta y justificada; regla anti-recaída
    hecha código; guardián anti-enterrado; honestidad completa.
```

## Las seis preguntas

**1. ¿Compila y pasan las pruebas? — SÍ.**
`Informes/pruebas/revision-EditMode.xml`: 529 total, 527 pasadas, **0 fallidas**, 2
saltadas. `revision-PlayMode.xml`: 117 total, 107 pasadas, **0 fallidas**, 10 saltadas.
Cero en rojo en ambas suites contra la línea base 514/92/0. Su propia pasada
(`nimbo-tiendas-PlayMode.xml`) tenía 4 fallos — verifiqué uno a uno y son los cuatro de
`TemaEnLaIslaTests` que su informe atribuye al agente de tema; en la revisión final ya
están resueltos. Ningún fallo fue nunca de tiendas.

**2. ¿Se ha salido de su carpeta? — NO, con una nota.**
Sus ficheros atribuibles: `Economy/Shops/ShopDefinition.cs`, `UI/Shop/ShopPanel.cs`, y
los cuatro de pruebas que el encargo le manda escribir. `UiRoot.cs` intacto (diff vacío,
y las líneas 235-237 siguen con `tienda_comida/muebles/ropa`). La nota: la referencia
`Nimbo.UI` en `Nimbo.Tests.asmdef` no la necesita ninguna prueba suya — la necesita
`TemaBotonesTests.cs`, fichero de otro agente que ya vive en el mismo ensamblado. Es una
línea, está declarada en su §2 («la usa TemaBotonesTests… no es muerta») y sin ella el
árbol compartido no compila para nadie. Coscha necesaria y revelada, no invasión; el
orquestador decide si la atribución formal pasa al agente tema.

**3. ¿Está enchufado? — SÍ, y es la mitad fuerte del trabajo.**
La prueba de juego existe y cruza la costura por donde el jugador pasa: escena `Isla`
real, botones reales pulsados con la secuencia completa de puntero (no reflexión — su
propia nota explica que los callbacks no disparan así), monedero afirmado por eventos que
solo `EconomyService.TryBuy` publica (`EconomyService.cs:92-108`; único llamador en todo
el juego: `ShopPanel.cs:154`). Además codifica las tres variantes de la enfermedad: la
costura misma (`CosturaDeTiendasTests.TodaTiendaQueAnunciaUnaZonaExisteEnElCatalogo`,
con los ids sacados de `IslandLayout.FirstIsland()` y jamás copiados), el listón
(`CadaZonaConTiendaTieneSurtidoTodosLosDias`, 30 días × tienda) y el enterrado
(`panel.Pick(centro) == botón` en ambas pruebas de juego).

**4. ¿Es lo mínimo que funciona? — SÍ.**
Alinear tres ids, un `Debug.LogError` ante id desconocido, un `ScrollView` de ocho líneas
y retoques quirúrgicos a dos tests que escribían el contrato al revés. Sin banderas, sin
configuración, sin abstracciones para casos que nadie pidió. La ayuda `AbrirCandados()`
del test de juego está justificada con números (3 objetos de nivel 1 entre ~90 muebles,
8 huecos al día) y prueba la costura, no la progresión.

**5. ¿Los comentarios explican el porqué? — SÍ.**
`ShopDefinition.cs:28-36` explica por qué el id habla el idioma del plano y el nombre
bonito va en `DisplayName`; las `<remarks>` de `Get` explican por qué grita **y** devuelve
null; el comentario del `ScrollView` (`ShopPanel.cs:53-61`) explica el colapso flexbox y
por qué estuvo oculto desde siempre. Todo `///`, en español, del porqué.

**6. ¿Miente en algo? — NO en lo sustantivo.**
Contrasté las referencias duras: `IslandLayout.cs:44,60,68` declara exactamente los tres
ids que dice; `StockOf` maneja el null de `Get` devolviendo vacío (`EconomyService.cs:133`);
las garantías del huerto corren y pasan (`LaTiendaDeComidaTraeSemillasTodosLosDias`,
`NingunCultivoRentaMuchoMasQueLosDemas` — verde en revision-EditMode.xml). Deriva menor de
números de línea en su informe (ver defectos), pero las afirmaciones son ciertas.

## Defectos (menores, no bloqueantes)

1. **Deriva de líneas en el informe**: dice `ShopPanel.cs:140` para el llamador de
   `TryBuy` y es `ShopPanel.cs:154`; dice `HighestIslanderLevel` en `158-168` y empieza
   en `168`; «Hoy no queda nada» estaba en `96-99` y ahora es `112`. Desfase esperable
   tras sus propias ediciones; las afirmaciones siguen siendo verdaderas. Para la próxima:
   re-medir antes de firmar.
2. **La línea de `Nimbo.Tests.asmdef` es de otro**: funcionalmente necesaria en el árbol
   compartido y declarada, pero conceptualmente pertenece al agente tema. Que el
   orquestador la apunte a quien corresponda.
3. **§7, los dos Unity muertos ajenos**: no verificable post-hoc, y saltarse el cerrojo
   matando procesos de otros intentos está fuera del «espera» literal. Lo salvo porque
   está declarado con PID, hora y patrón, afirma no haber borrado ningún lockfile, y la
   cola estaba bloqueada para los cinco agentes. Si vuelve a pasar, que se documente
   igual y se avise al orquestador en vivo, no solo en el informe.

## AFIRMACIONES SIN RESPALDO

- §4 «volcado de rects del panel desde la prueba»: el volcado no está conservado en
  ningún sitio; lo que queda es la aserción `Pick(centro)` que codifica la misma verdad.
  Aceptable, pero el volcado crudo habría sido mejor evidencia.
- §7 «+18 nimbos medidos entre dos lecturas» y los detalles de los PIDs: inverificables.
  Declarados con suficiente especificidad como para tomarlos por buenos.

## LO QUE ESTÁ BIEN (que no se deshaga)

- **El lado que cedió**: el catálogo hablando el idioma del plano es la dirección
  correcta — `IslandLayout.cs:44,60,68` declara los ids, `DisplayName` guarda el nombre
  comercial, y el patrón coincide con `zona_*`/`seed_*` del resto del proyecto. No
  revertir hacia ids bonitos en el catálogo.
- **La regla anti-recaída hecha código**: los ids del test salen de
  `IslandLayout.FirstIsland()`, no de una copia. Si alguien renombra una zona, el test
  sigue al mundo en vez de quedarse verde mirando un idioma muerto.
- **El contrato nuevo**: id desconocido = error visible + vacío (igual que
  `ItemCatalog.GetItem`), y el test antiguo que escribía el silencio como comportamiento
  correcto quedó reescrito con `LogAssert.Expect`. Ese silencio era el cómplice de las
  semanas de rotura.
- **El guardián anti-enterrado**: `panel.Pick(centro) == botón` convierte la variante
  «enterrado» del catálogo de enfermedades en aserción. Mantenerlo en cualquier refactor
  de `ShopPanel`.
