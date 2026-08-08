#!/usr/bin/env python3
"""Valida los 4 catálogos de Isla Nimbo según el encargo."""

import json, os, re, sys
from collections import Counter

OUT = "/home/vaknadesu/Proyectos/isla-nimbo/Docs/Contratos"
FILES = [
    ("catalogo_comida.json", 45),
    ("catalogo_ropa.json", 60),
    ("catalogo_muebles.json", 80),
    ("catalogo_acabados.json", 40),
]

errors = []

# ── 1. Cargar y contar ──────────────────────────────────────────

all_ids = []
all_display_names = []
all_items = []

for filename, expected_count in FILES:
    path = os.path.join(OUT, filename)
    try:
        with open(path, "r", encoding="utf-8") as fh:
            data = json.load(fh)
    except (FileNotFoundError, json.JSONDecodeError) as e:
        errors.append(f"{filename}: no se pudo cargar — {e}")
        continue

    items = data.get("items", [])
    actual = len(items)

    if actual != expected_count:
        errors.append(f"{filename}: {actual} items, se esperaban {expected_count}")
    else:
        print(f"✓ {filename}: {actual} items (OK)")

    for item in items:
        all_ids.append(item.get("catalogId", ""))
        all_display_names.append(item.get("displayName", ""))
        all_items.append((filename, item))

# ── 2. Unicidad de catalogId ────────────────────────────────────

id_counts = Counter(all_ids)
dupes = {k: v for k, v in id_counts.items() if v > 1}
if dupes:
    for k, v in dupes.items():
        errors.append(f"catalogId duplicado: '{k}' aparece {v} veces")
else:
    print(f"✓ catalogId únicos en {len(all_ids)} items (OK)")

# ── 3. Formato de catalogId ─────────────────────────────────────

invalid_ids = []
for cid in all_ids:
    if not cid:
        invalid_ids.append("(vacío)")
        continue
    if cid != cid.lower():
        invalid_ids.append(f"'{cid}' — tiene mayúsculas")
    if " " in cid:
        invalid_ids.append(f"'{cid}' — tiene espacios")
    # busca tildes, eñes y caracteres no ascii
    if not all(ord(c) < 128 for c in cid):
        invalid_ids.append(f"'{cid}' — tiene caracteres no ASCII")

if invalid_ids:
    for e in invalid_ids:
        errors.append(f"catalogId inválido: {e}")
else:
    print(f"✓ Todos los catalogId en minúsculas, sin espacios, sin tildes/eñes (OK)")

# ── 4. Unicidad de displayName ──────────────────────────────────

name_counts = Counter(all_display_names)
dupe_names = {k: v for k, v in name_counts.items() if v > 1}
if dupe_names:
    for k, v in dupe_names.items():
        errors.append(f"displayName duplicado: '{k}' aparece {v} veces")
else:
    print(f"✓ displayName únicos en {len(all_display_names)} items (OK)")

# ── 5. unlockLevel ──────────────────────────────────────────────

bad_levels = []
all_levels = []
for filename, item in all_items:
    lvl = item.get("unlockLevel")
    if lvl is None or not isinstance(lvl, (int, float)) or lvl < 1 or lvl > 50:
        bad_levels.append(f"{filename}: '{item.get('catalogId')}' unlockLevel={lvl}")
    else:
        all_levels.append(lvl)

if bad_levels:
    for e in bad_levels:
        errors.append(f"unlockLevel fuera de rango: {e}")
else:
    at_10 = sum(1 for l in all_levels if l <= 10)
    at_25 = sum(1 for l in all_levels if l <= 25)
    total = len(all_levels)
    pct_10 = at_10 / total * 100
    pct_25 = at_25 / total * 100
    print(f"✓ unlockLevel entre 1-50 (OK)")
    print(f"  Disponible en nivel ≤10: {at_10}/{total} = {pct_10:.1f}%")
    print(f"  Disponible en nivel ≤25: {at_25}/{total} = {pct_25:.1f}%")

# ── 6. needBonus sin 'mood' ─────────────────────────────────────

mood_offenders = []
for filename, item in all_items:
    bonus = item.get("needBonus")
    if bonus and isinstance(bonus, dict) and "mood" in bonus:
        mood_offenders.append(f"{filename}: '{item.get('catalogId')}' tiene 'mood' en needBonus")

if mood_offenders:
    for e in mood_offenders:
        errors.append(f"CLAVE MOOD PROHIBIDA: {e}")
else:
    print(f"✓ Ningún needBonus contiene 'mood' (OK)")

# ── 7. Reparto por tramos de 10 niveles ─────────────────────────

print("\n── Reparto por nivel ──")
ranges = [(1, 10), (11, 20), (21, 30), (31, 40), (41, 50)]
for lo, hi in ranges:
    count = sum(1 for l in all_levels if lo <= l <= hi)
    pct = count / len(all_levels) * 100
    bar = "█" * max(1, int(pct / 2))
    print(f"  Nivel {lo:2d}-{hi:2d}: {count:3d} items ({pct:5.1f}%) {bar}")

# ── 8. Verificar que no haya needBonus vacío en muebles funcionales ──

# Los muebles con function 'decor' deben tener needBonus vacío
decor_with_bonus = []
for filename, item in all_items:
    if item.get("function") == "decor":
        bonus = item.get("needBonus", {})
        if bonus and len(bonus) > 0:
            decor_with_bonus.append(f"{filename}: '{item.get('catalogId')}' es decor pero tiene needBonus={bonus}")

if decor_with_bonus:
    for e in decor_with_bonus:
        errors.append(f"Mueble decor con bonus: {e}")
else:
    print(f"✓ Los muebles 'decor' tienen needBonus vacío (OK)")

# ── 9. Verificar foodKind válidos ───────────────────────────────

valid_food_kinds = {"snack", "homemade", "restaurant", "dessert", "drink"}
bad_food_kinds = []
for filename, item in all_items:
    fk = item.get("foodKind")
    if fk and fk not in valid_food_kinds:
        bad_food_kinds.append(f"{filename}: '{item.get('catalogId')}' foodKind={fk}")

if bad_food_kinds:
    for e in bad_food_kinds:
        errors.append(f"foodKind inválido: {e}")
else:
    print(f"✓ foodKind válidos (OK)")

# ── 10. Verificar slot de ropa válidos ──────────────────────────

valid_slots = {"outfit", "hat", "accessory"}
bad_slots = []
for filename, item in all_items:
    slot = item.get("slot")
    if slot and slot not in valid_slots:
        bad_slots.append(f"{filename}: '{item.get('catalogId')}' slot={slot}")

if bad_slots:
    for e in bad_slots:
        errors.append(f"slot inválido: {e}")
else:
    print(f"✓ slots de ropa válidos (OK)")

# ── RESULTADO ───────────────────────────────────────────────────

print(f"\n{'═' * 50}")
if errors:
    print(f"❌ {len(errors)} ERRORES encontrados:")
    for e in errors:
        print(f"  • {e}")
    sys.exit(1)
else:
    print("✅ VALIDACIÓN COMPLETA — 0 errores")
