using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Comprueba que los dieciséis tipos forman una tabla coherente. Los números
    /// concretos ya los verifica un script contra el JSON; esto verifica lo que solo
    /// se ve al montarlos juntos y ejecutarlos.
    /// </summary>
    public class PersonalityTests
    {
        private PersonalityService _service;

        [SetUp]
        public void SetUp() => _service = PersonalityRoster.CreateService();

        [Test]
        public void HayDieciseisTiposYNingunoRepiteIndice()
        {
            var indices = new HashSet<int>();
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
            {
                var behaviour = _service.ByIndex(i);
                Assert.IsNotNull(behaviour, $"falta el tipo {i}");
                Assert.IsTrue(indices.Add(behaviour.TypeIndex), $"índice repetido: {i}");
            }
        }

        [Test]
        public void ElSignoDeLosEjesLlevaASuTipo()
        {
            // El perfil canónico de cada tipo tiene que devolver ese mismo tipo: es la
            // propiedad de la que depende todo el creador de personajes.
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
            {
                var profile = PersonalityProfile.FromTypeIndex(i);
                Assert.AreEqual(i, profile.TypeIndex, $"el perfil canónico de {i} deriva mal");
                Assert.AreEqual(i, _service.For(profile).TypeIndex);
            }
        }

        [Test]
        public void LaCompatibilidadEsSimetrica()
        {
            for (int a = 0; a < PersonalityProfile.TypeCount; a++)
            for (int b = 0; b < PersonalityProfile.TypeCount; b++)
                Assert.AreEqual(_service.CompatibilityBetween(a, b),
                                _service.CompatibilityBetween(b, a),
                                $"asimetría entre {a} y {b}");
        }

        [Test]
        public void NadieCongeniaNiChocaConsigoMismo()
        {
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
                Assert.AreEqual(0, _service.CompatibilityBetween(i, i),
                                $"el tipo {i} tiene opinión sobre sí mismo");
        }

        [Test]
        public void LosIdentificadoresSonUnicos()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
                Assert.IsTrue(ids.Add(_service.ByIndex(i).Id),
                              $"id repetido en el tipo {i}");
        }

        [Test]
        public void TodosTienenFraseParaCadaTono()
        {
            var rng = new Rng(1234);
            foreach (LineMood mood in System.Enum.GetValues(typeof(LineMood)))
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
            {
                string line = _service.ByIndex(i).PickLine(mood, ref rng);
                Assert.IsNotEmpty(line, $"el tipo {i} no tiene frase para {mood}");
            }
        }

        [Test]
        public void LosMultiplicadoresDeNecesidadSonRazonables()
        {
            // Ninguno puede quedar tan bajo que la necesidad no baje nunca, ni tan alto
            // que el habitante viva en rojo permanente.
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
            for (int n = 0; n < NeedState.Count; n++)
            {
                float m = _service.ByIndex(i).NeedDecayMultiplier((NeedKind)n);
                Assert.That(m, Is.InRange(0.5f, 1.5f),
                            $"el tipo {i} tiene {m} en {(NeedKind)n}");
            }
        }

        [Test]
        public void LaMediaDeCadaNecesidadRondaUno()
        {
            for (int n = 0; n < NeedState.Count; n++)
            {
                float sum = 0f;
                for (int i = 0; i < PersonalityProfile.TypeCount; i++)
                    sum += _service.ByIndex(i).NeedDecayMultiplier((NeedKind)n);

                float average = sum / PersonalityProfile.TypeCount;
                Assert.That(average, Is.InRange(0.9f, 1.1f),
                            $"{(NeedKind)n} tiene media {average}: la isla entera va sesgada");
            }
        }

        [Test]
        public void NingunTipoSeCallaAnteTodasLasPeticiones()
        {
            for (int i = 0; i < PersonalityProfile.TypeCount; i++)
            {
                float total = 0f;
                foreach (RequestKind kind in System.Enum.GetValues(typeof(RequestKind)))
                    total += _service.ByIndex(i).RequestWeight(kind);

                Assert.Greater(total, 0f, $"el tipo {i} no pediría nunca nada");
            }
        }

        [Test]
        public void DosTiposOpuestosNoReaccionanIgualATodo()
        {
            // El 0 y el 15 son opuestos en los cuatro ejes. Si reaccionasen igual a las
            // doce situaciones, la personalidad no se notaría en pantalla.
            var a = _service.ByIndex(0);
            var b = _service.ByIndex(15);

            int diferencias = 0;
            foreach (PersonalityReaction reaction in System.Enum.GetValues(typeof(PersonalityReaction)))
                if (a.ReactTo(reaction) != b.ReactTo(reaction)) diferencias++;

            Assert.Greater(diferencias, 5,
                           $"{a.DisplayName} y {b.DisplayName} solo se diferencian en {diferencias} de 12");
        }
    }
}
