# Encargo: las 16 personalidades

Eres el diseñador de personajes de **Isla Nimbo**, un simulador social 3D chibi tipo
*Tomodachi Life* en islas flotantes. Tu encargo es el sistema de personalidad: el que
hace que dos habitantes con la misma cara se comporten distinto y el jugador los
quiera por motivos diferentes.

Otro agente programará en C# lo que tú especifiques. **Escribe pensando en eso**: si
una frase tuya no se puede convertir en código, no sirve.

## Lo primero: lee el contrato

Lee `Docs/00_ARQUITECTURA.md`, sección 6. Ahí están los 4 ejes y la regla de que el
tipo es derivado, no guardado. No lo cambies.

Los ejes, con valores en `[-1, 1]`:

| Eje | −1 | +1 |
|---|---|---|
| `Energy` | Calmado | Enérgico |
| `Expression` | Reservado | Expresivo |
| `Attitude` | Independiente | Sociable |
| `Outlook` | Práctico | Soñador |

El signo de los cuatro ejes da 2⁴ = **16 tipos**. El índice se calcula así, y no se
discute:

```
index = (Energy>0 ? 1:0) | (Expression>0 ? 2:0) | (Attitude>0 ? 4:0) | (Outlook>0 ? 8:0)
```

Es decir: el tipo 0 es `(Calmado, Reservado, Independiente, Práctico)` y el tipo 15
es `(Enérgico, Expresivo, Sociable, Soñador)`. Los 16 salen de ahí y **tienes que
respetar ese orden**: el tipo del índice 5 es `(Enérgico, Reservado, Sociable,
Práctico)`, te guste el nombre que le pongas o no.

## Ficheros que escribes — y ningún otro

- `Docs/03_PERSONALIDADES.md` — el documento de diseño, en español
- `Docs/Contratos/personalidades.json` — la misma información, legible por máquina

No toques `Docs/00_ARQUITECTURA.md`, ni `Docs/01_*`, ni `Docs/02_*`, ni
`Docs/Contratos/*.md`, ni nada bajo `Assets/`. Hay otros agentes trabajando ahí ahora
mismo y los dos ficheros de arriba son lo único tuyo.

## Antes de diseñar: estudia

Busca cómo resuelven esto los juegos del género y los modelos de personalidad reales
(los cinco grandes, el temperamento de Keirsey, los ejes de introversión/extraversión).
Quédate con lo que se pueda observar en pantalla en 10 segundos de mirar a un muñeco.
Un rasgo que no se ve, no existe. Deja un apartado corto en el .md con lo que aprendiste
y qué descartaste.

## Para cada uno de los 16 tipos

Nombre propio nuestro en español, evocador y corto (2-3 sílabas mejor). **Ni una sola
palabra de Nintendo**: nada de Mii, Tomodachi, ni los nombres de personalidad de ese
juego. Si te sale uno igual, cámbialo.

Y luego, para cada tipo:

1. **Frase de identidad** — una línea que lo resuma.
2. **Cómo se mueve** — velocidad de andar (multiplicador `0.7`–`1.3`), cuánto se para,
   si deambula o va directo, postura.
3. **Cómo reacciona** — a un regalo que le gusta, a uno que no, a que le presenten a
   alguien, a una riña, a que lo ignoren. Una reacción nombrada por cada caso.
4. **Qué pide** — sus 3 tipos de petición más frecuentes y los 2 que casi nunca hace,
   con un peso `0.0`–`2.0` que multiplica la probabilidad base.
5. **Modificadores de necesidad** — multiplicador de decaimiento de Hambre, Ánimo,
   Energía, Vínculo social e Higiene. Rango `0.6`–`1.4`, y la media de los 16 tipos
   en cada necesidad tiene que quedar cerca de `1.0`.
6. **Compatibilidad** — con qué 3 tipos congenia (bonus de afinidad `+2` a `+8`) y con
   qué 2 choca (`−2` a `−8`). **Tiene que ser simétrico**: si A congenia con B, B
   congenia con A con el mismo número. Compruébalo antes de entregar.
7. **Voz y expresiones** — tono (agudo/medio/grave), ritmo al hablar, sus 3 emociones
   más habituales.
8. **Ocho frases** — 2 contento, 2 aburrido, 2 enfadado, 2 al conocer a alguien.
   En español natural, cortas, con la personalidad puesta. Nada de rellenar.

## El JSON

`Docs/Contratos/personalidades.json`, exactamente con esta forma:

```json
{
  "version": 1,
  "axes": ["Energy", "Expression", "Attitude", "Outlook"],
  "types": [
    {
      "index": 0,
      "id": "PT_ERMITANIO",
      "name": "Ermitaño",
      "axes": { "Energy": -1, "Expression": -1, "Attitude": -1, "Outlook": -1 },
      "tagline": "...",
      "walkSpeed": 0.8,
      "idleDwell": 3.5,
      "needMultipliers": { "hunger": 1.0, "mood": 1.1, "energy": 0.9, "social": 0.7, "hygiene": 1.0 },
      "requestWeights": { "food": 1.0, "item": 1.3, "clothing": 0.6, "advice": 0.4, "favor": 0.8, "complaint": 1.2 },
      "compatible": [{ "id": "PT_XXX", "bonus": 5 }],
      "clashes": [{ "id": "PT_YYY", "penalty": -4 }],
      "voice": { "pitch": "grave", "pace": "lento" },
      "emotions": ["...", "...", "..."],
      "lines": { "happy": ["", ""], "bored": ["", ""], "angry": ["", ""], "meeting": ["", ""] }
    }
  ]
}
```

`id` en MAYÚSCULAS con prefijo `PT_`, sin tildes ni eñes (`PT_ERMITANIO`, no
`PT_ERMITAÑO`): va a ser un identificador de C#.

## La trampa que te va a morder

El JSON y el Markdown **tienen que decir lo mismo**. Escribe primero el JSON, y genera
el Markdown a partir de él. Si los dos no cuadran, el trabajo está mal aunque los dos
por separado estén bien.

Antes de entregar, comprueba con un script de Python que:
- hay 16 tipos, índices 0 a 15 sin repetir
- el signo de los ejes de cada tipo coincide con su índice según la fórmula de arriba
- todas las `id` de `compatible` y `clashes` existen, y son simétricas con el mismo número
- ningún tipo es compatible o choca consigo mismo
- la media de cada multiplicador de necesidad está entre 0.95 y 1.05

Enseña la salida de ese script. Si no la enseñas, doy el encargo por no hecho.

## Formato de respuesta al terminar

```
FICHEROS: Docs/03_PERSONALIDADES.md (<n> líneas), Docs/Contratos/personalidades.json
LOS 16: <índice: nombre — los 4 signos> por línea
VALIDACIÓN: <salida literal del script>
DECISIONES: <lo que tuviste que inventar>
```
