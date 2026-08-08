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
- Ventanas modales que tapen la isla entera: los paneles ocupan un lado y dejan ver
  a los habitantes.
- Cualquier animación que dure más de 250 ms.
