using System.Collections;
using Nimbo.Art.PlayerView;
using Nimbo.Art.World;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Island;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que el Árbol Nimbo exista **en el juego**: registrado, en pie, y con una forma
    /// de hablarle desde el mundo.
    /// </summary>
    /// <remarks>
    /// Es el quinto sistema que estaba escrito, registrado y apagado: el servicio
    /// vivía en el registro y no había ni un cartel, ni un botón, ni una tecla que
    /// llamara a su única función pública. Por eso la prueba de la costura —
    /// <see cref="DelanteDelArbolElCartelOfreceHablarle"/>— no pregunta por el
    /// servicio, sino por lo único que el jugador puede mirar: el cartel del
    /// interactuador cuando te pones delante.
    /// </remarks>
    public class ArbolEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator ElArbolEstaRegistradoYEnPieEnElCentro()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<NimboTree>(out _),
                "el servicio del árbol no está en el registro: en el juego no existe");

            var arbol = GameObject.Find("Árbol Nimbo");
            Assert.That(arbol, Is.Not.Null, "no hay ningún Árbol Nimbo levantado");

            float alCentro = new Vector2(arbol.transform.position.x,
                                         arbol.transform.position.z).magnitude;
            Assert.That(alCentro, Is.LessThan(1f),
                "el árbol no está en el centro de la plaza, que es donde se le busca");
        }

        [UnityTest]
        public IEnumerator HablarleEnLaIslaCargadaCierraElDiaYEntrega()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet(out NimboTree arbol));
            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economy));
            Assert.IsTrue(ServiceRegistry.TryGet<Core.Time.GameClock>(out var clock));

            long antes = economy.Wallet.Coins;

            Assert.IsTrue(arbol.TryTalk(out TreeGift regalo), "el primer día negó el regalo");
            Assert.IsFalse(arbol.CanTalkToday, "habló y el día siguió abierto");

            Assert.That(economy.Wallet.Coins - antes, Is.EqualTo(regalo.Coins),
                "en la isla cargada, lo prometido por el árbol no llegó a la cartera");

            // Dormir hasta la mañana siguiente: el ritual tiene que reabrirse, que es
            // toda la razón de ser de este sistema.
            int minutes = (24 - clock.Hour + 8) % 24 * 60 - clock.Minute;
            if (minutes <= 0) minutes += 24 * 60;
            clock.Advance(minutes);

            Assert.IsTrue(arbol.CanTalkToday,
                "al día siguiente el árbol seguía callado: sin reabertura no hay ritual");
        }

        [UnityTest]
        public IEnumerator DelanteDelArbolElCartelOfreceHablarle()
        {
            yield return CargarYEmpezar();

            var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.That(interactor, Is.Not.Null, "no hay protagonista en el mundo");
            Assert.IsTrue(ServiceRegistry.TryGet(out NimboTree arbol));

            // Los vecinos pasean por la plaza, que es justo donde está el árbol, y el
            // cartel de una persona siempre gana al de un objeto. Se les manda lejos
            // para que esta prueba mida el cartel del árbol y no la suerte de quién
            // esté paseando por ahí ese día.
            var mundo = Object.FindFirstObjectByType<WorldView>();
            Assert.That(mundo, Is.Not.Null, "no hay isla levantada");
            var lejos = Data.World.Archipelago.HomeCentre + Vector3.forward * 10f;
            foreach (var islander in ServiceRegistry.Get<IIslanderRegistry>().All)
                if (mundo.TryGetIslander(islander.Id, out var cuerpo))
                    cuerpo.position = lejos;

            // Al este del tronco, a cinco metros: dentro del alcance del cartel y
            // fuera del colisionador del pie, que ensancha metro y medio.
            Teletransportar(interactor.transform, new Vector3(5f, 2f, 0f));
            interactor.transform.rotation = Quaternion.LookRotation(Vector3.left);

            yield return new WaitForSeconds(0.5f);   // el objetivo se recalcula despacio

            float caida = Vector3.Distance(
                new Vector3(interactor.transform.position.x, 0f, interactor.transform.position.z),
                new Vector3(5f, 0f, 0f));
            Assert.That(caida, Is.LessThan(6f),
                "el protagonista no está donde se le dejó: se ha caído o lo han rescatado");

            // Por nombre y no por enum: el valor TargetKind.Tree llega con el enganche
            // del interactuador (Informes/informe-arbol.md §D) y así esta prueba
            // compila antes de aplicarlo y lo verifica en cuanto está.
            Assert.That(interactor.Kind.ToString(), Is.EqualTo("Tree"),
                $"delante del árbol sale «{interactor.Kind}»: «{interactor.Prompt}». " +
                "Falta el enganche D del informe-arbol.md en PlayerInteractor.cs");

            Assert.That(interactor.Prompt, Does.Contain("Árbol").And.Contains("Hablar"),
                $"el cartel no ofrece hablarle: «{interactor.Prompt}»");

            // Pulsar E de verdad: Act es privada porque es la única tecla de acción
            // del juego, pero es exactamente lo que el jugador dispara.
            var act = typeof(PlayerInteractor).GetMethod("Act",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            Assert.That(act, Is.Not.Null, "no encuentro Act en el interactuador");
            act.Invoke(interactor, null);

            Assert.IsFalse(arbol.CanTalkToday,
                "el cartel estaba y la pulsación no llegó al árbol: el caso Tree falta " +
                "en Act (enganche D de Informes/informe-arbol.md)");
        }

        /// <summary>
        /// Lleva al protagonista a un punto, de verdad.
        /// </summary>
        /// <remarks>
        /// Hay que apagar el <c>CharacterController</c> para moverlo: con él encendido
        /// se come el cambio de posición. Lo mismo que hace
        /// <c>MinijuegosEnLaIslaTests</c>, que fue quien lo descubrió.
        /// </remarks>
        private static void Teletransportar(Transform who, Vector3 to)
        {
            var controller = who.GetComponent<CharacterController>();

            if (controller != null) controller.enabled = false;
            who.position = to;
            if (controller != null) controller.enabled = true;
        }
    }
}
