using Nimbo.Art.CameraWork;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    [TestFixture]
    public class CameraRigTests
    {
        private const float IslandRadius = 100f;
        private const float MaxPivotRadius = IslandRadius * 1.1f; // 110

        private static CameraRig NewRig()
        {
            var rig = new CameraRig(IslandRadius);
            // Bajamos la suavidad para que las pruebas corran en pocos pasos.
            rig.Sharpness = 10f;
            return rig;
        }

        // ── 1. Advance acerca Current a Target sin pasarse ─────────────────

        [Test]
        public void Advance_MovesCurrentTowardTarget_WithoutOvershoot()
        {
            var rig = NewRig();

            rig.Target = new CameraPose
            {
                Pivot = new Vector3(20f, 0f, 30f),
                Distance = 60f,
                Yaw = 120f,
                Pitch = 50f,
            };

            // Avanzar un fotograma pequeño.
            rig.Advance(1f / 60f);

            CameraPose c = rig.Current;
            CameraPose t = rig.Target;

            // Cada componente se ha movido hacia el target.
            float d0_pivot = Vector3.Distance(
                new Vector3(0f, 0f, 0f), t.Pivot);
            float d1_pivot = Vector3.Distance(c.Pivot, t.Pivot);
            Assert.Less(d1_pivot, d0_pivot, "El pivote debería haberse acercado al target.");

            float d0_dist = Mathf.Abs(150f - t.Distance);
            float d1_dist = Mathf.Abs(c.Distance - t.Distance);
            Assert.Less(d1_dist, d0_dist, "La distancia debería haberse acercado al target.");

            // Con suavizado exponencial nunca se pasa de largo (es monótono).
            Assert.GreaterOrEqual(t.Pivot.x, c.Pivot.x - 0.001f,
                "No debería pasarse del target en X.");
            Assert.LessOrEqual(0f, c.Pivot.x + 0.001f,
                "No debería retroceder respecto al origen.");
        }

        // ── 2. Con deltaSeconds grande tampoco se pasa ni oscila ───────────

        [Test]
        public void Advance_WithLargeDeltaTime_DoesNotOvershootOrOscillate()
        {
            var rig = NewRig();

            rig.Target = new CameraPose
            {
                Pivot = new Vector3(50f, 0f, -30f),
                Distance = 200f,
                Yaw = 270f,
                Pitch = 30f,
            };

            // Medio segundo: el t será grande (~0.993) pero < 1.
            rig.Advance(0.5f);

            CameraPose c = rig.Current;
            CameraPose t = rig.Target;

            // El pivote no se ha pasado del target.
            Assert.LessOrEqual(c.Pivot.x, t.Pivot.x + 0.001f,
                "No debería pasarse del target en X con dt grande.");
            Assert.GreaterOrEqual(c.Pivot.z, t.Pivot.z - 0.001f,
                "No debería pasarse del target en Z con dt grande.");

            // La distancia tampoco.
            if (t.Distance > 150f)
                Assert.LessOrEqual(c.Distance, t.Distance + 0.001f,
                    "No debería pasarse del target en distancia con dt grande.");
            else
                Assert.GreaterOrEqual(c.Distance, t.Distance - 0.001f,
                    "No debería pasarse del target en distancia con dt grande.");
        }

        // ── 3. Independencia de los fotogramas ─────────────────────────────

        [Test]
        public void Advance_IsFrameRateIndependent()
        {
            // Sesenta pasos de 1/60 s.
            var rig60 = NewRig();
            rig60.Target = new CameraPose
            {
                Pivot = new Vector3(10f, 0f, 20f),
                Distance = 80f,
                Yaw = 90f,
                Pitch = 40f,
            };
            for (int i = 0; i < 60; i++)
                rig60.Advance(1f / 60f);

            // Seis pasos de 1/6 s (mismo tiempo total: 1 segundo).
            var rig6 = NewRig();
            rig6.Target = new CameraPose
            {
                Pivot = new Vector3(10f, 0f, 20f),
                Distance = 80f,
                Yaw = 90f,
                Pitch = 40f,
            };
            for (int i = 0; i < 6; i++)
                rig6.Advance(1f / 6f);

            // Con suavizado exponencial ambos llegan al mismo sitio porque el
            // factor efectivo depende del tiempo total, no del tamaño del paso.
            CameraPose a = rig60.Current;
            CameraPose b = rig6.Current;

            float tol = 1e-5f;
            Assert.AreEqual(a.Pivot.x, b.Pivot.x, tol, "Pivot.x debería coincidir.");
            Assert.AreEqual(a.Pivot.y, b.Pivot.y, tol, "Pivot.y debería coincidir.");
            Assert.AreEqual(a.Pivot.z, b.Pivot.z, tol, "Pivot.z debería coincidir.");
            Assert.AreEqual(a.Distance, b.Distance, tol, "Distance debería coincidir.");
            Assert.AreEqual(a.Pitch, b.Pitch, tol, "Pitch debería coincidir.");
            Assert.AreEqual(a.Yaw, b.Yaw, tol, "Yaw debería coincidir.");
        }

        // ── 4. Pitch entre 12° y 78° ───────────────────────────────────────

        [Test]
        public void Target_Pitch_ClampedBetween12And78()
        {
            var rig = NewRig();

            rig.Target = new CameraPose { Pitch = 500f };
            Assert.AreEqual(78f, rig.Target.Pitch, "Pitch 500 debería quedarse en 78.");

            rig.Target = new CameraPose { Pitch = -90f };
            Assert.AreEqual(12f, rig.Target.Pitch, "Pitch -90 debería quedarse en 12.");

            rig.Target = new CameraPose { Pitch = 0f };
            Assert.AreEqual(12f, rig.Target.Pitch, "Pitch 0 debería quedarse en 12.");

            rig.Target = new CameraPose { Pitch = 45f };
            Assert.AreEqual(45f, rig.Target.Pitch, "Pitch 45 debería quedarse en 45.");

            rig.Target = new CameraPose { Pitch = 90f };
            Assert.AreEqual(78f, rig.Target.Pitch, "Pitch 90 debería quedarse en 78.");
        }

        [Test]
        public void Orbit_ClampsPitch()
        {
            var rig = NewRig();
            // Empezamos en 45 y bajamos mucho.
            rig.Orbit(0f, -100f);
            Assert.AreEqual(12f, rig.Target.Pitch, "Bajar 100° desde 45 debería quedarse en 12.");

            // Subimos mucho.
            rig.Orbit(0f, 200f);
            Assert.AreEqual(78f, rig.Target.Pitch, "Subir 200° debería quedarse en 78.");
        }

        // ── 5. Distancia entre 8 y 220 ─────────────────────────────────────

        [Test]
        public void Target_Distance_ClampedBetween8And220()
        {
            var rig = NewRig();

            rig.Target = new CameraPose { Distance = 500f };
            Assert.AreEqual(220f, rig.Target.Distance, "Distance 500 debería quedarse en 220.");

            rig.Target = new CameraPose { Distance = 0f };
            Assert.AreEqual(8f, rig.Target.Distance, "Distance 0 debería quedarse en 8.");

            rig.Target = new CameraPose { Distance = -10f };
            Assert.AreEqual(8f, rig.Target.Distance, "Distance -10 debería quedarse en 8.");

            rig.Target = new CameraPose { Distance = 100f };
            Assert.AreEqual(100f, rig.Target.Distance, "Distance 100 debería quedarse en 100.");
        }

        [Test]
        public void Zoom_ClampsDistance()
        {
            var rig = NewRig();
            rig.Target = new CameraPose { Distance = 20f };

            rig.Zoom(200f); // acerca mucho
            Assert.AreEqual(8f, rig.Target.Distance, "Acercar 200 desde 20 debería quedarse en 8.");

            rig.Zoom(-500f); // aleja mucho
            Assert.AreEqual(220f, rig.Target.Distance, "Alejar 500 debería quedarse en 220.");
        }

        // ── 6. El pivote no sale del círculo ───────────────────────────────

        [Test]
        public void Target_Pivot_ClampedToCircle()
        {
            var rig = NewRig();

            // Dentro del círculo: no se toca.
            rig.Target = new CameraPose
            {
                Pivot = new Vector3(50f, 0f, 50f),
                Distance = 100f,
                Pitch = 45f,
            };
            float mag = new Vector2(rig.Target.Pivot.x, rig.Target.Pivot.z).magnitude;
            Assert.LessOrEqual(mag, MaxPivotRadius,
                "Un punto dentro del círculo no debería moverse.");

            // Fuera del círculo: se recorta al borde.
            rig.Target = new CameraPose
            {
                Pivot = new Vector3(200f, 0f, 200f),
                Distance = 100f,
                Pitch = 45f,
            };
            float magClamped = new Vector2(rig.Target.Pivot.x, rig.Target.Pivot.z).magnitude;
            Assert.LessOrEqual(magClamped, MaxPivotRadius + 0.001f,
                "Un punto fuera del círculo debería recortarse al radio máximo.");
            Assert.GreaterOrEqual(magClamped, MaxPivotRadius - 0.001f,
                "Un punto fuera del círculo debería quedarse exactamente en el borde.");
        }

        [Test]
        public void Pan_ClampsPivotToCircle()
        {
            var rig = NewRig();
            // El pivote por defecto está en el origen. Lo desplazamos hasta el borde.
            rig.Pan(new Vector3(MaxPivotRadius + 50f, 0f, 0f));

            float mag = new Vector2(rig.Target.Pivot.x, rig.Target.Pivot.z).magnitude;
            Assert.LessOrEqual(mag, MaxPivotRadius + 0.001f,
                "Pan más allá del círculo debería recortarse.");
        }

        // ── 7. Yaw gira por el lado corto ──────────────────────────────────

        [Test]
        public void Advance_Yaw_TakesShortestPath()
        {
            // Situación: Current en 350° y Target en 10°. El camino corto son 20°
            // hacia adelante (350→370≡10), nunca los 340° del camino largo que
            // pasarían por 180°.
            var rig = NewRig();
            rig.Target = new CameraPose { Yaw = 350f, Distance = 150f, Pitch = 45f };
            rig.SnapToTarget();
            rig.Target = new CameraPose { Yaw = 10f, Distance = 150f, Pitch = 45f };

            // Avanzamos en pasos pequeños y comprobamos que el yaw nunca pasa
            // por la zona del camino largo.
            for (int i = 0; i < 200; i++)
            {
                rig.Advance(1f / 60f);
                float yaw = rig.Current.Yaw;

                // Si hubiera ido por el camino largo, el yaw estaría cerca de 180°.
                // Con el camino corto, está siempre en [350, 360) o [0, 10].
                Assert.IsFalse(
                    yaw > 30f && yaw < 330f,
                    $"Yaw {yaw} en paso {i}: no debería pasar por el camino largo (~180°)."
                );
            }

            // Al final debe haber llegado a 10°.
            Assert.AreEqual(10f, rig.Current.Yaw, 0.1f,
                "Tras suficientes pasos, el yaw debería converger a 10°.");
        }

        // ── 8. SnapToTarget iguala Current y Target ────────────────────────

        [Test]
        public void SnapToTarget_MakesCurrentEqualToTarget()
        {
            var rig = NewRig();

            rig.Target = new CameraPose
            {
                Pivot = new Vector3(30f, 0f, -40f),
                Distance = 75f,
                Yaw = 215f,
                Pitch = 33f,
            };

            // Antes del snap, Current no coincide con Target.
            Assert.AreNotEqual(rig.Target.Pivot, rig.Current.Pivot);

            rig.SnapToTarget();

            Assert.AreEqual(rig.Target.Pivot, rig.Current.Pivot);
            Assert.AreEqual(rig.Target.Distance, rig.Current.Distance, 0.001f);
            Assert.AreEqual(rig.Target.Yaw, rig.Current.Yaw, 0.001f);
            Assert.AreEqual(rig.Target.Pitch, rig.Current.Pitch, 0.001f);
        }

        // ── 9. Position y Rotation son coherentes ──────────────────────────

        [Test]
        public void Position_Rotation_CameraLooksAtPivot()
        {
            var rig = NewRig();

            rig.Target = new CameraPose
            {
                Pivot = new Vector3(15f, 0f, -10f),
                Distance = 42f,
                Yaw = 60f,
                Pitch = 35f,
            };
            rig.SnapToTarget();

            Vector3 pos = rig.Position;
            Quaternion rot = rig.Rotation;

            // La cámara mira al pivote: Rotation * forward debe apuntar
            // de Position hacia Pivot.
            Vector3 lookDir = rot * Vector3.forward;
            Vector3 expectedDir = (rig.Current.Pivot - pos).normalized;

            Assert.AreEqual(expectedDir.x, lookDir.x, 0.001f,
                "Look direction X debería apuntar al pivote.");
            Assert.AreEqual(expectedDir.y, lookDir.y, 0.001f,
                "Look direction Y debería apuntar al pivote.");
            Assert.AreEqual(expectedDir.z, lookDir.z, 0.001f,
                "Look direction Z debería apuntar al pivote.");
        }

        [Test]
        public void Position_Rotation_Coherent_AfterAdvance()
        {
            // La coherencia se mantiene incluso durante la transición.
            var rig = NewRig();

            rig.Target = new CameraPose
            {
                Pivot = new Vector3(-20f, 0f, 25f),
                Distance = 90f,
                Yaw = 300f,
                Pitch = 55f,
            };

            for (int i = 0; i < 10; i++)
            {
                rig.Advance(1f / 30f);

                Vector3 pos = rig.Position;
                Vector3 lookDir = rig.Rotation * Vector3.forward;
                Vector3 expectedDir = (rig.Current.Pivot - pos).normalized;

                Assert.AreEqual(expectedDir.x, lookDir.x, 0.001f,
                    $"Coherencia X tras paso {i}.");
                Assert.AreEqual(expectedDir.y, lookDir.y, 0.001f,
                    $"Coherencia Y tras paso {i}.");
                Assert.AreEqual(expectedDir.z, lookDir.z, 0.001f,
                    $"Coherencia Z tras paso {i}.");
            }
        }

        // ── Extra: Yaw se normaliza a [0, 360) ─────────────────────────────

        [Test]
        public void Target_Yaw_AlwaysNormalized()
        {
            var rig = NewRig();

            rig.Target = new CameraPose { Yaw = -90f, Distance = 100f, Pitch = 45f };
            Assert.AreEqual(270f, rig.Target.Yaw, 0.001f, "-90° debería normalizarse a 270°.");

            rig.Target = new CameraPose { Yaw = 720f, Distance = 100f, Pitch = 45f };
            Assert.AreEqual(0f, rig.Target.Yaw, 0.001f, "720° debería normalizarse a 0°.");

            rig.Target = new CameraPose { Yaw = 361f, Distance = 100f, Pitch = 45f };
            Assert.AreEqual(1f, rig.Target.Yaw, 0.001f, "361° debería normalizarse a 1°.");
        }
    }
}
