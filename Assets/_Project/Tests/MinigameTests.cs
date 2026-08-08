using System.Collections.Generic;
using Nimbo.Core.Util;
using Nimbo.Events.Minigames;
using NUnit.Framework;

namespace Nimbo.Tests
{
    public class MinigameTests
    {
        // ─────────────────────────────────────────────────────────────────
        // 1. Los tres terminan siempre
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Cooking_TerminaEnMenosDe500Pasos()
        {
            var game = new CookingGame();
            game.Start(42, 3);

            int steps = 0;
            var rng = new Rng(99);
            while (!game.IsOver && steps < 500)
            {
                game.Step(rng.Range(0, 3));
                steps++;
            }

            Assert.Less(steps, 500, "no terminó en 500 pasos");
            Assert.IsTrue(game.IsOver, "IsOver debería ser true");
        }

        [Test]
        public void Fishing_TerminaEnMenosDe500Pasos()
        {
            var game = new FishingGame();
            game.Start(42, 3);

            int steps = 0;
            var rng = new Rng(99);
            while (!game.IsOver && steps < 500)
            {
                game.Step(rng.Range(0, 2));
                steps++;
            }

            Assert.Less(steps, 500, "no terminó en 500 pasos");
            Assert.IsTrue(game.IsOver, "IsOver debería ser true");
        }

        [Test]
        public void Rhythm_TerminaEnMenosDe500Pasos()
        {
            var game = new RhythmGame();
            game.Start(42, 3);

            int steps = 0;
            while (!game.IsOver && steps < 500)
            {
                game.Step(0); // desfase 0 → siempre acierta
                steps++;
            }

            Assert.Less(steps, 500, "no terminó en 500 pasos");
            Assert.IsTrue(game.IsOver, "IsOver debería ser true");
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. Determinismo: misma semilla + mismas entradas = mismo resultado
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Cooking_MismaSemillaMismoResultado()
        {
            var inputs = new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2, 0 };

            var a = new CookingGame();
            a.Start(12345, 3);
            foreach (int input in inputs)
                if (!a.IsOver) a.Step(input);
            var resultA = a.Finish();

            var b = new CookingGame();
            b.Start(12345, 3);
            foreach (int input in inputs)
                if (!b.IsOver) b.Step(input);
            var resultB = b.Finish();

            Assert.AreEqual(resultA.Points, resultB.Points);
            Assert.AreEqual(resultA.Successes, resultB.Successes);
            Assert.AreEqual(resultA.Failures, resultB.Failures);
            Assert.AreEqual(resultA.Coins, resultB.Coins);
            Assert.AreEqual(resultA.Experience, resultB.Experience);
            Assert.AreEqual(resultA.IsVictory, resultB.IsVictory);
        }

        [Test]
        public void Fishing_MismaSemillaMismoResultado()
        {
            var inputs = new[] { 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 };

            var a = new FishingGame();
            a.Start(12345, 3);
            foreach (int input in inputs)
                if (!a.IsOver) a.Step(input);
            var resultA = a.Finish();

            var b = new FishingGame();
            b.Start(12345, 3);
            foreach (int input in inputs)
                if (!b.IsOver) b.Step(input);
            var resultB = b.Finish();

            Assert.AreEqual(resultA.Points, resultB.Points);
            Assert.AreEqual(resultA.Successes, resultB.Successes);
            Assert.AreEqual(resultA.Failures, resultB.Failures);
            Assert.AreEqual(resultA.Coins, resultB.Coins);
            Assert.AreEqual(resultA.Experience, resultB.Experience);
            Assert.AreEqual(resultA.IsVictory, resultB.IsVictory);
        }

        [Test]
        public void Rhythm_MismaSemillaMismoResultado()
        {
            var inputs = new[] { 0, 30, -10, 0, 200, 0, 0, 45, 0, 0, 0, 0, 0, 0 };

            var a = new RhythmGame();
            a.Start(12345, 3);
            foreach (int input in inputs)
                if (!a.IsOver) a.Step(input);
            var resultA = a.Finish();

            var b = new RhythmGame();
            b.Start(12345, 3);
            foreach (int input in inputs)
                if (!b.IsOver) b.Step(input);
            var resultB = b.Finish();

            Assert.AreEqual(resultA.Points, resultB.Points);
            Assert.AreEqual(resultA.Successes, resultB.Successes);
            Assert.AreEqual(resultA.Failures, resultB.Failures);
            Assert.AreEqual(resultA.Coins, resultB.Coins);
            Assert.AreEqual(resultA.Experience, resultB.Experience);
            Assert.AreEqual(resultA.IsVictory, resultB.IsVictory);
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. Cocina: tres fallos terminan la partida
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Cooking_TresFallosTerminan()
        {
            // Con 3 pasos, acción 0 fija y (2/3)^3 ≈ 30 % de fallar los tres,
            // así que en 100 semillas alguna seguro que pierde por 3 fallos.
            bool foundDefeat = false;
            for (uint seed = 0; seed < 100; seed++)
            {
                var game = new CookingGame();
                game.Start(seed, 1);
                while (!game.IsOver)
                    game.Step(0);
                var r = game.Finish();
                if (!r.IsVictory && r.Failures >= 3)
                {
                    foundDefeat = true;
                    break;
                }
            }
            Assert.IsTrue(foundDefeat,
                "tras 100 semillas con acción fija 0, debería aparecer una derrota por 3 fallos");
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. Pesca: tensión al máximo → escapa
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Fishing_TensionMaximaEscapa()
        {
            var cfg = UnityEngine.ScriptableObject.CreateInstance<MinigameConfig>();
            cfg.FishingBaseDistance = 200;
            cfg.FishingDistancePerDifficulty = 0;
            cfg.FishingReelDistance = 1;
            cfg.FishingReelTension = 30;
            cfg.FishingWaitTensionDrop = 1;
            cfg.FishingBasePull = 10;
            cfg.FishingPullRange = 5;
            cfg.FishingPullPerDifficulty = 5;

            var game = new FishingGame(cfg);
            game.Start(42, 1);

            while (!game.IsOver)
                game.Step(0); // Reel siempre

            var result = game.Finish();
            Assert.IsFalse(result.IsVictory,
                "con tensión alta y avance mínimo el sedal debería romperse");
            Assert.AreEqual(1, result.Failures);
        }

        // ─────────────────────────────────────────────────────────────────
        // 5. Ritmo: Hit con desfase 0 = Perfect
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Rhythm_DesfaseCeroEsPerfect()
        {
            var game = new RhythmGame();
            game.Start(42, 1);
            game.Step(0);

            Assert.AreEqual(HitRating.Perfect, game.LastRating);
            Assert.AreEqual(1, game.Perfects);
        }

        // ─────────────────────────────────────────────────────────────────
        // 6. Ni monedas ni experiencia negativas
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Cooking_NuncaDevuelveNegativos()
        {
            var game = new CookingGame();
            game.Start(42, 1);

            while (!game.IsOver)
                game.Step(0); // fallar todo

            var result = game.Finish();
            Assert.GreaterOrEqual(result.Coins, 0);
            Assert.GreaterOrEqual(result.Experience, 0);
            Assert.GreaterOrEqual(result.Points, 0);
        }

        [Test]
        public void Fishing_NuncaDevuelveNegativos()
        {
            var game = new FishingGame();
            game.Start(42, 1);

            while (!game.IsOver)
                game.Step(0); // Reel hasta romper

            var result = game.Finish();
            Assert.GreaterOrEqual(result.Coins, 0);
            Assert.GreaterOrEqual(result.Experience, 0);
            Assert.GreaterOrEqual(result.Points, 0);
        }

        [Test]
        public void Rhythm_NuncaDevuelveNegativos()
        {
            var game = new RhythmGame();
            game.Start(42, 1);

            while (!game.IsOver)
                game.Step(999); // fallar todas

            var result = game.Finish();
            Assert.GreaterOrEqual(result.Coins, 0);
            Assert.GreaterOrEqual(result.Experience, 0);
            Assert.GreaterOrEqual(result.Points, 0);
        }

        // ─────────────────────────────────────────────────────────────────
        // 7. Extras: semillas distintas dan resultados distintos
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Cooking_SemillasDistintasDivergen()
        {
            // Comparar UN par de semillas por el marcador final es frágil: con las
            // entradas fijas la partida acaba siempre a los tres fallos, y el número
            // de aciertos antes de eso coincide por azar cerca de un tercio de las
            // veces. Lo que sí tiene que divergir es la receta: si la semilla no la
            // moviera, todas las partidas serían la misma.
            var inputs = new[] { 0, 0, 0, 0, 0, 0, 0 };
            var recipes = new HashSet<string>();
            var scores = new HashSet<(int, int, int)>();

            foreach (uint seed in new uint[] { 111, 999, 4242, 7, 31337, 2024, 88, 5150 })
            {
                var game = new CookingGame();
                game.Start(seed, 3);
                recipes.Add(game.RecipeName);

                foreach (int i in inputs) if (!game.IsOver) game.Step(i);
                var result = game.Finish();
                scores.Add((result.Successes, result.Failures, result.Points));
            }

            Assert.Greater(recipes.Count, 1,
                "ocho semillas dan siempre la misma receta: la semilla no se usa");
            Assert.Greater(scores.Count, 1,
                "ocho semillas dan siempre el mismo marcador: las acciones no cambian");
        }

        // ─────────────────────────────────────────────────────────────────
        // 8. Victoria en pesca: recoger con cuidado
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Fishing_AlternandoGana()
        {
            var game = new FishingGame();
            game.Start(42, 1); // distancia = 30

            // Alternar: reel, wait, reel, wait...
            int step = 0;
            while (!game.IsOver && step < 200)
            {
                game.Step(step % 2); // 0, 1, 0, 1...
                step++;
            }

            var result = game.Finish();
            // Con suficiente alternancia debería ganar
            Assert.IsTrue(result.IsVictory,
                "alternando reel y wait debería pescar sin romper el sedal");
        }

        // ─────────────────────────────────────────────────────────────────
        // 9. Ritmo: combo se rompe con Miss
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Rhythm_MissRompeCombo()
        {
            var game = new RhythmGame();
            game.Start(42, 2); // varias notas

            // dos perfectos
            game.Step(0);
            Assert.AreEqual(1, game.CurrentCombo);
            game.Step(0);
            Assert.AreEqual(2, game.CurrentCombo);

            // un fallo
            game.Step(999);
            Assert.AreEqual(0, game.CurrentCombo);
            Assert.AreEqual(2, game.MaxCombo, "max combo debería guardar la racha anterior");
        }

        // ─────────────────────────────────────────────────────────────────
        // 10. Dificultad fuera de rango se clampea
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Cooking_DificultadCeroSeClampeaAUno()
        {
            var game = new CookingGame();
            game.Start(42, 0);
            Assert.AreEqual(3, game.TotalSteps, "diff 0 → 3 pasos (base 2 + 1)");
        }

        [Test]
        public void Cooking_DificultadAltaSeClampeaACinco()
        {
            var game = new CookingGame();
            game.Start(42, 99);
            Assert.AreEqual(7, game.TotalSteps, "diff 99 → 7 pasos (base 2 + 5)");
        }

        [Test]
        public void Fishing_DificultadAltaFunciona()
        {
            var game = new FishingGame();
            game.Start(42, 5);

            // alternar con cabeza
            int step = 0;
            while (!game.IsOver && step < 300)
            {
                // Si la tensión está alta, aguantar
                if (game.Tension > 60)
                    game.Step(1);
                else
                    game.Step(0);
                step++;
            }

            Assert.IsTrue(game.IsOver);
            var result = game.Finish();
            Assert.GreaterOrEqual(result.Coins, 0);
            Assert.GreaterOrEqual(result.Experience, 0);
        }

        [Test]
        public void Rhythm_DificultadAltaMasNotas()
        {
            var low = new RhythmGame();
            low.Start(42, 1);
            int lowNotes = low.TotalNotes;

            var high = new RhythmGame();
            high.Start(42, 5);
            int highNotes = high.TotalNotes;

            Assert.Greater(highNotes, lowNotes,
                "dificultad 5 debería tener más notas que dificultad 1");
        }
    }
}
