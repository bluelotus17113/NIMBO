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
            _ => Blip("click", new[] { 880f }, 0.05f, 0.18f),
        };

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
