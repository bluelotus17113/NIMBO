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

## El límite de la máquina

Esta máquina tiene **7,6 GB de RAM**, y Unity solo ya se lleva un buen trozo. Con cinco
agentes vivos más una compilación completa quedaban 1,3 GB libres y **Unity se colgó
después de «Unloading Unused Serialized files»**: sin error, sin log, sin XML, durante
treinta y siete minutos. La misma suite pasó a la primera con la máquina en reposo.

Por eso: **la suite completa se corre con pocos agentes vivos, o con ninguno.** Mientras
trabajas, usa siempre el filtro con el nombre de tus pruebas —tarda treinta segundos en
vez de veinte minutos y consume una fracción—. La suite entera la corre el orquestador
entre olas.

Si ves a Unity parado más de diez minutos sin escribir en su log, no esperes: no está
compilando, está colgado.

**Y no cortes una corrida de Unity a mitad.** Si matas el proceso que la lanzó, Unity no
se queda trabajando por su cuenta: pierde el canal de IPC (`IPC stream failed to read`),
se queda colgado en pie y bloquea la cola hasta que alguien lo mata. Si una corrida tarda
más de lo que aguanta tu herramienta, lánzala de fondo y vuelve a mirarla, no la cortes.

### El número real de agentes que caben

Medido, no estimado: **cada agente ocupa ~500 MB y Unity ~1,9 GB con su worker de
importación.** Esta máquina tiene 7,6 GB, de los que el escritorio y el orquestador ya se
llevan lo suyo.

Con cinco agentes vivos y una compilación en marcha quedaban **200 MB libres**, y ahí
Unity no falla: se queda mudo. Ni error, ni log, ni XML — treinta y siete minutos de
nada. Es el mismo síntoma que un cuelgue de código y no lo es.

Por eso: **como mucho tres agentes a la vez, y ninguno más mientras corre la suite
completa.** La flota no la limita el modelo ni la cuota: la limita la memoria.
