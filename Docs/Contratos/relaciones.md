# Contrato: Sistema de relaciones

> **Módulo dueño:** `Nimbo.Social`
> **Datos en:** `Nimbo.Data.Relationship`, `Nimbo.Data.RelationshipBook`
> **Configuración:** `RelationshipsConfig.asset` ⚙️

---

## 1. Concepto

Cada par de habitantes tiene una relación bilateral asimétrica (A→B puede
ser distinto de B→A). Las relaciones se representan como un grafo dirigido
con pesos en `[-100, 100]`.

---

## 2. Estructura de datos

```csharp
[Serializable]
public struct Relationship {
    public int sourceId;       // el que siente
    public int targetId;       // hacia quién
    public float affinity;     // [-100, 100]
    public RelationState state;
    public bool isRomantic;    // la relación tiene componente romántico?
    public float firstMetGameHour;
    public float lastInteractionGameHour;
    public int interactionCount;
    public List<string> memoryTags;  // recuerdos compartidos (máx 10)
}
```

### 2.1 Estados de relación

```csharp
public enum RelationState {
    // Rama principal
    Unknown,        // no se conocen (o afinidad = 0 tras reset)
    Acquaintance,   // se han presentado
    Friend,         // afinidad ≥ 25
    BestFriend,     // afinidad ≥ 75

    // Rama negativa
    Dislike,        // afinidad ≤ -25
    Rival,          // afinidad ≤ -75

    // Rama romántica (paralela, se activa con flag isRomantic)
    Crush,          // uno siente atracción (no mutuo necesariamente)
    Dating,         // mutuo, saliendo juntos
    Partner,        // relación estable
    Married,        // casados
    ParentChild     // padre/madre e hijo (relación especial, no cambia con afinidad)
}
```

---

## 3. Máquina de estados

### 3.1 Transiciones de la rama social

| Origen | Condición | Destino | Nota |
|---|---|---|---|
| `Unknown` | `firstMetGameHour` se asigna | `Acquaintance` | Primera interacción |
| `Acquaintance` | `affinity >= 25` | `Friend` | Se caen bien |
| `Acquaintance` | `affinity <= -25` | `Dislike` | Se caen mal |
| `Friend` | `affinity >= 75` | `BestFriend` | Amistad profunda |
| `Friend` | `affinity <= -25` | `Dislike` | La amistad se rompe |
| `BestFriend` | `affinity <= -25` | `Dislike` | Caída fuerte desde amistad |
| `Dislike` | `affinity >= 25` | `Friend` | Reconciliación |
| `Dislike` | `affinity <= -75` | `Rival` | Enemistad declarada |
| `Rival` | `affinity >= -25` | `Dislike` | La rivalidad se enfría |

### 3.2 Transiciones de la rama romántica

La rama romántica corre en paralelo: un habitante puede ser `BestFriend` Y
`Crush` a la vez. El estado `isRomantic` es un flag booleano, y el estado
romántico se guarda aparte.

| Origen | Condición | Destino | Nota |
|---|---|---|---|
| — | `state == Friend && compatibility >= 0.4 && affinity >= 50 && isRomantic == false` | `isRomantic = true` + `Crush` | Se activa el interés romántico |
| `Crush` (A→B unilateral) | `B→A también es Crush` | `Dating` (mutuo) | Ambos sienten lo mismo |
| `Crush` (unilateral) | El otro rechaza (affinity < 25 o evento) | `isRomantic = false`, `affinity -= 10` | Corazón roto |
| `Dating` | `affinity >= 80` | `Partner` | Relación seria |
| `Dating` | `affinity <= 15` | Ruptura: `isRomantic = false`, `affinity -= 20` | Vuelven a `Friend` o `Dislike` según afinidad |
| `Partner` | Evento de propuesta + `affinity >= 85` + anillo | `Married` | El jugador presencia la propuesta |
| `Partner` | Ruptura: `affinity <= 10` | `isRomantic = false`, `affinity -= 30`, `state` recalculado | Divorcio emocional |
| `Married` | `affinity <= -50` | Divorcio: `isRomantic = false`, ambos pierden -20 Ánimo | Evento traumático |
| `Married` | Evento aleatorio (probabilidad 5% por semana de juego) + `affinity >= 85` + espacio | `ParentChild` con nuevo habitante | Nace/adoptan un hijo |

---

## 4. Cambios de afinidad

### 4.1 Eventos positivos

| Evento | Cambio base ⚙️ | Límite diario | Nota |
|---|---|---|---|
| Charla breve | `+3` | 3/día | Automática si comparten sala |
| Charla larga (>2 min juego) | `+8` | 2/día | Requiere que ambos estén libres |
| Regalo recibido (gusta) | `+15` | 1/día | Compatibilidad con personalidad |
| Regalo recibido (neutral) | `+5` | 1/día | |
| Actividad juntos (minijuego) | `+12` | 2/día | |
| Petición cumplida (involucra al otro) | `+10` | sin límite | Solo si la petición era sobre este habitante |
| Fiesta o evento de isla | `+5` a todos los presentes | 1/evento | |
| Defendido en queja | `+20` | — | Cuando el jugador falla a favor en una Complaint |
| Cumpleaños (felicitación) | `+8` | 1/día | Solo el día del cumpleaños |

### 4.2 Eventos negativos

| Evento | Cambio base ⚙️ | Nota |
|---|---|---|
| Discusión (evento espontáneo) | `-15` | Probabilidad baja, modulada por tipos incompatibles |
| Regalo que no gusta | `-5` | Si el objeto no cuadra con su estilo |
| Petición ignorada que los involucraba | `-8` | |
| Petición rechazada que los involucraba | `-12` | |
| Ser objeto de queja | `-20` | Si el jugador falla en contra en una Complaint |
| Rumores (evento de isla) | `-10` entre los implicados | |
| Divorcio | `-30` mutuo | Además: -30 Ánimo para ambos |

### 4.3 Decaimiento pasivo

Cada día de juego (24h), si dos habitantes no han interactuado:

```csharp
// Solo afecta si están por encima de Acquaintance
if (rel.state >= RelationState.Acquaintance && hoursSinceLastInteraction >= 24f) {
    // Acercamiento gradual a 0 (olvido)
    float decay = -1f; // ⚙️
    rel.affinity += decay; // tiende hacia 0 desde cualquier dirección
    // Si estaba en negativo, +1 (se acerca a 0). Si positivo, -1.
    // Corregido:
    // if (rel.affinity > 0) rel.affinity -= decay;
    // if (rel.affinity < 0) rel.affinity += decay;
}
```

---

## 5. Multiplicador de compatibilidad

La compatibilidad entre personalidades (ver `02_MECANICAS.md` sección 3.2)
se aplica como multiplicador sobre **todos** los cambios de afinidad:

```csharp
float multiplier = 0.5f + Compatibility(a, b) * 0.5f; // rango [0.0, 1.0] → [0.5, 1.5]
float actualChange = baseChange * multiplier;
```

Dos personalidades idénticas: `compat = 1.0` → `mult = 1.0` (cambio sin modificar).
Dos personalidades opuestas: `compat = -1.0` → `mult = 0.0` (cambio anulado).

---

## 6. Eventos de relación automáticos

El motor social evalúa eventos cada hora de juego para cada par de habitantes
que comparten ubicación:

### 6.1 Discusión espontánea

```csharp
float argumentChance = 0.02f; // ⚙️ base 2% por hora compartida

// Factores que aumentan la probabilidad:
if (rel.state == RelationState.Dislike) argumentChance *= 3f;
if (rel.state == RelationState.Rival)  argumentChance *= 5f;
if (Compatibility(a, b) < -0.5f)      argumentChance *= 2f;
if (aNeeds.mood < 30f)                argumentChance *= 1.5f; // uno está de mal humor

if (RngService.NextFloat() < argumentChance) {
    PublishEvent(new SocialArgumentEvent(a.id, b.id));
}
```

### 6.2 Acercamiento espontáneo

```csharp
float bondChance = 0.05f; // ⚙️ base 5%

if (rel.state == RelationState.Crush)     bondChance *= 2f;
if (rel.state == RelationState.Dating)    bondChance *= 2.5f;
if (Compatibility(a, b) > 0.5f)          bondChance *= 2f;

if (RngService.NextFloat() < bondChance) {
    rel.affinity += 3f * CompatibilityMultiplier(a, b);
}
```

---

## 7. Gestos especiales

### 7.1 Regalos

Una vez al día, el jugador puede hacer que un habitante le dé un regalo a otro.
El objeto se compra en la tienda (ver `economia.md`). La afinidad ganada
depende de cuánto le guste al receptor:

```csharp
float GiftAffinity(IslanderId giver, IslanderId receiver, Item item) {
    float baseGain = 10f; // ⚙️
    float matchBonus = ItemPersonalityMatch(item, receiver.personality);
    // matchBonus: 0.0 (no le gusta nada) a 1.0 (le encanta)
    float result = baseGain * (0.3f + matchBonus * 1.4f); // rango [3, 24]
    return result * CompatibilityMultiplier(giver, receiver);
}
```

### 7.2 Propuesta de matrimonio

Cuando dos habitantes están en estado `Partner` con afinidad ≥ 85, el jugador
puede comprar un anillo (200 nimbos ⚙️) y desencadenar la propuesta:

- Éxito base: `70%` ⚙️
- +1% por cada punto de afinidad sobre 85
- La compatibilidad añade ±15%
- Si falla: `-10` afinidad, se puede reintentar en 3 días de juego

### 7.3 Hijos

Cuando una pareja casada tiene un hijo:
- Se genera un nuevo habitante con personalidad mezclada de los padres:
  cada eje = `(padre.eje + madre.eje) / 2 + random(-0.2, 0.2)`
- El aspecto se genera proceduralmente mezclando rasgos de los padres
- El nuevo habitante tiene relación `ParentChild` con ambos padres
- Empieza con nivel 1 y apartamento básico

---

## 8. Consultas del sistema de relaciones

El `RelationshipBook` de cada habitante expone:

```csharp
// Los 5 mejores amigos
List<(int id, float affinity)> TopFriends(int count = 5);

// El peor enemigo
(int id, float affinity)? WorstEnemy();

// ¿Son pareja?
bool IsRomanticWith(int otherId);

// Todos los habitantes en un estado dado
List<int> GetAllInState(RelationState state);

// Afinidad media con los 5 mejores (para cálculo de ánimo)
float Top5AverageAffinity();
```
