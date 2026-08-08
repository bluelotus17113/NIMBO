# Contrato: Sistema de vivienda

> **Módulo dueño:** `Nimbo.Housing`
> **Datos en:** `Nimbo.Data.HomeAssignment`, `Nimbo.Data.RoomLayout`
> **Configuración:** `HousingConfig.asset` ⚙️

---

## 1. Concepto

Cada habitante tiene un apartamento que el jugador puede decorar al estilo
*Los Sims*: rejilla, catálogo de muebles, colocación libre con reglas de
colisión y capas.

El apartamento es una habitación vista en planta (top-down 3D) con paredes
alrededor. La cámara puede orbitar pero la colocación se hace en la vista
de planta.

---

## 2. Dimensiones

| Parámetro | Valor ⚙️ | Nota |
|---|---|---|
| Columnas (X) | `10` | Eje horizontal de la rejilla |
| Filas (Y) | `8` | Eje vertical de la rejilla |
| Altura de pared | `3` unidades | Decoración de pared entre 1.5 y 2.8 unidades |
| Tamaño de celda | `1.0` unidad Unity | Cada celda es 1×1 unidad |
| Apartamentos iniciales | `1` | Se amplían con progresión de isla |

---

## 3. Capas de colocación

Cada celda de la rejilla tiene 6 capas, en orden de renderizado (abajo → arriba):

| Capa | Orden Z | Ejemplos | Regla |
|---|---|---|---|
| `Floor` | 0 | Baldosa, parqué | Textura de suelo. 1 por celda. |
| `Rug` | 1 | Alfombra | Decorativo. Puede solaparse con Floor. |
| `Furniture` | 2 | Silla, mesa, cama, sofá | Ocupa celdas. No se solapa con otro Furniture. |
| `Counter` | 3 | Objeto sobre mueble | Requiere Furniture debajo. Ej: plato sobre mesa. |
| `WallLow` | 4 | Zócalo, papel pintado | En el perímetro de la habitación. |
| `WallHigh` | 5 | Cuadro, estantería, reloj | Ídem. Altura ≥ 1.5 unidades. |

---

## 4. Tamaños de mueble

Todo mueble ocupa un rectángulo de `sizeX × sizeY` celdas:

| Tamaño | Celdas | Ejemplos |
|---|---|---|
| Pequeño | `1×1` | Silla, lámpara de pie, maceta, papelera |
| Mediano A | `2×1` | Mesa pequeña, banco, cómoda |
| Mediano B | `1×2` | Estantería alta, armario estrecho |
| Grande A | `2×2` | Mesa de comedor, cama individual |
| Grande B | `3×2` | Sofá, cama doble |
| Extra grande | `3×3` | Cama king, isla de cocina |

---

## 5. Reglas de colocación

### 5.1 Furniture

```csharp
bool CanPlaceFurniture(FurnitureItem item, int x, int y, Rotation rot, RoomLayout layout) {
    // 1. Cabe en los límites de la rejilla
    (int sx, int sy) = RotatedSize(item, rot);
    if (x + sx > 10 || y + sy > 8 || x < 0 || y < 0) return false;

    // 2. No solapa con otro mueble ya colocado
    for (int ix = x; ix < x + sx; ix++)
    for (int iy = y; iy < y + sy; iy++) {
        if (layout.cells[ix, iy].furniture != null) return false;
    }

    // 3. Regla de acceso: al menos un lado adyacente libre
    if (!HasFreeAdjacentCell(x, y, sx, sy, layout)) return false;

    return true;
}
```

### 5.2 Counter

```csharp
bool CanPlaceCounter(Item item, int x, int y, RoomLayout layout) {
    // Tiene que haber un Furniture en esa celda
    return layout.cells[x, y].furniture != null
        && layout.cells[x, y].furniture.supportsCounters
        && layout.cells[x, y].counter == null; // solo uno por celda
}
```

### 5.3 WallHigh

Los objetos de pared se colocan en celdas del perímetro (x == 0, x == 9, y == 0, y == 7)
y tienen restricción de altura. Dos objetos de pared no pueden solaparse en la misma celda.

### 5.4 Rotación

Rotaciones permitidas: `0°`, `90°`, `180°`, `270°`. Al rotar, `sizeX` y `sizeY`
se intercambian si el ángulo es 90° o 270°.

---

## 6. Catálogo de muebles

Cada mueble pertenece a una categoría y tiene un estilo (para compatibilidad
con la personalidad del habitante).

```csharp
[Serializable]
public struct FurnitureItem {
    public string id;             // "chair_wooden_01"
    public string displayName;    // "Silla de madera"
    public FurnitureCategory category;
    public int sizeX, sizeY;      // tamaño en celdas
    public int cost;              // en nimbos
    public bool supportsCounters; // se pueden poner cosas encima?
    public FurnitureStyle style;  // moderno, rústico, kawaii, elegante, minimal
    public NeedsBonus[] bonuses;  // qué necesidades satisface
    public string[] validLayers;  // en qué capas se puede colocar
}

[Serializable]
public struct NeedsBonus {
    public NeedType need;   // hunger, energy, social, hygiene, mood
    public float value;     // cuánto aporta
    public bool perUse;     // true = por uso, false = pasivo diario
}

public enum FurnitureCategory {
    Seat,        // sillas, sofás, bancos
    Table,       // mesas, escritorios
    Bed,         // camas, hamacas
    Storage,     // armarios, estanterías
    Appliance,   // nevera, cocina, lavabo, ducha
    Decoration,  // cuadros, plantas, alfombras
    Lighting,    // lámparas
    Special      // objetos únicos de eventos
}

public enum FurnitureStyle {
    Modern,
    Rustic,
    Kawaii,
    Elegant,
    Minimal
}
```

### 6.1 Bonus por mueble

| Mueble | Necesidad | Valor | Tipo |
|---|---|---|---|
| Cama | `energy` | `+12/hora` al dormir | `perUse` |
| Sofá | `energy` | `+6/hora` al descansar | `perUse` |
| Ducha | `hygiene` | `+60` por uso | `perUse` |
| Bañera | `hygiene` | `+100` por uso, `+10 mood` | `perUse` |
| Nevera | `hunger` | `+15` por snack | `perUse` |
| Cocina | `hunger` | `+35` por comida preparada | `perUse` |
| Mesa de comedor | `social` | `+5` por comida compartida | `perUse` |
| Planta | `mood` | `+1` pasivo/día | pasivo |
| Cuadro bonito | `mood` | `+2` pasivo/día | pasivo |
| Equipo de música | `mood` | `+3` pasivo/día | pasivo |
| Estantería | — | Almacena objetos | — |

El bono pasivo diario se aplica una vez cada 24h de juego a las 08:00.

---

## 7. Coste de muebles

| Categoría | Rango de precios (nimbos) ⚙️ |
|---|---|
| Seat | `20 – 150` |
| Table | `30 – 200` |
| Bed | `80 – 500` |
| Storage | `40 – 250` |
| Appliance | `100 – 600` |
| Decoration | `10 – 100` |
| Lighting | `15 – 80` |
| Special | `200 – 1000` |

Ver `economia.md` para la tabla de equilibrio completa.

---

## 8. Ampliación del apartamento

| Nivel de isla | Tamaño | Coste (nimbos) ⚙️ |
|---|---|---|
| Fase 1 (inicio) | `10×8` (80 celdas) | Gratis |
| Fase 2 | `12×10` (120 celdas) | `500` |
| Fase 3 | `14×12` (168 celdas) | `1500` |
| Fase 4 (máximo) | Segunda planta `10×8` adicional | `3000` |

La ampliación es opcional: el jugador paga y la habitación crece. La nueva
zona se añade en la dirección que elija el jugador (o arriba en caso de
segunda planta).

---

## 9. Estados de la rejilla

```csharp
[Serializable]
public struct RoomLayout {
    public int sizeX, sizeY;
    public CellData[,] cells;
    public List<FurniturePlacement> placements; // muebles colocados
    public WallColor wallColor;
    public FloorType floorType;
}

[Serializable]
public struct CellData {
    public FloorType floor;
    public RugPlacement? rug;
    public int? furniturePlacementId;  // índice en placements
    public int? counterItemId;         // objeto sobre el mueble
    public int? wallLowItemId;
    public int? wallHighItemId;
}
```

---

## 10. Validación al guardar

Antes de guardar, el sistema valida que:
1. Ningún mueble está fuera de límites
2. No hay dos muebles solapados
3. Los objetos Counter tienen un Furniture debajo
4. Los WallHigh/WallLow están en el perímetro

Si falla la validación, se revierte al último layout válido y se loguea un error.
