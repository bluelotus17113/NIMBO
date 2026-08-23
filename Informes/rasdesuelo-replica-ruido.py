#!/usr/bin/env python3
"""Réplica en float32 del ruido de ValueNoise.cs (Nimbo.Core.Util), para medir
el campo que siembra las flores del prado de la aldea.

Motivo: el veredicto rasdesuelo (23/08/2026, defecto 2) mide que el centro del
encuadre flores_a_ras NO es «el pico más alto» del campo, como decía un
comentario. Este script reproduce la medida de forma independiente.

El algoritmo está copiado a mano de Assets/_Project/Scripts/Core/Util/ValueNoise.cs
(Hash/At/Variation) respetando float32 en cada operación — Mathf.Lerp es
a + (b-a)*t con t ya en [0,1] aquí. La isla-aldea se siembra con
worldOffset = Vector3.zero y escala 11 (WorldView.cs:170-172, Meadow.cs:442),
así que las coordenadas son las del mundo directamente.

Uso: python3 Informes/rasdesuelo-replica-ruido.py
"""

import numpy as np

F = np.float32


def frac(x):
    return F(x - np.floor(x))


def hash2(px, py):
    x = frac(F(px * F(127.1)))
    y = frac(F(py * F(311.7)))
    d = F(x * F(x + F(34.23)) + y * F(y + F(34.23)))
    x = F(x + d)
    y = F(y + d)
    return frac(F(x * y * F(95.43)))


def lerp(a, b, t):
    return F(a + F(b - a) * t)


def at(px, py):
    ix = np.floor(px)
    iy = np.floor(py)
    fx = F(px - ix)
    fy = F(py - iy)
    fx = F(fx * fx * F(F(3) - F(2) * fx))
    fy = F(fy * fy * F(F(3) - F(2) * fy))
    a = hash2(ix, iy)
    b = hash2(F(ix + 1), iy)
    c = hash2(ix, F(iy + 1))
    d = hash2(F(ix + 1), F(iy + 1))
    return lerp(lerp(a, b, fx), lerp(c, d, fx), fy)


def variation(x, z):
    px = F(np.asarray(x, dtype=np.float32) / F(11))
    py = F(np.asarray(z, dtype=np.float32) / F(11))
    return F(at(px, py) * F(0.68) + at(F(px * F(2.7)), F(py * F(2.7))) * F(0.32))


def disco(radio, paso):
    ys = np.arange(-radio, radio + paso, paso, dtype=np.float32)
    xs = np.arange(-radio, radio + paso, paso, dtype=np.float32)
    xx, zz = np.meshgrid(xs, ys)
    dentro = xx * xx + zz * zz <= radio * radio
    return xx[dentro], zz[dentro]


def main():
    # 1) Pico global sobre el disco de la aldea (radio 100, Archipelago.VillageRadius).
    xs, zs = disco(100.0, 0.25)
    v = variation(xs, zs)
    i = int(np.argmax(v))
    print(f"pico global (malla 0,25 m): Variation={float(v[i]):.4f} "
          f"en ({float(xs[i]):.2f}, {float(zs[i]):.2f})")

    # Refino alrededor del argmax con paso fino.
    cx, cz = float(xs[i]), float(zs[i])
    xs2, zs2 = disco(1.0, 0.01)
    xs2, zs2 = F(xs2 + F(cx)), F(zs2 + F(cz))
    v2 = variation(xs2, zs2)
    j = int(np.argmax(v2))
    print(f"pico refinado (malla 0,01 m): Variation={float(v2[j]):.4f} "
          f"en ({float(xs2[j]):.2f}, {float(zs2[j]):.2f})")

    # 2) Valor en el centro elegido para el encuadre flores_a_ras.
    elegido = variation(F(54.5), F(-11.5))
    print(f"Variation en el centro elegido (54.5, -11.5): {float(elegido):.4f}")

    # 3) Estadísticas locales: círculo de 5 m alrededor del centro elegido.
    lx, lz = disco(5.0, 0.05)
    lv = variation(F(lx + F(54.5)), F(lz + F(-11.5)))
    print(f"círculo 5 m alrededor del elegido: {100.0 * float(np.mean(lv >= F(0.70))):.1f} % "
          f"con Variation ≥ 0,70 · media {float(np.mean(lv)):.3f}")

    # 4) Lo mismo alrededor del pico verdadero, por si se quisiera la foto canónica.
    px_, pz_ = float(xs2[j]), float(zs2[j])
    qx, qz = disco(5.0, 0.05)
    qv = variation(F(qx + F(px_)), F(qz + F(pz_)))
    print(f"círculo 5 m alrededor del pico:    {100.0 * float(np.mean(qv >= F(0.70))):.1f} % "
          f"con Variation ≥ 0,70 · media {float(np.mean(qv)):.3f}")


if __name__ == "__main__":
    main()
