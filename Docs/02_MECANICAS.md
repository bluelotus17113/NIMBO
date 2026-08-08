# Isla Nimbo — Especificación de mecánicas

> **Este documento es normativo para los programadores.** Las fórmulas, tablas y
> números que aquí aparecen son los que hay que implementar. Si algo no está aquí,
> no existe todavía.
>
> Los detalles de cada sistema están en `Docs/Contratos/<sistema>.md`. Este
> documento da la vista transversal y las reglas que no pertenecen a un solo módulo.

---

## 1. Módulos y sus contratos

| Módulo | Contrato | Qué especifica |
|---|---|---|
| Simulation | `necesidades.md` | Hambre, Ánimo, Energía, Vínculo social, Higiene |
| Simulation | `peticiones.md` | Peticiones de habitantes al jugador |
| Social | `relaciones.md` | Grafo social, afinidad, estados |
| Housing | `vivienda.md` | Rejilla, muebles, colocación |
| Economy | `economia.md` | Moneda, ingresos, tiendas, equilibrio |
| Simulation | `progresion.md` | Nivel de habitante, desbloqueo de isla |
| Core | `tiempo.md` | Escala temporal, ciclo día/noche, offline |

---

## 2. Reglas transversales

### 2.1 Numeración

Todo número que un diseñador tocaría se marca con `⚙️` y vivirá en un
`ScriptableObject`. Los valores por defecto que se dan son los de arranque.

### 2.2 Aleatoriedad

Cualquier tirada usa `RngService` (que envuelve `System.Random` con semilla
guardable). Nada de `UnityEngine.Random` en lógica de juego.

Las curvas de probabilidad usan pesos (`weight`), no ternarios anidados:

```csharp
float totalWeight = weights.Sum();
float roll = rng.NextFloat() * totalWeight;
```

### 2.3 Delta time en fórmulas

Todas las fórmulas que dependen del tiempo usan `gameDeltaHours`, que es el
`Time.deltaTime` convertido a horas de juego:

```csharp
float gameDeltaHours = (Time.deltaTime * gameSecondsPerRealSecond) / 3600f;
```

### 2.4 Umbrales y redondeo

Los valores de necesidad y afinidad se guardan como `float` pero se muestran
como `int` redondeado hacia abajo (`Mathf.FloorToInt`). Las comparaciones de
umbral usan el `float`.

### 2.5 Eventos de Core

Los eventos que cruzan módulos se declaran en `Nimbo.Core.Events.GameEvents`.
Si tu módulo necesita emitir uno que no existe, lo pides al orquestador. No
lo añades tú.

### 2.6 ScriptableObjects de configuración

Cada contrato produce al menos un `⚙️` ScriptableObject:

| SO | Path |
|---|---|
| `NeedsConfig` | `Assets/_Project/Settings/NeedsConfig.asset` |
| `RequestsConfig` | `Assets/_Project/Settings/RequestsConfig.asset` |
| `RelationshipsConfig` | `Assets/_Project/Settings/RelationshipsConfig.asset` |
| `HousingConfig` | `Assets/_Project/Settings/HousingConfig.asset` |
| `EconomyConfig` | `Assets/_Project/Settings/EconomyConfig.asset` |
| `ProgressionConfig` | `Assets/_Project/Settings/ProgressionConfig.asset` |
| `TimeConfig` | `Assets/_Project/Settings/TimeConfig.asset` |

---

## 3. Fórmulas compartidas

### 3.1 Cálculo de ánimo compuesto

El ánimo de un habitante no se guarda directamente: se deriva cada tick de
simulación a partir del estado de sus necesidades y sus relaciones.

```csharp
float CompositeMood(NeedState[] needs, RelationshipSummary rels) {
    float needScore = needs.Average(n => NormalizeNeed(n));
    float relScore = rels.AverageAffinity / 100f;  // [-1, 1]
    return Mathf.Clamp((needScore * 0.7f + relScore * 0.3f) * 100f, 0f, 100f);
}
```

### 3.2 Compatibilidad entre personalidades

```csharp
float Compatibility(PersonalityProfile a, PersonalityProfile b) {
    float energy = 1f - Mathf.Abs(a.Energy - b.Energy);
    float expression = 1f - Mathf.Abs(a.Expression - b.Expression);
    float attitude = 1f - Mathf.Abs(a.Attitude - b.Attitude);
    float outlook = 1f - Mathf.Abs(a.Outlook - b.Outlook);

    // Attitude pesa más en compatibilidad: dos sociables se llevan bien,
    // dos independientes también, pero un sociable y un independiente chocan
    return (energy * 0.2f + expression * 0.2f + attitude * 0.35f + outlook * 0.25f);
}
```

Resultado en `[-1, 1]`. Se usa como multiplicador en cambios de afinidad y en
la generación de peticiones.

### 3.3 Curva de experiencia

```csharp
int XpForLevel(int level) {
    return Mathf.RoundToInt(100f * level * Mathf.Sqrt(level));
}
```

Esto da ~14,850 XP para nivel 50 (unos 100 encargos completados).

---

## 4. Referencia rápida de valores globales

| Símbolo | Valor | Significado |
|---|---|---|
| `GAME_SECONDS_PER_REAL_SECOND` | `60f` ⚙️ | 1s real = 1min juego |
| `MAX_ISLANDERS` | `50` ⚙️ | Máximo de habitantes en la isla |
| `STARTING_NIMBOS` | `200` ⚙️ | Dinero inicial del jugador |
| `MAX_APARTMENT_GRID_X` | `10` ⚙️ | Columnas de la rejilla de vivienda |
| `MAX_APARTMENT_GRID_Y` | `8` ⚙️ | Filas de la rejilla de vivienda |
