using System;
using UnityEngine;

namespace Nimbo.Events.Scheduling
{
    /// <summary>
    /// Probabilidades y aforos del sistema de eventos. Los números se ajustan desde
    /// el inspector y se guardan como asset.
    /// </summary>
    [CreateAssetMenu(fileName = "EventsConfig", menuName = "Isla Nimbo/Configuración de eventos")]
    public sealed class EventsConfig : ScriptableObject
    {
        [Header("Planificación")]
        [Tooltip("Probabilidad base de que salga un evento en su ventana, por hora.")]
        [Range(0f, 1f)] public float BaseChancePerHour = 0.30f;

        [Tooltip("Mínimo de horas entre el final de un evento y el siguiente.")]
        [Min(0)] public int CooldownHours = 3;

        [Tooltip("Un evento dura como mucho esto, en horas de juego. Después se resuelve solo.")]
        [Min(1)] public int MaxEventDurationHours = 4;

        [Header("Concierto")]
        [Tooltip("Cuántos isleños como máximo van al concierto (sin contar al artista).")]
        [Min(1)] public int ConcertMaxAudience = 6;

        [Tooltip("Ánimo que reparte el concierto entre los asistentes.")]
        [Range(0f, 30f)] public float ConcertMoodBoost = 10f;

        [Tooltip("Afinidad extra entre asistentes que comparten concierto.")]
        [Range(0f, 10f)] public float ConcertAffinityBoost = 3f;

        [Header("Sueños")]
        [Tooltip("Probabilidad de que un isleño dormido tenga un sueño, por noche.")]
        [Range(0f, 1f)] public float DreamChancePerNight = 0.40f;

        [Tooltip("Los soñadores (Outlook > 0) multiplican la probabilidad base por esto.")]
        [Range(1f, 3f)] public float DreamerMultiplier = 1.5f;

        [Tooltip("Afinidad que mueve un sueño —positiva o negativa según el tono—.")]
        [Range(0f, 5f)] public float DreamAffinityDelta = 2f;

        [Header("Tablón de noticias")]
        [Tooltip("Máximo de titulares que guarda el tablón.")]
        [Min(1)] public int MaxHeadlines = 20;
    }
}
