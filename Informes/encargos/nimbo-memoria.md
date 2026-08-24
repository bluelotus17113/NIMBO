---
description: Haz que los vecinos de Isla Nimbo se acuerden de lo de ayer: la Crónica lo guarda todo y nadie te lo menciona nunca al hablar.
mode: all
model: opencode/x-preview-f-free
temperature: 0.25
tools:
  write: true
  edit: true
  bash: true
  read: true
  grep: true
  glob: true
---

# El hueco, ya medido

La vida de la aldea pasa sola y queda registrada: amistades y riñas que nacen de encuentros
casuales (`IslanderBrain.cs:174-194`), flechazos, bodas, bebés, y una Crónica que lo cuenta
por días (`ChroniclePanel`).

Pero al hablar con un vecino, **nunca te menciona nada de eso**. La memoria existe, está
guardada, y la conversación no la usa. El analista lo puso entre los huecos hacia Harvest
Moon: *«falta que te mencionen lo de ayer en conversación»*.

Es lo que separa un vecino con vida de un vecino con estadísticas: que se acuerde.

# Tu carpeta

`Assets/_Project/Scripts/Social/**`.

**Es el módulo mejor probado del repo** — cinco ficheros de pruebas de editor más dos de
juego, 650 líneas en `SocialService.cs`. Léelas antes de tocar: te dicen qué garantiza este
sistema, y romperlo sería el peor negocio de la noche.

Si hablar pasa por `PlayerInteractor` o por la ficha, **eso no es tuyo**: describe el
enganche con línea y texto propuesto y lo aplica el orquestador.

# Lo que quiero

Que al hablar con alguien, **a veces**, diga algo que solo tiene sentido por lo que pasó.
«Ayer vi a Bea con Leo en la plaza». «Todavía estoy enfadada con Mia». «Gracias por lo del
otro día».

Tres cosas que decidirán si esto funciona o molesta:

**A veces, no siempre.** Un vecino que cada vez que le hablas te cuenta el cotilleo del día
se vuelve un tablón de anuncios. Elige una probabilidad y justifícala.

**Que envejezca.** Lo de ayer se menciona; lo de hace tres semanas, no. Y si ya te lo dijo,
que no te lo repita.

**Que suene a esa persona.** Hay 16 tipos de personalidad con un fichero cada uno en
`Personality/Types/`. Un tímido y un cotilla no cuentan lo mismo ni de la misma forma. Si
no te da tiempo a las 16 voces, hazlo por ejes y dilo.

# Cuidado con

**No inventes un sistema de memoria nuevo.** Lo que pasó ya está guardado — en la Crónica,
en las relaciones, en los estados de conflicto. Búscalo y léelo. Este repo tiene precedentes
de sistemas reimplementados al lado de otro que ya existía.

# Comprobar

Una prueba de editor que fabrique un suceso —una riña, una pareja nueva— y compruebe que
el vecino **puede** mencionarlo; y otra de que **deja de hacerlo** cuando el suceso
envejece. La segunda importa igual: sin ella, el sistema queda repitiendo lo mismo para
siempre y nadie lo nota hasta que juega media hora.

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
