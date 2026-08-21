#ifndef NIMBO_ANIME_INCLUDED
#define NIMBO_ANIME_INCLUDED

// El estilo de la isla, en un sitio, porque lo comparten el shader de todo y el de
// la vegetación y no pueden discrepar: si el prado y la brizna que crece encima
// dibujan la sombra de dos maneras, se ve la junta.
//
// La regla es una sola y viene de las referencias (AnimeGrass y AnimeTree). En
// Blender es «Diffuse BSDF → Shader to RGB → ColorRamp»: coger la luz, olvidar que
// es luz y usarla como índice en una rampa de colores **elegidos a mano**. No es
// albedo por luz. Por eso una copa de árbol en un fondo de anime tiene cuatro
// verdes y no un degradado: son cuatro colores pintados, no uno oscurecido.
//
// Las cuatro bandas y dónde cambian salen medidas de la rampa del follaje de la
// referencia. Las transiciones son estrechas pero no duras —0,072 y 0,061 de
// ancho—: eso es lo que separa el dibujo pintado del cel-shading de plástico.

// ── ruido de valor ────────────────────────────────────────────────────────────
//
// Para la variación de color a gran escala (el punto 2 de la lámina). Sin ella un
// prado es una mancha lisa del mismo verde hasta el horizonte, que es exactamente
// lo que se veía antes.

float NimboHash(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p + 34.23);
    return frac(p.x * p.y * 95.43);
}

float NimboNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = NimboHash(i);
    float b = NimboHash(i + float2(1, 0));
    float c = NimboHash(i + float2(0, 1));
    float d = NimboHash(i + float2(1, 1));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

/// Dos octavas: la ancha hace las manchas y la corta les rompe el borde.
float NimboVariation(float3 positionWS, float scale)
{
    float2 p = positionWS.xz / max(scale, 0.001);
    return NimboNoise(p) * 0.68 + NimboNoise(p * 2.7) * 0.32;
}

// ── el viento ─────────────────────────────────────────────────────────────────
//
// En la referencia son dos nodos de ruido 4D: uno gira la hoja sobre su propio eje
// y otro la empuja. Aquí, en un shader de vértices, la ráfaga sale del **ruido
// evaluado en la posición del mundo**, no de un seno del tiempo. Con un seno todo
// el prado se mueve a la vez y se lee como una tela; con el ruido la racha cruza
// el prado y cada mata la coge cuando le toca.
//
// El peso es la máscara raíz-punta al cuadrado: la raíz no se despega del suelo,
// que es el fallo que convierte un césped en un montón de briznas patinando.

float3 NimboWind(float3 positionWS, half mask, float2 direction,
                 float scale, float speed, float strength)
{
    if (strength <= 0.0001) return 0;

    float2 drift = direction * (speed * _Time.y);
    float gust = NimboNoise(positionWS.xz / max(scale, 0.001) + drift) - 0.5;

    // La segunda octava, más corta y más rápida, es el temblor de la hoja suelta.
    float flutter = NimboNoise(positionWS.xz * (3.7 / max(scale, 0.001)) + drift * 2.6) - 0.5;

    float amount = (gust + flutter * 0.35) * strength * mask * mask;

    // Se hunde un poco al doblarse: una brizna que solo se desplaza de lado se
    // estira, y una hierba que se estira parece de goma.
    return float3(direction.x * amount, -abs(amount) * 0.25, direction.y * amount);
}

// ── la rampa ──────────────────────────────────────────────────────────────────

struct NimboBands
{
    half3 deep;      // el fondo del follaje, donde no llega ni el rebote
    half3 shadow;    // la banda plana de sombra
    half3 lit;       // el color propio del objeto
    half3 high;      // el filo por donde entra el sol
    half  edgeLow;   // dónde salta de sombra a luz
    half  softLow;   // y en cuánto: estrecho, no un degradado
    half  edgeHigh;
    half  softHigh;
};

/// Un escalón suave centrado en <c>edge</c> y de ancho <c>soft</c>.
half NimboStep(half edge, half soft, half x)
{
    half half_ = max(soft, 0.0001h) * 0.5h;
    return smoothstep(edge - half_, edge + half_, x);
}

/// La pareja de colores de la mancha, aplicada sobre el color ya pintado.
///
/// En la referencia la variación es una mezcla entre **dos** verdes elegidos —«cool
/// feature, choose the right pair»— hecha antes del sombreado. Aquí la pareja no se
/// elige aparte: son los dos extremos de la propia rampa, escritos como multiplicador
/// del color de luz. Sale la misma mancha fría-oscura contra cálida-clara, con dos
/// colores menos que mantener a mano, y no puede salirse de la paleta porque está
/// hecha de la paleta.
///
/// Multiplicar y no mezclar es lo que hace que la mancha se vea igual en la banda de
/// sombra que en la de luz: mezclando, la mancha desaparecía justo donde el terreno
/// entra en sombra y se veía el corte.
half3 NimboMottle(half3 colour, half3 cool, half3 warm, half noise, half amount)
{
    half3 tint = lerp(warm, cool, noise);
    return colour * lerp(half3(1.0h, 1.0h, 1.0h), tint, amount);
}

/// El color pintado para ese nivel de luz y esa oclusión.
half3 NimboShade(NimboBands bands, half level, half occlusion)
{
    half3 colour = lerp(bands.shadow, bands.lit, NimboStep(bands.edgeLow, bands.softLow, level));
    colour = lerp(colour, bands.high, NimboStep(bands.edgeHigh, bands.softHigh, level));

    // La banda profunda entra por dos caminos: donde la luz no llega nada y donde
    // la geometría se tapa a sí misma. En la referencia son dos nodos distintos
    // —el principio de la rampa y un Ambient Occlusion multiplicado— y hacen lo
    // mismo, así que aquí es un solo número.
    half deepness = occlusion * smoothstep(0.0h, 0.11h, level);
    return lerp(bands.deep, colour, deepness);
}

#endif
