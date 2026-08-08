#!/usr/bin/env python3
"""Reparte unlockLevel para que ~50% este en ≤10 y ~75% en ≤25."""

import json, os, math

OUT = "/home/vaknadesu/Proyectos/isla-nimbo/Docs/Contratos"
FILES = [
    "catalogo_comida.json",
    "catalogo_ropa.json",
    "catalogo_muebles.json",
    "catalogo_acabados.json",
]

all_items = []

for fname in FILES:
    with open(os.path.join(OUT, fname), "r", encoding="utf-8") as fh:
        data = json.load(fh)
    for item in data["items"]:
        all_items.append((fname, item, data))

# Target: 225 items, ~112 at ≤10, ~169 at ≤25, ~56 at 26-50
# Assign levels using a curve: levels 1-10 get more items, then thinner spread
# Strategy: assign quantile-based levels

total = len(all_items)
target_10 = int(total * 0.50)  # 112
target_25 = int(total * 0.75)  # 169

# Sort items so lower-priced stuff gets lower levels (makes economic sense)
all_items.sort(key=lambda x: x[1].get("price", 10))

# Assign levels:
# - First 50%: spread across 1-10
# - Next 25%: spread across 11-25
# - Last 25%: spread across 26-50
for idx, (fname, item, data) in enumerate(all_items):
    if idx < target_10:
        # Evenly spread in 1-10
        item["unlockLevel"] = max(1, math.ceil((idx + 1) / target_10 * 10))
    elif idx < target_25:
        # Evenly spread in 11-25
        pos = idx - target_10
        size = target_25 - target_10
        item["unlockLevel"] = 11 + math.floor(pos / size * 15)
    else:
        # Evenly spread in 26-50
        pos = idx - target_25
        size = total - target_25
        item["unlockLevel"] = 26 + math.floor(pos / size * 25)

# Write back
for fname in FILES:
    # Rebuild the items list from our global list, preserving order within each file
    with open(os.path.join(OUT, fname), "r", encoding="utf-8") as fh:
        data = json.load(fh)

    for item in data["items"]:
        cid = item["catalogId"]
        # Find matching item in our list
        for _, our_item, _ in all_items:
            if our_item["catalogId"] == cid:
                item["unlockLevel"] = our_item["unlockLevel"]
                break

    with open(os.path.join(OUT, fname), "w", encoding="utf-8") as fh:
        json.dump(data, fh, ensure_ascii=False, indent=2)

# Quick stats
levels = [item["unlockLevel"] for _, item, _ in all_items]
for lo, hi in [(1, 10), (11, 20), (21, 30), (31, 40), (41, 50)]:
    count = sum(1 for l in levels if lo <= l <= hi)
    print(f"  Nivel {lo:2d}-{hi:2d}: {count:3d} ({count/total*100:5.1f}%)")

print(f"  ≤10: {sum(1 for l in levels if l <= 10)} ({sum(1 for l in levels if l <= 10)/total*100:.1f}%)")
print(f"  ≤25: {sum(1 for l in levels if l <= 25)} ({sum(1 for l in levels if l <= 25)/total*100:.1f}%)")
print("✅ Niveles redistribuidos.")
