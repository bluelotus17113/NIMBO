# Encargo: los 15 ficheros de personalidad que faltan

Traduces a C# la tabla de personalidades que ya está cerrada y validada. **No
diseñas nada nuevo**: los números están decididos y tu trabajo es que el código diga
exactamente lo mismo que el JSON. Lo único que sí inventas son las reacciones
(sección «Lo único que decides tú»).

## Lee esto antes de escribir una línea

1. `Assets/_Project/Scripts/Personality/Types/Ermitano.cs` — **la plantilla**. Es el
   tipo 0, ya escrito y compilando. Los quince tuyos son ese fichero con otros datos.
2. `Docs/Contratos/personalidades.json` — **la fuente de la verdad** de los números.
3. `Assets/_Project/Scripts/Personality/Runtime/PersonalityDefinition.cs` — los campos.

## Ficheros que escribes — y ningún otro

Uno por tipo, todos en `Assets/_Project/Scripts/Personality/Types/`:

| index | id | Clase y fichero |
|---|---|---|
| 1 | `PT_ATLETA` | `Atleta.cs` |
| 2 | `PT_ARTESANO` | `Artesano.cs` |
| 3 | `PT_AUDAZ` | `Audaz.cs` |
| 4 | `PT_AFABLE` | `Afable.cs` |
| 5 | `PT_LIDER` | `Lider.cs` |
| 6 | `PT_ANFITRION` | `Anfitrion.cs` |
| 7 | `PT_FIESTERO` | `Fiestero.cs` |
| 8 | `PT_POETA` | `Poeta.cs` |
| 9 | `PT_VISIONARIO` | `Visionario.cs` |
| 10 | `PT_ARTISTA` | `Artista.cs` |
| 11 | `PT_GENIO` | `Genio.cs` |
| 12 | `PT_ROMANTICO` | `Romantico.cs` |
| 13 | `PT_EXPLORADOR` | `Explorador.cs` |
| 14 | `PT_CUENTISTA` | `Cuentista.cs` |
| 15 | `PT_ENTUSIASTA` | `Entusiasta.cs` |

Los nombres de clase van sin tildes ni eñes (`Lider`, `Romantico`, `Anfitrion`) porque
son identificadores de C#. El `DisplayName` **sí** lleva tilde: ese lo lee el jugador.

**No toques nada más.** Ni `Ermitano.cs`, ni `Runtime/`, ni `Core/`, ni `Data/`, ni
ningún fichero de `Docs/`. Si algo parece que está mal, lo dices en el informe y sigues.

## Las cuatro tablas de traducción

**Necesidades** — `needMultipliers` del JSON a `NeedDecay`:

| JSON | C# |
|---|---|
| `hunger` | `[NeedKind.Hunger]` |
| `energy` | `[NeedKind.Energy]` |
| `social` | `[NeedKind.Social]` |
| `hygiene` | `[NeedKind.Hygiene]` |
| `mood` | **NO es una necesidad** → va al campo `MoodDecay` |

Esa última fila es la trampa principal de este encargo. `mood` no es un `NeedKind`;
no existe `NeedKind.Mood` y si lo escribes no compila. Va suelto, en `MoodDecay`.

**Peticiones** — `requestWeights` a `RequestWeights`. Son once y van las once:

| JSON | C# |
|---|---|
| `food` | `RequestKind.Food` |
| `item` | `RequestKind.Object` |
| `clothing` | `RequestKind.Clothes` |
| `advice` | `RequestKind.Advice` |
| `favor` | `RequestKind.Favor` |
| `complaint` | `RequestKind.Complaint` |
| `socialIntro` | `RequestKind.SocialIntro` |
| `activity` | `RequestKind.Activity` |
| `islandBuilding` | `RequestKind.IslandBuilding` |
| `confession` | `RequestKind.Confession` |
| `reconcile` | `RequestKind.Reconcile` |

Fíjate en que `item` → `Object` y `clothing` → `Clothes`. No se llaman igual.

**Voz** — de `voice` a `VoiceConfig`:

| `pitch` | `VoicePitch` | | `pace` | `Speed` |
|---|---|---|---|---|
| `muy grave` | `VeryLow` | | `muy lento` | `0.75f` |
| `grave` | `Low` | | `lento` | `0.85f` |
| `medio` | `Mid` | | `medio` | `1.0f` |
| `agudo` | `High` | | `rapido` | `1.15f` |
| `muy agudo` | `VeryHigh` | | `muy rapido` | `1.3f` |

`Warble` (0 plano, 1 cantarín) y `Nasal` los eliges tú entre 0 y 1, acordes al tipo.

**Afinidades** — cada entrada de `compatible` y de `clashes` es un `new AffinityBias(id, valor)`.
El `bonus` va en positivo y el `penalty` **con su signo negativo**, tal cual está en el
JSON. La tabla ya está simetrizada: no la toques ni la "arregles".

## Lo único que decides tú

**`Reactions`** — las doce situaciones de `PersonalityReaction`, todas, para cada tipo.
Es donde el tipo se nota. Elige entre los doce valores de `Emotion` (`Neutral`, `Happy`,
`Ecstatic`, `Sad`, `Angry`, `Sleepy`, `Hungry`, `Bored`, `Surprised`, `Love`, `Proud`,
`Worried`).

Que no sean todas iguales entre tipos: al Ermitaño que lo ignoren le pone contento
(`Ignored → Happy`) y al Anfitrión debería hundirlo. Si dos tipos opuestos reaccionan
igual a todo, el trabajo está mal hecho. Pon un comentario corto al final de la línea
solo cuando la elección sorprenda, como en la plantilla.

**`SignatureEmotions`** — tres valores del enum `Emotion`, distintos entre sí, que
traduzcan las tres palabras del campo `emotions` del JSON (que están en castellano
libre: «euforia», «asombro», «desconfianza»…). Elige la más cercana de las doce.

## Lo que NO decides

Los números. `walkSpeed`, `idleDwell`, los multiplicadores, los pesos y las afinidades
se copian del JSON tal cual. Las cuatro parejas de frases, igual: copiadas literales,
con sus tildes y su puntuación. Si una frase del JSON te parece mejorable, la copias
igual y lo dices en el informe.

## Verificación obligatoria

Escribe y ejecuta un script de Python que, leyendo el JSON y los quince `.cs`:

- confirme que existen los quince ficheros con la clase correcta
- extraiga de cada `.cs` el `TypeIndex`, `Id`, `WalkSpeed`, `IdleDwell` y `MoodDecay`
  y los compare con el JSON
- cuente que cada fichero tiene 4 entradas de `NeedDecay`, 11 de `RequestWeights`,
  12 de `Reactions`, 4 tonos de `Lines` con 2 frases cada uno, y tantos `AffinityBias`
  como `compatible` + `clashes` tenga en el JSON
- avise si dos tipos tienen exactamente el mismo mapa de `Reactions`

**Enseña la salida del script.** Sin ella doy el encargo por no hecho.

No intentes compilar con Unity: no lo tienes disponible y tardarías más en pelearte
con eso que en escribir los quince ficheros. De compilar me encargo yo.

## Formato de respuesta al terminar

```
FICHEROS: <los 15, con líneas de cada uno>
VERIFICACIÓN: <salida literal del script>
REACCIONES: <2 ejemplos de decisiones de reacción que te parezcan las más
             características, y por qué>
PEGAS: <lo que viste mal en el JSON y copiaste igual, o "ninguna">
```
