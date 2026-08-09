using Nimbo.Core.Events;
using UnityEngine;

namespace Nimbo.Core.Settings
{
    /// <summary>
    /// Los volúmenes que el jugador ha elegido, y que sobreviven al cierre.
    /// </summary>
    /// <remarks>
    /// Vive en <c>Core</c> y no en la interfaz ni en el sonido a propósito: los dos
    /// necesitan las mismas tres claves, y si cada uno se escribiera las suyas, el
    /// día que alguien renombre una el ajuste se pierde en silencio — el menú
    /// seguiría enseñando el valor guardado y el sonido tirando del de por defecto.
    ///
    /// No va dentro de la partida. Es un ajuste de la máquina, no de la isla:
    /// empezar de cero no tiene por qué devolverte la música a todo volumen.
    /// </remarks>
    public static class AudioPrefs
    {
        private const string MusicKey = "nimbo.volumen.musica";
        private const string SfxKey   = "nimbo.volumen.efectos";
        private const string VoiceKey = "nimbo.volumen.voces";

        public const float DefaultMusic = 0.35f;
        public const float DefaultSfx   = 0.60f;
        public const float DefaultVoice = 0.70f;

        public static float Get(AudioChannel channel) => channel switch
        {
            AudioChannel.Music => PlayerPrefs.GetFloat(MusicKey, DefaultMusic),
            AudioChannel.Sfx   => PlayerPrefs.GetFloat(SfxKey, DefaultSfx),
            _                  => PlayerPrefs.GetFloat(VoiceKey, DefaultVoice),
        };

        /// <summary>Guarda y avisa. Quien esté sonando se entera por el evento.</summary>
        public static void Set(AudioChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyOf(channel), value);
            EventBus.Publish(new VolumeChanged(channel, value));
        }

        /// <summary>Escribe a disco. Se llama al salir del menú, no en cada arrastre.</summary>
        public static void Flush() => PlayerPrefs.Save();

        public static void ResetToDefaults()
        {
            Set(AudioChannel.Music, DefaultMusic);
            Set(AudioChannel.Sfx, DefaultSfx);
            Set(AudioChannel.Voice, DefaultVoice);
            Flush();
        }

        private static string KeyOf(AudioChannel channel) => channel switch
        {
            AudioChannel.Music => MusicKey,
            AudioChannel.Sfx   => SfxKey,
            _                  => VoiceKey,
        };
    }
}
