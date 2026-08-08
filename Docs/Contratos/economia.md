# Contrato: Sistema de economía

> **Módulo dueño:** `Nimbo.Economy`
> **Datos en:** `Nimbo.Data.Wallet`, `Nimbo.Data.Inventory`
> **Configuración:** `EconomyConfig.asset` ⚙️

---

## 1. Concepto

La economía de Isla Nimbo es cerrada y predecible: el jugador gana nimbos (⭐)
resolviendo peticiones y participando en actividades, y los gasta en muebles,
ropa, comida y eventos.

No hay inflación, no hay préstamos, no hay inversiones. Es una economía de
bolsillo para un juego cozy.

---

## 2. Moneda

| Símbolo | Nombre | Representación |
|---|---|---|
| ⭐ | Nimbo | `int`. Sin decimales. |

No hay microtransacciones ni moneda premium. Todo se gana jugando.

---

## 3. Estructura de datos

```csharp
[Serializable]
public struct Wallet {
    public int nimbos;
    public int totalEarned;   // estadística acumulada
    public int totalSpent;     // estadística acumulada
}

[Serializable]
public struct Inventory {
    public List<InventorySlot> items; // objetos que el jugador posee
    public int maxSlots;              // empieza en 20 ⚙️, ampliable
}

[Serializable]
public struct InventorySlot {
    public string itemId;
    public int quantity;      // para consumibles (comida, ingredientes)
}
```

---

## 4. Fuentes de ingreso

### 4.1 Peticiones cumplidas

Ver detalle en `peticiones.md`. Resumen:

| Tipo de petición | Nimbos base ⚙️ |
|---|---|
| Food | `10` |
| Object | `15` |
| Clothes | `20` |
| Advice | `5` |
| Favor | `25` |
| Complaint | `15` |
| SocialIntro | `10` |
| Activity | `30` |
| IslandBuilding | `50` |

### 4.2 Otras fuentes

| Fuente | Nimbos | Frecuencia | Nota |
|---|---|---|---|
| Bono diario de conexión | `20` ⚙️ | 1/día real | Se reinicia a las 00:00 UTC |
| Venta de objeto del inventario | `50%` del precio de compra ⚙️ | Cuando quiera | Redondeo hacia abajo |
| Minijuego (puntuación alta) | `15-40` ⚙️ | Ilimitado | Escala con la puntuación |
| Evento de isla | `30-100` ⚙️ | 1-2/semana juego | Ej: festival, concurso |
| Logro desbloqueado | `50-200` ⚙️ | Una vez por logro | |
| Regalo de visitante | `10-50` ⚙️ | Aleatorio | Habitante agradecido |

---

## 5. Sumideros (gastos)

### 5.1 Tienda de muebles

Ver `vivienda.md` sección 7 para la tabla por categoría. El gasto principal
del juego está aquí.

### 5.2 Tienda de ropa

| Tipo de prenda | Precio (nimbos) ⚙️ |
|---|---|
| Camiseta | `30 – 80` |
| Pantalón / falda | `40 – 100` |
| Zapatos | `20 – 60` |
| Accesorio (gafas, gorra) | `15 – 50` |
| Conjunto completo | `100 – 300` |
| Disfraz (tematizado) | `150 – 400` |

### 5.3 Comida y bebida

| Tipo | Precio ⚙️ | Repone |
|---|---|---|
| Snack (tienda) | `5` | `+15` hambre |
| Bebida (máquina) | `3` | `+8` energía |
| Ingrediente (para cocinar) | `8` | — |
| Comida de restaurante | `20` | `+50` hambre, `+5` ánimo |
| Café | `7` | `+10` energía |

### 5.4 Objetos especiales

| Objeto | Precio ⚙️ | Uso |
|---|---|---|
| Anillo de boda | `200` | Propuesta de matrimonio |
| Regalo genérico | `30` | Para dar a otro habitante |
| Regalo premium | `80` | Mayor ganancia de afinidad |
| Poción de ánimo | `25` | `+30` ánimo instantáneo |
| Bebida energética | `15` | `+25` energía instantánea |

### 5.5 Ampliación de vivienda

| Ampliación | Precio ⚙️ |
|---|---|
| 10×8 → 12×10 | `500` |
| 12×10 → 14×12 | `1500` |
| Segunda planta | `3000` |

---

## 6. Tiendas y rotación de stock

### 6.1 Tiendas disponibles

| Tienda | Vende | Edificio necesario |
|---|---|---|
| `NimboMart` | Comida, bebida, objetos cotidianos | Fase 1 (inicial) |
| `Muebles Nimbo` | Muebles (rotación diaria) | Fase 1 (inicial) |
| `Boutique Celeste` | Ropa y accesorios | Fase 2 |
| `Antigüedades Nimbo` | Objetos raros, decoración premium | Fase 3 |
| `Mercado flotante` | Objetos de evento, coleccionables | Fase 4 |

### 6.2 Rotación de stock

Cada tienda tiene `N` slots de stock que se regeneran:

| Tienda | Slots ⚙️ | Renovación ⚙️ |
|---|---|---|
| NimboMart | `12` | Cada 8h de juego |
| Muebles Nimbo | `8` | Cada 24h de juego |
| Boutique Celeste | `10` | Cada 12h de juego |
| Antigüedades Nimbo | `4` | Cada 48h de juego |
| Mercado flotante | `6` | Cada 72h de juego (evento) |

Los objetos que no se compran se pierden al rotar (FOMO suave). El jugador
puede pagar `10` nimbos para forzar una rotación anticipada (máx. 3 veces
por día de juego).

---

## 7. Inventario del jugador

El inventario empieza con `20` slots ⚙️ y se puede ampliar:

| Ampliación | Slots totales | Coste ⚙️ |
|---|---|---|
| Inicial | `20` | — |
| Mochila mediana | `30` | `100` |
| Mochila grande | `40` | `250` |
| Baúl mágico | `60` | `500` |

Los objetos de tipo mueble no ocupan slot: van directos al almacén de vivienda
(ilimitado). Solo consumibles, ropa y objetos de regalo ocupan inventario.

---

## 8. Tabla de equilibrio

**Objetivo:** un jugador que juega 30 minutos reales al día (30h de juego)
debe poder comprar 1-2 muebles decorativos o 1 mueble funcional, y ahorrar
para una ampliación en ~1 semana real de juego.

### 8.1 Ingresos esperados por hora real de juego (≈60h de juego)

| Fuente | Nimbos/hora real (estimado) |
|---|---|
| Peticiones cumplidas | `50 – 120` |
| Bono diario | `20` |
| Minijuegos | `15 – 60` |
| Ventas | `0 – 30` |
| **Total/hora real** | **`85 – 230`** |

### 8.2 Tiempo para objetivos

| Objetivo | Coste | Horas reales (estimado) |
|---|---|---|
| Primer mueble decorativo | `30` | 10-20 min |
| Primer electrodoméstico | `150` | 45-90 min |
| Renovar una habitación (varios muebles) | `400` | 2-4h |
| Primera ampliación | `500` | 3-6h |
| Segunda ampliación | `1500` | 8-16h |
| Segunda planta | `3000` | 15-30h |
| Anillo + boda | `200` | 1-2h |

### 8.3 Anti-arruinarse

- El bono diario (`20` nimbos) garantiza que incluso sin jugar "bien" el
  jugador puede comprar snacks y bebida.
- Las peticiones de comida urgente siempre se pueden resolver con items de 5
  nimbos (snack).
- No hay gastos obligatorios recurrentes (impuestos, alquiler, facturas). Es
  un juego cozy, no un simulador de precariedad.

### 8.4 Anti-hacerse-rico-sin-querer

- Los minijuegos tienen rendimiento decreciente: +40 la primera vez al día,
  +25 la segunda, +15 la tercera, +5 las siguientes.
- Vender objetos da solo el 50% del precio de compra.
- El precio de rotación forzada de tienda escala: 10, 25, 50 nimbos.
- No hay interés compuesto ni inversiones.

---

## 9. Precios de referencia rápida

| Concepto | Nimbos |
|---|---|
| Snack | `5` |
| Comida restaurante | `20` |
| Camiseta básica | `30` |
| Silla | `20 – 60` |
| Mesa | `50 – 150` |
| Cama | `100 – 400` |
| Sofá | `80 – 250` |
| Ducha | `150 – 400` |
| Cocina | `200 – 500` |
| Nevera | `150 – 350` |
| Cuadro / planta | `10 – 60` |
| Anillo de boda | `200` |
| Ampliación 1 | `500` |
| Ampliación 2 | `1500` |
| Segunda planta | `3000` |
