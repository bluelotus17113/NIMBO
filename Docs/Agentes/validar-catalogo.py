#!/usr/bin/env python3
"""Comprueba los cuatro catalogos de contenido. Validador del orquestador."""
import json
import pathlib
import re
import sys
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1] / "Contratos"
ESPERADO = {
    "catalogo_comida.json": (45, "food_"),
    "catalogo_ropa.json": (60, "cloth_"),
    "catalogo_muebles.json": (80, ("furn_", "deco_")),
    "catalogo_acabados.json": (40, ("wall_", "floor_")),
}
NECESIDADES = {"hunger", "energy", "social", "hygiene"}
ID_VALIDO = re.compile(r"^[a-z0-9_]+$")

errores, avisos = [], []
todos_ids, todos_nombres = Counter(), Counter()
por_nivel = Counter()
total = 0

for fichero, (cuenta_esperada, prefijos) in ESPERADO.items():
    path = ROOT / fichero
    if not path.exists():
        errores.append(f"falta {fichero}")
        continue

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        errores.append(f"{fichero}: no es JSON valido ({exc})")
        continue

    items = data.get("items", [])
    total += len(items)
    if len(items) != cuenta_esperada:
        errores.append(f"{fichero}: {len(items)} elementos, se pidieron {cuenta_esperada}")

    if isinstance(prefijos, str):
        prefijos = (prefijos,)

    for it in items:
        cid = it.get("catalogId", "")
        todos_ids[cid] += 1
        todos_nombres[it.get("displayName", "")] += 1

        if not ID_VALIDO.match(cid):
            errores.append(f"{fichero}: catalogId no valido {cid!r}")
        if not cid.startswith(prefijos):
            avisos.append(f"{fichero}: {cid!r} no empieza por {prefijos}")

        nivel = it.get("unlockLevel")
        if not isinstance(nivel, int) or not 1 <= nivel <= 50:
            errores.append(f"{cid}: unlockLevel {nivel!r} fuera de [1, 50]")
        else:
            por_nivel[nivel] += 1

        precio = it.get("price")
        if not isinstance(precio, int) or precio < 0:
            errores.append(f"{cid}: precio {precio!r} invalido")

        if not it.get("description", "").strip():
            errores.append(f"{cid}: sin descripcion")

        bonus = it.get("needBonus")
        if isinstance(bonus, dict):
            sobran = set(bonus) - NECESIDADES
            if sobran:
                errores.append(f"{cid}: needBonus usa claves que no son necesidades: {sobran}")

for cid, n in todos_ids.items():
    if n > 1:
        errores.append(f"catalogId repetido {n} veces: {cid}")
for nombre, n in todos_nombres.items():
    if n > 1:
        avisos.append(f"displayName repetido {n} veces: {nombre!r}")

print(f"{total} objetos en total")
if total:
    for tope in (10, 25, 50):
        disponibles = sum(v for k, v in por_nivel.items() if k <= tope)
        print(f"  disponibles al nivel {tope:>2}: {disponibles:>3} ({disponibles * 100 // total}%)")

for a in avisos[:10]:
    print(f"AVISO: {a}")
if len(avisos) > 10:
    print(f"AVISO: … y {len(avisos) - 10} avisos mas")

if errores:
    print(f"\n{len(errores)} FALLOS:")
    for e in errores[:25]:
        print(f"  - {e}")
    if len(errores) > 25:
        print(f"  … y {len(errores) - 25} mas")
    sys.exit(1)

print("\nOK: los cuatro catalogos cumplen el contrato.")
