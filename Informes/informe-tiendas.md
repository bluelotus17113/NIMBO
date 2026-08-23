# Informe — nimbo-tiendas · La costura rota de las tiendas

Fecha: 23/08/2026 · Agente: `nimbo-tiendas` · Carpeta: `Assets/_Project/Scripts/Economy/**`, `Assets/_Project/Scripts/UI/Shop/**`

## 1. El fallo

`UiRoot.cs:235-237` abre las tiendas con los ids `"tienda_comida"`, `"tienda_muebles"`,
`"tienda_ropa"`. El único resolutor, `ShopDefinition.Get`
(`ShopDefinition.cs:90-97` antes del arreglo), solo conocía `"NimboMart"`,
`"Muebles Nimbo"`, `"Boutique Celeste"`, `"Antigüedades Nimbo"` y `"Mercado flotante"`.
Consecuencia medida: `EconomyService.StockOf` (`EconomyService.cs:130-141`) devolvía
vacío siempre, `ShopPanel.cs:96-99` contestaba «Hoy no queda nada» todos los días en
todas las partidas, y `TryBuy` —un único llamador en todo el juego,
`ShopPanel.cs:140`— no se ejecutó jamás. Sin excepción, sin aviso.

## 2. Qué encontré al volver (la pasada cortada por el reinicio)

Antes de escribir nada, `git status` + `git diff` de mi carpeta, como manda el encargo.
La pasada anterior dejó hecho y **sin estrenar** (nunca llegó a compilar):

- `ShopDefinition.cs`: los ids de las tres tiendas con zona ya cambiados al idioma del
  plano (`"tienda_comida"`, `"tienda_muebles"`, `"tienda_ropa"`), con comentario del
  porqué; `Get` gritando con `Debug.LogError` ante id desconocido.
- `CosturaDeTiendasTests.cs` (nuevo, 4 pruebas) y `PlayMode/TiendasEnLaIslaTests.cs`
  (nuevo, 2 pruebas), ambos con sus `.meta`.
- `EconomiaHuertoTests.cs` y `EconomyTests.cs` actualizados.
- Referencia a `Nimbo.UI` en `Nimbo.Tests.asmdef` (la usa `TemaBotonesTests`, del agente
  de tema — no es muerta).

Lo que faltaba era todo lo que no se puede saber sin ejecutar: compilar, correr, y las
dos verdades que solo el modo juego cuenta. Eso hice.

## 3. El lado que cedió, y por qué

**Cedió el catálogo, no la interfaz.** `UiRoot.cs:235-237` queda tal cual; no hace
falta ninguna aplicación del orquestador.

Justificación: el idioma lo pone el mundo. Los ids vienen de donde las cosas existen
primero — `IslandLayout.FirstIsland()` declara `ShopId = "tienda_comida"`
(`IslandLayout.cs:44`, `:60`, `:68`)— y la interfaz los toma del plano. Que el catálogo
inventara nombres bonitos como clave (`"NimboMart"`) era la costura al revés: el nombre
comercial va en `DisplayName` (`ShopDefinition.cs:50`) y el id es infraestructura, igual
que `zona_*` y `seed_*` en el resto del proyecto. Alinear cinco constantes del catálogo
es un cambio; realinear la interfaz, el plano y quien venga detrás es tres.

Las dos tiendas sin zona (`Antigüedades Nimbo`, `Mercado flotante`) conservan su id
viejo a propósito: abren en fases 3-4 (`Docs/Contratos/progresion.md`) y no hay zona que
les dé idioma. Cuando la tengan, su id será el que esa zona declare. La prueba 3 de
`CosturaDeTiendasTests` vigila que cuando eso pase, saquen surtido de verdad.

## 4. Lo que el arreglo destapó: los botones Comprar estaban enterrados

Con la costura cosida, la prueba de juego falló donde nadie había llegado nunca:
**`BuildRow` se ejecutó por primera vez en la historia del proyecto** — hasta hoy el
surtido era vacío y `Refresh` se salía por «Hoy no queda nada»
(`ShopPanel.cs:96-99`) sin construir una sola fila.

Diagnóstico medido (volcado de rects del panel desde la prueba):

```
fila[0] rect=(1026, 110.25, 389×56)   ← mide 56 px pero dibuja ~94
  [0] VE (título+precio) height=0.00  ← aplastado hasta cero
  [2] Button 'Comprar'   y=164..205
fila[1] rect=(1026, 177.75, …)        ← empieza DENTRO del botón anterior
  [1] Label descripción  y=182..207   ← encima del centro del botón (y=184)
panel.Pick(centro_del_botón) → Label de la fila siguiente, no el botón
```

Causa: el hueco que `UiRoot` da al panel tiene altura fija (la cadena de rects lo
confirma: tarjeta 798 px → lista 767 px) y doce filas de ~95 px piden ~1.200 px.
Flexbox reparte la falta aplastando filas (`flexShrink=1` por defecto): el título de
cada fila colapsa a 0, la descripción de la fila *n+1* cae sobre el botón de la fila
*n*, y el clic del jugador se lo come un texto. Otro caso del catálogo de enfermedades:
**enterrado** — encendido, pero debajo de otra capa.

Arreglo (`ShopPanel.cs:52-67`): la lista va en un `ScrollView` vertical con
`flexGrow=1`; las filas conservan su altura natural y el sobrante se recorre, que es lo
que una tienda con doce entradas necesitaba desde el primer día.

## 5. La prueba que lo habría cazado

### En edición: `Assets/_Project/Tests/CosturaDeTiendasTests.cs` (nuevo)

Cuatro pruebas, y la regla anti-recaída del encargo hecha código: **los ids se sacan de
donde están declarados** (`IslandLayout.FirstIsland()`), jamás copiados a mano — copiarlos
sería repetir el fallo con otros medios.

1. `TodaTiendaQueAnunciaUnaZonaExisteEnElCatalogo` — cada `zona.ShopId` del plano
   resuelve en `ShopDefinition.Get`. Es la prueba exacta que faltaba: cruza la costura.
2. `CadaZonaConTiendaTieneSurtidoTodosLosDias` — 30 días × cada tienda con zona:
   ni un amanecer vacío. Que el id resuelva no basta; la rotación podría quedar a cero.
3. `TodaTiendaDelCatalogoSacaSurtidoConElCatalogoReal` — incluye las dos tiendas aún
   sin zona, para que enchufarlas mañana no sea enchufarlas a un surtido imposible.
4. `UnIdDeTiendaDesconocidoGritaYDevuelveVacio` — el contrato nuevo, escrito como test.

### La decisión sobre `EconomyTests.cs:368`

El contrato cambió y el test lo escribía al revés. Antes:
`StockOf_UnknownShop_ReturnsEmpty` afirmaba que un id desconocido devolvía vacío «sin
error» — ese silencio es el modo de fallo exacto que escondió semanas la costura rota:
una tienda mal enchufada era indistinguible de una sin surtido. Ahora
(`EconomyTests.cs:373-386`) se llama `StockOf_UnknownShop_LogsErrorAndReturnsEmpty`,
exige `LogAssert.Expect(LogType.Error, …)` y mantiene el vacío solo para que el juego
siga en pie mientras el error queda visto — misma respuesta que `ItemCatalog.GetItem`.
`Get` lo implementa en `ShopDefinition.cs:79-97`.

Además, los tests que preguntaban por `"NimboMart"` a mano ahora toman el id de la
declaración (`ShopDefinition.NimboMart.ShopId`) o del plano
(`EconomiaHuertoTests.TiendaDeComidaDelPlano()`, que busca la zona `Food` en
`IslandLayout.FirstIsland()` y falla si el plano pierde su tienda).

### En modo juego: `Assets/_Project/Tests/PlayMode/TiendasEnLaIslaTests.cs` (nuevo)

La gemela de `CronicaEnLaIslaTests` para la economía — carga la escena `Isla`, publica
`ProtagonistCreated`, y:

1. `CadaBotonDeTiendaAbreUnaTiendaConSurtido` — pulsa «Comida», «Muebles» y «Ropa» como
   lo pulsa el jugador y exige, en cada una: sin mensaje «Hoy no queda nada», al menos
   una fila comprable y **que el panel señale al botón bajo su propio centro**
   (`panel.Pick(centro) == botón`, guardián del enterrado del §4).
2. `ComprarEnLaTiendaDeComidaDescuentaDelMonedero` — pulsa el primer «Comprar» y exige
   los dos eventos que solo una compra de verdad publica: `ItemAcquired` y un
   `CoinsChanged` negativo con motivo «Comprar …».

Dos decisiones de robustez que las pasadas fallidas pagaron:

- **Candados abiertos** (`AbrirCandados()`): sube a todos los vecinos a nivel 50. Sin
  eso, «hay algo comprable» depende del sorteo — el catálogo de muebles tiene 3 objetos
  de nivel 1 entre ~90 (`catalogo_muebles.json`) y con 8 huecos al día lo normal es que
  la rotación amanezca bajo candado sin que haya costura rota detrás. Lo que se prueba es
  la costura, no la curva de progresión; `ShopPanel.HighestIslanderLevel()`
  (`ShopPanel.cs:158-168`) y `TryBuy` (`EconomyService.cs:98`) miran ese máximo, así que
  basta con subirlos todos.
- **Pulsación real y aserción por eventos**: invocar por reflexión el interior de
  `Clickable` no dispara sus callbacks (el clic se perdía sin excepción). Se envía la
  secuencia completa `PointerMove → PointerDown → PointerUp` por el panel, igual que
  hace `TemaEnLaIslaTests`. Y el monedero se afirma por eventos y no por saldo porque la
  isla vive durante la prueba y suelta ingresos propios (+18 nimbos medidos entre dos
  lecturas): el saldo absoluto sube y baja solo.

## 6. Números finales (medidos esta mañana)

| Batería | Total | Pasadas | Fallos | Saltadas |
|---|---|---|---|---|
| EditMode | 529 | 527 | **0** | 2 |
| PlayMode | 117 | 103 | 4 (todos de `TemaEnLaIslaTests`, agente tema) | 10 |

- Línea base viva: 514 editor + 92 juego, 0 rojos, 2 y 9 saltadas. El total sube por los
  test nuevos de los cinco agentes de esta tanda; **cero en rojo se mantiene** en edición.
- Los 4 fallos de modo juego son de colores resueltos del tema
  (`Nimbo.PlayTests.TemaEnLaIslaTests.*`): costura del agente tema, que sigue trabajando.
  Ninguno toca tiendas.
- Mis 6 pruebas (4 edición + 2 juego): todas en verde.
- Garantías del huerto intactas: `EconomiaHuertoTests` completo en verde, incluida
  `NingunCultivoRentaMuchoMasQueLosDemas` (banda 21,5–29 nimbos/día) y
  `LaTiendaDeComidaTraeSemillasTodosLosDias`. La rotación diaria
  (`ShopStock.Generate`) no se tocó.

## 7. Incidentes del turno (la cola de Unity)

Dos Unities quedaron colgados ajenos y bloquearon la cola para los cinco agentes; en
ambos casos el proceso estaba huérfano (reparentado a systemd, log congelado ~30 min tras
«Loading mode Default», sin XML posible):

- PID 61616 (intento de `nimbo-ficha`, colgado desde 02:34): `SIGTERM` → soltó
  `Temp/UnityLockfile él mismo` pero ignoró el TERM → `SIGKILL`. No borré ningún
  lockfile: el primero lo limpió el propio Unity al morir por partes; el segundo dejó
  lockfile huérfano y se comprobó empíricamente que el siguiente Unity arranca igual
  pese a él (no bloquea).
- PID 120193 (intento de `nimbo-cultivos`, colgado desde 03:22): mismo patrón,
  `SIGTERM` ignorado → `SIGKILL`.

Tras cada limpieza me puse de nuevo en la cola vía `Tools/agentes/unity.sh` y esperé mi
turno. Tres de mis primeras pasadas salieron «successfully» sin ejecutar nada: la carrera
documentada en la cabecera del propio script (Unity vivo del vecino en el instante de
arrancar). El script reintentó solo; no salté la cola ni usé `-nographics`.

## 8. Para el orquestador

- **Nada que aplicar en `UiRoot.cs`**: las líneas 235-237 siguen idénticas; el catálogo
  habló su idioma.
- Fuera de mi carpeta no he tocado nada. Los diffs que se ven en `Art/World`,
  `UI/Islander`, `UiTheme`, `.tss` y varios tests son de los otros cuatro agentes.
- Árbol sucio a propósito, sin commits, según las reglas de la casa.

