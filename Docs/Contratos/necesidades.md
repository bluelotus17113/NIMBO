# Contrato: Sistema de necesidades

> **Módulo dueño:** `Nimbo.Simulation`
> **Datos en:** `Nimbo.Data.NeedState`
> **Configuración:** `NeedsConfig.asset` ⚙️

---

## 1. Las cinco necesidades

Cada habitante tiene 5 barras en `[0, 100]`:

| Necesidad | Campo C# | Ícono | Descripción |
|---|---|---|---|
| Hambre | `hunger` | 🍎 | Alimentación. Baja constantemente. |
| Ánimo | `mood` | 😊 | Estado emocional. Derivado, no decae solo. |
| Energía | `energy` | ⚡ | Vigor físico. Baja al hacer actividades. |
| Vínculo social | `social` | 💬 | Conexión con otros. Baja con el aislamiento. |
| Higiene | `hygiene` | 🛁 | Limpieza personal. Baja con el tiempo. |

---

## 2. Estructura de datos

```csharp
[Serializable]
public struct NeedState {
    public float hunger;      // 0..100
    public float mood;        // 0..100 (derivado cada tick)
    public float energy;      // 0..100
    public float social;      // 0..100
    public float hygiene;     // 0..100
}
```

`saturation` y `comfort` se mencionan en el GDD pero se implementan como
consecuencia de las 5 necesidades principales, no como barras independientes.
Si el GDD quiere añadir barras nuevas, este contrato se actualiza.

---

## 3. Decaimiento por hora de juego

Estos son los valores base. Cada tick de simulación (cada minuto de juego)
se aplica `valor * gameDeltaHours`.

| Necesidad | Decaimiento/hora | Tiempo 100→0 (horas juego) | Nota |
|---|---|---|---|
| Hambre | `-4.0f` ⚙️ | ~21h | Un habitante pide comida ~3 veces/día |
| Energía | `-5.0f` ⚙️ | ~17h | Duermen al llegar al umbral de sueño |
| Social | `-1.5f` ⚙️ | ~55h | La soledad tarda en notarse |
| Higiene | `-2.5f` ⚙️ | ~33h | Una ducha al día basta |

El Ánimo **no decae por sí mismo**: es un valor derivado (ver sección 5).

### 3.1 Moduladores de decaimiento

La personalidad afecta la velocidad de decaimiento:

```csharp
float decayMultiplier = 1f;

// Energy: los enérgicos queman más rápido
decayMultiplier += personality.Energy * 0.15f; // [+15% a -15%]

// La actividad actual también modula
if (currentActivity == ActivityType.Sleeping) {
    energyDecay = -2.0f;  // reposo: solo -2/hora en vez de -5
    hungerDecay *= 0.5f;  // medio metabolismo
}
if (currentActivity == ActivityType.Exercising) {
    energyDecay *= 1.8f;  // quema rápida
    hungerDecay *= 1.3f;
}
```

---

## 4. Umbrales y efectos

Cada necesidad tiene 4 bandas. El comportamiento del habitante cambia en cada una.

| Banda | Rango | Color | Efecto en comportamiento |
|---|---|---|---|
| Crítico | `[0, 15]` | Rojo | El habitante no hace nada más hasta resolverlo |
| Bajo | `(15, 35]` | Naranja | Probabilidad alta de generar petición |
| Normal | `(35, 80]` | Verde | Funcionamiento normal |
| Pleno | `(80, 100]` | Azul | Bonus: +10% afinidad ganada en interacciones |

### 4.1 Efectos específicos por necesidad en crítico

| Necesidad | Efecto en crítico |
|---|---|
| Hambre | El habitante se desmaya si llega a 0. Pierde -3 de Energía extra/hora. |
| Energía | Se duerme en el sitio. Ignora cualquier otra orden o petición. |
| Social | -5 de Ánimo/hora. Genera peticiones de "quiero hablar con X". |
| Higiene | Los demás habitantes evitan interactuar (-20% afinidad en interacciones). |

---

## 5. Cálculo del Ánimo

El ánimo no se guarda: se deriva cada tick en `SimulationEngine.UpdateMood()`.

```csharp
float DeriveMood(NeedState needs, RelationshipBook rels) {
    // Contribución de necesidades (cada una mapeada a [-1, 1])
    float hungerContrib  = MapNeedToMood(needs.hunger,  30f, 70f);
    float energyContrib  = MapNeedToMood(needs.energy,  25f, 75f);
    float socialContrib  = MapNeedToMood(needs.social,  20f, 80f);
    float hygieneContrib = MapNeedToMood(needs.hygiene, 20f, 80f);

    float needAvg = (hungerContrib + energyContrib + socialContrib + hygieneContrib) * 0.25f;

    // Contribución social: afinidad media con los 5 mejores amigos
    float relAvg = rels.Top5AverageAffinity() / 100f;  // [-1, 1]

    float raw = (needAvg * 0.7f + relAvg * 0.3f);
    return Mathf.Clamp((raw + 1f) * 50f, 0f, 100f);
}

float MapNeedToMood(float value, float lowThreshold, float highThreshold) {
    // Sigmoid suave: devuelve [-1, 1]
    // Debajo de lowThreshold tiende a -1, encima de highThreshold tiende a +1
    float t = (value - lowThreshold) / (highThreshold - lowThreshold);
    return Mathf.Clamp(t * 2f - 1f, -1f, 1f);
}
```

---

## 6. Acciones que modifican necesidades

### 6.1 Comida (afecta Hambre)

| Tipo de comida | Hambre ganado | Coste (nimbos) ⚙️ |
|---|---|---|
| Snack | `+15` | 5 |
| Comida casera | `+35` | 0 (usa ingredientes) |
| Comida de restaurante | `+50` | 20 |
| Banquete (evento social) | `+80` | 0 (evento) |

Comer también da `+5` de Ánimo si el Hambre estaba por debajo de 35.

### 6.2 Descanso (afecta Energía)

| Tipo de descanso | Energía ganada/hora | Condición |
|---|---|---|
| Dormir en cama propia | `+12` ⚙️ | Necesita cama asignada |
| Dormir en sofá | `+6` ⚙️ | Mueble tipo sofá en el apartamento |
| Siesta (sillón) | `+20` instantáneo | Una vez al día |
| Café | `+10` instantáneo | Comprado en tienda |

Dormir también detiene el decaimiento de las demás necesidades (50% de velocidad).

### 6.3 Socialización (afecta Vínculo social)

| Interacción | Social ganado | Condición |
|---|---|---|
| Charla breve | `+5` | Dos habitantes en misma sala |
| Charla larga | `+15` | Charla de >2 min de juego |
| Actividad juntos | `+20` | Minijuego, comida, paseo |
| Recibir visita en casa | `+25` | Un habitante visita al otro |
| Llamada telefónica | `+8` | Sin necesidad de estar cerca |

### 6.4 Higiene

| Acción | Higiene ganado | Condición |
|---|---|---|
| Lavarse la cara | `+15` | Cualquier apartamento con lavabo |
| Ducha | `+60` | Ducha o bañera en el apartamento |
| Baño relajante | `+100` | Bañera. Tarda el doble que una ducha. +10 Ánimo extra. |
| Piscina | `+40` | Edificio de piscina en la isla |

---

## 7. Tick de simulación

Cada minuto de juego, `SimulationEngine.Tick(IslanderId id, float gameDeltaHours)`:

1. Aplica decaimiento a `hunger`, `energy`, `social`, `hygiene`
2. Aplica moduladores por personalidad y actividad
3. Recalcula `mood` con `DeriveMood()`
4. Evalúa umbrales: si alguna necesidad entra en crítico, publica `NeedCriticalEvent`
5. Si alguna necesidad entra en bajo, añade peso a la cola de peticiones
6. Clampea todos los valores a `[0, 100]`

---

## 8. Umbrales de generación de peticiones

Cuando una necesidad cae a la banda "baja" (≤35), el sistema de peticiones
recibe un peso extra para generar una petición relacionada:

| Necesidad | Peso base de petición | Urgencia (multiplicador cada hora en bajo) |
|---|---|---|
| Hambre | `30` ⚙️ | `×1.15` |
| Energía | `15` ⚙️ | `×1.10` |
| Social | `20` ⚙️ | `×1.08` |
| Higiene | `10` ⚙️ | `×1.12` |
| Ánimo (derivado) | `25` ⚙️ | `×1.20` |

Los detalles de la generación están en `peticiones.md`.

---

## 9. Inicialización

Cuando se crea un habitante nuevo:

```csharp
var needs = new NeedState {
    hunger  = RandomRange(60f, 80f),
    mood    = 70f,
    energy  = RandomRange(70f, 90f),
    social  = RandomRange(40f, 60f),
    hygiene = RandomRange(70f, 90f)
};
```

Cuando se carga una partida, se cargan los valores guardados.
