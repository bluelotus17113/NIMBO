using System.Collections;
using System.IO;
using System.Text;
using Nimbo.Art.Audio;
using Nimbo.Core.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Saca los tres fondos a disco para poder **oírlos**.
    /// </summary>
    /// <remarks>
    /// Los tests dicen que la noche es más grave y que la fiesta se mueve más, y eso
    /// es todo lo que un test puede decir de una música. Si suena bien hay que
    /// escucharlo, igual que el arte hay que mirarlo — y para escucharlo hace falta un
    /// fichero.
    ///
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaMusica</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaMusica
    {
        private const string Salida = "Capturas";

        /// <summary>Un bucle entero de cada humor, con la misma semilla.</summary>
        [UnityTest]
        public IEnumerator GrabaLosTresFondos()
        {
            Directory.CreateDirectory(Salida);

            foreach (MusicMood mood in System.Enum.GetValues(typeof(MusicMood)))
            {
                var clip = SoundBank.BuildAmbience(seed: 7, mood: mood, seconds: 32f);
                string path = Path.Combine(Salida, $"musica_{mood}.wav".ToLowerInvariant());

                EscribirWav(clip, path);
                Debug.Log($"[CapturaMusica] {path} — {clip.length:0.0} s");

                Object.DestroyImmediate(clip);
                yield return null;
            }

            Assert.Pass();
        }

        /// <summary>
        /// WAV de 16 bits, mono. A mano porque Unity no sabe exportar un AudioClip y
        /// la cabecera son cuarenta y cuatro bytes.
        /// </summary>
        private static void EscribirWav(AudioClip clip, string path)
        {
            var muestras = new float[clip.samples * clip.channels];
            clip.GetData(muestras, 0);

            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream, Encoding.ASCII);

            int datos = muestras.Length * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + datos);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                                   // tamaño del bloque fmt
            writer.Write((short)1);                             // PCM sin comprimir
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);   // bytes por segundo
            writer.Write((short)(clip.channels * 2));           // bytes por muestra
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(datos);

            foreach (float muestra in muestras)
                writer.Write((short)(Mathf.Clamp(muestra, -1f, 1f) * short.MaxValue));
        }
    }
}
