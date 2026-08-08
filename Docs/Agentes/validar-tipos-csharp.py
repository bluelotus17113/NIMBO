#!/usr/bin/env python3
"""Compara los 16 ficheros de personalidad en C# con personalidades.json.

Validador del orquestador. El agente que escribio los ficheros trae el suyo; la
gracia esta en que sean dos, escritos por separado y sin mirarse.
"""
import json
import pathlib
import re
import sys

RAIZ = pathlib.Path(__file__).resolve().parents[2]
TIPOS = RAIZ / "Assets/_Project/Scripts/Personality/Types"
JSON = RAIZ / "Docs/Contratos/personalidades.json"

CLASES = {
    0: "Ermitano", 1: "Atleta", 2: "Artesano", 3: "Audaz",
    4: "Afable", 5: "Lider", 6: "Anfitrion", 7: "Fiestero",
    8: "Poeta", 9: "Visionario", 10: "Artista", 11: "Genio",
    12: "Romantico", 13: "Explorador", 14: "Cuentista", 15: "Entusiasta",
}

NEED_CS = {"hunger": "Hunger", "energy": "Energy", "social": "Social", "hygiene": "Hygiene"}
REQ_CS = {
    "food": "Food", "item": "Object", "clothing": "Clothes", "advice": "Advice",
    "favor": "Favor", "complaint": "Complaint", "socialIntro": "SocialIntro",
    "activity": "Activity", "islandBuilding": "IslandBuilding",
    "confession": "Confession", "reconcile": "Reconcile",
}

data = json.loads(JSON.read_text(encoding="utf-8"))
por_indice = {t["index"]: t for t in data["types"]}

errores, avisos = [], []
reacciones_por_tipo = {}


def num(texto, patron, campo, fichero):
    m = re.search(patron, texto)
    if not m:
        errores.append(f"{fichero}: no se encontro {campo}")
        return None
    return float(m.group(1))


for indice, clase in sorted(CLASES.items()):
    path = TIPOS / f"{clase}.cs"
    esperado = por_indice[indice]

    if not path.exists():
        errores.append(f"falta {clase}.cs")
        continue

    src = path.read_text(encoding="utf-8")

    if f"class {clase} : PersonalityBehaviourBase" not in src:
        errores.append(f"{clase}.cs: la clase no hereda de PersonalityBehaviourBase")

    # --- escalares -------------------------------------------------------
    m = re.search(r'Id\s*=\s*"([^"]+)"', src)
    if not m or m.group(1) != esperado["id"]:
        errores.append(f"{clase}.cs: Id {m.group(1) if m else '?'} != {esperado['id']}")

    idx = num(src, r"TypeIndex\s*=\s*(\d+)", "TypeIndex", f"{clase}.cs")
    if idx is not None and int(idx) != indice:
        errores.append(f"{clase}.cs: TypeIndex {int(idx)} != {indice}")

    for campo, clave in (("WalkSpeed", "walkSpeed"), ("IdleDwell", "idleDwell")):
        v = num(src, rf"{campo}\s*=\s*([\d.]+)f", campo, f"{clase}.cs")
        if v is not None and abs(v - esperado[clave]) > 0.001:
            errores.append(f"{clase}.cs: {campo} {v} != {esperado[clave]}")

    mood = num(src, r"MoodDecay\s*=\s*([\d.]+)f", "MoodDecay", f"{clase}.cs")
    if mood is not None and abs(mood - esperado["needMultipliers"]["mood"]) > 0.001:
        errores.append(
            f"{clase}.cs: MoodDecay {mood} != {esperado['needMultipliers']['mood']}")

    if "NeedKind.Mood" in src:
        errores.append(f"{clase}.cs: usa NeedKind.Mood, que no existe")

    # --- tablas ----------------------------------------------------------
    for clave, cs in NEED_CS.items():
        v = num(src, rf"\[NeedKind\.{cs}\]\s*=\s*([\d.]+)f", f"NeedDecay {cs}", f"{clase}.cs")
        if v is not None and abs(v - esperado["needMultipliers"][clave]) > 0.001:
            errores.append(
                f"{clase}.cs: NeedDecay[{cs}] {v} != {esperado['needMultipliers'][clave]}")

    for clave, cs in REQ_CS.items():
        v = num(src, rf"\[RequestKind\.{cs}\]\s*=\s*([\d.]+)f",
                f"RequestWeights {cs}", f"{clase}.cs")
        if v is not None and abs(v - esperado["requestWeights"][clave]) > 0.001:
            errores.append(
                f"{clase}.cs: RequestWeights[{cs}] {v} != {esperado['requestWeights'][clave]}")

    reacciones = dict(re.findall(
        r"\[PersonalityReaction\.(\w+)\]\s*=\s*Emotion\.(\w+)", src))
    if len(reacciones) != 12:
        errores.append(f"{clase}.cs: {len(reacciones)} reacciones, tienen que ser 12")
    reacciones_por_tipo[clase] = tuple(sorted(reacciones.items()))

    # --- afinidades ------------------------------------------------------
    sesgos = {i: int(v) for i, v in re.findall(
        r'new AffinityBias\("(\w+)",\s*(-?\d+)\)', src)}
    esperados = {c["id"]: c["bonus"] for c in esperado["compatible"]}
    esperados.update({c["id"]: c["penalty"] for c in esperado["clashes"]})
    if sesgos != esperados:
        faltan = set(esperados) - set(sesgos)
        sobran = set(sesgos) - set(esperados)
        distintos = {k for k in set(sesgos) & set(esperados) if sesgos[k] != esperados[k]}
        errores.append(f"{clase}.cs: afinidades mal — faltan {faltan or '{}'}, "
                       f"sobran {sobran or '{}'}, distintas {distintos or '{}'}")

    # --- frases ----------------------------------------------------------
    for tono, clave in (("Happy", "happy"), ("Bored", "bored"),
                        ("Angry", "angry"), ("Meeting", "meeting")):
        bloque = re.search(rf"\[LineMood\.{tono}\]\s*=\s*new\[\]\s*\{{(.*?)\}}", src, re.S)
        if not bloque:
            errores.append(f"{clase}.cs: falta el bloque de frases {tono}")
            continue
        frases = re.findall(r'"((?:[^"\\]|\\.)*)"', bloque.group(1))
        esperadas = esperado["lines"][clave]
        if len(frases) != len(esperadas):
            errores.append(f"{clase}.cs: {tono} tiene {len(frases)} frases, "
                           f"el JSON trae {len(esperadas)}")
            continue
        for a, b in zip(frases, esperadas):
            if a.replace('\\"', '"') != b:
                errores.append(f"{clase}.cs: {tono} no copia la frase literal\n"
                               f"      cs   : {a}\n      json : {b}")

# --- tipos que reaccionan igual a todo ------------------------------------
vistos = {}
for clase, firma in reacciones_por_tipo.items():
    if firma in vistos:
        avisos.append(f"{clase} y {vistos[firma]} reaccionan igual a las 12 situaciones")
    else:
        vistos[firma] = clase

for a in avisos:
    print(f"AVISO: {a}")

if errores:
    print(f"\n{len(errores)} FALLOS:")
    for e in errores[:30]:
        print(f"  - {e}")
    if len(errores) > 30:
        print(f"  … y {len(errores) - 30} mas")
    sys.exit(1)

print(f"\nOK: los 16 ficheros C# dicen lo mismo que el JSON.")
print(f"    {len(set(vistos))} mapas de reaccion distintos de 16 posibles.")
