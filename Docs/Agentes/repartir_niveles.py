#!/usr/bin/env python3
"""Reparte unlockLevel por categoría: cada una tiene items en todo 1-50."""

import json, os, math, random

OUT = "/home/vaknadesu/Proyectos/isla-nimbo/Docs/Contratos"
FILES = [
    "catalogo_comida.json",
    "catalogo_ropa.json",
    "catalogo_muebles.json",
    "catalogo_acabados.json",
]

random.seed(42)

all_levels = []

for fname in FILES:
    path = os.path.join(OUT, fname)
    with open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)

    items = data["items"]
    n = len(items)

    # Target: ~50% at ≤10, ~75% at ≤25 within each file
    target_10 = max(1, int(n * 0.50))
    target_25 = max(target_10 + 1, int(n * 0.75))

    # Sort by price so cheaper things unlock earlier (within each category)
    sorted_items = sorted(enumerate(items), key=lambda x: x[1].get("price", 10))

    for rank, (orig_idx, item) in enumerate(sorted_items):
        if rank < target_10:
            # Spread across 1-10
            lvl = max(1, round(1 + rank / max(1, target_10 - 1) * 9))
        elif rank < target_25:
            # Spread across 11-25
            pos = rank - target_10
            size = max(1, target_25 - target_10 - 1)
            lvl = 11 + round(pos / size * 14)
        else:
            # Spread across 26-50
            pos = rank - target_25
            size = max(1, n - target_25 - 1)
            lvl = 26 + round(pos / size * 24)

        # Small random jitter to avoid all items landing on same level
        jitter = random.choice([-1, 0, 0, 1]) if lvl > 1 and lvl < 50 else 0
        lvl = max(1, min(50, lvl + jitter))

        items[orig_idx]["unlockLevel"] = lvl
        all_levels.append(lvl)

    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, ensure_ascii=False, indent=2)

    # Per-file stats
    lvls = [i["unlockLevel"] for i in items]
    print(f"{fname}: {n} items, nivel {min(lvls)}-{max(lvls)}, ≤10={sum(1 for l in lvls if l<=10)} ({sum(1 for l in lvls if l<=10)/n*100:.0f}%), ≤25={sum(1 for l in lvls if l<=25)} ({sum(1 for l in lvls if l<=25)/n*100:.0f}%)")

# Global stats
total = len(all_levels)
for lo, hi in [(1, 10), (11, 20), (21, 30), (31, 40), (41, 50)]:
    count = sum(1 for l in all_levels if lo <= l <= hi)
    print(f"  Global {lo:2d}-{hi:2d}: {count:3d} ({count/total*100:5.1f}%)")
print(f"  Global ≤10: {sum(1 for l in all_levels if l <= 10)} ({sum(1 for l in all_levels if l <= 10)/total*100:.1f}%)")
print(f"  Global ≤25: {sum(1 for l in all_levels if l <= 25)} ({sum(1 for l in all_levels if l <= 25)/total*100:.1f}%)")
print("✅ Niveles redistribuidos por categoría.")
