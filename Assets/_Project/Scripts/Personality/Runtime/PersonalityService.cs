using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Personality.Runtime
{
    /// <summary>
    /// Reúne los dieciséis comportamientos y responde por todos.
    /// </summary>
    /// <remarks>
    /// Los recibe montados en vez de buscarlos por reflexión: quien los crea es el
    /// arranque, en un solo sitio y a la vista. Cuesta una línea por tipo y a cambio
    /// no hay magia que falle en una compilación con IL2CPP.
    /// </remarks>
    public sealed class PersonalityService : IPersonalityService
    {
        private readonly PersonalityBehaviourBase[] _byIndex =
            new PersonalityBehaviourBase[PersonalityProfile.TypeCount];

        private readonly Dictionary<string, PersonalityBehaviourBase> _byId =
            new Dictionary<string, PersonalityBehaviourBase>(PersonalityProfile.TypeCount);

        public PersonalityService(IEnumerable<PersonalityBehaviourBase> behaviours)
        {
            var indexById = new Dictionary<string, int>(PersonalityProfile.TypeCount);

            foreach (var behaviour in behaviours)
            {
                int index = behaviour.TypeIndex;
                if (index < 0 || index >= _byIndex.Length)
                {
                    Debug.LogError($"PersonalityService: {behaviour.Id} dice ser del tipo {index}");
                    continue;
                }
                if (_byIndex[index] != null)
                {
                    Debug.LogError($"PersonalityService: el tipo {index} lo reclaman " +
                                   $"{_byIndex[index].Id} y {behaviour.Id}");
                    continue;
                }

                _byIndex[index] = behaviour;
                _byId[behaviour.Id] = behaviour;
                indexById[behaviour.Id] = index;
            }

            for (int i = 0; i < _byIndex.Length; i++)
                if (_byIndex[i] == null) Debug.LogError($"PersonalityService: falta el tipo {i}");

            // Segunda pasada: hasta ahora los sesgos eran ids, y ya se pueden resolver
            // a índices porque los dieciséis están dentro.
            for (int i = 0; i < _byIndex.Length; i++) _byIndex[i]?.ResolveBiases(indexById);

            WarnAboutAsymmetries();
        }

        public IPersonalityBehaviour ByIndex(int typeIndex) =>
            typeIndex >= 0 && typeIndex < _byIndex.Length ? _byIndex[typeIndex] : null;

        public IPersonalityBehaviour For(in PersonalityProfile profile) => _byIndex[profile.TypeIndex];

        public IPersonalityBehaviour ById(string personalityId) =>
            personalityId != null && _byId.TryGetValue(personalityId, out var b) ? b : null;

        /// <summary>
        /// Simétrica por construcción: si los dos tipos no dicen lo mismo el uno del
        /// otro, se queda con la media. Vale más una tabla coherente que una fiel.
        /// </summary>
        public int CompatibilityBetween(int typeIndexA, int typeIndexB)
        {
            var a = ByIndex(typeIndexA);
            var b = ByIndex(typeIndexB);
            if (a == null || b == null) return 0;

            int fromA = a.AffinityBiasToward(typeIndexB);
            int fromB = b.AffinityBiasToward(typeIndexA);
            return fromA == fromB ? fromA : Mathf.RoundToInt((fromA + fromB) * 0.5f);
        }

        /// <summary>
        /// Avisa en consola de los pares que no se corresponden. No rompe nada, pero
        /// una asimetría casi siempre es una errata en la tabla de personalidades.
        /// </summary>
        private void WarnAboutAsymmetries()
        {
            for (int a = 0; a < _byIndex.Length; a++)
            {
                if (_byIndex[a] == null) continue;
                for (int b = a + 1; b < _byIndex.Length; b++)
                {
                    if (_byIndex[b] == null) continue;

                    int fromA = _byIndex[a].AffinityBiasToward(b);
                    int fromB = _byIndex[b].AffinityBiasToward(a);
                    if (fromA != fromB)
                        Debug.LogWarning($"Personalidades asimétricas: {_byIndex[a].Id}→{_byIndex[b].Id}" +
                                         $" = {fromA} pero {_byIndex[b].Id}→{_byIndex[a].Id} = {fromB}");
                }
            }
        }
    }
}
