#!/usr/bin/env python3
"""Cierra personalidades.json: simetriza afinidades y completa los pesos que faltan.

La simetria es una restriccion global sobre 16x16 pares. Editandola a mano se arregla
un par y se rompe otro, que es justo lo que estaba pasando. Aqui se resuelve de una vez
y de forma reproducible.

Reglas, en este orden:

1. Si un par aparece a la vez como compatible y como opuesto, gana el de mayor valor
   absoluto; si empatan, se descarta el par entero (no se sabe que quisieron decir).
2. Si solo un lado declara la relacion, el otro la adopta con el mismo numero.
3. Si los dos la declaran con numeros distintos, se quedan con la media redondeada.

Y ademas: `requestWeights` solo traia seis de los once tipos de peticion del juego.
Los cinco que faltan se derivan de los ejes, con la regla documentada abajo, para que
cada tipo tenga carácter tambien en esas peticiones y no todos pidan lo mismo.
"""
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1] / "Contratos"
path = ROOT / "personalidades.json"
data = json.loads(path.read_text(encoding="utf-8"))
types = data["types"]
by_id = {t["id"]: t for t in types}

# --- 1. recoger lo declarado, en pares no ordenados -----------------------
declared = {}   # (a, b) ordenado alfabeticamente -> {a: valor, b: valor}
for t in types:
    for key, field in (("compatible", "bonus"), ("clashes", "penalty")):
        for entry in t.get(key, []):
            other = entry["id"]
            if other == t["id"] or other not in by_id:
                continue
            pair = tuple(sorted((t["id"], other)))
            declared.setdefault(pair, {})[t["id"]] = entry[field]

# --- 2. resolver cada par a un unico numero -------------------------------
resolved = {}
descartados = []
for pair, sides in declared.items():
    valores = list(sides.values())
    if len(valores) == 1:
        resolved[pair] = valores[0]
    elif valores[0] == valores[1]:
        resolved[pair] = valores[0]
    elif (valores[0] > 0) != (valores[1] > 0):
        # uno dice que congenian y el otro que chocan
        a, b = valores
        if abs(a) == abs(b):
            descartados.append(pair)
        else:
            resolved[pair] = a if abs(a) > abs(b) else b
    else:
        resolved[pair] = round((valores[0] + valores[1]) / 2)

# --- 3. reescribir las listas de los 16 -----------------------------------
for t in types:
    t["compatible"] = []
    t["clashes"] = []

for (a, b), value in sorted(resolved.items()):
    if value == 0:
        continue
    if value > 0:
        by_id[a]["compatible"].append({"id": b, "bonus": value})
        by_id[b]["compatible"].append({"id": a, "bonus": value})
    else:
        by_id[a]["clashes"].append({"id": b, "penalty": value})
        by_id[b]["clashes"].append({"id": a, "penalty": value})

# --- 4. completar los pesos de peticion que faltaban ----------------------
# Los seis originales se respetan. Los cinco nuevos salen del eje que mas manda
# en cada uno, y se quedan en [0.3, 1.8] para que ninguno se apague del todo.
DERIVADOS = {
    "socialIntro":    lambda ax: 1.0 + ax["Attitude"] * 0.5,
    "activity":       lambda ax: 1.0 + ax["Energy"] * 0.5,
    "islandBuilding": lambda ax: 1.0 + ax["Outlook"] * 0.4,
    "confession":     lambda ax: 1.0 + (ax["Expression"] + ax["Outlook"]) * 0.25,
    "reconcile":      lambda ax: 1.0 + (ax["Attitude"] - ax["Energy"]) * 0.25,
}

for t in types:
    pesos = t["requestWeights"]
    for clave, formula in DERIVADOS.items():
        if clave not in pesos:
            pesos[clave] = round(min(1.8, max(0.3, formula(t["axes"]))), 2)

data["version"] = 2
data["notas"] = ("Simetria de afinidades y pesos derivados cerrados por "
                 "Docs/Agentes/cerrar-personalidades.py")

path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

compat = sum(len(t["compatible"]) for t in types) // 2
clash = sum(len(t["clashes"]) for t in types) // 2
print(f"{compat} parejas que congenian, {clash} que chocan")
if descartados:
    print(f"descartados por contradiccion irresoluble: {descartados}")
print(f"pesos de peticion por tipo: {len(types[0]['requestWeights'])}")
