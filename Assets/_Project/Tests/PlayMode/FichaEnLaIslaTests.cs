using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Social;
using Nimbo.UI.Islander;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que la ficha de un habitante exista **en el juego** y cumpla sus tres contratos:
    /// que quepa, que no borre lo que dice y que enseñe la vida social de la isla.
    /// </summary>
    /// <remarks>
    /// Los tres fallos eran de la misma familia: el sistema funcionaba y la forma de
    /// mirarlo lo dejaba en nada. La ficha medía más que la pantalla y lo que se caía
    /// era justo la tarjeta de relaciones; el refresco de 0,4 s borraba cada aviso
    /// antes de poder leerlo; y saber quién anda con quién pedía abrir las fichas de
    /// una en una.
    ///
    /// La sección social se prueba suelta —como hizo <see cref="CortejoEnLaIslaTests"/>—
    /// contra los servicios registrados de la isla de verdad, y la ficha entera se abre
    /// contra ellos también.
    /// </remarks>
    public class FichaEnLaIslaTests
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
        public IEnumerator LaFichaLlevaScroll_PorqueMideMasQueLaPantalla()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThan(0));

            var ficha = new IslanderPanel();
            ficha.Show(censo.All[0].Id);

            Assert.That(BuscarTodos<ScrollView>(ficha.Root).Count, Is.GreaterThanOrEqualTo(1),
                "la ficha apila seis tarjetas y mide más de lo que da la pantalla: " +
                "sin scroll, lo que se sale por abajo —las relaciones— no existe");
        }

        [UnityTest]
        public IEnumerator LasSieteTarjetasDeLaFichaEstanDondeSeLeen()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));

            var ficha = new IslanderPanel();
            ficha.Show(censo.All[0].Id);

            var textos = Textos(ficha.Root);

            Assert.That(textos, Does.Contain("Te está pidiendo"));
            Assert.That(textos, Does.Contain("Qué le dices"));
            Assert.That(textos, Does.Contain("Qué le gusta"),
                "regalar funciona y la interfaz no lo nombraba en ningún sitio");
            Assert.That(textos, Does.Contain("Trabajo"));
            Assert.That(textos, Does.Contain("Su casa"));
            Assert.That(textos, Does.Contain("Con quién anda"));
            Assert.That(textos, Does.Contain("Quién anda con quién"),
                "la vida social de la isla entera se tenía que recomponer abriendo " +
                "fichas de una en una");
        }

        [UnityTest]
        public IEnumerator ElMapaSocialNombraParejasYRinas()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThanOrEqualTo(3),
                "hacen falta tres vecinos para enseñar a la vez una pareja y una riña");

            var uno = censo.All[0];
            var otro = censo.All[1];
            var tercero = censo.All[2];

            // Una pareja asentada, por los dos lados: la agenda es asimétrica a
            // propósito y las etapas se escriben en las dos.
            Sembrar(uno, otro.Id, RomanceStage.Dating, ConflictStage.None);
            Sembrar(otro, uno.Id, RomanceStage.Dating, ConflictStage.None);

            // Y una riña abierta con el tercero, que es lo otro que hay que ver de un
            // vistazo sin abrir fichas.
            Sembrar(uno, tercero.Id, RomanceStage.None, ConflictStage.Feud);
            Sembrar(tercero, uno.Id, RomanceStage.None, ConflictStage.Feud);

            var ficha = new IslanderPanel();
            ficha.Show(tercero.Id);

            var textos = Textos(ficha.Root);

            Assert.That(textos, Does.Contain("saliendo"),
                "la pareja está sembrada en los datos y el mapa no la nombra");
            Assert.That(textos, Does.Contain("enemistados"),
                "la riña abierta es justo lo que el mapa promete enseñar");

            // El flechazo es direccional y se cuenta como historia distinta por lado:
            // mismo criterio que usa el tablón de noticias para no repetirse.
            Sembrar(otro, tercero.Id, RomanceStage.Crush, ConflictStage.None);
            ficha.Refresh();

            Assert.That(Textos(ficha.Root), Does.Contain("le gusta"));
        }

        [UnityTest]
        public IEnumerator ElAvisoDeUnGestoSobreviveALosRefrescos()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));

            var seccion = new SocialSection();
            string id = censo.All[0].Id;
            seccion.Refresh(id);
            seccion.Say("aviso de prueba");

            // Tres pasadas seguidas: el refresco real corre cada 0,4 s y antes de este
            // arreglo cualquiera de ellas borraba el aviso recién escrito.
            seccion.Refresh(id);
            seccion.Refresh(id);
            seccion.Refresh(id);

            Assert.That(Textos(seccion.Root), Does.Contain("aviso de prueba"),
                "el aviso vive menos de medio segundo si cada refresco lo tira: así " +
                "nadie llega a leer nunca «por hoy ya está bien»");

            seccion.Refresh(censo.All[1].Id);

            Assert.That(Textos(seccion.Root), Does.Not.Contain("aviso de prueba"),
                "el aviso habla de una persona concreta: cambiada la persona, se va");
        }

        [UnityTest]
        public IEnumerator LosBotonesNoSeDerribanSiNoCambiaNada()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));

            var seccion = new SocialSection();
            string id = censo.All[0].Id;
            seccion.Refresh(id);

            var antes = Botones(seccion.Root);
            Assert.That(antes, Is.Not.Empty);

            seccion.Refresh(id);
            seccion.Refresh(id);

            var despues = Botones(seccion.Root);

            Assert.That(despues.Count, Is.EqualTo(antes.Count),
                "sin cambios de etapa ni de puerta, la fila tiene que ser la misma");

            for (int i = 0; i < antes.Count; i++)
                Assert.AreSame(antes[i], despues[i],
                    "derribar y reponer los botones bajo el cursor se come los clics");

            // Y en la ficha entera pasa lo mismo: peticiones incluidas.
            var ficha = new IslanderPanel();
            ficha.Show(id);
            var filaFicha = Botones(ficha.Root);
            ficha.Refresh();
            var filaTrasRefresco = Botones(ficha.Root);

            Assert.That(filaTrasRefresco.Count, Is.EqualTo(filaFicha.Count));
            if (filaFicha.Count > 0)
                Assert.AreSame(filaFicha[0], filaTrasRefresco[0],
                    "el refresco de 0,4 s no tiene por qué reconstruir las peticiones " +
                    "si no ha cambiado ninguna");
        }

        [UnityTest]
        public IEnumerator LosGustosDelVecinoSalenEnSuFicha()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia));

            var vecino = censo.All[0];

            // Una comida de verdad del catálogo y no un id inventado: el catálogo
            // escupe un error de log por cada desconocido, y un error de log dentro
            // de una prueba la tumba aunque la tarjeta lo llegue a pintar bonito.
            // Si esta isla se creó sin catálogo de comida, la tarjeta dice en su
            // lugar que nadie ha apuntado nada, y eso también vale.
            var comidas = new List<string>();
            foreach (var comida in economia.ItemsOfCategory(ItemCategory.Food))
                comidas.Add(comida.CatalogId);
            if (comidas.Count > 0)
            {
                vecino.Tastes.LovedFoods.Clear();
                vecino.Tastes.LovedFoods.Add(comidas[0]);
            }

            var ficha = new IslanderPanel();
            ficha.Show(vecino.Id);

            var textos = Textos(ficha.Root);

            bool visible = textos.Exists(t =>
                t.Contains("Le encanta") || t.Contains("Nadie ha apuntado"));
            Assert.IsTrue(visible,
                "sin esta tarjeta, regalar es un sistema invisible: existe, da " +
                "experiencia y abre logros, y la interfaz no lo decía nunca");
        }

        [UnityTest]
        public IEnumerator CadaEstadoTieneUnaPalabra()
        {
            yield return CargarYEmpezar();

            var record = RelationshipRecord.NewWith("alguien");

            record.Romance = RomanceStage.Married;
            Assert.That(SocialLabels.StatusOf(record), Is.EqualTo("casados"));

            record.Romance = RomanceStage.Dating;
            Assert.That(SocialLabels.StatusOf(record), Is.EqualTo("saliendo"));

            // Recién declarado no es «se conocen»: falta la respuesta y eso se dice.
            record.Romance = RomanceStage.Confessed;
            Assert.That(SocialLabels.StatusOf(record), Is.EqualTo("pendiente de respuesta"));

            record.Romance = RomanceStage.None;
            record.Conflict = ConflictStage.Feud;
            Assert.That(SocialLabels.StatusOf(record), Is.EqualTo("enemistados"));
        }

        /// <summary>Escribe a mano una relación en la agenda de un vecino.</summary>
        private static void Sembrar(Data.Islanders.IslanderData islander, string otherId,
                                    RomanceStage romance, ConflictStage conflict)
        {
            var record = islander.Relationships.GetOrCreate(otherId);
            record.Romance = romance;
            record.Conflict = conflict;
            if (romance != RomanceStage.None) record.Friendship = FriendshipStage.Friend;
            islander.Relationships.Set(record);
        }

        private static List<T> BuscarTodos<T>(VisualElement root) where T : VisualElement
        {
            var found = new List<T>();
            Buscar(root, found);
            return found;
        }

        private static void Buscar<T>(VisualElement element, List<T> found)
            where T : VisualElement
        {
            if (element is T hit) found.Add(hit);

            for (int i = 0; i < element.childCount; i++)
                Buscar(element[i], found);
        }

        private static List<string> Textos(VisualElement root)
        {
            var textos = new List<string>();
            Recorrer(root, textos);
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text))
                textos.Add(button.text);
            else if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }

        private static List<Button> Botones(VisualElement root) => BuscarTodos<Button>(root);
    }
}
