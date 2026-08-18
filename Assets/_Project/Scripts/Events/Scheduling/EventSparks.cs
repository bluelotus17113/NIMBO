using System;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;

namespace Nimbo.Events.Scheduling
{
    /// <summary>
    /// En una fiesta la gente coincide, y de ahí sale algo (§15.3).
    /// </summary>
    /// <remarks>
    /// Es lo que hace que organizar un evento valga la pena, y es también el límite de
    /// lo que el jugador puede hacer con la vida ajena: **no empareja a nadie**, monta
    /// el sitio donde se conocen. Tú tiras la piedra; las ondas las hace el agua.
    ///
    /// **Un flechazo por fiesta como mucho**, el de la pareja que mejor pega. Tirando
    /// por cada par de asistentes salen sesenta y seis tiradas en una isla llena, y la
    /// mitad de la aldea se enamoraría el mismo sábado: eso no es una fiesta memorable,
    /// es un sorteo. Con uno solo, la fiesta deja **una** historia, que es lo que luego
    /// se cuenta.
    ///
    /// Y sube el aprecio de todos con todos un poco, que es lo que de verdad hace una
    /// fiesta: la gente se conoce aunque no pase nada más.
    /// </remarks>
    public sealed class EventSparks : IDisposable
    {
        private readonly SocialConfigView _config;
        private readonly Action<EventDefinition> _onEnded;
        private readonly EventScheduler _scheduler;

        /// <summary>Lo que se caen mejor por haber estado en lo mismo.</summary>
        private const float MinglingAffinity = 4f;

        public EventSparks(EventScheduler scheduler, SocialConfigView config = null)
        {
            _scheduler = scheduler;
            _config = config ?? SocialConfigView.Default;

            _onEnded = OnEventEnded;
            _scheduler.EventEnded += _onEnded;
        }

        public void Dispose() => _scheduler.EventEnded -= _onEnded;

        private void OnEventEnded(EventDefinition def)
        {
            if (def == null || def.IsTriggered) return;

            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;
            if (!ServiceRegistry.TryGet<ISocialService>(out var social)) return;
            if (!ServiceRegistry.TryGet<IPersonalityService>(out var personalities)) return;

            var all = registry.All;
            if (all.Count < 2) return;

            string bestA = null, bestB = null;
            float best = float.MinValue;

            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    var a = all[i];
                    var b = all[j];

                    // Haber estado en lo mismo acerca, pase lo que pase. Es la mitad
                    // silenciosa de una fiesta y la que se nota a las semanas.
                    social.ApplyAffinity(a.Id, b.Id, MinglingAffinity);

                    if (!Free(social, a) || !Free(social, b)) continue;

                    float compatibility = Compatibility(personalities, a, b);
                    if (compatibility < _config.CrushMinCompatibility) continue;

                    // La afinidad de después: la fiesta acaba de sumar, y contarla ya
                    // subida es lo que permite que una noche buena sea el empujón que
                    // faltaba en vez de quedarse a las puertas.
                    float affinity = social.GetRelationship(a.Id, b.Id).Affinity;
                    if (affinity < _config.CrushMinAffinity) continue;

                    float score = compatibility * 100f + affinity;
                    if (score <= best) continue;

                    best = score;
                    bestA = a.Id;
                    bestB = b.Id;
                }
            }

            if (bestA == null) return;

            Spark(registry, bestA, bestB);
        }

        /// <summary>Le nace el flechazo al que mejor lo tenía de los dos.</summary>
        /// <remarks>
        /// A uno y no a los dos: un flechazo correspondido de golpe se salta el tramo
        /// más bonito de la rama romántica, que es el rato en que uno de los dos todavía
        /// no lo sabe.
        /// </remarks>
        private static void Spark(IIslanderRegistry registry, string aId, string bId)
        {
            if (!registry.TryGet(aId, out var a)) return;

            var book = a.Relationships;
            var record = book.GetOrCreate(bId);
            if (record.Romance != RomanceStage.None) return;

            record.Romance = RomanceStage.Crush;
            book.Set(record);

            EventBus.Publish(new RomanceStageChanged(aId, bId, RomanceStage.Crush));
        }

        private static bool Free(ISocialService social, IslanderData islander) =>
            social.PartnerOf(islander.Id) == null;

        private static float Compatibility(IPersonalityService personalities,
                                           IslanderData a, IslanderData b)
        {
            int bias = personalities.CompatibilityBetween(
                a.Personality.TypeIndex, b.Personality.TypeIndex);

            // La misma cuenta que usa el módulo social, copiada aquí porque el de
            // eventos no lo ve. Si algún día divergen, el sitio bueno es el social.
            float axes = 1f - (
                Math.Abs(a.Personality.Energy - b.Personality.Energy) * 0.20f +
                Math.Abs(a.Personality.Expression - b.Personality.Expression) * 0.20f +
                Math.Abs(a.Personality.Attitude - b.Personality.Attitude) * 0.35f +
                Math.Abs(a.Personality.Outlook - b.Personality.Outlook) * 0.25f);

            return Math.Clamp(axes + bias * 0.05f, -1f, 1f);
        }
    }

    /// <summary>Los dos umbrales de flechazo, para no arrastrar el módulo social entero.</summary>
    public sealed class SocialConfigView
    {
        public float CrushMinAffinity { get; }
        public float CrushMinCompatibility { get; }

        public SocialConfigView(float minAffinity, float minCompatibility)
        {
            CrushMinAffinity = minAffinity;
            CrushMinCompatibility = minCompatibility;
        }

        /// <summary>Los mismos que trae <c>SocialConfig</c> de serie.</summary>
        public static SocialConfigView Default => new SocialConfigView(50f, 0.4f);
    }
}
