using System.Collections;
using Nimbo.Art.PlayerView;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que los tres minijuegos existan **en el juego**: servicio, pantalla y por dónde.
    /// </summary>
    /// <remarks>
    /// Son el caso más claro de sistema apagado que ha tenido este proyecto: tres
    /// ficheros de lógica con su suite de pruebas en verde y **cero usos** fuera de su
    /// propia carpeta. Nadie los construía, nadie los arrancaba y nadie cobraba sus
    /// puntos. Por eso lo que se comprueba aquí no es que funcionen —de eso ya se
    /// encarga <c>MinigameTests</c>— sino que se pueda llegar a ellos.
    /// </remarks>
    public class MinijuegosEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
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
        public IEnumerator LosMinijuegosEstanRegistrados()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.IsRegistered<IMinigameService>(),
                "nadie construyó el servicio: los tres vuelven a ser tres ficheros sueltos");
        }

        [UnityTest]
        public IEnumerator ElFogonEstaLevantadoJuntoALaCasa()
        {
            yield return CargarYEmpezar();

            var stove = Buscar("fogon");
            Assert.That(stove, Is.Not.Null,
                "la cocina volvería a ser una pestaña de menú sin cocina en ninguna parte");

            float lejos = Vector3.Distance(stove.position, Data.Player.PlayerHome.Stove);
            Assert.That(lejos, Is.LessThan(1f), "el fogón no está donde dice que está");

            // Y no encima de la mesa de trabajo: son dos verbos y hace falta poder
            // ponerse delante de uno sin activar el otro.
            float aLaMesa = Vector3.Distance(Data.Player.PlayerHome.Stove,
                                             Data.Player.PlayerHome.Bench);
            Assert.That(aLaMesa, Is.GreaterThan(Data.Player.PlayerHome.UseRange * 2f),
                "el fogón y la mesa se pisan: al acercarte saldría el cartel de la que caiga");
        }

        [UnityTest]
        public IEnumerator PedirUnMinijuegoAbreLaPantalla()
        {
            yield return CargarYEmpezar();

            Assert.That(Visible("minijuego"), Is.False, "no puede abrirse sola");

            EventBus.Publish(new MinigameRequested(MinigameKind.Fishing, 1));
            yield return null;

            Assert.That(Visible("minijuego"), Is.True,
                "el aviso se publicó y no lo escuchaba nadie: la pantalla no está enchufada");

            Assert.IsTrue(ServiceRegistry.TryGet<IMinigameService>(out var minijuegos));
            Assert.That(minijuegos.Running, Is.EqualTo(MinigameKind.Fishing));

            minijuegos.Abandon();
        }

        [UnityTest]
        public IEnumerator ConLaCanaEnLaManoElBordeDeLaIslaOfrecePescar()
        {
            yield return CargarYEmpezar();

            var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.That(interactor, Is.Not.Null, "no hay protagonista en el mundo");

            Assert.IsTrue(ServiceRegistry.TryGet<IInventoryService>(out var mochila));
            Assert.That(mochila.TryStore("tool_cana", 1, out _), Is.Not.EqualTo(
                StoreResult.UnknownItem), "el catálogo no conoce la caña");
            PonerEnLaMano(mochila, "tool_cana");
            Assert.That(mochila.ToolInHand, Is.EqualTo(ToolKind.FishingRod));

            // El borde este de tu propia isla: no hay vecinos que se pongan delante, y
            // el cartel de una persona siempre gana al de un objeto.
            //
            // Dentro del margen de sobra y no pegado al canto: el contorno del prado
            // baja hacia el vacío en el último trozo, y ahí el muñeco se cae y la red de
            // seguridad lo devuelve al principio. Eso fue lo que hizo fallar esta prueba
            // la primera vez —«sale ""»— y no tenía nada que ver con la caña.
            var borde = Data.World.Archipelago.HomeCentre +
                        new Vector3(Data.World.Archipelago.HomeRadius * 0.83f, 2f, 0f);
            Teletransportar(interactor.transform, borde);

            yield return new WaitForSeconds(0.4f);   // el objetivo se recalcula despacio

            float aLaOrilla = Vector3.Distance(
                new Vector3(interactor.transform.position.x, 0f, interactor.transform.position.z),
                new Vector3(borde.x, 0f, borde.z));
            Assert.That(aLaOrilla, Is.LessThan(6f),
                "el protagonista no está donde se le dejó: se ha caído y lo han devuelto");

            Assert.That(interactor.Kind, Is.EqualTo(TargetKind.FishingSpot),
                $"en el borde y con la caña sale «{interactor.Prompt}»");
        }

        /// <summary>
        /// Lleva al protagonista a un punto, de verdad.
        /// </summary>
        /// <remarks>
        /// Hay que apagar el <c>CharacterController</c> para moverlo: con él encendido
        /// se come el cambio de posición y lo deja donde estaba. Es lo mismo que hace la
        /// red de seguridad del <c>PlayerBody</c> para rescatarlo, y es lo que hizo
        /// fallar esta prueba dos veces: el muñeco no se había caído, es que nunca se
        /// movió del sitio donde aparece.
        /// </remarks>
        private static void Teletransportar(Transform who, Vector3 to)
        {
            var controller = who.GetComponent<CharacterController>();

            if (controller != null) controller.enabled = false;
            who.position = to;
            if (controller != null) controller.enabled = true;
        }

        private static void PonerEnLaMano(IInventoryService bag, string catalogId)
        {
            var slots = bag.Slots;
            for (int i = 0; i < slots.Count && i < bag.HotbarSize; i++)
            {
                if (slots[i].CatalogId != catalogId || slots[i].Quantity <= 0) continue;
                bag.Select(i);
                return;
            }
            Assert.Fail($"{catalogId} no ha entrado en la barra de abajo");
        }

        private static Transform Buscar(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>¿Hay en pantalla un elemento con ese nombre y está visible?</summary>
        private static bool Visible(string name)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;

                var found = document.rootVisualElement.Q(name);
                if (found != null && found.style.display == DisplayStyle.Flex) return true;
            }
            return false;
        }
    }
}
