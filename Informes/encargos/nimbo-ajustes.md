---
description: Dale a Isla Nimbo unos ajustes de pantalla: hoy solo hay tres volúmenes y el jugador no tiene forma de defenderse si algo no le encaja.
mode: all
model: opencode/x-preview-f-free
temperature: 0.2
tools:
  write: true
  edit: true
  bash: true
  read: true
  grep: true
  glob: true
---

# El hueco, medido

`OptionsPanel.cs:9-12` tiene **tres volúmenes y nada más**. Ni resolución, ni modo
ventana, ni escala de interfaz.

Y este proyecto **ya sufrió un incidente de resolución**: el HUD montado a 1920 mostrándose
en una ventana de 1280, todo al 67 %, el texto pareciendo pisado. Está corregido en el
asset (`NimboPanelSettings.asset:21-29`: `ScaleWithScreenSize`, referencia 1920×1080,
`match = 1` por altura, y `SceneBuilder.cs:78-80` lo reescribe en cada reconstrucción),
pero **el jugador no tiene ninguna palanca** si algo no le cuadra en su pantalla.

La auditoría dejó además dos riesgos vivos:
- `match = 1` solo por altura, con anchos de panel fijos y sumables: en proporciones
  estrechas el flexbox comprime y trunca texto.
- Fuentes base de 9-11 px en el hotbar (`HotbarView.cs:143,151,158`). El escalado
  proporcional ni las agrava ni las arregla: son pequeñas a cualquier resolución.

# Tu carpeta

`Assets/_Project/Scripts/UI/Menu/OptionsPanel.cs` y lo que crees en `UI/Menu/`.

**No toques `NimboPanelSettings.asset` ni `SceneBuilder.cs`.** Son ajustes globales del
proyecto: si tu solución los necesita, descríbelo en el informe y lo decide el
orquestador. Hay precedente esta misma noche de un agente que cambió un ajuste global
para hacer funcionar su prueba, y hubo que revertirlo.

# Lo que quiero

**Resolución, modo ventana y escala de interfaz**, y que **se guarden**. Un ajuste que se
pierde al salir es peor que no tenerlo. Mira cómo persisten los tres volúmenes y sigue ese
camino.

Y la escala de interfaz es la que de verdad resuelve el problema de fondo: quien no vea
las fuentes de 9 px tiene ahí su respuesta sin que haya que rediseñar el hotbar.

# Cuidado con

**Cambiar la resolución en pleno juego reconstruye el layout.** Comprueba qué pasa con un
panel abierto: es donde este tipo de ajustes rompe cosas.

# Comprobar

Una prueba de que un ajuste cambiado **sobrevive a guardar y volver a leer**. Y otra de
que la escala de interfaz llega de verdad al panel — que se aplique al `PanelSettings` en
runtime, no que el deslizador se mueva.

# Isla Nimbo — las reglas de la casa

Juego de Unity 6000.5.5f1 + URP en `~/Proyectos/isla-nimbo`. Rune Factory en el cielo
más gestión tipo Tomodachi: una isla flotante con vecinos que viven solos.

## Lo que se te pide siempre

**Cada afirmación tuya lleva una ruta `fichero:línea` o un número que mediste.** «Convendría
mejorar el inventario» no vale. «`InventoryPanel.cs:210` dibuja 40 huecos fijos y
`_economy.Stack(id)` (línea 233) se llama dentro del bucle, así que son 40 consultas por
fotograma» sí vale.

**No ves el juego.** Puedes leer código, compilar y contar pruebas. No sabes si algo se
siente bien. No escribas «queda fluido» ni «se ve mejor»: di qué escribiste y qué midió
la prueba. Lo de si se siente bien lo decide el usuario mirándolo.

## Cómo se compila y se prueba, y es obligatorio

Unity solo deja **un** proceso dentro del proyecto, y además hay una carrera conocida:
cuando el anterior suelta el cerrojo pero todavía se está cerrando, el siguiente entra, ve
el lockfile del proyecto y **sale con éxito sin ejecutar nada**. Dos agentes ya lo
sufrieron sin darse cuenta. Por eso no lances Unity a mano: usa el envoltorio, que hace la
cola, espera a que el anterior se cierre del todo y **comprueba que el XML existe de
verdad** antes de darlo por bueno.

    Tools/agentes/unity.sh EditMode <tuNombre>
    Tools/agentes/unity.sh PlayMode <tuNombre>

Te imprime el recuento y los nombres de las que fallen. Puede tardar en darte el turno:
**espera**. Si te dice que no pudo correr, **no digas que las pruebas pasaron**.

La línea base viva es **588 pruebas de editor y 142 de juego, cero en rojo, 2 y 10 saltadas**.
Si tu cambio baja de ahí, lo has roto.

## Dónde se escribe

**Todo lo que escribas para que lo lea otro va en `Informes/` dentro del proyecto**, nunca
en `/tmp`. opencode bloquea leer ficheros fuera del proyecto —un agente se pasó nueve
intentos rebotando contra eso— así que `/tmp` es un agujero negro: se puede escribir y
nadie lo puede leer después. Los resultados de las pruebas caen solos en
`Informes/pruebas/`.

## Lo que no se toca, nunca

- `~/.config/unity3d/Nimbo/Isla Nimbo/` — la partida del jugador. **Ni leer para
  escribir, ni copiar encima, ni nada.** Ya se perdió una isla de treinta días este mes.
- Ficheros fuera de la carpeta que te asigne tu encargo. Trabajamos varios a la vez y lo
  que se rompe con agentes en paralelo no es el trabajo de cada uno: es la costura. Si
  necesitas engancharte a algo de otro, **descríbelo en el informe** —qué evento, qué
  llamada, en qué línea— y lo hace el orquestador.
- `git push`, `git reset --hard`, `git checkout --`, `rm -rf`. No commitees: deja el
  árbol sucio y ya lo reviso.

## Cómo está hecho este proyecto

- **Todo el arte se genera por código.** No hay un solo modelo importado. Mallas, texturas,
  música: C# que las construye en el arranque. Si tu solución pasa por importar un asset,
  no es la solución.
- **Ensamblados separados** (`Nimbo.Core`, `Nimbo.Data`, `Nimbo.Art`, `Nimbo.UI`,
  `Nimbo.Game`, `Nimbo.Social`, `Nimbo.Events`…). `Nimbo.Art` **no ve** `Nimbo.Events`.
  Para hablar entre módulos: `EventBus` (`Nimbo.Core.Events.GameEvents`) o
  `ServiceRegistry` + una interfaz en `Nimbo.Core.Services.Contracts`. Si tu cambio no
  compila por una referencia que falta, la respuesta casi nunca es añadir la referencia.
- **Comentarios `///` que explican el porqué, no el qué.** Nadie necesita leer «incrementa
  el contador»; sí necesita saber por qué 0,45 y no 0,5. Es la convención del repo entero
  y se espera de ti.
- Español en comentarios, nombres de prueba y mensajes de assert. Los identificadores de
  código, en inglés como está el resto.

## La regla que este proyecto aprendió a golpes

**Un sistema no está hecho hasta que hay una prueba que carga la escena `Isla` de verdad y
comprueba que el jugador tiene por dónde llegar a él.** Cuatro sistemas de aquí estaban
escritos, probados y apagados: nadie los había enchufado y ningún test lo delataba.
Mira `Assets/_Project/Tests/PlayMode/CronicaEnLaIslaTests.cs` como modelo.

Variantes de la misma enfermedad, por si te toca alguna:
- **El enchufe en un agujero que no existe**: una referencia por nombre —un id de zona, de
  receta— que no está en el otro lado. No hay excepción, no hay aviso: simplemente no pasa.
- **El listón donde no llega nadie**: un umbral por debajo del mínimo que el sistema puede
  producir, así que nunca se cumple.
- **Enterrado**: encendido, pero con una forma de mirarlo que nadie usaría dos veces.

## Cuándo se mide la costura

**El verificador de costura solo corre ENTRE olas, nunca durante.** Su trabajo es mirar
si las piezas encajan, y no se puede medir un encaje mientras seis agentes están moviendo
las piezas: se pasó una hora y cuarenta minutos esperando a que compilara un árbol que
otro agente estaba editando en ese momento. Hizo bien en esperar —comprobó que los errores
no eran suyos antes de tocar nada— pero la espera no tenía final.

Lo mismo vale para cualquier verificador: se lanza cuando la ola ha entregado, no antes.
