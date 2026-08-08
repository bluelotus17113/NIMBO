# Contrato: Sistema de tiempo

> **Módulo dueño:** `Nimbo.Core`
> **Datos en:** `Nimbo.Data.GameTime`
> **Configuración:** `TimeConfig.asset` ⚙️

---

## 1. Concepto

El tiempo de juego avanza independientemente del tiempo real, pero a una
escala fija. Existe un ciclo día/noche completo que gobierna los horarios
de los habitantes, la iluminación y la rotación de tiendas.

---

## 2. Escala temporal

| Parámetro | Valor ⚙️ | Significado |
|---|---|---|
| `GAME_SECONDS_PER_REAL_SECOND` | `60f` | 1 segundo real = 1 minuto de juego |
| `GAME_MINUTES_PER_REAL_MINUTE` | `60f` | 1 minuto real = 1 hora de juego |
| `GAME_HOURS_PER_REAL_HOUR` | `60f` | 1 hora real = 2.5 días de juego |
| `REAL_MINUTES_PER_GAME_DAY` | `24f` | Un día de juego dura 24 minutos reales |

### 2.1 Fórmula de conversión

```csharp
public struct GameTime {
    public int day;          // día de juego (empieza en 1)
    public int hour;         // 0..23
    public int minute;       // 0..59
    public float totalGameHours; // horas totales transcurridas desde el inicio
}

GameTime RealToGame(float realSecondsSinceStart) {
    float totalGameSeconds = realSecondsSinceStart * GAME_SECONDS_PER_REAL_SECOND;
    float totalGameHours = totalGameSeconds / 3600f;

    int day = Mathf.FloorToInt(totalGameHours / 24f) + 1;
    int hour = Mathf.FloorToInt(totalGameHours % 24f);
    int minute = Mathf.FloorToInt((totalGameSeconds % 3600f) / 60f);

    return new GameTime { day = day, hour = hour, minute = minute, totalGameHours = totalGameHours };
}
```

---

## 3. Ciclo día/noche

| Franja | Horas de juego | Iluminación | Ambiente |
|---|---|---|---|
| 🌅 Amanecer | `05:00 – 07:00` | Transición noche → día | Pájaros, luz cálida |
| ☀️ Mañana | `07:00 – 12:00` | Día pleno | Actividad alta |
| 🌤️ Mediodía | `12:00 – 14:00` | Día pleno (luz cenital) | Hora de comer |
| 🌤️ Tarde | `14:00 – 18:00` | Día pleno | Actividad normal |
| 🌇 Atardecer | `18:00 – 20:00` | Transición día → noche | Luz dorada |
| 🌙 Noche | `20:00 – 22:00` | Noche | Farolas, ventanas iluminadas |
| 🌙 Noche profunda | `22:00 – 05:00` | Noche oscura | La mayoría duerme |

---

## 4. Horarios de los habitantes

Cada habitante tiene un horario base que se modula por su personalidad
(eje `Energy` principalmente).

### 4.1 Horario estándar (Energy ≈ 0)

| Hora | Actividad |
|---|---|
| `06:30` | Despertar |
| `07:00 – 08:00` | Rutina matutina (ducha, desayuno) |
| `08:00 – 12:00` | Actividades libres (socializar, peticiones, paseo) |
| `12:00 – 13:00` | Almuerzo |
| `13:00 – 18:00` | Actividades libres |
| `18:00 – 19:00` | Cena |
| `19:00 – 22:00` | Ocio (minijuegos, visitas, TV) |
| `22:00 – 06:30` | Dormir |

### 4.2 Moduladores por personalidad

```csharp
int WakeUpHour(IslanderId id) {
    var p = GetPersonality(id);
    // Energy ∈ [-1, 1]
    // Enérgico: se levanta más temprano
    // Calmado: se levanta más tarde
    return Mathf.RoundToInt(6.5f - p.Energy * 1.5f);
    // Energy = +1 (enérgico) → 5:00
    // Energy =  0 (neutro)   → 6:30
    // Energy = -1 (calmado)  → 8:00
}

int BedTimeHour(IslanderId id) {
    var p = GetPersonality(id);
    // Enérgico: se acuesta más tarde (más horas de actividad)
    return Mathf.RoundToInt(22f + p.Energy * 1.5f);
    // Energy = +1 → 23:30
    // Energy =  0 → 22:00
    // Energy = -1 → 20:30
}
```

### 4.3 Excepciones al horario

- Si una necesidad está en **crítico**, se interrumpe la actividad actual.
- Si hay una **petición activa** del jugador, se pospone el sueño hasta 1h.
- Durante un **evento de isla**, el horario se suspende y todos participan.
- Los `Attitude` muy independientes (`< -0.6`) ignoran la hora de comer
  socialmente y comen solos cuando tienen hambre.

---

## 5. Tiempo offline (juego cerrado)

Cuando el jugador cierra el juego y vuelve a abrirlo, el tiempo de juego
ha avanzado. Pero no avanza a la misma velocidad que en juego activo.

### 5.1 Reglas de avance offline

```csharp
float CalculateOfflineGameHours(float realSecondsAway) {
    // Máximo de avance offline: 8 horas de juego
    float maxGameHours = 8f; // ⚙️

    // Velocidad reducida al 25% mientras estás fuera
    float offlineGameSeconds = realSecondsAway * GAME_SECONDS_PER_REAL_SECOND * 0.25f;
    float offlineGameHours = offlineGameSeconds / 3600f;

    return Mathf.Min(offlineGameHours, maxGameHours);
}
```

### 5.2 Qué pasa durante el tiempo offline

1. Las necesidades decaen al 50% de la velocidad normal.
2. Las peticiones **no se generan** offline (el juego no decide sin el jugador).
3. Las peticiones activas **caducan** si su tiempo expiró.
4. Los eventos de relación automáticos (discusiones, acercamientos) corren
   una sola iteración al volver, no una por cada hora offline.
5. Las tiendas rotan su stock una vez si pasó el umbral de renovación.
6. Los habitantes se recolocan en sus camas (si es de noche) o en zonas
   comunes (si es de día).

### 5.3 Resumen al volver

Al reabrir el juego, se muestra un resumen de "Lo que pasó mientras no
estabas":

```
🌙 Dormiste 6 horas reales (1.5 días de juego)

📉 Tus habitantes:
  - Ana se fue a dormir (energía: 23 → crítica, durmió 7h)
  - Luis tuvo hambre y comió solo (hambre: 12 → 47)
  - Marta y Luis discutieron (afinidad: 45 → 30)

🏪 Tiendas renovadas:
  - Muebles Nimbo tiene objetos nuevos

📬 Peticiones caducadas: 1 (Ana quería un snack)
```

---

## 6. Días especiales

El año de juego tiene `365` días ⚙️. Existen días marcados en el calendario:

| Día | Tipo | Efecto |
|---|---|---|
| Día aleatorio por habitante | Cumpleaños | Petición especial, +50% afinidad |
| Cada 30 días de juego | Festival de la isla | Todos los habitantes en el parque, +ánimo global |
| Día 180 | Festival de mediano año | Tiendas con 30% descuento |
| Día 365 | Festival de fin de año | Fuegos artificiales, evento especial |

---

## 7. Estaciones

| Días de juego | Estación | Efecto visual | Efecto en juego |
|---|---|---|---|
| `1 – 90` | 🌸 Primavera | Pétalos, colores pastel | +10% afinidad ganada (amor en el aire) |
| `91 – 182` | ☀️ Verano | Sol intenso, verdes vivos | Los habitantes prefieren actividades al aire libre |
| `183 – 273` | 🍂 Otoño | Hojas naranjas | +20% nimbos en peticiones (cosecha) |
| `274 – 365` | ❄️ Invierno | Nieve, luces cálidas | Los habitantes pasan más tiempo en casa |

---

## 8. Reloj global

```csharp
public static class GameClock {
    // Expone el tiempo actual
    public static GameTime Current { get; private set; }

    // Evento que se dispara cada minuto de juego
    public static event Action<GameTime> OnMinuteTick;

    // Evento que se dispara cada hora de juego
    public static event Action<GameTime> OnHourTick;

    // Evento que se dispara al cambiar de día
    public static event Action<int> OnDayChanged; // nuevo día

    // Avanzar el reloj. Lo llama el GameLoop cada frame.
    public static void Tick(float realDeltaSeconds);

    // Forzar avance offline. Lo llama el SaveSystem al cargar.
    public static void CatchUp(float realSecondsAway);
}
```

`GameClock` es parte de `Nimbo.Core` y es el único dueño del tiempo.
Ningún otro módulo modifica `GameTime`; solo lo leen.

---

## 9. Sincronización con Unity

El `GameClock` no usa `Time.time` directamente (no es determinista).
En su lugar:

```csharp
// En GameLoop.Update():
void Update() {
    float realDelta = Time.deltaTime;
    GameClock.Tick(realDelta);
    // El resto de sistemas reaccionan a OnMinuteTick / OnHourTick
}
```

El bucle de simulación corre a 1 tick por minuto de juego (= 1 segundo real).
Entre ticks, el juego responde a input y renderiza normalmente. Esto separa
la lógica de simulación de la tasa de frames.
