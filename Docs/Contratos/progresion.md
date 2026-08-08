# Contrato: Sistema de progresión

> **Módulo dueño:** `Nimbo.Simulation`
> **Datos en:** `Nimbo.Data.ProgressionState`
> **Configuración:** `ProgressionConfig.asset` ⚙️

---

## 1. Concepto

La progresión tiene dos ejes independientes:

1. **Nivel de habitante:** cada habitante sube de nivel cumpliendo peticiones.
   Desbloquea ropa, peinados, expresiones y slots de inventario.
2. **Fases de la isla:** el conjunto de la isla desbloquea edificios, tiendas
   y tipos de petición según el nivel medio de los habitantes.

---

## 2. Estructura de datos

```csharp
[Serializable]
public struct ProgressionState {
    public int level;            // 1..50
    public int xp;               // experiencia acumulada
    public int totalPetsFulfilled;
    public int totalFavorsDone;
    public float playTimeGameHours;
}

[Serializable]
public struct IslandProgression {
    public IslandPhase currentPhase;
    public int totalBuildingsUnlocked;
    public List<string> unlockedBuildingIds;
    public int specialEventsCompleted;
}
```

---

## 3. Niveles de habitante

### 3.1 Curva de experiencia

```csharp
int XpForLevel(int level) {
    // level 1 → 100 XP
    // level 10 → ~3,162 XP
    // level 25 → ~12,500 XP
    // level 50 → ~35,355 XP
    return Mathf.RoundToInt(100f * level * Mathf.Sqrt(level));
}

int XpToNextLevel(int currentLevel, int currentXp) {
    return XpForLevel(currentLevel + 1) - currentXp;
}
```

| Nivel | XP para subir | XP acumulada |
|---|---|---|
| 1 → 2 | `100` | `100` |
| 2 → 3 | `283` | `383` |
| 5 → 6 | `1,118` | `2,462` |
| 10 → 11 | `3,162` | `12,745` |
| 25 → 26 | `12,500` | `106,462` |
| 50 (max) | — | `~353,553` |

### 3.2 Fuentes de XP

| Actividad | XP base ⚙️ |
|---|---|
| Petición cumplida | `20 – 50` (según tipo, ver `peticiones.md`) |
| Petición cumplida con bonus personalidad | `×1.25` |
| Petición crítica cumplida | `×1.5` |
| Minijuego completado | `10 – 30` |
| Nueva relación forjada (Friend) | `50` |
| Nueva relación romántica (Dating) | `100` |
| Mueble colocado por primera vez | `5` |
| Ropa nueva equipada | `5` |

### 3.3 Recompensas por nivel

| Nivel | Desbloqueo |
|---|---|
| 1 | Expresión: saludo. 3 slots de ropa. |
| 3 | Expresión: alegría. 1 slot de inventario extra. |
| 5 | Expresión: enfado. 1 peinado nuevo. |
| 7 | Expresión: tristeza. 1 slot de inventario extra. |
| 10 | Expresión: sorpresa. Puede visitar a otros habitantes. |
| 12 | 1 peinado nuevo. 1 color de pelo nuevo. |
| 15 | Puede cocinar (usa ingredientes). Receta básica. |
| 18 | Expresión: vergüenza. 1 slot de inventario extra. |
| 20 | Puede tener pareja (desbloquea rama romántica). |
| 22 | Receta avanzada. |
| 25 | Puede proponer matrimonio. |
| 28 | 1 peinado nuevo. |
| 30 | 1 slot de inventario extra. Baile especial. |
| 35 | Receta experta. |
| 40 | Traje especial de gala. |
| 45 | Corona/isla personalizada (minifloat). |
| 50 | Traje de leyenda. Estrella en el paseo de la fama. |

---

## 4. Fases de la isla

La fase de la isla se calcula con la **media de nivel de todos los
habitantes** (redondeada hacia abajo):

```csharp
IslandPhase CalculatePhase() {
    float avgLevel = GetAllIslanders().Average(i => i.level);

    if (avgLevel >= 30) return IslandPhase.Phase4;
    if (avgLevel >= 20) return IslandPhase.Phase3;
    if (avgLevel >= 10) return IslandPhase.Phase2;
    return IslandPhase.Phase1;
}
```

### 4.1 Edificios y desbloqueos por fase

| Fase | Nivel medio | Edificios que abren | Condición extra |
|---|---|---|---|
| **Fase 1** | `1 – 9` | Apartamento del jugador, NimboMart, Muebles Nimbo, Parque central | Inicio |
| **Fase 2** | `10 – 19` | Boutique Celeste, Restaurante, Piscina, 2 ranuras de habitantes nuevas | `totalPetsFulfilled >= 20` |
| **Fase 3** | `20 – 29` | Antigüedades Nimbo, Cine, Gimnasio, ampliación de apartamento disponible, +2 ranuras de habitantes | `totalPetsFulfilled >= 60` |
| **Fase 4** | `30 – 50` | Mercado flotante, Salón de eventos, Segunda planta, +4 ranuras de habitantes, Isla personalizada | `totalPetsFulfilled >= 120` |

### 4.2 Secuencia de apertura de edificios

Dentro de cada fase, los edificios no abren todos a la vez: abren uno por uno,
con 2-3 días de juego de separación, para dar sensación de progreso continuo.

```csharp
float DaysBetweenBuildingUnlocks = 3f; // ⚙️ días de juego entre desbloqueos
```

---

## 5. Habitantes y ranuras

La isla empieza con **3 habitantes** (más el jugador). Las ranuras se
desbloquean así:

| Hito | Ranuras totales ⚙️ |
|---|---|
| Inicio | `4` (3 habitantes + jugador) |
| Fase 2 | `6` |
| Fase 3 | `8` |
| Fase 4 | `12` |
| Eventos especiales | Hasta `MAX_ISLANDERS = 50` |

Los habitantes nuevos llegan a la isla en globo (evento de llegada). El
jugador puede rechazar a un habitante nuevo, pero la ranura queda ocupada
hasta que llegue otro.

---

## 6. Logros y coleccionables

Los logros son hitos puntuales que dan nimbos y a veces desbloquean contenido.
No definen la progresión principal, pero la complementan.

| Logro | Condición | Recompensa ⚙️ |
|---|---|---|
| Primer amigo | Un habitante llega a `Friend` | `50` nimbos |
| Alma de la fiesta | 5 habitantes en un mismo evento | `100` nimbos |
| Celestino | Primera pareja formada | `150` nimbos |
| Constructor | 50 muebles colocados | `100` nimbos |
| Decorador experto | 200 muebles colocados | `200` nimbos |
| Millonario | 5000 nimbos en cartera | Traje de millonario |
| Filántropo | 50 peticiones cumplidas | `200` nimbos |
| Héroe de la isla | 200 peticiones cumplidas | Corona de héroe |

---

## 7. Anti-grind

- Cumplir peticiones da XP incluso si el habitante está en nivel máximo
  (la XP se sigue acumulando para la media de la isla).
- Los minijuegos dan menos XP que las peticiones: la mejor forma de
  progresar es atender a los habitantes, no farmear minijuegos.
- No hay penalización por ignorar peticiones (más allá de la pérdida de
  afinidad y ánimo). El jugador puede tomarse el juego a su ritmo.
