# Contrato de estilo — interfaz cozy de Isla Nimbo

> Este documento manda sobre el gusto de nadie. Si una pantalla se salta una de
> estas reglas, está mal aunque quede bonita: lo que importa es que las quince
> pantallas parezcan del mismo juego.

La interfaz es **suave, redonda y de tonos pastel**. Nada de esquinas vivas, nada de
colores saturados, nada de bordes negros. La referencia mental es una papelería
japonesa, no un panel de control.

---

## 1. La paleta

Todo color sale de aquí. **No se inventan colores nuevos**: si hace falta uno que no
está, se pide y se añade a este documento y a `UiTheme.cs` a la vez.

### Fondos y superficies

| Nombre | Hex | Para qué |
|---|---|---|
| `Cream` | `#FFF8F0` | el fondo de un panel |
| `CreamDeep` | `#F6ECE0` | el fondo de una fila alterna o un hueco |
| `Sky` | `#BDE3F2` | el fondo de la pantalla, detrás de todo |
| `SkySoft` | `#DCF0F7` | cabeceras y zonas destacadas |

### Tinta

| Nombre | Hex | Para qué |
|---|---|---|
| `Ink` | `#5C4A42` | el texto normal. **Marrón cálido, nunca negro.** |
| `InkSoft` | `#9A8B82` | texto secundario, etiquetas, descripciones |
| `InkFaint` | `#C9BDB4` | texto desactivado y separadores |

### Acentos pastel

| Nombre | Hex | Para qué |
|---|---|---|
| `Peach` | `#FFC7A8` | la acción principal, lo que el jugador va a pulsar |
| `PeachDeep` | `#F0A47D` | esa misma acción pulsada o su texto sobre claro |
| `Mint` | `#B8E6C8` | lo que va bien, lo confirmado, la amistad |
| `Lavender` | `#D6C7F0` | lo romántico y lo mágico |
| `Butter` | `#FCE8A8` | avisos suaves y monedas |
| `Rose` | `#F5C0CB` | lo que va mal, sin llegar a alarma |
| `Sage` | `#CBDDB4` | naturaleza, plantas, el parque |

### Superficies que reaccionan y el velo

| Nombre | Hex | Para qué |
|---|---|---|
| `CreamPress` | `#EADFD1` | crema con el ratón encima o apretada |
| `Scrim` | `rgba(59, 46, 41, .55)` | el velo que se echa sobre la isla |

**Hay un solo velo.** Lo usan el menú y la pausa, que son las dos únicas cosas que se
ponen delante de la isla entera. Es tinta y no negro —apaga el 3D sin ensuciarlo de
gris— y translúcido de verdad: la isla se tiene que seguir viendo detrás, que es lo
que hace que sea un alto y no otra pantalla. La pausa llevaba uno azul al 82 % que de
translúcido no tenía nada; dos velos distintos en el mismo juego eran uno de más.

Regla dura: **ningún color con saturación por encima del 45 %**. Si un color pide
atención a gritos, no es de este juego.

---

## 2. Formas

- **Radio de esquina**: 18 px en paneles, 14 px en filas y botones, 999 px en
  etiquetas y contadores (una etiqueta es siempre una cápsula).
- **Nada de bordes.** La separación entre superficies se hace con color de fondo, no
  con líneas. Si de verdad hace falta un borde, es de 2 px en `InkFaint`.
- **Nada de sombras duras.** UI Toolkit no da sombra suave barata, así que la
  profundidad se hace con un `CreamDeep` detrás de un `Cream`, y ya está.
- Los iconos, si los hay, son **formas geométricas de colores de la paleta**. Nada de
  emoji: desentonan y se ven distintos en cada sistema.

---

## 3. Espacio y tipografía

| Cosa | Valor |
|---|---|
| Margen entre paneles | 12 px |
| Relleno dentro de un panel | 16 px |
| Separación entre filas | 10 px |
| Título de panel | 20 px, negrita, `Ink` |
| Subtítulo | 15 px, negrita, `Ink` |
| Texto normal | 14 px, `Ink` |
| Texto secundario | 13 px, `InkSoft` |
| Texto de botón | 15 px, negrita |

El texto **respira**: `whiteSpace = Normal` en todo lo que sea una frase, para que
parta de línea en vez de recortarse.

---

## 4. Botones

Tres tipos y ni uno más:

1. **Principal** — fondo `Peach`, texto `Ink`, radio 14. Uno por pantalla. Es lo que
   el jugador ha venido a hacer.
2. **Secundario** — fondo `CreamDeep`, texto `Ink`, radio 14. Todos los demás.
3. **Apagado** — fondo `CreamDeep`, texto `InkFaint`, y `SetEnabled(false)`.

Un botón desactivado **se queda en su sitio**, no desaparece. Ver lo que aún no
puedes hacer es información, y esconderlo la borra.

El texto de un botón dice **qué va a pasar**: «Comprar», «Ayudarle», «Ahora no». Ni
«Aceptar» ni «OK», que no dicen nada.

---

## 5. El tono de los textos

Castellano natural y cálido, tuteando al jugador. Frases cortas.

- Bien: «Ahora mismo, nada. Está a gusto.»
- Mal: «No hay peticiones activas para este isleño.»

Cuando algo no se puede hacer, se dice **por qué**, no que ha fallado.

- Bien: «Te faltan 40 nimbos.»
- Mal: «Compra fallida.»

Nada de signos de exclamación en cadena, ni de mayúsculas para gritar. Este juego no
levanta la voz nunca.

---

## 6. Lo que no se hace, jamás

- Colores fuera de la paleta.
- Esquinas de radio 0.
- Negro puro (`#000`) o blanco puro (`#FFF`) en texto o fondo.
- Emoji en la interfaz.
- Barras de progreso rojas salvo para una necesidad en estado crítico.
- Cualquier animación que dure más de 250 ms.
- **Más de un sitio para lo mismo.** Si una pantalla ya vive en el menú, no se abre
  además una copia suelta desde otro botón.

> **Regla derogada.** Hasta ahora aquí ponía «nada de ventanas modales que tapen la
> isla: los paneles ocupan un lado y dejan ver a los habitantes». Se cumplió, y el
> resultado fue once botones encendidos a la vez por los bordes de la pantalla, cada
> uno abriendo su tarjeta en un sitio distinto. Sale más caro que lo que evitaba: la
> isla se veía por los huecos que dejaban los paneles. Desde el menú único (§8) hay
> **una** ventana centrada, con velo translúcido —la isla se sigue viendo detrás— y
> nada más encendido mientras está abierta.

---

## 7. Movimiento y estados

Nada aparece de golpe y nada se mueve más de lo que dura un parpadeo.

| Cosa | Duración | Curva |
|---|---|---|
| Color de un botón al pasar por encima | 120 ms | `EaseOutCubic` |
| Abrir o cerrar el menú | 180 ms | `EaseOutCubic` |
| Cambiar de sección dentro del menú | 160 ms | `EaseOutCubic` |

Todo lo pulsable tiene **tres estados** y no se negocia: reposo, ratón encima
(`CreamPress` sobre superficies crema, `PeachDeep` sobre melocotón) y apretado, que
se hunde al 96 % de su tamaño. Sin lo tercero, un clic no se acusa de ninguna manera
y parece que se ha perdido.

Se consigue con `UiTheme.Animate`, `UiTheme.Hoverable` y `UiTheme.Pressable`, y ya
va puesto en `Action`, `Secondary`, `Close` y las pestañas: **no se escribe a mano**.

La barra de desplazamiento de fábrica es gris de editor y desentona con todo. Toda
`ScrollView` pasa por `UiTheme.StyleScroll`.

---

## 8. El menú

Hay **un** menú y se abre con `Tab`, con `M` por el mapa o con el único botón que
queda en pantalla. Dentro van, en esta columna: Mochila, Hacer, Vecinos, Mapa y
Logros. Se cierra con `Esc`, con la cruz o pinchando fuera.

Reglas:

- **En pantalla, jugando, solo hay tres cosas**: el reloj arriba a la izquierda, las
  monedas y el botón del menú arriba a la derecha, y la barra abajo. Nada más. Un
  contador a cero no se enseña.
- **Lo que se abre solo no lleva botón.** La tienda se abre al entrar en la tienda,
  el cajón al usar el cajón, la mesa al ponerse en la mesa. Un botón que abre una
  tienda desde el otro lado de la isla sobra.
- **Los modos no son pantallas.** Construir, amueblar y decorar se comen la pantalla
  entera y apagan el resto de la interfaz, así que no son pestañas: van al pie de la
  columna, bajo «Aquí puedes», y solo aparecen los que se puedan usar donde estás —en
  la calle, construir y decorar; dentro de casa, amueblar y nada más—.
  Decorar no mueve la cámara y los otros dos sí, pero eso no lo hace una pantalla:
  lo que decide es que se entra y se sale de él, y que mientras dura no hay otra cosa
  que hacer. Se sale con `Esc`.
- Con el menú abierto, el protagonista no anda ni interactúa. El reloj sí corre: esto
  no es una pausa.
- **`Esc` cierra una capa cada vez**, de fuera adentro: primero el modo, luego el
  menú, y solo cuando no queda nada abierto pausa la partida. La pausa y los ajustes
  están una capa por encima (`MainMenuView`, su propio `UIDocument`) y ahí sí para el
  reloj. Esa tecla tiene **un solo dueño por capa**: si dos la escuchan a la vez,
  una cierra el menú mientras la otra pausa, y se ven las dos cosas de golpe.
- La pausa también se abre desde el pie de la columna. No es un sitio nuevo para lo
  mismo: es la misma pantalla con otra puerta, porque un atajo que no está escrito en
  ninguna parte no lo encuentra media gente.
