using System;
using System.Collections.Generic;
using Nimbo.CharacterCreator.Appearance;
using Nimbo.CharacterCreator.Presets;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.CharacterCreator
{
    /// <summary>
    /// Fabrica habitantes: los que llegan solos a la isla, los que nacen y los que el
    /// jugador empieza a modelar desde cero.
    /// </summary>
    public sealed class IslanderFactory : IIslanderFactory
    {
        private readonly IIslanderRegistry _registry;
        private readonly IPersonalityService _personalities;
        private readonly GameClock _clock;
        private readonly IReadOnlyList<string> _foodIds;

        public IslanderFactory(IIslanderRegistry registry, IPersonalityService personalities,
                               GameClock clock, IReadOnlyList<string> foodIds)
        {
            _registry = registry;
            _personalities = personalities;
            _clock = clock;
            _foodIds = foodIds;
        }

        public IslanderData CreateRandom(string seed = null)
        {
            var rng = string.IsNullOrEmpty(seed) ? Rng.FromTime() : Rng.FromSeed(seed);
            var islander = NewShell(ref rng);

            islander.Appearance = AppearanceRandomizer.Random(ref rng);
            islander.Personality = RandomPersonality(ref rng);
            islander.Identity.DisplayName = NameBank.UnusedName(ref rng, IsNameTaken);
            islander.Voice = VoiceFor(islander.Personality, ref rng);
            islander.Tastes = RandomTastes(ref rng);

            return islander;
        }

        public IslanderData CreateChild(IslanderData parentA, IslanderData parentB)
        {
            if (parentA == null || parentB == null)
            {
                Debug.LogError("IslanderFactory: no se puede tener un hijo sin dos padres");
                return null;
            }

            // Semilla derivada de los dos padres: el mismo par siempre da el mismo
            // primer hijo, y así una partida recargada no cambia de bebé.
            var rng = Rng.FromSeed($"{parentA.Id}+{parentB.Id}+{_clock.ElapsedMinutes}");
            var child = NewShell(ref rng);

            child.Appearance = AppearanceRandomizer.Blend(
                parentA.Appearance, parentB.Appearance, ref rng);
            child.Personality = BlendPersonality(parentA.Personality, parentB.Personality, ref rng);
            child.Identity.DisplayName = NameBank.UnusedName(ref rng, IsNameTaken);
            child.Voice = VoiceFor(child.Personality, ref rng);
            child.Voice.Pitch = VoicePitch.VeryHigh;   // es un bebé
            child.Tastes = BlendTastes(parentA.Tastes, parentB.Tastes, ref rng);

            return child;
        }

        public IslanderData CreateBlank()
        {
            var rng = Rng.FromTime();
            var islander = NewShell(ref rng);

            islander.Appearance = AppearanceData.Default;
            islander.Personality = new PersonalityProfile(0.25f, 0.25f, 0.25f, 0.25f);
            islander.Identity.DisplayName = "";
            islander.Voice = VoiceConfig.Default;
            islander.Tastes = RandomTastes(ref rng);

            return islander;
        }

        private IslanderData NewShell(ref Rng rng) => new IslanderData
        {
            Identity = new IslanderIdentity
            {
                Id = Guid.NewGuid().ToString("N"),
                Nickname = NameBank.UnusedNickname(ref rng, IsNicknameTaken),
                Birthday = NameBank.RandomBirthday(ref rng),
                IsPlayerAvatar = false,
            },
            Needs = NeedState.Fresh,
            Mood = MoodState.Fresh,
            Progression = ProgressionState.Fresh,
            Activity = IslanderActivity.Idle,
            ArrivalMinute = _clock.ElapsedMinutes,
        };

        private bool IsNameTaken(string name)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Identity.DisplayName == name) return true;
            return false;
        }

        /// <summary>
        /// El apodo también tiene que ser único, y no es un capricho: la lista de
        /// habitantes enseña <c>ShortName</c>, que es el apodo cuando lo hay. Dos
        /// apodos iguales son dos botones iguales para dos personas distintas.
        /// </summary>
        private bool IsNicknameTaken(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return false;

            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Identity.Nickname == nickname) return true;
            return false;
        }

        /// <summary>
        /// Una personalidad al azar, pero no uniforme: los ejes tienden a los extremos.
        /// </summary>
        /// <remarks>
        /// Si se sortearan planos, casi todos saldrían con los cuatro ejes cerca de
        /// cero — es decir, sosos, y además de un tipo que apenas se les notaría. Con
        /// esta curva la isla tiene carácter desde el primer día.
        /// </remarks>
        private static PersonalityProfile RandomPersonality(ref Rng rng)
        {
            return new PersonalityProfile(Axis(ref rng), Axis(ref rng), Axis(ref rng), Axis(ref rng));

            static float Axis(ref Rng rng)
            {
                float sign = rng.Chance(0.5f) ? 1f : -1f;
                return sign * rng.Range(0.35f, 1f);
            }
        }

        /// <summary>
        /// La personalidad de un hijo sale cerca del punto medio de sus padres, pero
        /// con ruido suficiente para que pueda caer en otro tipo. La media exacta haría
        /// que todos los hijos de la isla acabaran siendo lo mismo.
        /// </summary>
        private static PersonalityProfile BlendPersonality(in PersonalityProfile a,
                                                           in PersonalityProfile b, ref Rng rng)
        {
            return new PersonalityProfile(
                Mix(a.Energy, b.Energy, ref rng),
                Mix(a.Expression, b.Expression, ref rng),
                Mix(a.Attitude, b.Attitude, ref rng),
                Mix(a.Outlook, b.Outlook, ref rng));

            static float Mix(float x, float y, ref Rng rng)
            {
                float middle = (x + y) * 0.5f;
                float value = middle + rng.Range(-0.45f, 0.45f);

                // Un eje casi en cero deja el tipo colgando de un decimal. Se empuja
                // fuera de la zona muerta para que el habitante tenga carácter.
                if (Mathf.Abs(value) < 0.2f) value = value >= 0f ? 0.2f : -0.2f;
                return Mathf.Clamp(value, -1f, 1f);
            }
        }

        /// <summary>La voz que le pega a su tipo, con un poco de variación personal.</summary>
        private VoiceConfig VoiceFor(in PersonalityProfile profile, ref Rng rng)
        {
            var template = _personalities.For(profile).VoiceTemplate;
            template.Speed = Mathf.Clamp(template.Speed + rng.Range(-0.1f, 0.1f), 0.6f, 1.6f);
            template.Warble = Mathf.Clamp01(template.Warble + rng.Range(-0.15f, 0.15f));
            template.Nasal = Mathf.Clamp01(rng.Range(0f, 0.6f));
            return template;
        }

        /// <summary>
        /// Dos comidas que le encantan y dos que odia, del catálogo. Fijas desde que
        /// nace: descubrirlas dándole de comer y verle la cara es medio juego.
        /// </summary>
        private TasteProfile RandomTastes(ref Rng rng)
        {
            var tastes = new TasteProfile();
            if (_foodIds == null || _foodIds.Count < 4) return tastes;

            var pool = new List<string>(_foodIds);
            rng.Shuffle(pool);

            tastes.LovedFoods.Add(pool[0]);
            tastes.LovedFoods.Add(pool[1]);
            tastes.HatedFoods.Add(pool[2]);
            tastes.HatedFoods.Add(pool[3]);
            return tastes;
        }

        /// <summary>Un hijo hereda un gusto de cada padre y estrena los suyos propios.</summary>
        private TasteProfile BlendTastes(TasteProfile a, TasteProfile b, ref Rng rng)
        {
            var tastes = RandomTastes(ref rng);

            if (a?.LovedFoods.Count > 0) tastes.LovedFoods[0] = rng.Pick(a.LovedFoods);
            if (b?.HatedFoods.Count > 0) tastes.HatedFoods[0] = rng.Pick(b.HatedFoods);

            // Que no acabe odiando justo lo que le encanta.
            tastes.HatedFoods.RemoveAll(food => tastes.LovedFoods.Contains(food));
            return tastes;
        }
    }
}
