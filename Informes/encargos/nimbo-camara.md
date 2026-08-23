---
description: Pasa Isla Nimbo a tercera persona: baja la cámara de 15 m y 48° a la altura de los ojos, y arregla las pruebas que fijan los topes viejos.
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

# Lo que ya se midió por ti

Léelo entero antes de tocar nada: **`Informes/nimbo-informe-camara.md`** (dentro del proyecto). Lo escribió otro
agente que se pasó una hora midiendo esto y trae el plan fichero por fichero. Resumen:

**La cámara ya no es cenital.** Es de seguimiento: detrás del jugador a **15 m y pitch
48°** (`IslandCamera.cs:35-37`). La conversión es sobre todo bajar el pitch (~18°) y
acortar la distancia (~5,5 m). Los topes de `CameraRig` no dejan llegar ahí: pitch mínimo
12°, distancia mínima 8 m (`CameraRig.cs:23-26`).

**La trampa clásica ya está desactivada.** `PlayerBody.ReadMove()` (`:328-348`) ya proyecta
el `forward` de la cámara sobre el plano y compone el movimiento con eso. **No la toques:
funciona igual a cualquier pitch.**

**El suelo duro está a +3 m** (`CameraRig.cs:36`) porque los tejados no tienen
colisionador. Al bajar a la altura de los ojos ese argumento se cae.

# Tu carpeta

`Assets/_Project/Scripts/Art/Camera/**`, `Assets/_Project/Tests/CameraRigTests.cs` y las
pruebas de cámara de `Tests/PlayMode/BootTests.cs`. Nada más.

**No toques `PlayerBody.cs`, `BuildModeView.cs` ni `FurnishModeView.cs`.** Los dos modos
ya tienen cámara propia y son inmunes; el informe lo justifica.

# El orden, y respétalo

Cada paso tiene que dejar el juego arrancando y la suite en verde:

**Paso 1 — los topes.** Bajar `MinPitch` y `MinDistance` (`CameraRig.cs:23-26`) para que la
pose de tercera persona sea alcanzable, y actualizar los números de las pruebas que los
fijan: `Target_Pitch_ClampedBetween12And78` (`:141`), `Orbit_ClampsPitch` (`:162`),
`LaCamaraNoSeMeteDentroDelSuelo` (`:391`), `ElSueloSeMideDesdeElMundoYNoDesdeElPivote`
(`:407`), `SubirLaCamaraNoLeCambiaLoQueMira` (`:453`). **El juego no cambia ni un píxel en
este paso** y la matemática nueva queda probada.

**Paso 2 — la pose.** `FollowPose` a tercera persona (`IslandCamera.cs:35-37`) y decidir
qué hace el ratón con el pitch. Hoy el pitch está clavado a propósito y solo se mira en
horizontal (`:380-388`, nota en `:371-377`). En tercera persona el jugador tiene que poder
mirar arriba y abajo: **decide tú entre ratón libre con topes o auto-alineado tras unos
segundos sin tocar nada, y justifica la elección en el informe.**

Aquí se cae seguro `LaCamaraEmpiezaEncuadrandoAlProtagonista`
(`BootTests.cs:284-303`), que exige `camera.y > body.y + 5`. A la altura de los ojos la
cámara va a 2-2,5 m sobre el muñeco. **Reescríbela por lo que de verdad importa** —que
encuadre al protagonista— en vez de por una altura: distancia corta y producto escalar
alto hacia él. Una prueba que fija una altura se rompe cada vez que se ajusta la cámara, y
este repo ya tiene el comentario escrito (`BootTests.cs:271-273`).

# Lo que NO es tuyo aunque lo veas

El informe lista más cosas que arreglar a ras de suelo —el agujero del borde de la isla,
las flores tumbadas, la niebla, los colisionadores de los árboles—. **Todo eso lo llevan
otros agentes en paralelo.** Si al probar ves que la cámara atraviesa un árbol, es
esperado: no lo arregles, anótalo.

# Cómo se mira esto

Ya existe `Tests/PlayMode/CapturaEstilo.cs`, que retrata cuatro encuadres con la cámara en
posiciones **escritas a mano** (y por eso siguen valiendo). Añade un encuadre nuevo desde
detrás del protagonista, para que el usuario pueda comparar antes y después de verdad. Sin
foto, nadie puede juzgar si 5,5 m y 18° son los números correctos — y no lo son
necesariamente: son un punto de partida, no una medida.

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

La línea base viva es **514 pruebas de editor y 92 de juego, cero en rojo, 2 y 9 saltadas**.
Si tu cambio baja de ahí, lo has roto.

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
