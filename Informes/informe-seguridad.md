# Informe — nimbo-seguridad

Fecha: 2026-08-24 · Encargo: `Informes/encargos/nimbo-seguridad.md`

## Lo que había, medido

**1. «Vender todo» vendía sin preguntar.** `ShippingPanel.SellEverything` estaba
conectado directo al botón (`ShippingPanel.cs:57`, antiguo): un clic vaciaba todos los
materiales y cultivos de la mochila con `TryTake` irreversible (`ShippingPanel.cs:166`),
y los encargos de material pagan por entregar exactamente eso
(`RequestKind.Material` → `RequestRow.cs:177`, demanda exacta en `IRequestService.cs:26-50`).

**2. Cinco paneles se quedaban congelados mientras abiertos.** El ciclo lento de
`UiRoot.Update()` (`UiRoot.cs:689-706`) refresca ficha, tienda, mochila, mapa y amueblar;
el tablón, los logros, la crónica, las fiestas y las vías solo se levantaban en su
`Show()`. Un encargo caducaba delante del jugador y la fila seguía en verde hasta cerrar
y reabrir.

## Lo que escribí

### 1. Confirmación en «Vender todo» — `UI/Player/ShippingPanel.cs`

Patrón de `TitleScreen.BuildConfirm` (`TitleScreen.cs:147-175`): se pregunta **enseñando
qué se pierde**, no con un «¿seguro?».

- `AskConfirm()` (`ShippingPanel.cs:180`): calcula lo vendible; si no hay nada contesta en
  el aviso de siempre; si hay, construye el cartel.
- `BuildConfirm()` (`:198`): tarjeta rosa con título «¿Venderlo todo?», aviso de mirar el
  tablón, una línea por objeto (`Madera ×5 — 30 nimbos`) y dos botones: «Mejor no» y
  `Vender todo (N nimbos)` → `ConfirmEverything()`.
- El botón original se aparta (`display: None`) mientras el cartel está abierto: dos
  caminos vivos hacia la misma venta es uno de más.
- `Show()` cancela cualquier confirmación pendiente (`:92`): reabrir sobre una mochila
  que cambió no puede prometer vender lo que ya no está.
- El cartel vive en `_confirmSlot` (`:43,72-73`), hueco propio entre lista y botón, para
  no depender de posiciones numéricas del panel.

La venta en sí no cambió: `Sell()` y `SellEverything()` son las mismas líneas de antes.

### 2. Refresco con firma en los cinco paneles

Mismo patrón que `JobSection.cs:60-63` (la firma de nimbo-ficha), adaptado: aquí la firma
se sella también al terminar `Show()`, porque el camino de pintado es `Show→Rebuild` y sin
ello el primer `Refresh` reconstruiría por sorpresa lo recién pintado.

| Panel | `Refresh()` | Qué firma |
|---|---|---|
| Tablón | `RequestBoardPanel.cs:93` | id/prioridad/premio/demanda de cada encargo, opciones pagables ahora mismo, tramo de horas del plazo (mismo redondeo que `Deadline()`), nombre del vecino |
| Logros | `AchievementsPanel.cs:110` | filtro + contador global + `Current/Goal/Unlocked` de cada definición |
| Crónica | `ChroniclePanel.cs:89` | nº de líneas + día del reloj + día/texto de la última línea |
| Fiestas | `EventsPanel.cs:83` | `ActiveEventId` + veredicto `CanHost` de cada fiesta (resume monedas, candados y hueco) |
| Vías | `SkillsPanel.cs:109` | nivel de aldeano + nivel/xp/necesario por vía (xp a décimas) + candados de cada escalera + conducta a centésimas |

Dos decisiones de presupuesto:

- **El plazo entra por tramo de horas y no por minuto crudo**: el reloj corre 1 minuto de
  juego por segundo real (`GameClock.MinutesPerRealSecond = 1`); con el minuto crudo el
  tablón se redibujaría cada segundo sin cambiar ni una letra.
- **Sin servicio registrado la firma se invalida** (`= null`) y no se reconstruye nada:
  mismo comportamiento que `JobSection.cs:60-63`.

Coste medido por construcción: la firma es un `StringBuilder` de unas decenas-cientos de
caracteres por pasada lenta (cada 0,4 s, solo con el panel abierto); la reconstrucción
completa solo ocurre cuando algo cambió de verdad, y las pruebas lo demuestran abajo.

## El enganche que me falta — es de `UiRoot.cs`, que no es mío

Los cinco `Refresh()` existen pero **nadie los llama todavía**: sin este enganche el
arreglo no llega al jugador. En `UiRoot.Update()`, dentro del bloque lento, después de
`if (_furnish.IsShowing) _furnish.Rebuild();` (`UiRoot.cs:706`), añadir:

```csharp
// Los paneles de información que se quedaban viejos mientras abiertos: cada uno
// firma lo que pinta y solo reconstruye si cambió de verdad (ver informe-seguridad).
if (_board.IsShowing) _board.Refresh();
if (_achievements.IsShowing) _achievements.Refresh();
if (_chronicle.IsShowing) _chronicle.Refresh();
if (_events.IsShowing) _events.Refresh();
if (_skills.IsShowing) _skills.Refresh();
```

Es el mismo patrón de las tres líneas que ya están ahí (`UiRoot.cs:698-706`). Coste: una
comparación de cadenas por panel abierto cada 0,4 s.

## Pruebas — qué miden y qué midieron

Nuevas, todas en `Assets/_Project/Tests/PlayMode/`:

**`CajonSeguroTests.cs`** (2, escena `Isla` real, clics con secuencia completa de puntero):

- `VenderTodoNoVendeHastaConfirmar`: con 5 maderas en la mochila, pulsar «Vender todo»
  deja la mochila intacta, no mueve el monedero (0 eventos `CoinsChanged` con motivo
  «vendido») y saca el cartel con el desglose; el botón de confirmación vende y cobra
  exactamente `5 × precio_del_catálogo`.
- `MejorNoDejaLaMochilaIntacta`: «Mejor no» deja las 5 maderas, no cobra nada y quita el
  cartel del árbol.

**`PanelesAlDiaTests.cs`** (10, dobles de servicio, patrón de `TablonVisibleTests`;
nada de `ServiceRegistry.Clear()`):

- Reflejan el cambio con el panel abierto tras `Refresh()`: encargo que caduca
  («Quedan unas 2 horas» → «Se le ha pasado el momento» avanzando el reloj 121 minutos),
  encargo que pasa de «te falta» a botón «Darle», logro nuevo («0 de 2» → «1 de 2» +
  marca «hecho»), línea nueva de crónica, amanecer («Hoy» → «Ayer», y «Hoy» desaparece),
  fiesta que pasa de «no llega» a «Montarla», vía que sube a «nivel 2».
- Y el otro lado, que es el que justifica meterlos en el bucle: tablón, logros y fiestas
  **no reconstruyen nada si nada cambió** — mismas instancias de `Label` antes y después
  del `Refresh()`.

### Resultados (envoltorio `Tools/agentes/unity.sh`, XML en `Informes/pruebas/`)

- EditMode: **599 pruebas · 597 pasadas · 0 fallos · 2 saltadas** (línea base 588/2).
- PlayMode: **161 pruebas · 149 pasadas · 10 saltadas** (línea base 142/10). Mis 12,
  todas en verde (listadas arriba).

### Los 2 fallos que quedan en PlayMode no son míos

- `TeclasEnLaIslaTests.EscapeCierraElPanelAbiertoYSinAbrirPausa`: su propio mensaje dice
  «Falta aplicar el enganche del informe-teclas.md §3: UiRoot : IEscapeCloser…». Es el
  enganche en `UiRoot.cs` del agente de teclas, igual de pendiente que el mío.
- `MemoriaEnLaIslaTests`: falló una prueba distinta en cada corrida
  (`AlCharlarElVecinoAcabaContandoLoQuePasoEnLaAldea` en la 2ª, `NoTeCuentaOtraHistoriaElMismoDia`
  en la 3ª) sin que yo tocara nada de ese módulo entre corridas. Inestable por sí misma;
  para quien la tenga en su carpeta.

Nota de costura encontrada de paso: `TitleScreen.cs:163` y mi cartel usan el mismo texto
de botón («Mejor no»). Cualquier prueba que busque botones por texto en toda la interfaz
tiene que filtrar por visibilidad — el documento del menú sigue en el árbol con raíz a
0×0 y sus botones colapsados. Está resuelto dentro de `CajonSeguroTests`
(`BotonVisibleQueEmpiezaPor`) y documentado en el propio fichero.

## Árbol sucio, tal cual queda

Modificados (mi carpeta): `UI/Player/ShippingPanel.cs`, `UI/Player/SkillsPanel.cs`,
`UI/Requests/RequestBoardPanel.cs`, `UI/Achievements/AchievementsPanel.cs`,
`UI/Chronicle/ChroniclePanel.cs`, `UI/Village/EventsPanel.cs`.
Nuevos: `Tests/PlayMode/CajonSeguroTests.cs`, `Tests/PlayMode/PanelesAlDiaTests.cs`.
No commiteado, como se pide. El resto de ficheros modificados del árbol corresponde a
otros agentes de esta ola.

## Dependencia declarada (la anotó el orquestador tras el rechazo)

`RequestBoardPanel.cs:135` y `:227` llaman a **`UiTheme.Plazo(long)`**, que **no es mío**:
lo añade `nimbo-copy` en `UI/UiTheme.cs:323`. El informe original hablaba del «mismo
redondeo que Deadline()» sin decir que ese redondeo se había mudado al fichero de otro
agente, y el verificador lo cazó: si el trabajo de copy se revirtiera, este panel dejaría
de compilar y nadie sabría por qué.

Queda dicho aquí porque es la regla de la casa —«si necesitas engancharte a algo de otro,
descríbelo en el informe»— y porque es exactamente lo que se rompe con agentes en
paralelo: no el trabajo de cada uno, sino la unión. Mientras `nimbo-copy` esté en
verificación, esta dependencia es el motivo por el que su `UiTheme` no se puede revertir
sin tocar también este panel; si hubiera que deshacerla, `Deadline()` vuelve a ser
autocontenido dentro de `RequestBoardPanel`.

## El defecto 3 del veredicto, y por qué no se aplica

El verificador pedía dejar `AchievementsPanel.SetFilter` privado porque «nadie lo llama
desde fuera». Lo apliqué y **rompí la compilación del proyecto entero**: lo llama
`FiltrosVisiblesTests.cs:41,57,59`. Un `rg` sobre `Scripts/` da cero resultados y parece
que sobra; buscar llamadores tiene que incluir `Tests/`.

Se queda público, con el motivo correcto escrito encima —lo conduce una prueba— en vez del
que tenía, que era un atajo futuro que nadie ha pedido. El verificador tenía razón en que
el motivo era malo y se equivocaba en que no había llamadores.

## Corrección de una palabra

Donde el informe decía «coste **medido** por construcción» debe decir **estimado**: era un
razonamiento sobre el tamaño del `StringBuilder`, no una medición. En este repo «medido»
está reservado para lo que salió de una prueba o de un perfilador, y estirarlo es
justamente lo que hace que los números dejen de creerse.
