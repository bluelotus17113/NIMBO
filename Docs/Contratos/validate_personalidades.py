#!/usr/bin/env python3
"""Valida personalidades.json — encargo de las 16 personalidades."""

import json
import sys
from pathlib import Path

JSON_PATH = Path(__file__).parent / "personalidades.json"


def load():
    with open(JSON_PATH) as f:
        return json.load(f)


def check_count(data):
    types = data["types"]
    assert len(types) == 16, f"Esperados 16 tipos, hay {len(types)}"
    indices = sorted(t["index"] for t in types)
    assert indices == list(range(16)), f"Índices incorrectos: {indices}"
    print("✓ 16 tipos con índices 0-15")


def check_axes(data):
    for t in data["types"]:
        idx = t["index"]
        e = 1 if t["axes"]["Energy"] > 0 else 0
        x = 1 if t["axes"]["Expression"] > 0 else 0
        a = 1 if t["axes"]["Attitude"] > 0 else 0
        o = 1 if t["axes"]["Outlook"] > 0 else 0
        expected = e | (x << 1) | (a << 2) | (o << 3)
        assert idx == expected, (
            f"Tipo {t['id']} índice={idx}, "
            f"ejes ({t['axes']['Energy']},{t['axes']['Expression']},"
            f"{t['axes']['Attitude']},{t['axes']['Outlook']}) "
            f"→ debería ser {expected}"
        )
    print("✓ Ejes coinciden con índices")


def check_ids_exist(data):
    ids = {t["id"] for t in data["types"]}
    for t in data["types"]:
        for c in t["compatible"]:
            assert c["id"] in ids, f"{t['id']}: compatible {c['id']} no existe"
            assert c["id"] != t["id"], f"{t['id']} compatible consigo mismo"
        for c in t["clashes"]:
            assert c["id"] in ids, f"{t['id']}: clash {c['id']} no existe"
            assert c["id"] != t["id"], f"{t['id']} choca consigo mismo"
    print("✓ IDs de compatibles y clashes existen y no son autoreferencias")


def check_compat_clash_no_overlap(data):
    """Un par de tipos no puede ser a la vez compatible y chocar."""
    for t in data["types"]:
        comp_ids = {c["id"] for c in t["compatible"]}
        clash_ids = {c["id"] for c in t["clashes"]}
        overlap = comp_ids & clash_ids
        assert not overlap, (
            f"{t['id']} tiene {overlap} como compatible Y clash"
        )
    print("✓ Ningún tipo es compatible y clash a la vez")


def check_symmetry(data):
    """Compatibilidades y clashes deben ser simétricos con el mismo número."""
    id_to_type = {t["id"]: t for t in data["types"]}
    for t in data["types"]:
        for c in t["compatible"]:
            target = id_to_type[c["id"]]
            mirror = [x for x in target["compatible"] if x["id"] == t["id"]]
            assert len(mirror) == 1, (
                f"Asimetría: {t['id']} compatible con {c['id']} pero no recíproco"
            )
            assert mirror[0]["bonus"] == c["bonus"], (
                f"Bonus asimétrico: {t['id']}→{c['id']}={c['bonus']} "
                f"pero {c['id']}→{t['id']}={mirror[0]['bonus']}"
            )
        for c in t["clashes"]:
            target = id_to_type[c["id"]]
            mirror = [x for x in target["clashes"] if x["id"] == t["id"]]
            assert len(mirror) == 1, (
                f"Asimetría: {t['id']} choca con {c['id']} pero no recíproco"
            )
            assert mirror[0]["penalty"] == c["penalty"], (
                f"Penalty asimétrico: {t['id']}→{c['id']}={c['penalty']} "
                f"pero {c['id']}→{t['id']}={mirror[0]['penalty']}"
            )
    print("✓ Compatibilidades y clashes son simétricos")


def check_need_averages(data):
    needs = ["hunger", "mood", "energy", "social", "hygiene"]
    for need in needs:
        avg = sum(t["needMultipliers"][need] for t in data["types"]) / 16
        assert 0.95 <= avg <= 1.05, (
            f"Media de {need} = {avg:.3f}, fuera de [0.95, 1.05]"
        )
        print(f"  {need}: media = {avg:.3f} ✓")


def check_ranges(data):
    for t in data["types"]:
        assert 0.7 <= t["walkSpeed"] <= 1.3, (
            f"{t['id']} walkSpeed={t['walkSpeed']} fuera de [0.7, 1.3]"
        )
        assert 0.5 <= t["idleDwell"] <= 5.0, (
            f"{t['id']} idleDwell={t['idleDwell']} sospechoso"
        )
        for need, val in t["needMultipliers"].items():
            assert 0.6 <= val <= 1.4, (
                f"{t['id']} need {need}={val} fuera de [0.6, 1.4]"
            )
        assert len(t["compatible"]) == 3, (
            f"{t['id']} tiene {len(t['compatible'])} compatibles (deben ser 3)"
        )
        assert len(t["clashes"]) == 2, (
            f"{t['id']} tiene {len(t['clashes'])} clashes (deben ser 2)"
        )
        for c in t["compatible"]:
            assert 2 <= c["bonus"] <= 8, (
                f"{t['id']} bonus={c['bonus']} fuera de [2,8]"
            )
        for c in t["clashes"]:
            assert -8 <= c["penalty"] <= -2, (
                f"{t['id']} penalty={c['penalty']} fuera de [-8,-2]"
            )
        assert len(t["emotions"]) == 3, f"{t['id']} emotions != 3"
        for mood in ["happy", "bored", "angry", "meeting"]:
            assert len(t["lines"][mood]) == 2, (
                f"{t['id']} lines.{mood} != 2 frases"
            )
    print("✓ Rangos de walkSpeed, idleDwell, needs, compat, clash, emotions, lines OK")


def main():
    data = load()
    errors = []
    checks = [
        check_count, check_axes, check_ids_exist,
        check_compat_clash_no_overlap, check_symmetry, check_ranges, check_need_averages
    ]
    for check in checks:
        try:
            check(data)
        except AssertionError as e:
            errors.append(str(e))
            print(f"✗ {e}")

    if errors:
        print(f"\n❌ {len(errors)} ERRORES")
        sys.exit(1)
    else:
        print("\n✅ Validación completa — todo correcto")


if __name__ == "__main__":
    main()
