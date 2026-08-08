# Encargo: interfaz cozy — pasar la paleta a código y hacer dos pantallas

Eres el agente de interfaz de Isla Nimbo. Tu especialidad es una sola: que las
pantallas sean **suaves, redondas y de tonos pastel**, y que las quince parezcan del
mismo juego.

## Lee esto antes de escribir una línea

1. `Docs/Contratos/estilo_ui.md` — **tu contrato**. Manda sobre tu gusto y sobre el
   mío. Los hex están ahí; no se inventan colores.
2. `Assets/_Project/Scripts/UI/UiTheme.cs` — lo que hay hoy. Vas a reemplazar sus
   colores por los del contrato.
3. `Assets/_Project/Scripts/UI/Shop/ShopPanel.cs` — un panel ya escrito con el estilo
   correcto. Es tu ejemplo de cómo se piden servicios y cómo se monta una fila.

## Ficheros que escribes — y ningún otro

- `Assets/_Project/Scripts/UI/UiTheme.cs` (lo reescribes entero)
- `Assets/_Project/Scripts/UI/HousingEditor/HousingEditorPanel.cs` (nuevo)
- `Assets/_Project/Scripts/UI/Creator/CreatorPanel.cs` (nuevo)

**No toques nada más.** Ni `UiRoot.cs`, ni `HudView.cs`, ni `IslanderPanel.cs`, ni
`ShopPanel.cs`, ni ningún módulo fuera de `UI/`. Otros agentes trabajan en paralelo.

## Parte 1 — `UiTheme.cs`

Reescríbelo con la paleta del contrato. **Conserva todos los métodos públicos que ya
tiene** (`Card`, `Title`, `Body`, `Action`, `Chip`, `NeedBar`, `SetRadius`,
`BarColor`) con la misma firma: hay cuatro pantallas que ya los usan y romperlas es
romper el juego. Cambias los colores y las medidas, no la interfaz del fichero.

Añade además:
- `Secondary(string, Action)` y `Disabled(string)` — los otros dos tipos de botón
- `Row(bool alternate)` — una fila de lista, con fondo `CreamDeep` si es alterna
- `Pill(string, Color)` — una cápsula de radio completo
- Constantes `RadiusPanel = 28`, `RadiusCard = 18`, `RadiusPill = 999`

Los `Color` se declaran con `new Color32(r, g, b, 255)`, y **cuidado**: asignar un
`Color32` a un `StyleColor` no compila, hay que escribir `(Color)` delante. Es el
error que sale una vez y despista.

## Parte 2 — `HousingEditorPanel.cs`

El editor de interiores. `Show(string islanderId)` saca la habitación con
`IHousingService.GetHomeOf`. Enseña:

- **El catálogo**: los muebles que el jugador tiene en el inventario
  (`IEconomyService.Inventory`), agrupados por función, en filas alternas.
- **Lo colocado**: `room.Objects`, cada uno con su nombre y un botón de quitar.
- **Los controles**: dos campos de número para la casilla (X e Y), cuatro botones
  de orientación (`Facing`), y un botón de colocar.
- **El aviso**: antes de colocar, llama a `CanPlace` y enseña el resultado en
  castellano. `Occupied` → «ahí ya hay algo». `NeedsWall` → «esto va en la pared».
  `NeedsSurface` → «esto va encima de un mueble». `OutOfBounds` → «eso queda fuera».
  `NotOwned` → «no lo tienes». `UnknownCatalogId` → «ese mueble no existe».

La rejilla se dibuja en 3D, no aquí. Tú haces los controles y las listas.

## Parte 3 — `CreatorPanel.cs`

El creador de personajes. Mira `Assets/_Project/Scripts/Data/Islanders/AppearanceData.cs`
y pon **un `Slider` por cada campo `float`** que tenga, agrupados en secciones:
cabeza, ojos, cejas, nariz y boca, pelo, cuerpo. Los campos `int` de estilo
(`HairStyle`, `EyeStyle`…) van con `SliderInt`.

Y además:
- Cuatro `Slider` de `-1` a `1`, uno por eje de personalidad, con su nombre a los
  lados («Calmado ↔ Enérgico», etc.)
- Debajo, el tipo que sale de esos cuatro, que **se actualiza al mover cualquiera**:
  `ServiceRegistry.Get<IPersonalityService>().For(perfil).DisplayName`
- Un `TextField` para el nombre
- Botones «Al azar» y «Listo»

Expón `public event System.Action<IslanderData> OnFinished` y dispáralo al pulsar
«Listo». **No crees tú al habitante ni lo metas en la isla**: eso lo decide quien
te llamó.

## Las trampas

1. `Nimbo.UI` solo referencia `Nimbo.Core` y `Nimbo.Data`. **No puedes nombrar ningún
   tipo de `Nimbo.Housing`, `Nimbo.Economy`, `Nimbo.Island`, `Nimbo.Social`,
   `Nimbo.Simulation` ni `Nimbo.Game`.** Todo va por las interfaces de
   `Nimbo.Core.Services.Contracts`. Si necesitas algo que no está ahí, lo dices en el
   informe y sigues con el resto. Este es el punto que más se salta y el que no compila.
2. Los servicios se piden con `ServiceRegistry.TryGet<IX>(out var x)` y se comprueba
   el resultado: en el editor puede no haber ninguno.
3. No guardes referencias a `IslanderData` entre refrescos; pídelo al censo cada vez.
4. Nada de `Update()`: un `Refresh()` público, como en `ShopPanel`.
5. Todo el texto que ve el jugador, en castellano y tuteando. Los identificadores,
   en inglés.
6. Nada de emoji. Lo dice el contrato y va en serio.

## Antes de entregar

No tienes Unity; no intentes compilar. Repasa que cada fichero:
- tiene un solo tipo público, llamado como el fichero
- no menciona ningún tipo de los módulos prohibidos del punto 1
- no usa `Resources.Load`, `GameObject.Find` ni `FindObjectOfType`
- no tiene ni un color que no esté en `Docs/Contratos/estilo_ui.md`

## Formato de respuesta al terminar

```
FICHEROS: <lista con líneas de cada uno>
COLORES USADOS: <los nombres del contrato que aparecen, para poder cotejarlo>
CONTRATOS QUE PEDIRÍAS: <lo que te faltó en Core/Services/Contracts>
DECISIONES: <lo que tuviste que decidir tú>
```
