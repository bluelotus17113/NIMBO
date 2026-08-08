using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    public enum VoicePitch { VeryLow = 0, Low = 1, Mid = 2, High = 3, VeryHigh = 4 }

    /// <summary>Cómo suena un habitante. La voz se sintetiza, no se graba.</summary>
    [Serializable]
    public struct VoiceConfig
    {
        public VoicePitch Pitch;
        [Range(0.6f, 1.6f)] public float Speed;
        [Range(0f, 1f)] public float Warble;   // vibrato: 0 plano, 1 cantarín
        [Range(0f, 1f)] public float Nasal;

        public static VoiceConfig Default => new VoiceConfig
        {
            Pitch = VoicePitch.Mid, Speed = 1f, Warble = 0.35f, Nasal = 0.2f,
        };
    }

    /// <summary>
    /// Quién es. Lo que no cambia nunca (el <see cref="Id"/>) y lo que el jugador
    /// escribió al crearlo.
    /// </summary>
    [Serializable]
    public struct IslanderIdentity
    {
        public string Id;            // GUID, la clave de todo el juego
        public string DisplayName;   // "Marta"
        public string Nickname;      // cómo la llaman los demás; vacío = DisplayName
        public string Birthday;      // "03-14", solo mes y día
        public bool IsPlayerAvatar;

        public string ShortName => string.IsNullOrEmpty(Nickname) ? DisplayName : Nickname;
    }
}
