# Dónde se quedó la flota

Pausa por apagado de la máquina. Todo lo de abajo está commiteado en la rama
**`flota-nocturna`**; `master` sigue intacto.

## Estado del árbol al pausar

    EditMode   563 pruebas · 561 pasadas · 0 fallos · 2 saltadas
    PlayMode   sin medir — se cortó a mitad al apagar

**La suite de juego es lo primero que hay que correr al volver.** El enganche del
Árbol Nimbo se aplicó entero pero su prueba (`DelanteDelArbolElCartelOfreceHablarle`)
no llegó a verificarse: estaba corriendo justo cuando se paró.

    Tools/agentes/unity.sh PlayMode retomo

## Lo que está hecho y verificado

Trece veredictos cerrados: **6 aprobados, 6 rechazados, 1 «encaja»**.

Aprobado: tiendas · cultivos · falda · acabados · bloqueos · costura.

Rechazado y **ya corregido por el agente, pendiente de re-verificar**:
ropa · colisiones · rasdesuelo. Sus correcciones entraron pero **ningún verificador las
ha visto todavía**. Ése es el segundo paso al volver.

Rechazado y **cerrado por el orquestador**: cámara · ficha · tema · árbol — sus defectos
eran de procedimiento o los arreglé yo (ver más abajo).

## Lo que aplicó el orquestador a mano

Los tres monolitos no son de ningún agente, así que estos cambios los hice yo:

1. **El enganche del Árbol Nimbo**, siete partes (A–G) sobre `GameEvents`,
   `ITreeService` (nuevo), `GameBootstrap`, `PlayerInteractor`, `WorldView`, `UiRoot` y
   `NimboTree`. El agente lo especificó; yo lo apliqué.
2. **`ArbolNimboTests.cs`** — la prueba que el comentario del agente prometía y que no
   existía. La fórmula de crecimiento está escrita dos veces porque `Nimbo.Art` no ve
   `Nimbo.Island`; esta prueba es lo único que impide que las dos copias se separen.
3. **Las tres líneas de cámara en `Isla.unity`** (`_followDistance` 15→5,5,
   `_followPitch` 48→18, `_followHeight` 1,1→1,2). Ratificadas: sin ellas la tercera
   persona no llega al juego, porque los valores serializados de la escena pisan los
   defaults de C#.
4. **`Tools/agentes/unity.sh` sin `-quit`.** Ratificado: `-quit` junto a `-runTests`
   mata el runner antes de que arranque, y ésa era la causa real de que Unity saliera
   «successfully» sin ejecutar la suite.
5. **La niebla de `Isla.unity:21-22`** (30/260) — pendiente de ratificar formalmente,
   el verificador de `rasdesuelo` lo pide.
6. `HomeSection` invalidando su firma como `JobSection`, el assert muerto de
   `FichaEnLaIslaTests`, y la mina de directorio de `DiagnosticoTooltip`.

## Al volver, en este orden

1. `Tools/agentes/unity.sh PlayMode retomo` — saber si el Árbol quedó enchufado.
2. Verificar las correcciones de **ropa**, **colisiones** y **rasdesuelo**.
3. Correr **`nimbo-costura`** otra vez: han trabajado seis agentes más desde la última
   pasada y la costura es lo que se rompe con trabajo en paralelo.
4. Artifact con la Ola 3 y 4, y decidir si `flota-nocturna` se mezcla a `master`.

## Lo que queda del reconocimiento, sin tocar

- **Creador de personajes sin voz ni ropa** pese al `[x]` del GDD. La voz existe, se
  sortea y se sintetiza; el jugador no la elige nunca.
- **Los ~14 defectos de interfaz** que quedan de los 19 del auditor.
- **Los huecos hacia Harvest Moon**: agenda visible del habitante, festivales que
  cambien el decorado, que te mencionen lo de ayer.
- **El pelo y las caras vistos de cerca** — el casquete es un cacillo abierto y la cara
  un parche de una sola cara. A 5 m de cámara se ve constantemente. Comparte carpeta con
  `nimbo-ropa`, por eso no se lanzó.

## Cómo se relanza la flota

    Tools/agentes/lanza.sh nimbo-NOMBRE Informes/informe-NOMBRE.md "el encargo"

Los encargos están en `Informes/encargos/` (copia sincronizada de
`~/.config/opencode/agent/`, porque opencode bloquea leer fuera del proyecto).
El panel: `Tools/agentes/panel.sh`.
