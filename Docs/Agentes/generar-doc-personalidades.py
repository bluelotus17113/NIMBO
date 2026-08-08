#!/usr/bin/env python3
"""Genera Docs/03_PERSONALIDADES.md a partir del JSON.

Se genera en vez de escribirse a mano porque el encargo pedia que los dos ficheros
dijeran lo mismo, y la unica forma de garantizarlo es que uno salga del otro.
"""
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
data = json.loads((ROOT / "Contratos" / "personalidades.json").read_text(encoding="utf-8"))
types = sorted(data["types"], key=lambda t: t["index"])
name_of = {t["id"]: t["name"] for t in types}

EJES = [
    ("Energy", "Calmado", "Enérgico"),
    ("Expression", "Reservado", "Expresivo"),
    ("Attitude", "Independiente", "Sociable"),
    ("Outlook", "Práctico", "Soñador"),
]

out = []
w = out.append

w("# Isla Nimbo — Las 16 personalidades\n")
w("> Generado desde `Docs/Contratos/personalidades.json` por")
w("> `Docs/Agentes/generar-doc-personalidades.py`. **No editar a mano**: se edita el")
w("> JSON y se vuelve a generar, que es lo que mantiene a los dos diciendo lo mismo.\n")

w("## Cómo funciona\n")
w("Cuatro ejes continuos en `[-1, 1]`. El signo de los cuatro da uno de dieciséis")
w("tipos, y el tipo no se guarda nunca: se deriva. Así el creador de personajes solo")
w("mueve deslizadores y el tipo cae solo.\n")
w("| Eje | −1 | +1 | Bit |")
w("|---|---|---|---|")
for i, (axis, low, high) in enumerate(EJES):
    w(f"| `{axis}` | {low} | {high} | {1 << i} |")
w("")
w("```")
w("index = (Energy>0 ? 1:0) | (Expression>0 ? 2:0) | (Attitude>0 ? 4:0) | (Outlook>0 ? 8:0)")
w("```\n")

w("Un habitante con los cuatro ejes cerca de cero es de su tipo «a medias» y se")
w("comporta de forma más neutra: la intensidad modula, el tipo clasifica.\n")

w("## Tabla resumen\n")
w("| # | Tipo | Ejes | Frase |")
w("|---|---|---|---|")
for t in types:
    signos = " ".join(("+" if t["axes"][a] > 0 else "−") + a[0] for a, _, _ in EJES)
    w(f"| {t['index']} | **{t['name']}** | {signos} | *{t['tagline']}* |")
w("")

w("## Ficha de cada tipo\n")
for t in types:
    ax = t["axes"]
    w(f"### {t['index']}. {t['name']}  \n")
    w(f"*«{t['tagline']}»*\n")
    w("| | |")
    w("|---|---|")
    w("| Identificador | `" + t["id"] + "` |")
    rasgos = ", ".join((high if ax[a] > 0 else low).lower() for a, low, high in EJES)
    w(f"| Ejes | {rasgos} |")
    w(f"| Al andar | ×{t['walkSpeed']}, se para {t['idleDwell']} s entre destinos |")
    voz = t.get("voice", {})
    w(f"| Voz | tono {voz.get('pitch', '?')}, ritmo {voz.get('pace', '?')} |")
    w(f"| Emociones | {', '.join(t.get('emotions', []))} |")
    w("")

    w("**Necesidades** — multiplicador de lo rápido que se le vacían:\n")
    nm = t["needMultipliers"]
    w("| " + " | ".join(nm.keys()) + " |")
    w("|" + "---|" * len(nm))
    w("| " + " | ".join(f"{v}" for v in nm.values()) + " |\n")

    pesos = t["requestWeights"]
    pide = sorted(pesos.items(), key=lambda kv: -kv[1])[:3]
    calla = sorted(pesos.items(), key=lambda kv: kv[1])[:2]
    w("**Qué pide** — " + ", ".join(f"`{k}` ×{v}" for k, v in pide) + ".  ")
    w("**Qué casi nunca pide** — " + ", ".join(f"`{k}` ×{v}" for k, v in calla) + ".\n")

    if t["compatible"]:
        w("**Congenia con** — " + ", ".join(
            f"{name_of[c['id']]} (+{c['bonus']})" for c in t["compatible"]) + ".  ")
    if t["clashes"]:
        w("**Choca con** — " + ", ".join(
            f"{c_['id'] and name_of[c_['id']]} ({c_['penalty']})" for c_ in t["clashes"]) + ".\n")

    w("**Frases**\n")
    etiquetas = {"happy": "Contento", "bored": "Aburrido",
                 "angry": "Enfadado", "meeting": "Al conocer a alguien"}
    for clave, etiqueta in etiquetas.items():
        for frase in t["lines"].get(clave, []):
            w(f"- *{etiqueta}:* «{frase}»")
    w("")

w("## Cómo se implementa\n")
w("Cada tipo es un fichero en `Assets/_Project/Scripts/Personality/Types/`, con una")
w("clase que hereda de `PersonalityBehaviourBase` y rellena una")
w("`PersonalityDefinition`. Un fichero por tipo, para que dieciséis manos puedan")
w("trabajar sin pisarse. El test `PersonalityTablesMatchJson` comprueba que los")
w("números del código siguen siendo los de este documento.\n")

path = ROOT / "03_PERSONALIDADES.md"
path.write_text("\n".join(out), encoding="utf-8")
print(f"{path} — {len(out)} lineas, {len(types)} tipos")
