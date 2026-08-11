# Isla Nimbo — el giro a aldea habitada

> Este documento explica **qué cambia y qué no** al pasar de mirador a aldea que se
> recorre. Manda sobre el GDD en lo que se contradigan; `00_ARQUITECTURA.md` sigue
> mandando sobre este.

Decidido el 9 de agosto de 2026.

---

## 1. Qué es el juego ahora

Un **Stardew Valley en el cielo**, en 3D con cámara cenital, donde:

- El jugador **tiene cuerpo**. Se crea en el creador de personajes al empezar, sale
  del mismo sitio que los vecinos, y anda por la isla.
- La aldea sigue **viva por su cuenta**, con las 16 personalidades, las relaciones y
  las agendas que ya funcionan. Los vecinos no son horarios pintados: siguen teniendo
  vida interior.
- Hay capa **RPG**: inventario con huecos, herramientas, materiales, crafteo y un
  huerto pequeño.

Lo que **no** es: un juego de optimizar el día. Ver más abajo.

## 2. La regla que resuelve la tensión

Tomodachi va de **mirar y que te sorprenda**. Stardew va de **hacer y optimizar**.
Tiran en direcciones opuestas y hay que elegir a propósito, porque si no se elige,
gana la que más ruido hace y el juego castiga por mirar — que es justo lo que hacía
que la aldea mereciera la pena.

**Lo que hace el protagonista es riqueza opcional, no trabajo obligatorio.**

En concreto, y esto es contrato:

- La aldea **progresa aunque hoy no juegues a nada**: los vecinos trabajan, cobran,
  se hacen amigos y suben de nivel sin ti.
- **No hay castigo por el reloj.** No te desmayas de madrugada, no se mueren los
  cultivos por dormir, no caduca nada por no estar.
- **No hay estaciones.** Un calendario con ventanas de siembra convierte el huerto en
  una obligación con fecha. Aquí un cultivo tarda lo que tarda y punto.
- El vigor **no mata**: a cero no puedes usar herramientas y andas más lento, nada más.

Si una mecánica nueva obliga a entrar a diario, está mal aunque sea divertida.

## 3. El bucle

Lo que hace el jugador, por orden de importancia:

1. **Convivir** — hablar, regalar, atender peticiones, ver qué se traen entre ellos.
   Esto ya existe entero y sigue siendo el corazón.
2. **Recorrer y recoger** — la isla tiene sitios con materiales: madera, piedra,
   hierbas, flores, restos de nube. Se van reponiendo solos.
3. **Craftear** — con lo recogido salen muebles, adornos, herramientas mejores y
   regalos, que alimentan lo que ya hay (editor de interiores, decorar la isla).
4. **Cuidar el huerto** — una parcela junto a tu casa. Labrar, sembrar, regar,
   recoger. Pequeño a propósito: es un gesto diario agradable, no una explotación.

El dinero deja de venir solo de sueldos: vender lo recogido y lo crafteado es la otra
mitad, y es la que le da sentido a salir de casa.

## 4. Qué se conserva (casi todo)

Sigue igual y **no se toca** por este giro:

`Nimbo.Personality` · `Nimbo.Simulation` · `Nimbo.Social` · `Nimbo.Housing` ·
`Nimbo.Economy` · `Nimbo.Events` · los catálogos · los logros · los adornos ·
los minijuegos · el guardado · el creador de personajes · el arte procedural.

El creador de personajes gana un uso nuevo —crear al protagonista— pero no cambia.

## 5. Qué es nuevo

| Módulo | Qué hace | Quién |
|---|---|---|
| `Nimbo.Player` | cuerpo del protagonista, movimiento, vigor, interacción | orquestador |
| `Nimbo.Items` | inventario con huecos, herramientas, objeto en mano | agente |
| `Nimbo.Crafting` | recetas y mesa de trabajo | agente |
| `Nimbo.Farming` | la parcela: labrar, sembrar, regar, crecer, recoger | agente |
| `Nimbo.Gathering` | nodos de recurso en el mundo y su reposición | agente |
| mundo caminable | trazado, colisión, props, puertas, NavMesh | orquestador |
| cámara de seguimiento | detrás y arriba del protagonista | orquestador |

## 6. Lo que de verdad cuesta

**No son los sistemas RPG: es el mundo.** Hoy la isla es un diorama — diez "zonas"
que son un edificio en un punto de un disco de 100 m, y los habitantes aparecen en
puntos de aparición sin andar de verdad por ningún sitio.

Una isla caminable necesita trazado con colisión, caminos que lleven a algún lado,
edificios en los que se entra, y NavMesh para que los vecinos vayan andando en vez de
aparecer. Eso es el grueso del trabajo y lo lleva el orquestador, porque es la pieza
que no se puede probar con un test.

**Alcance cerrado: una isla, bien hecha.** Nada de archipiélago hasta que la primera
esté terminada.

## 7. La cámara

La que hay es de **observación**: orbita y se acerca a mirar a un vecino cuando abres
su ficha. La matemática (`CameraRig`) vale y se queda; el componente pasa a seguir al
protagonista.

Enfocar a un vecino **sigue existiendo** — es lo que hace que esto no sea un Stardew
más — pero ahora es un modo en el que entras y del que sales, no el estado normal.

## 8. Entrada

El proyecto tiene `activeInputHandler: 0`: **solo el sistema de entrada antiguo**.
Todo lo nuevo usa `UnityEngine.Input`. Quien escriba `Keyboard.current` rompe el juego
en cuanto alguien toque una tecla.
