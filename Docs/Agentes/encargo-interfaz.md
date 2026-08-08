# Encargo: las pantallas de interfaz que faltan

La interfaz base ya existe: barra superior, ficha de habitante y la fila de abajo.
Faltan tres pantallas. Se construyen **en código con UI Toolkit**, sin ficheros
`.uxml` ni `.uss`, igual que las que ya están.

## Lee esto antes de escribir una línea

1. `Assets/_Project/Scripts/UI/UiTheme.cs` — **la paleta y los bloques básicos**.
   Úsalos: `Card`, `Title`, `Body`, `Action`, `Chip`, `NeedBar`, `SetRadius`.
   No inventes colores nuevos ni pongas medidas a ojo.
2. `Assets/_Project/Scripts/UI/Islander/IslanderPanel.cs` — **el ejemplo a seguir**.
   Mira cómo pide servicios, cómo se refresca y cómo monta filas.
3. `Assets/_Project/Scripts/Core/Services/Contracts/` — de ahí sacas los datos.

## Ficheros que escribes — y ningún otro

- `Assets/_Project/Scripts/UI/Shop/ShopPanel.cs`
- `Assets/_Project/Scripts/UI/HousingEditor/HousingEditorPanel.cs`
- `Assets/_Project/Scripts/UI/Creator/CreatorPanel.cs`

**No toques nada más.** Ni `UiTheme.cs`, ni `UiRoot.cs`, ni `IslanderPanel.cs`, ni
`HudView.cs`, ni ningún módulo fuera de `UI/`. Hay más agentes trabajando a la vez.

## Qué hace cada pantalla

**ShopPanel** — la tienda. Recibe un `shopId` en `Show(string shopId)`. Pide el stock
del día con `IEconomyService.StockOf`, y por cada objeto pinta una fila con nombre,
descripción, precio y un botón de comprar. El botón:
- se ve apagado y no hace nada si no llega el dinero o el nivel
- al comprar, llama a `TryBuy` y refresca la lista
Arriba, el saldo actual. Abajo, un botón de cerrar.

**HousingEditorPanel** — el editor de interiores. `Show(string islanderId)` pide la
habitación con `IHousingService.GetHomeOf`. Enseña:
- el catálogo de muebles que el jugador tiene en el inventario, por función
- lo ya colocado, con un botón de quitar por objeto
- botones de girar (las cuatro `Facing`) y de colocar en una casilla escrita a mano
- el resultado de `CanPlace` **antes** de colocar, con el motivo en castellano
  (`Occupied` → «ahí ya hay algo», `NeedsWall` → «esto va en la pared»…)
La rejilla visual la haré yo en 3D; **tú haces los controles y la lista**, no dibujes
la habitación.

**CreatorPanel** — el creador de personajes. Un `Slider` por cada campo numérico de
`AppearanceData` (mira el fichero: son unos veinte), agrupados por cabeza, ojos,
cejas, nariz y boca, pelo y cuerpo. Más:
- cuatro deslizadores de `-1` a `1`, uno por eje de personalidad, y debajo el nombre
  del tipo resultante, que se actualiza al mover cualquiera de los cuatro
  (`IPersonalityService.For(perfil).DisplayName`)
- un campo de texto para el nombre
- botones de «al azar» y de «listo»
Expón un evento `public event System.Action<IslanderData> OnFinished` y no crees tú
al habitante: el que llama decide qué hacer con él.

## Las trampas

1. `UiTheme.Action(texto, accion)` crea el botón ya con estilo. No uses `new Button()`
   a pelo o esa pantalla desentonará con las demás.
2. Los servicios se piden con `ServiceRegistry.TryGet<IX>(out var x)` y **se
   comprueba el resultado**. En el editor de Unity puede no haber ninguno registrado.
3. Un `Color32` no se convierte solo a `StyleColor`: hay que escribir `(Color)`
   delante. Es un error de compilación que sale una vez y confunde.
4. **No guardes referencias a `IslanderData` entre refrescos.** Pide el habitante al
   censo cada vez: si se ha mudado o se ha ido, la referencia vieja miente.
5. Nada de `Update()` reconstruyendo listas: un método `Refresh()` público que llama
   quien corresponda, como en `IslanderPanel`.
6. Todo el texto que ve el jugador, en castellano. Los identificadores, en inglés.

## Cómo se comprueba

No tienes Unity, así que no intentes compilar. Lo compilo yo. Repasa antes de
entregar que cada fichero:
- tiene un solo tipo público, y se llama como el fichero
- no menciona ningún tipo de `Nimbo.Game`, `Nimbo.Island`, `Nimbo.Social`,
  `Nimbo.Simulation` ni `Nimbo.Housing` (esos ensamblados **no** están referenciados
  desde `Nimbo.UI`: solo puedes usar `Nimbo.Core`, `Nimbo.Data` y los contratos)
- no usa `Resources.Load`, `GameObject.Find` ni `FindObjectOfType`

Ese tercer punto es el que más fácil se salta y el que no compila: si necesitas algo
que solo está en un módulo, es que hace falta un método nuevo en un contrato — lo
dices en el informe y sigues con lo demás.

## Formato de respuesta al terminar

```
FICHEROS: <lista con líneas de cada uno>
CONTRATOS QUE PEDIRÍAS: <lo que te faltó en Core/Services/Contracts>
DECISIONES: <lo que tuviste que decidir tú>
```
