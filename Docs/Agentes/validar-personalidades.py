#!/usr/bin/env python3
"""Comprueba Docs/Contratos/personalidades.json contra el contrato.

Es el validador del orquestador, no el del agente que escribió el fichero: la gracia
está en que sean dos, escritos por separado. Sale con codigo != 0 si algo falla.
"""
import json
import pathlib
import sys

AXES = ["Energy", "Expression", "Attitude", "Outlook"]
BITS = {"Energy": 1, "Expression": 2, "Attitude": 4, "Outlook": 8}
NEEDS = ["hunger", "mood", "energy", "social", "hygiene"]

path = pathlib.Path(__file__).resolve().parents[1] / "Contratos" / "personalidades.json"
errors, warnings = [], []

try:
    data = json.loads(path.read_text(encoding="utf-8"))
except Exception as exc:  # noqa: BLE001
    print(f"FALLO: no se pudo leer {path}: {exc}")
    sys.exit(2)

types = data.get("types", [])

# --- 16 tipos, indices 0..15 sin repetir ---------------------------------
if len(types) != 16:
    errors.append(f"hay {len(types)} tipos, tienen que ser 16")

seen = {}
for t in types:
    idx = t.get("index")
    if idx in seen:
        errors.append(f"indice {idx} repetido: {seen[idx]} y {t.get('id')}")
    seen[idx] = t.get("id")
faltan = sorted(set(range(16)) - set(seen))
if faltan:
    errors.append(f"faltan los indices {faltan}")

by_id = {t.get("id"): t for t in types}

# --- el signo de los ejes tiene que dar el indice ------------------------
for t in types:
    axes = t.get("axes", {})
    computed = 0
    for axis in AXES:
        v = axes.get(axis)
        if v is None:
            errors.append(f"{t.get('id')}: falta el eje {axis}")
            continue
        if v > 0:
            computed |= BITS[axis]
    if computed != t.get("index"):
        errors.append(
            f"{t.get('id')}: los ejes dan {computed} pero el indice dice {t.get('index')}")

# --- ids validos como identificador de C# -------------------------------
for t in types:
    tid = t.get("id", "")
    if not tid.startswith("PT_"):
        errors.append(f"id sin prefijo PT_: {tid!r}")
    if not tid.replace("_", "").isalnum() or not tid.isascii():
        errors.append(f"id no valido como identificador C#: {tid!r}")

# --- compatibilidades: existen, simetricas, sin auto-referencia ----------
def pairs(key, field):
    out = {}
    for t in types:
        for entry in t.get(key, []):
            other = entry.get("id")
            value = entry.get(field)
            if other not in by_id:
                errors.append(f"{t['id']}.{key} apunta a {other!r}, que no existe")
                continue
            if other == t["id"]:
                errors.append(f"{t['id']}.{key} se apunta a si mismo")
                continue
            out[(t["id"], other)] = value
    return out

compat = pairs("compatible", "bonus")
clash = pairs("clashes", "penalty")

for (a, b), v in compat.items():
    back = compat.get((b, a))
    if back is None:
        errors.append(f"compatible asimetrico: {a}->{b} existe, {b}->{a} no")
    elif back != v:
        errors.append(f"compatible asimetrico: {a}->{b}={v} pero {b}->{a}={back}")

for (a, b), v in clash.items():
    back = clash.get((b, a))
    if back is None:
        errors.append(f"clash asimetrico: {a}->{b} existe, {b}->{a} no")
    elif back != v:
        errors.append(f"clash asimetrico: {a}->{b}={v} pero {b}->{a}={back}")

for pair in compat:
    if pair in clash:
        errors.append(f"{pair[0]} y {pair[1]} son a la vez compatibles y opuestos")

# --- multiplicadores de necesidad: media cerca de 1 ----------------------
for need in NEEDS:
    vals = [t.get("needMultipliers", {}).get(need) for t in types]
    if any(v is None for v in vals):
        errors.append(f"falta needMultipliers.{need} en algun tipo")
        continue
    media = sum(vals) / len(vals)
    fuera = [t["id"] for t, v in zip(types, vals) if not 0.6 <= v <= 1.4]
    if fuera:
        errors.append(f"needMultipliers.{need} fuera de [0.6, 1.4] en {fuera}")
    if not 0.95 <= media <= 1.05:
        errors.append(f"needMultipliers.{need}: media {media:.3f}, tiene que estar en [0.95, 1.05]")
    else:
        print(f"  media {need:<8} = {media:.3f}  ok")

# --- frases y campos de presentacion ------------------------------------
for t in types:
    lines = t.get("lines", {})
    for mood in ("happy", "bored", "angry", "meeting"):
        got = lines.get(mood, [])
        if len(got) < 2:
            errors.append(f"{t['id']}.lines.{mood}: {len(got)} frases, hacen falta 2")
        if any(not isinstance(s, str) or not s.strip() for s in got):
            errors.append(f"{t['id']}.lines.{mood}: hay frases vacias")
    if not t.get("name"):
        errors.append(f"{t['id']}: sin nombre")
    speed = t.get("walkSpeed")
    if speed is None or not 0.7 <= speed <= 1.3:
        errors.append(f"{t['id']}: walkSpeed {speed} fuera de [0.7, 1.3]")

# --- nada de marcas ajenas ----------------------------------------------
PROHIBIDAS = ("mii", "tomodachi", "nintendo")
blob = json.dumps(data, ensure_ascii=False).lower()
for palabra in PROHIBIDAS:
    if palabra in blob:
        warnings.append(f"aparece la palabra {palabra!r} en el fichero")

# --- informe -------------------------------------------------------------
print()
for w in warnings:
    print(f"AVISO: {w}")
if errors:
    print(f"\n{len(errors)} FALLOS:")
    for e in errors:
        print(f"  - {e}")
    sys.exit(1)

print(f"\nOK: 16 tipos, {len(compat)//2} parejas compatibles, {len(clash)//2} opuestas.")
for t in sorted(types, key=lambda x: x["index"]):
    signos = "".join("+" if t["axes"][a] > 0 else "-" for a in AXES)
    print(f"  {t['index']:>2} {signos}  {t['name']:<14} {t['id']}")
