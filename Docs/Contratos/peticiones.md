# Contrato: Sistema de peticiones

> **Módulo dueño:** `Nimbo.Simulation`
> **Datos en:** `Nimbo.Data.IslanderRequest`
> **Configuración:** `RequestsConfig.asset` ⚙️

---

## 1. Concepto

El sistema de peticiones es el bucle de engagement principal: un habitante pide
algo, el jugador decide si lo satisface o no, y recibe recompensa o penalización.

Las peticiones **no son misiones** con UI compleja. Son burbujas de pensamiento
que aparecen sobre el habitante, con una animación de espera y un temporizador.

---

## 2. Estructura de datos

```csharp
[Serializable]
public struct IslanderRequest {
    public string id;                 // GUID único
    public int islanderId;           // el habitante que pide
    public int? targetIslanderId;    // si la petición implica a otro habitante
    public RequestType type;         // tipo de petición
    public RequestPriority priority; // prioridad actual
    public float createdAtGameHour;  // hora de juego en que se generó
    public float expiresAtGameHour;  // hora de juego en que caduca
    public bool fulfilled;           // se resolvió?
    public RequestResolution resolution; // cómo terminó
}
```

### 2.1 Tipos de petición

```csharp
public enum RequestType {
    Food,              // "Quiero un [tipo de comida]"
    Object,            // "Quiero [objeto decorativo/ropa]"
    Clothes,           // "Quiero cambiarme a [prenda]"
    Advice,            // "Necesito un consejo sobre [tema]"
    Favor,             // "¿Puedes [acción]?"
    Complaint,         // "Me cae mal X, haz algo"
    SocialIntro,       // "Quiero conocer a X"
    Activity,          // "Vamos a [minijuego/actividad]"
    IslandBuilding,    // "La isla necesita [edificio/mejora]"
}
```

### 2.2 Prioridad

```csharp
public enum RequestPriority {
    Low,       // capricho, no hay prisa
    Normal,    // necesidad moderada
    High,      // necesidad urgente
    Critical   // necesidad en banda crítica
}
```

### 2.3 Resolución

```csharp
public enum RequestResolution {
    Pending,      // aún en cola
    Fulfilled,    // el jugador la resolvió
    Ignored,      // caducó sin resolver
    Refused,      // el jugador dijo que no explícitamente
    Failed        // imposible de resolver (habitante se fue, etc.)
}
```

---

## 3. Generación de peticiones

### 3.1 Frecuencia base

Cada habitante puede tener **máximo 3 peticiones activas** a la vez.
El sistema evalúa si genera una nueva cada `30` minutos de juego ⚙️.

```csharp
bool ShouldGenerateRequest(IslanderId id) {
    var reqs = GetActiveRequests(id);
    if (reqs.Count >= 3) return false;

    // Probabilidad base del 40% cada 30 min de juego
    float baseChance = 0.40f; // ⚙️

    // Modificar por personalidad
    var personality = GetPersonality(id);
    baseChance += personality.Expression * 0.10f;  // expresivos piden más
    baseChance += personality.Attitude * 0.05f;    // sociables piden más

    return RngService.NextFloat() < baseChance;
}
```

### 3.2 Selección de tipo por peso

Cuando toca generar una petición, se elige el tipo con ruleta de pesos:

```csharp
RequestType PickRequestType(IslanderId id) {
    var needs = GetNeeds(id);
    var personality = GetPersonality(id);
    var rels = GetRelationships(id);

    var weights = new Dictionary<RequestType, float> {
        { Food,         NeedWeight(needs.hunger,  30f) },
        { Object,       15f + personality.Outlook * 5f },
        { Clothes,      10f + personality.Expression * 5f },
        { Advice,       NeedWeight(needs.mood,    20f) + personality.Attitude * 3f },
        { Favor,        12f },
        { Complaint,    HasNegativeRels(id) ? 20f : 2f },
        { SocialIntro,  CountUnknownIslanders(id) > 0 ? 18f : 3f },
        { Activity,     10f + personality.Energy * 8f },
        { IslandBuilding, 5f },
    };

    return WeightedPick(weights);
}

float NeedWeight(float value, float baseWeight) {
    if (value <= 15f) return baseWeight * 4f;   // crítico → urge
    if (value <= 35f) return baseWeight * 2f;   // bajo → probable
    return baseWeight;
}
```

### 3.3 Selección de objetivo (cuando aplica)

Para peticiones que involucran a otro habitante (`SocialIntro`, `Complaint`,
`Activity`):

```csharp
int PickTarget(IslanderId source, RequestType type) {
    switch (type) {
        case SocialIntro:
            // Prefiere habitantes con alta compatibilidad todavía desconocidos
            return PickByWeight(GetUnknownIslanders(source)
                .Select(id => (id, weight: Compatibility(source, id) * 10f + 5f)));
        case Complaint:
            // El habitante con peor afinidad
            return GetRelationships(source).LowestAffinity().id;
        case Activity:
            // Prefiere mejores amigos
            return PickByWeight(GetFriends(source)
                .Select(id => (id, weight: GetAffinity(source, id) / 10f)));
        default:
            return PickRandom(GetAllIslanders());
    }
}
```

---

## 4. Ciclo de vida

```
[Nueva] ──→ [En cola] ──→ [Mostrada al jugador]
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
              [Cumplida]  [Rechazada]  [Caducada/Ignorada]
```

### 4.1 Cola de peticiones

Todas las peticiones vivas van a una cola global. El juego muestra **1
petición activa a la vez** (la más urgente). El resto esperan en cola.

El orden de la cola es por `priority` (descendente) y luego por `createdAt`
(más antigua primero).

### 4.2 Caducidad

| Prioridad | Tiempo hasta caducar (horas juego) ⚙️ |
|---|---|
| Low | `8h` |
| Normal | `6h` |
| High | `4h` |
| Critical | `2h` |

Si una petición caduca sin resolverse:
- El habitante pierde `-10` de Ánimo
- Si era de prioridad Critical por necesidad, la necesidad se resuelve sola pero con penalización: el habitante come cualquier cosa / duerme en el suelo, ganando solo el 50% de lo que habría ganado con la petición.

### 4.3 Rechazo explícito

El jugador puede rechazar una petición. Consecuencias:
- `-5` de afinidad con el habitante que pidió
- `-8` de Ánimo para el habitante
- La petición no se vuelve a generar en `2h` de juego

---

## 5. Recompensas

### 5.1 Al cumplir una petición

| Tipo de petición | Nimbos base ⚙️ | XP base ⚙️ | Bonus |
|---|---|---|---|
| Food | `10` | `20` | +5 nimbos si era su comida favorita |
| Object | `15` | `30` | +10 nimbos si el objeto combina con su estilo |
| Clothes | `20` | `25` | El habitante se pone la prenda |
| Advice | `5` | `40` | +15 de afinidad con el habitante |
| Favor | `25` | `35` | Varía según la dificultad |
| Complaint | `15` | `20` | Resuelve o empeora la relación con el tercero |
| SocialIntro | `10` | `30` | Crea relación entre los dos habitantes |
| Activity | `30` | `50` | +20 de afinidad mutua entre participantes |
| IslandBuilding | `50` | `100` | Desbloquea edificio o mejora |

### 5.2 Bonificadores

```csharp
int CalculateReward(IslanderRequest req) {
    int baseNimbos = GetBaseNimbos(req.type);
    int baseXp = GetBaseXp(req.type);

    // Bonus por personalidad: si la petición alinea con su tipo, +25%
    if (RequestAlignsWithPersonality(req)) {
        baseNimbos = Mathf.RoundToInt(baseNimbos * 1.25f);
        baseXp = Mathf.RoundToInt(baseXp * 1.25f);
    }

    // Bonus por urgencia: peticiones críticas pagan +50%
    if (req.priority == RequestPriority.Critical) {
        baseNimbos = Mathf.RoundToInt(baseNimbos * 1.5f);
    }

    return (baseNimbos, baseXp);
}
```

---

## 6. Peticiones especiales

### 6.1 Peticiones de isla

Las peticiones `IslandBuilding` las genera el sistema, no un habitante
concreto. Aparecen cuando se cumplen las condiciones de desbloqueo (ver
`progresion.md`). El "habitante" que las pide es el alcalde o el habitante
con más afinidad con el jugador.

### 6.2 Petición de mudanza

Cuando un habitante llega a la isla por primera vez, su primera petición
siempre es `Favor`: "Ayúdame a instalar mi casa". Es una petición guiada que
enseña la mecánica de vivienda. Recompensa: `+50` nimbos, `+100` XP.

### 6.3 Petición de cumpleaños

Cada habitante tiene un cumpleaños (día del año de juego, 1-365). El día
anterior, su mejor amigo genera una petición `Object`: "Quiero un regalo
para X". Recompensa: `×2` en todas las ganancias de afinidad ese día.

---

## 7. Frecuencia esperada

Con 10 habitantes activos y las probabilidades arriba, el jugador recibe
aproximadamente **4-6 peticiones por hora real de juego**, o una cada
10-15 minutos. Esto mantiene un goteo constante sin abrumar.

---

## 8. Anti-frustración

- Una petición de `Food` urgente siempre puede resolverse con un snack
  genérico (existe en la tienda siempre), para que el jugador nunca se quede
  sin opciones.
- Si un habitante tiene 3 peticiones activas y una necesidad entra en
  Critical, la petición más antigua de prioridad menor se descarta
  automáticamente para hacer hueco.
- Las peticiones de `Advice` pueden responderse sin coste: el jugador solo
  elige una opción de diálogo.
