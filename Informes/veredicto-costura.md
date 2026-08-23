# Veredicto — costura entre los seis agentes

Fecha: 23/08/2026 · Agente: `nimbo-costura` · Alcance: la unión del trabajo de
**tiendas, cultivos, ficha, tema, cámara y falda** (encargos en `Informes/encargos/`,
informes en `Informes/informe-*.md`).

```
VEREDICTO: ENCAJA
```

Las seis piezas funcionan **juntas**: cero referencias rotas por nombre, cero servicios
sin registrar, ningún evento nuevo sin oyente, ninguna firma rota, y un jugador real
recorre la cadena entera —cámara → isla cerrada → tema → tienda → compra → ficha →
cultivo— en una prueba de modo juego que pasa. Las suites completas: **0 rojas**.

Lo que hay que arreglar no rompe la unión hoy, pero son heridas de costura abiertas
por trabajar en paralelo: van numeradas al final.

---

## REFERENCIAS POR NOMBRE

**186 comprobadas · 0 rotas.** Script repetible dejado en
**`/tmp/opencode/costura_nombres.py`** (copia durable junto a este informe:
`Informes/costura_nombres.py`; código de salida 0 = todo encaja).

| Categoría | Comprobadas | Rotas | Nota |
|---|---|---|---|
| Zonas (`IslandLayout` ↔ literales `"zona_*"`) | 38 | 0 | 10 zonas declaradas |
| Tiendas (`ShopDefinition.All` ↔ `IslandLayout.ShopId` + `UiRoot.Show`) | 6 | 0 | `UiRoot.cs:235-237` y `ShopDefinition.cs:50,54,58` hablan el mismo idioma |
| Cultivos (catálogo JSON ↔ switch de `FarmView.LookFor`, `FarmView.cs:146-161`) | 24 | 0 | los 12 `seedId` de `catalogo_cultivos.json` = los 12 `catalogId` de `catalogo_materiales.json` = los 12 casos del switch: la cadena tienda→inventario→siembra→dibujo está entera |
| Literales con forma de id en producción contra los 415 ids de los 12 catálogos | 64 | 0 | excluidos por regla documentada: comentarios `///`, prefijos (`StartsWith("seed_")`, `ItemCatalog.cs:93`), etiquetas de función (`HousingService.cs:184`, que sí tiene 12 muebles con `"function": "decor"` en `catalogo_muebles.json`) y nombres de elemento UI (`UiTheme.Card("logros")`) |
| `GameObject.Find`/`transform.Find` contra nombres creados | 7 | 0 | «Mundo» nace en la escena (`Isla.unity:506`), «Protagonista»/«Zonas» en código |
| Propiedades de shader usadas desde C# contra `.shader` declarados | 47 | 0 | las estándar URP (`_SrcBlend`, `_ZWrite`…) excluidas |

## EVENTOS SIN OYENTE

Ninguno nuevo de esta tanda: los seis agentes no añadieron ni un `Publish` ni un
`Subscribe` (verificado en sus diffs; el único bus en sus ficheros es el
`Subscribe<CoinsChanged>` previo de `ShopPanel.cs:75`). Los que estaban, siguen:

- `GameSaved` — publicado en `SaveSystem.cs:165`, nadie escucha.
- `NeedBandChanged` — `NeedsSimulator.cs:95`.
- `PlayerSpawned` — `WorldView.cs:527`.
- `SpouseHelped` — `SpouseChores.cs:63`. Su propio doc dice que quien dibuja el huerto
  tiene que repintar la casilla: nadie se suscribe, así que el regalo del cónyuge no se
  ve hasta que pasas por encima. Preexistente, fuera de las seis carpetas.
- `WeddingHeld` — `WeddingPlanner.cs:157` y `Courtship.cs:362`.

## OYENTES SIN EMISOR

Preexistentes, ninguno de los seis:

- `IslanderLeft` — lo esperan `AchievementService.cs:184` (el logro «logro_abandono»),
  `IslandService.cs:58` y `WorldView.cs:104`; nadie lo publica jamás. El logro es
  imposible por construcción.
- `RoomEdited` — `AchievementService.cs:189`.

## SERVICIOS PEDIDOS Y NO REGISTRADOS

**Ninguno.** Todos los `TryGet<T>`/`Get<T>` tienen registro en `GameBootstrap`
(`GameBootstrap.cs:348-369`) antes del `GameLoaded` que monta la interfaz. El arranque
ya publica `GameLoaded` desde `Launch()` y no desde `Awake` precisamente para eso
(`GameBootstrap.cs:150-156`). Registrados sin pedido aparente: `NimboTree` y
`WardrobeService` — falso positivo: se consumen por jerarquía y por constructor de
`GiftService` (`GiftService.cs:40`), no por el registro.

## FIRMAS QUE CAMBIARON

Ninguna rota. Comparado cada fichero modificado contra HEAD: solo adiciones
(`SocialSection.Say` internal nuevo con su `InternalsVisibleTo` mínimo,
`AssemblyInfo.cs`; consts de clase en `UiTheme.cs:83-85`; tipos anidados en
`FarmView`). El único cambio de valor público visible, `CameraRig.MinHeight` 3→0,4
(`CameraRig.cs:47`), tiene su prueba reescrita y en verde. Sin parámetros opcionales
ni argumentos por nombre tocados.

## LÓGICA DUPLICADA

1. **La paleta vive dos veces y hoy coincide**: `UiTheme.cs` (C#: Peach `#FFC7A8`,
   PeachDeep `#F0A47D`, CreamDeep `#F6ECE0`, InkFaint `#C9BDB4`, Ink `#5C4A42`) y
   `NimboRuntimeTheme.tss:33-95` (los mismos cinco hex escritos a mano). Coinciden
   medida por medida; cambiar uno sin el otro parte los estados sin romper ningún
   test de estructura. Candidato a comentario cruzado o generación.
2. **Los fábricas de puntero sintético están en dos pruebas**: `TemaEnLaIslaTests.cs:322-343`
   (`MoverA/Aprieta/Suelta`) y `TiendasEnLaIslaTests.cs:191-208` (inline). Es el mismo
   conocimiento difícil —solo `GetPooled(UnityEngine.Event)` es público en 6000.5.5f1—
   escrito dos veces; mi prueba de cadena usa el mismo patrón y lo documenta en su
   lugar. Tres copias que divergirán la primera vez que Unity mueva la API. Un
   `PunteroSintetico.cs` compartido lo arregla; no lo hice yo porque toca ficheros de
   otros dos agentes.
3. Bien resuelto, para constancia: `SocialLabels.StatusOf` es ahora la única fuente de
   etiquetas de relación (ficha y mapa social leen de ahí) y la compatibilidad sigue
   teniendo una sola implementación (`Social/Relationships/Compatibility.cs`); ni
   `IslandSocialCard` ni nadie la recalcula. La falda dejó `EdgeProfile`/`MeadowHeight`
   como copias únicas (`IslandMeshBuilder.cs:107-126`) y cose vértices reales vía
   depósito de un uso (`:33,156-157`): los pares de llamadas siguen siendo inmediatos
   (`WorldView.cs:119→122` y `255→258`).

## PRUEBA DE LA CADENA

**`Assets/_Project/Tests/PlayMode/CosturaDeLaIslaTests.cs`** (+`.meta`) — una sola
prueba, `LaCadenaEnteraDelJugadorFunciona`, que carga la escena `Isla` y recorre lo
tocado por los seis en el orden en que lo toca un jugador:

1. **cámara**: tercera persona de verdad (<8 m, mirando al muñeco);
2. **falda**: soldadura prado↔roca medida sobre las mallas VIVAS de la escena (48
   sectores, <1 cm) — no llamando al constructor, para que un cambio en el orden de
   `WorldView` se delate aquí;
3. **tema**: el botón «Comida» lleva `nimbo-btn-action` y nadie le pinta fondo inline
   (comparado contra botón virgen, mismo criterio que `TemaBotonesTests`);
4. **tiendas**: pulsa «Comida», exige surtido, que el botón Comprar esté al alcance
   del puntero (`Pick(centro)`), compra y exige `ItemAcquired` + `CoinsChanged` negativo;
5. **ficha**: pulsa el botón del vecino en la fila de abajo, exige scroll, «Quién anda
   con quién» y «Qué le gusta» dentro de la ficha abierta;
6. **cultivos**: siembra por `IFarmingService` lo primero del catálogo real, madura,
   y compara las piezas dibujadas con las que `FarmView.LookFor` declara para ese id
   (reflexión, sin ids copiados) — caer en la genérica sería rojo.

**PASA** (verde en la corrida filtrada y en la completa). Coste de compilación: un
error mío durante el desarrollo (`TryStore` devuelve `StoreResult`, no `bool`),
corregido antes de la corrida buena.

## PRUEBAS

**529 editor / 118 juego / 0 rojas.** Saltadas: 2 editor (las de siempre) y 10 juego
(las 9 herramientas `[Explicit]` de captura previas + `CapturaHuerto.RetrataLosDoce`,
añadida por cultivos). XML: `Informes/pruebas/costura-full-{EditMode,PlayMode}.xml`.
Línea base viva (514/92, 0 rojas) superada; los fallos que los informes intermedios
atribuían a temas/tiendas/cultivos ya no existen: los agentes terminaron sus iteraciones
y esta pasada los vio verdes a todos.

---

## LO QUE HAY QUE ARREGLAR ANTES DE SEGUIR

Por daño; ninguno bloquea funcionalmente hoy, todos son heridas de la unión:

1. **Ocho botones siguen con fondo inline sobre el tema nuevo** — el inline gana a
   cualquier pseudoclase, así que en ellos no existe el hover/pulsado que trajo tema
   (el anillo de foco sí sobrevive: ninguno pinta `borderColor` inline). Tema y ficha
   se lo señalaron mutuamente y cada uno tenía razón en no tocar la carpeta del otro;
   quedó sin dueño. Inventario completo, todos heredados de HEAD pero cuyo efecto
   cambió con el trabajo de tema:
   - `HomeSection.cs:132` («Ampliársela», CreamDeep) y `JobSection.cs:172` («Ponerle
     aquí», CreamDeep) — carpeta ficha.
   - `SocialSection.cs:198` («Declararte», Rose) y `:277` («Pedirle la mano», Rose) —
     carpeta ficha.
   - `ShopPanel.cs:72` («Cerrar», InkSoft) y `:160` («Comprar» apagado, InkSoft) —
     carpeta tiendas; el segundo es defendible como «apagado no promete», pero entonces
     merece clase propia en el tss, no inline.
   - `UiRoot.cs:523` (fila de habitantes, PanelDark) y `CreatorPanel.cs:80` («Al azar»,
     CreamDeep) — sin dueño asignado: UiRoot lo comparten cuatro agentes.
   Decisión que pide el orquestador: o esas intenciones de color suben a clases del
   `NimboRuntimeTheme.tss` (p. ej. `nimbo-btn-rose`), o se quitan. Mi prueba de cadena
   vigila el camino principal («Comida») pero ningún test vigila estos ocho.
2. **Tooltips escritos para nadie**: tema lo midió (`informe-tema.md` §4: cero
   `TooltipEvent` en runtime aunque el botón esté encendido; el emisor solo existe en
   `UnityEditor.UIElementsModule`) y ficha siguió escribiendo en ese canal:
   `JobSection.cs:160` y `SocialSection.cs:160,208,246,287` explican bloqueos que el
   jugador no leerá nunca. O manipulador propio de tooltip en runtime, o la información
   sale visible; mientras tanto, cada `.tooltip` nuevo es texto perdido.
3. **Unificar los fábricas de puntero sintético** (duplicado nº 2) en un ayudante
   compartido de `Tests/PlayMode/`: tres copias hoy, y el día que Unity cambie
   `GetPooled(Event)` habrá que arreglarlo tres veces.
4. **Cabos declarados por otros que siguen sueltos** (recordatorio, no descubrimiento
   mío): `WorldView.cs:258` llama a `BuildUnderside` sin la semilla 31 del prado de la
   línea 255 — la soldadura ya no lo necesita (lee el labio real), pero la llamada
   miente; tejados sin colisionador frente a la cámara a 0,4 m
   (`informe-camara.md`, cabo declarado en el código); y el flag `evento_puente_aparece`
   (`IslandLayout.cs:100`) que nadie marca nunca, así que el embarcadero es inalcanzable
   por construcción — preexistente a esta tanda.

---

### Cómo repetir la comprobación de nombres

    python3 /tmp/opencode/costura_nombres.py /home/vaknadesu/Proyectos/isla-nimbo

Salida 0 si todo encaja; lista fichero:línea de cada costura rota si no. Las reglas de
exclusión (comentarios, prefijos, etiquetas de función, nombres de elemento) están
documentadas en el propio script.
