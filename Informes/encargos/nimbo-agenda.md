---
description: Enseña la agenda del día de cada habitante en Isla Nimbo: el GDD la promete, la simulación la calcula entera, y no hay ninguna vista que la muestre.
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

# El fallo, ya medido

El GDD §2.2 promete que el jugador «ve la agenda del día» de un habitante. **Esa vista no
existe** — no hay ningún panel que la enseñe.

Y la agenda **está calculada**: `IslanderBrain.cs:58-95` decide qué hace cada vecino hora
por hora según sus necesidades y su personalidad. Toda la información está ahí dentro,
usándose para moverlos por el mundo, sin que nadie pueda leerla.

Es la enfermedad de este repo en su forma más pura: el sistema funciona, se nota jugando
que los vecinos tienen vida, y no hay forma de mirarla.

# Tu carpeta

`Assets/_Project/Scripts/UI/Islander/**` — la ficha de habitante, que es donde vive todo
lo de un vecino y a donde el jugador ya va a mirarlo.

**Cuidado: esa carpeta la trabajó `nimbo-ficha` esta madrugada** y su trabajo está en el
repo (mira `IslandSocialCard.cs`, `TastesSection.cs`, `SocialLabels.cs`). Léelo antes de
tocar: la ficha ya tiene `ScrollView` —añadido por él— y una vista social agregada. **Sigue
sus patrones en vez de inventar otros**, y no le deshagas nada.

`IslanderBrain` no es tuyo. Si la agenda no es alcanzable desde donde estás, describe el
enganche —qué contrato haría falta en `Core/Services/Contracts/`, con qué firma— y lo
aplica el orquestador. Es exactamente lo que hizo el agente del Árbol Nimbo con
`ITreeService`; mira su informe en `Informes/informe-arbol.md` como modelo, sección 3.

# Lo que quiero

Que al abrir la ficha de un vecino se pueda ver **qué va a hacer hoy y a qué hora**.
Piensa qué hace legible una agenda: no una lista de 24 filas, sino los momentos que
importan y dónde está ahora.

Y algo que da mucho por poco: **que se note que la agenda es suya**, o sea que dos vecinos
con personalidades distintas tengan días visiblemente distintos. Si la vista no deja ver
esa diferencia, está enseñando datos en vez de un personaje.

# Cuidado con

La ficha ya era demasiado alta —ése fue el defecto nº 1 de la auditoría de interfaz— y por
eso tiene scroll ahora. Añadir una tarjeta más empuja las de abajo. Di en el informe dónde
la colocaste y por qué ahí.

# Comprobar

Una prueba de juego que cargue `Isla`, abra la ficha de un vecino y compruebe que la
agenda **está y dice algo** — no una tarjeta vacía. Y si puedes, que dos vecinos distintos
den agendas distintas: eso es lo que demuestra que lee la simulación de verdad y no un
texto fijo.

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
