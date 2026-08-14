using System.Collections.Generic;
using Nimbo.Core.Util;
using UnityEngine;

namespace Nimbo.Art.Audio
{
    /// <summary>Los efectos del juego. Son pocos a propósito: un juego tranquilo no pita.</summary>
    public enum Sfx
    {
        Click = 0,
        Coin = 1,
        Levelup = 2,
        Place = 3,      // colocar un mueble
        Deny = 4,       // algo no se puede
        Happy = 5,      // el habitante se alegra
        Sad = 6,
        Unlock = 7,     // se abre una zona

        // Los verbos del protagonista. Iban mudos: el sonido escuchaba a la
        // simulación —monedas, ascensos, peticiones— y no a las manos de quien juega,
        // así que talar un abedul de seis hachazos no hacía ruido ninguno.
        Chop = 8,       // el hacha en la madera
        Mine = 9,       // el pico en la piedra
        Pick = 10,      // coger algo a mano
        Harvest = 11,   // el nodo cae, o se recoge del huerto
        Craft = 12,     // sale algo de la mesa de trabajo
        Sleep = 13,     // se duerme y amanece
    }

    /// <summary>
    /// Genera los efectos por síntesis y los guarda. Ningún fichero de audio en el
    /// proyecto, igual que no hay ningún modelo 3D.
    /// </summary>
    /// <remarks>
    /// Los clips se crean una vez y se reutilizan: son pocos, cortos y siempre
    /// iguales. Generar uno por cada clic llenaría la memoria de clips idénticos.
    /// </remarks>
    public static class SoundBank
    {
        public const int SampleRate = 22050;

        private static readonly Dictionary<Sfx, AudioClip> Cache = new Dictionary<Sfx, AudioClip>(8);

        public static AudioClip Get(Sfx sfx)
        {
            if (Cache.TryGetValue(sfx, out var cached) && cached != null) return cached;

            var clip = Build(sfx);
            Cache[sfx] = clip;
            return clip;
        }

        public static void Clear()
        {
            foreach (var clip in Cache.Values) Discard(clip);
            Cache.Clear();
        }

        /// <summary>
        /// Destruye de la forma que toque. <c>Destroy</c> no hace nada fuera de modo
        /// juego y además suelta un error: en el editor y en los tests hay que usar
        /// <c>DestroyImmediate</c>, y sin esto los clips se quedaban colgados.
        /// </summary>
        private static void Discard(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Object.Destroy(asset);
            else Object.DestroyImmediate(asset);
        }


        private static AudioClip Build(Sfx sfx) => sfx switch
        {
            // Notas cortas y redondas. Las escalas son mayores o pentatónicas: no hay
            // ni un intervalo disonante en todo el juego, ni siquiera para decir «no».
            Sfx.Click => Blip("click", new[] { 880f }, 0.05f, 0.18f),
            Sfx.Coin => Blip("moneda", new[] { 1046f, 1568f }, 0.09f, 0.24f),
            Sfx.Levelup => Blip("nivel", new[] { 523f, 659f, 784f, 1046f }, 0.34f, 0.26f),
            Sfx.Place => Blip("colocar", new[] { 392f, 523f }, 0.10f, 0.20f),

            // Para «no se puede», dos notas bajas descendentes: se entiende sin ser
            // desagradable. Un zumbido de error rompería el tono del juego entero.
            Sfx.Deny => Blip("no", new[] { 330f, 262f }, 0.14f, 0.18f),

            Sfx.Happy => Blip("contento", new[] { 659f, 880f }, 0.16f, 0.22f),
            Sfx.Sad => Blip("triste", new[] { 440f, 349f }, 0.24f, 0.18f),
            Sfx.Unlock => Blip("abrir", new[] { 523f, 784f, 1046f, 1319f }, 0.5f, 0.24f),

            // Golpear no es cantar: estos van con ruido y no con notas. Un hachazo
            // hecho de senos suena a xilófono, y lo que tiene que sonar es a madera.
            // La nota grave debajo es lo que le da el cuerpo y lo distingue del pico.
            Sfx.Chop => Thud("hacha", 150f, 0.16f, 0.55f, 0.30f),
            Sfx.Mine => Thud("pico", 260f, 0.13f, 0.30f, 0.26f),
            Sfx.Pick => Thud("coger", 420f, 0.09f, 0.72f, 0.15f),

            Sfx.Harvest => Blip("recoger", new[] { 784f, 1046f }, 0.14f, 0.22f),
            Sfx.Craft => Blip("crafteo", new[] { 587f, 784f, 1175f }, 0.28f, 0.24f),

            // Dormir baja y se apaga: es la única señal descendente amable del juego.
            Sfx.Sleep => Blip("dormir", new[] { 523f, 392f, 262f }, 0.6f, 0.20f),

            _ => Blip("click", new[] { 880f }, 0.05f, 0.18f),
        };

        /// <summary>
        /// Un golpe: ruido filtrado sobre una nota grave, apagándose deprisa.
        /// </summary>
        /// <remarks>
        /// El ruido lleva un filtro paso bajo de un polo, que es la diferencia entre
        /// «madera» y «estática de radio». <paramref name="brightness"/> es cuánto
        /// deja pasar: alto para coger una flor, bajo para un hachazo.
        ///
        /// La semilla es fija, así que el mismo golpe suena siempre igual. Sortearla
        /// haría que dos hachazos seguidos sonaran a dos materiales distintos.
        /// </remarks>
        private static AudioClip Thud(string name, float tone, float seconds,
                                      float brightness, float volume)
        {
            int total = Mathf.CeilToInt(SampleRate * seconds);
            var samples = new float[total];
            var rng = Rng.FromSeed(name);

            float filtered = 0f;

            for (int i = 0; i < total; i++)
            {
                float p = (float)i / total;
                float time = (float)i / SampleRate;

                // Ataque instantáneo y caída rápida: es lo que hace que se lea como
                // un impacto y no como una ráfaga de viento.
                float envelope = Mathf.Exp(-p * 9f);

                float noise = rng.Range(-1f, 1f);
                filtered += (noise - filtered) * brightness;

                float body = Mathf.Sin(time * tone * Mathf.PI * 2f);

                samples[i] = (filtered * 0.6f + body * 0.4f) * envelope * volume;
            }

            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Una secuencia de notas con envolvente suave. El armónico a la octava es lo
        /// que hace que suene a campana de juguete y no a onda pelada.
        /// </summary>
        private static AudioClip Blip(string name, float[] notes, float seconds, float volume)
        {
            int total = Mathf.CeilToInt(SampleRate * seconds);
            var samples = new float[total];
            int perNote = Mathf.Max(1, total / notes.Length);

            for (int n = 0; n < notes.Length; n++)
            {
                int start = n * perNote;
                int end = Mathf.Min(total, start + perNote);

                for (int i = start; i < end; i++)
                {
                    float p = (float)(i - start) / (end - start);
                    float time = (float)i / SampleRate;

                    // Caída exponencial: el ataque instantáneo y el apagado suave son
                    // lo que suena a percusión afinada.
                    float envelope = Mathf.Exp(-p * 4.5f) * Mathf.Clamp01(p / 0.02f);
                    float phase = time * notes[n] * Mathf.PI * 2f;

                    samples[i] = (Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f)
                               * envelope * volume;
                }
            }

            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Un fondo ambiental en bucle: acordes lentos sobre una escala pentatónica.
        /// </summary>
        /// <remarks>
        /// Pentatónica porque en ella no hay combinación de notas que suene mal, así
        /// que se puede sortear el orden y nunca desafina. Es el truco viejo de la
        /// música generativa y aquí encaja: la isla suena distinta cada partida y
        /// siempre suena bien.
        /// </remarks>
        public static AudioClip BuildAmbience(uint seed, float seconds = 32f)
        {
            // Do mayor pentatónica, dos octavas.
            float[] scale = { 261.6f, 293.7f, 329.6f, 392f, 440f,
                              523.3f, 587.3f, 659.3f, 784f, 880f };

            int total = Mathf.CeilToInt(SampleRate * seconds);
            var samples = new float[total];
            var rng = new Rng(seed);

            const float NoteSeconds = 2.6f;
            int noteSamples = Mathf.CeilToInt(SampleRate * NoteSeconds);
            int noteCount = Mathf.CeilToInt((float)total / noteSamples);

            for (int n = 0; n < noteCount; n++)
            {
                float root = scale[rng.Range(0, 5)];
                float high = scale[rng.Range(4, scale.Length)];

                int start = n * noteSamples;
                int end = Mathf.Min(total, start + noteSamples);

                for (int i = start; i < end; i++)
                {
                    float p = (float)(i - start) / (end - start);
                    float time = (float)i / SampleRate;

                    // Entra y sale suave: las notas se solapan y no hay cortes.
                    float envelope = Mathf.Sin(p * Mathf.PI) * 0.5f;

                    samples[i] += (Mathf.Sin(time * root * Mathf.PI * 2f) * 0.6f
                                 + Mathf.Sin(time * high * Mathf.PI * 2f) * 0.3f)
                                * envelope * 0.09f;
                }
            }

            // Rampa en los extremos para que el bucle no chasque al volver al principio.
            int fade = Mathf.Min(SampleRate / 2, total / 4);
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                samples[i] *= k;
                samples[total - 1 - i] *= k;
            }

            var clip = AudioClip.Create("ambiente", total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
