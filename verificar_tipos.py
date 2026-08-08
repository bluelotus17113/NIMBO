#!/usr/bin/env python3
"""Verifica que los 15 ficheros .cs coincidan con el JSON."""

import json
import os
import re
import sys

JSON_PATH = "Docs/Contratos/personalidades.json"
TYPES_DIR = "Assets/_Project/Scripts/Personality/Types"

EXPECTED = {
    1: "Atleta.cs",
    2: "Artesano.cs",
    3: "Audaz.cs",
    4: "Afable.cs",
    5: "Lider.cs",
    6: "Anfitrion.cs",
    7: "Fiestero.cs",
    8: "Poeta.cs",
    9: "Visionario.cs",
    10: "Artista.cs",
    11: "Genio.cs",
    12: "Romantico.cs",
    13: "Explorador.cs",
    14: "Cuentista.cs",
    15: "Entusiasta.cs",
}

with open(JSON_PATH, "r", encoding="utf-8") as f:
    data = json.load(f)

json_by_index = {}
for t in data["types"]:
    json_by_index[t["index"]] = t

errors = []
warnings = []

# --- 1. Existen los 15 ficheros ---
print("=" * 60)
print("1. EXISTENCIA DE FICHEROS")
print("=" * 60)
for idx, fname in EXPECTED.items():
    path = os.path.join(TYPES_DIR, fname)
    if not os.path.isfile(path):
        errors.append(f"FALTA {fname}")
        print(f"  ✗ {fname} — NO ENCONTRADO")
    else:
        print(f"  ✓ {fname}")

# --- 2. Extraer y comparar campos numéricos ---
print()
print("=" * 60)
print("2. CAMPOS NUMÉRICOS (TypeIndex, Id, WalkSpeed, IdleDwell, MoodDecay)")
print("=" * 60)

def extract_field(text, pattern, group=1):
    m = re.search(pattern, text)
    return m.group(group) if m else None

all_reactions = {}  # idx → tuple of 12 emotions, para chequear duplicados

for idx, fname in EXPECTED.items():
    path = os.path.join(TYPES_DIR, fname)
    if not os.path.isfile(path):
        continue

    with open(path, "r", encoding="utf-8") as f:
        text = f.read()

    j = json_by_index[idx]
    name = j["name"]
    pid = j["id"]

    # TypeIndex
    cs_idx = extract_field(text, r'TypeIndex\s*=\s*(\d+)')
    if cs_idx is None or int(cs_idx) != idx:
        errors.append(f"{fname}: TypeIndex esperado={idx}, leído={cs_idx}")

    # Id
    cs_id = extract_field(text, r'Id\s*=\s*"([^"]+)"')
    if cs_id != pid:
        errors.append(f"{fname}: Id esperado={pid}, leído={cs_id}")

    # WalkSpeed
    cs_ws = extract_field(text, r'WalkSpeed\s*=\s*([\d.]+)f')
    if cs_ws is None or abs(float(cs_ws) - j['walkSpeed']) > 0.001:
        errors.append(
            f"{fname}: WalkSpeed esperado={j['walkSpeed']}, leído={cs_ws}"
        )

    # IdleDwell
    cs_idle = extract_field(text, r'IdleDwell\s*=\s*([\d.]+)f')
    if cs_idle is None or abs(float(cs_idle) - j['idleDwell']) > 0.001:
        errors.append(
            f"{fname}: IdleDwell esperado={j['idleDwell']}, leído={cs_idle}"
        )

    # MoodDecay
    cs_mood = extract_field(text, r'MoodDecay\s*=\s*([\d.]+)f')
    expected_mood_val = j['needMultipliers']['mood']
    if cs_mood is None or abs(float(cs_mood) - expected_mood_val) > 0.001:
        errors.append(
            f"{fname}: MoodDecay esperado={expected_mood_val}, leído={cs_mood}"
        )

    # NeedDecay count (4 exactas, sin Mood)
    need_count = len(re.findall(r'NeedKind\.\w+\]\s*=', text))
    if need_count != 4:
        errors.append(
            f"{fname}: NeedDecay esperadas=4, encontradas={need_count}"
        )

    # NeedDecay: comprobar que NO aparece NeedKind.Mood
    if "NeedKind.Mood" in text or "NeedKind.mood" in text:
        errors.append(f"{fname}: NeedKind.Mood encontrado (no debe compilar)")

    # RequestWeights count (11)
    req_count = len(re.findall(r'RequestKind\.\w+\]\s*=', text))
    if req_count != 11:
        errors.append(
            f"{fname}: RequestWeights esperadas=11, encontradas={req_count}"
        )

    # Reactions count (12)
    react_matches = re.findall(
        r'PersonalityReaction\.(\w+)\]\s*=\s*Emotion\.(\w+)', text
    )
    react_count = len(react_matches)
    if react_count != 12:
        errors.append(
            f"{fname}: Reactions esperadas=12, encontradas={react_count}"
        )
    else:
        # Guardar para chequeo de duplicados
        all_reactions[idx] = tuple(em for _, em in react_matches)

    # Lines: 4 tonos, 2 frases cada uno
    happy_count = len(re.findall(r'LineMood\.Happy\]\s*=\s*new\[\]', text))
    bored_count = len(re.findall(r'LineMood\.Bored\]\s*=\s*new\[\]', text))
    angry_count = len(re.findall(r'LineMood\.Angry\]\s*=\s*new\[\]', text))
    meeting_count = len(re.findall(r'LineMood\.Meeting\]\s*=\s*new\[\]', text))
    if happy_count != 1 or bored_count != 1 or angry_count != 1 or meeting_count != 1:
        errors.append(
            f"{fname}: Lines incompletas (H:{happy_count} B:{bored_count} A:{angry_count} M:{meeting_count})"
        )

    # Frases por tono: 2 cada uno
    for tone in ["Happy", "Bored", "Angry", "Meeting"]:
        pattern = rf'LineMood\.{tone}\]\s*=\s*new\[\]\s*\{{(.*?)\}}'
        m = re.search(pattern, text, re.DOTALL)
        if m:
            frases = re.findall(r'"([^"]*)"', m.group(1))
            if len(frases) != 2:
                errors.append(
                    f"{fname}: LineMood.{tone} esperaba 2 frases, tiene {len(frases)}"
                )

    # AffinityBias count
    n_compat = len(j.get("compatible", []))
    n_clash = len(j.get("clashes", []))
    expected_bias = n_compat + n_clash
    cs_bias = len(re.findall(r'new AffinityBias\(', text))
    if cs_bias != expected_bias:
        errors.append(
            f"{fname}: AffinityBias esperados={expected_bias} "
            f"({n_compat} compat + {n_clash} clash), encontrados={cs_bias}"
        )

    # Comprobar cada compatible/clash
    for c in j.get("compatible", []):
        pattern = rf'new AffinityBias\("{c["id"]}",\s*{c["bonus"]}\)'
        if not re.search(pattern, text):
            errors.append(
                f"{fname}: falta compatible {c['id']} bonus={c['bonus']}"
            )

    for cl in j.get("clashes", []):
        pattern = rf'new AffinityBias\("{cl["id"]}",\s*{cl["penalty"]}\)'
        if not re.search(pattern, text):
            errors.append(
                f"{fname}: falta clash {cl['id']} penalty={cl['penalty']}"
            )

    print(f"  ✓ {fname}: idx={cs_idx} id={cs_id} ws={cs_ws} idle={cs_idle} mood={cs_mood} "
          f"needs={need_count} reqs={req_count} reacts={react_count} biases={cs_bias}")

# --- 3. Duplicados de Reactions ---
print()
print("=" * 60)
print("3. DUPLICADOS DE REACTIONS")
print("=" * 60)

seen = {}
for idx, reacts in all_reactions.items():
    if reacts in seen:
        other = seen[reacts]
        warnings.append(
            f"⚠ {EXPECTED[idx]} y {EXPECTED[other]} tienen EXACTAMENTE "
            f"el mismo mapa de Reactions"
        )
    else:
        seen[reacts] = idx

if not warnings:
    print("  ✓ Ningún tipo tiene reacciones idénticas a otro")

# --- Resumen final ---
print()
print("=" * 60)
print("RESUMEN")
print("=" * 60)

if errors:
    print(f"\n❌ {len(errors)} ERROR(ES):")
    for e in errors:
        print(f"  • {e}")
else:
    print("\n✅ Todos los checks pasaron sin errores.")

if warnings:
    for w in warnings:
        print(w)

sys.exit(1 if errors else 0)
