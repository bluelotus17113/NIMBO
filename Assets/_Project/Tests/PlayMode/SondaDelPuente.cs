using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Mide el suelo del paso entre las dos islas, metro a metro.
    /// </summary>
    /// <remarks>
    /// Se le pregunta al mundo a qué altura está lo que se pisa, en línea recta desde
    /// tu casa hasta la aldea. Es como se encontró que el puente iba clavado a altura
    /// cero mientras el borde del prado se hunde hasta menos uno con ochenta y cinco.
    /// </remarks>
    public static class Paso
    {
        public const float DesdeZ = -180f;
        public const float HastaZ = -70f;

        /// <summary>Lo que sube un <c>CharacterController</c>. Sale de <c>PlayerBody</c>.</summary>
        public const float PasoMaximo = 0.45f;

        public readonly struct Punto
        {
            public readonly float Z;
            public readonly float Altura;
            public readonly string Sobre;

            public Punto(float z, float altura, string sobre)
            {
                Z = z; Altura = altura; Sobre = sobre;
            }
        }

        /// <summary>
        /// Recorre el paso midiendo el suelo. Deja fuera al protagonista.
        /// </summary>
        /// <remarks>
        /// El muñeco está plantado en el camino y el rayo le da en la cabeza: sin
        /// quitarlo, la medida decía que había un escalón de tres metros y medio donde
        /// solo había alguien de pie.
        /// </remarks>
        public static List<Punto> Medir()
        {
            var puntos = new List<Punto>();

            for (float z = DesdeZ; z <= HastaZ; z += 1f)
            {
                var desde = new Vector3(0f, 40f, z);
                var golpes = Physics.RaycastAll(desde, Vector3.down, 90f,
                                                ~0, QueryTriggerInteraction.Ignore);

                float mejor = float.NegativeInfinity;
                string sobre = null;

                foreach (var golpe in golpes)
                {
                    if (EsElMuñeco(golpe.collider.transform)) continue;
                    if (golpe.point.y <= mejor) continue;

                    mejor = golpe.point.y;
                    sobre = golpe.collider.name;
                }

                if (sobre != null) puntos.Add(new Punto(z, mejor, sobre));
            }

            return puntos;
        }

        private static bool EsElMuñeco(Transform t)
        {
            for (var n = t; n != null; n = n.parent)
                if (n.name == "Protagonista") return true;
            return false;
        }

        public static string Tabla(List<Punto> puntos)
        {
            var texto = new StringBuilder("     z   altura  sobre qué\n");
            for (int i = 0; i < puntos.Count; i++)
            {
                float salto = i == 0 ? 0f : puntos[i].Altura - puntos[i - 1].Altura;
                texto.Append($"{puntos[i].Z,6:0} {puntos[i].Altura,8:0.00}  {puntos[i].Sobre}");
                texto.AppendLine(Mathf.Abs(salto) > PasoMaximo ? $"   ← escalón de {salto:0.00} m" : "");
            }
            return texto.ToString();
        }
    }

    /// <summary>
    /// Se tiene que poder ir andando de tu isla a la aldea.
    /// </summary>
    /// <remarks>
    /// Esta prueba nace de jugar. El puente estaba clavado a altura cero y el borde de
    /// la isla del jugador se hunde —`BuildSurface` le resta `t⁶ × 3,2` para que la
    /// hierba se doble hacia el vacío—, así que en la punta había **un escalón de 1,85
    /// metros** para un muñeco que sube 0,45: la aldea entera, con sus vecinos, sus
    /// tiendas y todos los recursos, era inalcanzable a pie.
    ///
    /// No lo vio nadie porque en la punta de la aldea el desnivel es de cuatro
    /// centímetros —esa isla es más grande y el puente cae en su parte llana— y porque
    /// ninguna prueba andaba: todas comprobaban servicios.
    /// </remarks>
    public class PasoEntreIslasTests
    {
        [UnityTest]
        public IEnumerator SePuedeCruzarElPuenteAndando()
        {
            yield return Aldea.Cargar();

            var puntos = Paso.Medir();
            Assert.That(puntos.Count, Is.GreaterThan(50), "la sonda no ha medido casi nada");

            float peor = 0f;
            float peorZ = 0f;

            for (int i = 1; i < puntos.Count; i++)
            {
                // Solo cuenta lo que hay que subir: bajar un escalón siempre se puede.
                float sube = puntos[i].Altura - puntos[i - 1].Altura;
                if (sube <= peor) continue;

                peor = sube;
                peorZ = puntos[i].Z;
            }

            Assert.That(peor, Is.LessThanOrEqualTo(Paso.PasoMaximo),
                        $"escalón de {peor:0.00} m en z={peorZ:0}, y el muñeco sube " +
                        $"{Paso.PasoMaximo} m: no se puede llegar a la aldea andando");
        }

        /// <summary>Y sin agujeros por los que colarse al vacío.</summary>
        [UnityTest]
        public IEnumerator ElPasoNoTieneAgujeros()
        {
            yield return Aldea.Cargar();

            var puntos = Paso.Medir();

            float anterior = float.NaN;
            foreach (var punto in puntos)
            {
                if (!float.IsNaN(anterior))
                    Assert.That(punto.Z - anterior, Is.LessThanOrEqualTo(1.5f),
                                $"no hay suelo entre z={anterior:0} y z={punto.Z:0}");
                anterior = punto.Z;
            }
        }
    }

    /// <summary>Imprime la tabla entera. Para mirarla, no para pasar.</summary>
    [Explicit("Herramienta de diagnóstico, no una prueba.")]
    public class SondaDelPuente
    {
        [UnityTest]
        public IEnumerator MideElPaso()
        {
            yield return Aldea.Cargar();

            var puntos = Paso.Medir();
            Debug.Log($"[sonda]\n{Paso.Tabla(puntos)}");
            Assert.Pass();
        }
    }
}
