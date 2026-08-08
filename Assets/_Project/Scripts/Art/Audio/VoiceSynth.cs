using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.Audio
{
    /// <summary>
    /// Sintetiza la voz balbuceada de un habitante a partir de su
    /// <see cref="VoiceConfig"/> y de lo que dice.
    /// </summary>
    /// <remarks>
    /// No pronuncia palabras: hace sílabas sin sentido con la entonación de la frase.
    /// Es lo que hace el género y no es una limitación técnica, es la decisión de
    /// diseño correcta — una voz de verdad envejece a la décima vez que la oyes y
    /// obliga a grabar cada línea; un balbuceo se puede generar para cualquier texto
    /// y sigue teniendo carácter a las mil.
    ///
    /// La onda se genera con dos formantes y un poco de ruido. Suena a juguete
    /// porque tiene que sonar a juguete.
    /// </remarks>
    public static class VoiceSynth
    {
        public const int SampleRate = 22050;

        /// <summary>Duración de una sílaba, en segundos.</summary>
        private const float SyllableSeconds = 0.11f;

        /// <summary>Una frase hablada por ese habitante. El clip es de un solo uso.</summary>
        public static AudioClip Speak(in VoiceConfig voice, string line, string ownerId = null)
        {
            int syllables = Mathf.Clamp(CountSyllables(line), 1, 14);
            float speed = Mathf.Clamp(voice.Speed, 0.6f, 1.6f);
            float perSyllable = SyllableSeconds / speed;

            int total = Mathf.CeilToInt(SampleRate * perSyllable * syllables);
            var samples = new float[total];

            // La semilla sale del habitante y de la frase: la misma persona diciendo
            // lo mismo suena igual, y dos personas distintas nunca suenan iguales.
            var rng = Rng.FromSeed($"{ownerId}|{line}");

            float baseHz = PitchHz(voice.Pitch);
            int perSyllableSamples = Mathf.Max(1, total / syllables);

            for (int s = 0; s < syllables; s++)
            {
                // Entonación: sube un poco en medio y baja al final, como una frase.
                float t = syllables == 1 ? 0.5f : (float)s / (syllables - 1);
                float contour = Mathf.Sin(t * Mathf.PI) * 0.18f - t * 0.12f;
                float hz = baseHz * (1f + contour + rng.Range(-0.05f, 0.05f));

                int start = s * perSyllableSamples;
                int end = Mathf.Min(total, start + perSyllableSamples);
                WriteSyllable(samples, start, end, hz, voice, ref rng);
            }

            var clip = AudioClip.Create($"voz_{ownerId ?? "x"}", total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void WriteSyllable(float[] samples, int start, int end, float hz,
                                          in VoiceConfig voice, ref Rng rng)
        {
            int length = end - start;
            if (length <= 0) return;

            // Dos formantes: el segundo es lo que hace que suene a vocal y no a pitido.
            float formant = rng.Range(1.6f, 3.1f);
            float nasal = Mathf.Clamp01(voice.Nasal);
            float warble = Mathf.Clamp01(voice.Warble);

            for (int i = 0; i < length; i++)
            {
                float p = (float)i / length;
                float time = (start + i) / (float)SampleRate;

                // Envolvente con ataque y caída rápidos: sin ella cada sílaba chasca.
                float envelope = Mathf.Min(p / 0.15f, (1f - p) / 0.35f);
                envelope = Mathf.Clamp01(envelope);

                // El vibrato es lo cantarín de la voz.
                float vibrato = 1f + Mathf.Sin(time * 34f) * warble * 0.045f;
                float phase = time * hz * vibrato * Mathf.PI * 2f;

                float wave = Mathf.Sin(phase) * 0.62f
                           + Mathf.Sin(phase * formant) * 0.24f * (0.4f + nasal * 0.6f)
                           + Mathf.Sin(phase * 0.5f) * 0.14f;

                samples[start + i] = wave * envelope * 0.34f;
            }
        }

        /// <summary>La frecuencia base de cada tono de voz, en hercios.</summary>
        private static float PitchHz(VoicePitch pitch) => pitch switch
        {
            VoicePitch.VeryLow => 132f,
            VoicePitch.Low => 168f,
            VoicePitch.Mid => 214f,
            VoicePitch.High => 268f,
            _ => 330f,
        };

        /// <summary>
        /// Cuenta sílabas a ojo: grupos de vocales. No hace falta que sea exacto —
        /// solo que una frase larga suene más larga que una corta.
        /// </summary>
        public static int CountSyllables(string line)
        {
            if (string.IsNullOrEmpty(line)) return 1;

            int count = 0;
            bool inVowel = false;

            for (int i = 0; i < line.Length; i++)
            {
                bool vowel = IsVowel(line[i]);
                if (vowel && !inVowel) count++;
                inVowel = vowel;
            }
            return Mathf.Max(1, count);
        }

        private static bool IsVowel(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'a': case 'e': case 'i': case 'o': case 'u':
                case 'á': case 'é': case 'í': case 'ó': case 'ú':
                case 'ü':
                    return true;
                default:
                    return false;
            }
        }
    }
}
