using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using UnityEngine;

namespace Nimbo.Personality.Runtime
{
    /// <summary>
    /// La fontanería que comparten los dieciséis tipos. Cada tipo solo rellena su
    /// <see cref="PersonalityDefinition"/>; de responder a las preguntas se encarga esto.
    /// </summary>
    public abstract class PersonalityBehaviourBase : IPersonalityBehaviour
    {
        private readonly PersonalityDefinition _def;
        private readonly Dictionary<string, int> _biasByOtherId;

        /// <summary>Los sesgos se resuelven a índice de tipo tarde, cuando ya existen los dieciséis.</summary>
        private int[] _biasByTypeIndex;

        protected PersonalityBehaviourBase(PersonalityDefinition definition)
        {
            _def = definition;
            _biasByOtherId = new Dictionary<string, int>(definition.Biases.Count);
            foreach (var bias in definition.Biases) _biasByOtherId[bias.OtherId] = bias.Amount;
        }

        public string Id => _def.Id;
        public int TypeIndex => _def.TypeIndex;
        public string DisplayName => _def.DisplayName;
        public string Tagline => _def.Tagline;
        public float WalkSpeedMultiplier => _def.WalkSpeed;
        public float IdleDwellSeconds => _def.IdleDwell;
        public VoiceConfig VoiceTemplate => _def.Voice;

        /// <summary>Lo rápido que su ánimo vuelve al nivel de fondo. Lo usa la simulación.</summary>
        public float MoodDecayMultiplier => _def.MoodDecay;

        public IReadOnlyList<Emotion> SignatureEmotions => _def.SignatureEmotions;

        public float NeedDecayMultiplier(NeedKind need) =>
            _def.NeedDecay.TryGetValue(need, out float m) ? m : 1f;

        public float RequestWeight(RequestKind kind) =>
            _def.RequestWeights.TryGetValue(kind, out float w) ? w : 1f;

        public Emotion ReactTo(PersonalityReaction reaction) =>
            _def.Reactions.TryGetValue(reaction, out var e) ? e : Emotion.Neutral;

        public string PickLine(LineMood mood, ref Rng rng)
        {
            if (!_def.Lines.TryGetValue(mood, out var lines) || lines.Length == 0) return string.Empty;
            return lines[rng.Range(0, lines.Length)];
        }

        public int AffinityBiasToward(int otherTypeIndex)
        {
            if (_biasByTypeIndex == null)
            {
                Debug.LogWarning($"{Id}: se preguntó por afinidad antes de resolver los sesgos");
                return 0;
            }
            return otherTypeIndex >= 0 && otherTypeIndex < _biasByTypeIndex.Length
                ? _biasByTypeIndex[otherTypeIndex]
                : 0;
        }

        /// <summary>
        /// Convierte los sesgos escritos por id (<c>PT_XXX</c>) a índices de tipo.
        /// Lo llama <see cref="PersonalityService"/> una vez creados los dieciséis:
        /// un tipo no puede resolverlo en su constructor porque los demás aún no existen.
        /// </summary>
        internal void ResolveBiases(IReadOnlyDictionary<string, int> indexById)
        {
            _biasByTypeIndex = new int[PersonalityProfile.TypeCount];
            foreach (var pair in _biasByOtherId)
            {
                if (indexById.TryGetValue(pair.Key, out int index))
                    _biasByTypeIndex[index] = pair.Value;
                else
                    Debug.LogError($"{Id}: sesgo hacia {pair.Key}, que no es ningún tipo conocido");
            }
        }

        internal IReadOnlyDictionary<string, int> RawBiases => _biasByOtherId;
    }
}
