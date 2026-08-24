# Informe — nimbo-hud

Fecha: 2026-08-24 · Encargo: `Informes/encargos/nimbo-hud.md`

Carpeta trabajada: `Assets/_Project/Scripts/UI/Hud/HudView.cs`,
`UI/Achievements/AchievementsPanel.cs`, `UI/Decor/DecorPanel.cs`, más las dos pruebas
nuevas (`Tests/FiltrosVisiblesTests.cs`, `Tests/PlayMode/HudEnLaIslaTests.cs`).
**No he tocado `UiRoot.cs`, `GameEvents.cs`, ni ningún fichero fuera de mi carpeta.**
El árbol queda sucio, sin commits.

---

## 1. Lo que he cambiado

### 1.1 El badge de peticiones abre el tablón — funciona hoy, sin tocar UiRoot

`HudView.cs:126-129`: el badge (`name = "hud-peticiones"`) publica
`RequestBoardRead` al recibir un `ClickEvent`. No es un canal nuevo: es **el mismo
aviso** que ya publica el tablón de la plaza (`PlayerInteractor.cs:896`) y que UiRoot
ya convierte en abrir/cerrar la pantalla (`UiRoot.cs:424-428`). Un canal nuevo para el
mismo gesto habría sido un segundo sitio que mantener sincronizado.

Medido: `HudEnLaIslaTests.PulsarElBadgeDePeticionesAbreElTablon` carga la escena
`Isla`, envía un `ClickEvent` sintético sobre el elemento montado y comprueba que
`tablon-encargos` pasa de `display: None` a `Flex`. La segunda prueba comprueba que el
segundo clic lo cierra (el canal es toggle). Las dos en verde.

### 1.2 Nimbos y aldeano: clicable, con callback — el enganche es del orquestador

- `HudView.cs:88-92` — bloque de monedas (`name = "hud-nimbos"`), clicable, avisa por
  `OnCoinsClicked` (`HudView.cs:43`).
- `HudView.cs:107-109` — bloque «aldeano» (`name = "hud-aldeano"`), clicable, avisa por
  `OnLevelClicked` (`HudView.cs:46`).

Por qué callback y no evento: no existe ningún evento de `GameEvents` que abra las vías
o la tienda, y ese fichero solo lo edita el orquestador. Y por qué no lo enchufo yo:
el HUD no conoce paneles — se monta antes que ellos (`UiRoot.cs:105` va antes de
`:127` y `:145`) — y `UiRoot.cs` no es mío. Hasta que se aplique el §2.A, los dos
clics son inertes (null-safe, sin excepción).

### 1.3 Filtros con estado visible — patrón copiado, no inventado

Mismo patrón que `CraftPanel.BuildStations` (`CraftPanel.cs:97-112`): la fila se
reconstruye en cada cambio y el chip activo lleva fondo melocotón inline.

- `AchievementsPanel.cs:62-67` fila `filtros-logros`; `SetFilter` público en `:79-84`;
  marcado del activo en `:165`.
- `DecorPanel.cs:83-88` fila `filtros-adornos`; `SetFilter` público en `:207-212`;
  marcado del activo en `:235`.

Antes, pulsar un filtro solo repintaba la lista (`AchievementsPanel.cs:81` y
`DecorPanel.cs:208` en su versión anterior): nada en pantalla decía qué filtro estaba
puesto. Ahora también el estado inicial dice algo: «Todo» arranca marcado.

`SetFilter` es público a propósito: es el mismo gesto que el chip, y un atajo futuro
que quiera abrir la lista ya filtrada usa el mismo camino que el clic.

Medido: `FiltrosVisiblesTests` (5 pruebas de editor): «Todo» activo al construir,
elegir otro lo marca y deja los demás con su aspecto de reposo, y volver a «Todo»
devuelve la marca. Compara contra un botón virgen y no contra una palabra clave
concreta, mismo cuidado de `TemaBotonesTests.AssertFondoFueraDelInline`.

---

## 2. Para el orquestador: enganches descritos, no aplicados

### 2.A Enchufar los dos callbacks del HUD

En `UiRoot.Mount()`, justo después de `root.Add(_hud.Root);` (`UiRoot.cs:106`),
añadir dos líneas:

```csharp
_hud.OnCoinsClicked = () => Toggle(() => _shop.Show("tienda_comida"), _shop.IsShowing);
_hud.OnLevelClicked = () => { if (_skills.IsShowing) _skills.Hide(); else _skills.Show(); };
```

- `Toggle` ya existe (`UiRoot.cs:354-357`) y es exactamente el gesto de los botones
  de tienda (`UiRoot.cs:237-239`).
- Elijo `tienda_comida` como tienda por defecto porque es la primera de la barra;
  si prefieres otra o una futura tienda «general», es cambiar ese id.
- Las lambdas capturan `this` y se evalúan al clic, así que da igual que `_shop` y
  `_skills` se construyan después en `Mount()` (`UiRoot.cs:127`, `:145`): nadie puede
  clicar a mitad de un montaje síncrono.

### 2.B Agrupación de la barra de acciones (`BuildActionBar`, `UiRoot.cs:225-311`)

Catorce botones idénticos. Los propios comentarios del fichero ya definen las familias;
solo falta que el ojo las vea. Propuesta mínima — **un movimiento y cuatro rayas**:

| Grupo | Botones | Líneas actuales |
|---|---|---|
| Tiendas | Comida · Muebles · Ropa | `:237-239` |
| Casa y mundo | Decorar · Construir/Amueblar · Ampliar la casa · Mapa | `:240-261` |
| Protagonista | Mochila · Hacer · **Vías** | `:262-269` + mover `:278-281` |
| Aldea | Fiestas · Encargos · Crónica · Logros | `:272-275`, `:286-300` |
| Gente | Nuevo habitante | `:301` |

1. **Único movimiento**: cortar el bloque `Add("Vías", ...)` (`UiRoot.cs:278-281`) y
   pegarlo justo después del bloque `Add("Hacer", ...)` (termina en `:269`). No lo
   digo yo: lo dice el comentario de encima (`:276-277`), que hoy su propio botón
   desmiente estando detrás de Fiestas.
2. **Separadores**: insertar uno después de Ropa (`:239`), de Mapa (`:261`), de Vías
   (en su nueva posición) y de Logros (`:300`). Fábrica local junto a `Add`:

```csharp
VisualElement Separador()
{
    var raya = new VisualElement();
    raya.style.width = 1;
    raya.style.backgroundColor = UiTheme.InkFaint;
    raya.style.marginLeft = 6;
    raya.style.marginRight = 14;
    raya.style.alignSelf = Align.Stretch;
    return raya;
}
```

Fondo inline aquí sí es legal: es un `VisualElement` tonto, no un botón — no hay
ninguna pseudoclase que pisar (la lección de `TemaBotonesTests` aplica a las fábricas
de botón, y ahí no se toca nada).

### 2.C Costura con Nimbo.Art: con el puntero capturado, TODO el HUD es inclicable

Medido: durante el juego normal la cámara captura el cursor
(`IslandCamera.cs:349-353`) mientras `ShouldLook` sea verdad (`:335-336`), y
`_freeLook` vale true por defecto (`:49`) sin que nadie lo cambie en runtime. Con el
cursor bloqueado no hay clic posible: ni en mi badge, ni en los nimbos, ni —esto es
lo gordo— **en los catorce botones de la barra de acciones**, que hoy solo son
clicables cuando ya hay un panel abierto o el juego está en pausa.

No lo arreglo: `IslandCamera.cs` es de `Nimbo.Art`. Opciones para quien decida:
(a) soltar el puntero cuando el puntero reposa sobre la interfaz, (b) dejarlo así y
dar atajos de teclado a lo que hoy solo tiene botón. Lo describo porque el encargo
§1 («atajos gratis») no existe de verdad hasta que esto se decida.

---

## 3. Costura: lo que rompí yo en esta ola, y cómo quedó

Añadí tooltips informativos a los tres bloques del HUD. `GateNotice` —la franja de
avisos de `nimbo-bloqueos`— recoge **cualquier** tooltip visible como explicación de
bloqueo (`GateNotice.cs:145-161`, compartido a propósito según su comentario
`:92-95`), y un texto siempre visible le ganaba el pase a las explicaciones reales:
la prueba `AvisosEnLaIslaTests.LaExplicacionDeUnBotonBloqueadoSeLeeSinPuntero` falló
con la franja diciendo «Tus nimbos · 8 más». Quité los tres tooltips
(`HudView.cs:89-91`, `:108`, `:127`) — además, según la propia medición de esa ola
(`informe-tema.md §4`), los tooltips no se despachan en runtime, así que no informaban
a nadie. Re-medido: las dos pruebas de `AvisosEnLaIslaTests` vuelven a verde.

---

## 4. Pruebas — lo medido, con el XML delante

Corridas con `Tools/agentes/unity.sh`; XML en `Informes/pruebas/nimbo-hud-*.xml`.

| Corrida | Resultado |
|---|---|
| EditMode completa (final) | **599 pruebas · 597 pasadas · 0 fallos · 2 saltadas** |
| PlayMode, solo `HudEnLaIslaTests` | **2/2 pasadas** |
| PlayMode, solo `TiendasEnLaIslaTests` | **2/2 pasadas** |

- Editor: línea base viva era 588/0/2; ahora 599/0/2. Cero en rojo. Las +11 son
  pruebas nuevas mías (5) y de los agentes paralelos (6). Mis 5:
  `Nimbo.Tests.FiltrosVisiblesTests.*`, todas `Passed` en el XML.
- Juego: mis 2 pruebas nuevas en verde. La suite completa de juego corrió tres veces
  durante la ola y el conjunto de fallos **cambió entre corridas** mientras otros
  agentes editaban: los rojos que vi están todos en ficheros ajenos y en pleno vuelo —
  `TeclasEnLaIslaTests.EscapeCierraElPanelAbiertoYSinAbrirPausa` dice por escrito que
  espera el enganche de `informe-teclas.md §3` en UiRoot, y `CajonSeguroTests` se
  editó (09:48) en mitad de mi segunda corrida. `TiendasEnLaIslaTests` falló dos veces
  dentro de la suite completa («tapado», `Pick` → null) y pasa 2/2 aislada: inestable
  por orden, no atribuible a mi diff — mis cambios no añaden ni mueven un solo
  elemento visible, y `ShopPanel.cs`/`TiendasEnLaIslaTests.cs` llevan sin tocarse desde
  el 23 de agosto (medido por fecha de modificación).

## 5. Ficheros que toco (todo el delta)

- `Assets/_Project/Scripts/UI/Hud/HudView.cs` — modificada
- `Assets/_Project/Scripts/UI/Achievements/AchievementsPanel.cs` — modificada
- `Assets/_Project/Scripts/UI/Decor/DecorPanel.cs` — modificada
- `Assets/_Project/Tests/FiltrosVisiblesTests.cs` — nueva (+ .meta generada por Unity)
- `Assets/_Project/Tests/PlayMode/HudEnLaIslaTests.cs` — nueva (+ .meta generada)
