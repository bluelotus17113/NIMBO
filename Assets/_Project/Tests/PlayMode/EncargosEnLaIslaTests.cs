using System.Collections;
using System.Collections.Generic;
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
    /// Que el tablón de encargos exista **en el juego**: en la plaza y en la barra.
    /// </summary>
    /// <remarks>
    /// La misma prueba que hizo falta para la crónica, por la misma razón. Las
    /// peticiones llevaban meses funcionando y solo se podían leer entrando en la ficha
    /// de cada vecino, de uno en uno: el sistema estaba encendido y la forma de mirarlo
    /// lo dejaba en nada. Un servicio impecable sin sitio donde abrirlo no existe.
    ///
    /// No se comprueba aquí que ponerse delante del tablón lo abra, y no es un olvido:
    /// el interactor da preferencia a las personas sobre los objetos dentro de dos
    /// metros y medio, y los vecinos pasean por la plaza. La prueba fallaría los días
    /// que a alguien le diera por pararse al lado del tablón, que es justo el tipo de
    /// prueba que acaba desactivada por pesada.
    /// </remarks>
    public class EncargosEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena.
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
        public IEnumerator ElTablonEstaLevantadoEnLaPlaza()
        {
            yield return CargarYEmpezar();

            var world = Object.FindFirstObjectByType<Art.World.WorldView>();
            Assert.That(world, Is.Not.Null, "no se ha montado la isla");

            var board = Buscar(world.transform, "Tablón de encargos");
            Assert.That(board, Is.Not.Null,
                "el tablón no está en el mundo: los encargos vuelven a leerse solo de menú");

            // Comparado en el sistema de la isla y no en coordenadas absolutas: así la
            // prueba sigue valiendo si algún día la aldea se mueve entera.
            var esperado = world.transform.TransformPoint(Data.World.Archipelago.RequestBoard);
            Assert.That(Vector3.Distance(board.position, esperado), Is.LessThan(1f),
                "está puesto, pero no donde se pasa al venir del puente");
        }

        [UnityTest]
        public IEnumerator HayUnBotonParaLosEncargos()
        {
            yield return CargarYEmpezar();

            var textos = TextosDeBotones();

            Assert.That(textos, Is.Not.Empty, "no encuentro la barra de acciones");
            Assert.That(textos, Does.Contain("Encargos"),
                "cruzar el puente para comprobar que no hay nada pendiente no lo hace nadie " +
                $"dos veces. Botones encontrados: {string.Join(", ", textos)}");
        }

        [UnityTest]
        public IEnumerator LoQuePideUnVecinoSePuedeConsultarYCobrar()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IRequestService>(out var peticiones));
            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThan(0), "hace falta alguien que pida algo");

            var encargo = peticiones.Raise(censo.All[0].Id, Data.Requests.RequestKind.Material);
            Assert.That(encargo.TargetId, Is.Not.Empty,
                "sin material que pedir el encargo nace vacío: la isla de verdad sí suelta cosas");

            var demanda = peticiones.DemandOf(encargo.RequestId);
            Assert.That(demanda.WantsAnItem, Is.True);
            Assert.That(demanda.Quantity, Is.GreaterThan(0));

            Assert.That(peticiones.Resolve(encargo.RequestId), Is.False,
                "con la mochila vacía no se puede atender, y en la isla montada tampoco");

            peticiones.Refuse(encargo.RequestId);
        }

        private static Transform Buscar(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = Buscar(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static List<string> TextosDeBotones() => Pulsables.Textos();

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text))
                textos.Add(button.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
