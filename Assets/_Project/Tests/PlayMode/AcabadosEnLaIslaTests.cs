using System.Collections;
using System.IO;
using Nimbo.Core.Events;
using Nimbo.Core.Save;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Housing;
using Nimbo.UI.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Los acabados se ven: entrar en casa, aplicar uno desde el panel de amueblar y
    /// comprobar que la habitación lo pinta de verdad, al momento y al volver a entrar.
    /// </summary>
    /// <remarks>
    /// Antes de este cableado, <c>SetWallpaper</c> llevaba meses en el contrato sin un
    /// solo llamador y las paredes se pintaban de un color escrito a mano: cuarenta
    /// acabados con precio en la tienda que nadie podía ver puestos. Cada pieza tenía
    /// su prueba y ninguna cubría la costura, que es donde se rompe siempre.
    /// </remarks>
    public class AcabadosEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // Igual que BootTests: el arranque sobrevive a cambiar de escena, y una
            // partida guardada por otra prueba arrancaría una isla que no es la de aquí.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);

            foreach (var path in new[] { SaveSystem.SavePath, SaveSystem.BackupPath })
                if (File.Exists(path)) File.Delete(path);
        }

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Prueba", Data.Islanders.AppearanceData.Default));
            yield return null;
            yield return null;
        }

        /// <summary>El nombre del material liso de ToonPalette para ese color del catálogo.</summary>
        private static string MaterialDe(string hexSinAlmohadilla) => "Nimbo_" + hexSinAlmohadilla;

        [UnityTest]
        public IEnumerator CambiarElPapelDeLaParedSeVeAlMomentoYAlVolverAEntrar()
        {
            yield return CargarYEmpezar();

            EventBus.Publish(new InteriorEntered("", "Tu casa"));
            yield return null;
            yield return null;

            var interior = Object.FindFirstObjectByType<Art.World.InteriorView>();
            Assert.IsNotNull(interior.CurrentRoom, "no hay habitación montada");

            var pared = GameObject.Find("pared_n");
            Assert.IsNotNull(pared, "la pared norte no se dibujó");

            // De serie pinta el papel por defecto del catálogo (wall_nube_blanca,
            // #F5F0E8), no un color escrito a mano que ningún id respalda.
            Assert.AreEqual(MaterialDe("F5F0E8"),
                            pared.GetComponent<MeshRenderer>().sharedMaterial.name,
                            "la pared de serie no es el papel por defecto del catálogo");

            var economy = ServiceRegistry.Get<IEconomyService>();
            const string papel = "wall_cielo_diurno"; // #B8D8F0 en catalogo_acabados.json
            economy.Inventory.Add(papel, 1);

            var panel = new FurnishPanel();
            panel.Show();

            Assert.IsTrue(panel.TryApplyFinish(papel),
                          "el panel no aplicó el papel teniendo la unidad en la mochila");
            yield return null;

            Assert.AreEqual(papel, interior.CurrentRoom.WallpaperNorth,
                            "el id guardado no cambió al aplicar el acabado");
            Assert.AreEqual(0, economy.Inventory.CountOf(papel),
                            "aplicar el papel no gastó la unidad");

            // La aplicación repinta por aviso: el objeto «pared_n» es otro, hay que
            // volver a buscarlo antes de mirarle el material.
            var repintada = GameObject.Find("pared_n");
            Assert.IsNotNull(repintada, "la reforma tiró la pared y no la volvió a levantar");
            Assert.AreEqual(MaterialDe("B8D8F0"),
                            repintada.GetComponent<MeshRenderer>().sharedMaterial.name,
                            "se aplicó el papel pero la pared sigue pintada del color de serie");

            // Y se queda: salir y volver a entrar tiene que leer el id guardado.
            EventBus.Publish(new InteriorExited());
            yield return null;

            EventBus.Publish(new InteriorEntered("", "Tu casa"));
            yield return null;
            yield return null;

            var otraVez = GameObject.Find("pared_n");
            Assert.AreEqual(MaterialDe("B8D8F0"),
                            otraVez.GetComponent<MeshRenderer>().sharedMaterial.name,
                            "al volver a entrar la pared volvió al color de serie: el acabado no se leyó del guardado");
        }

        [UnityTest]
        public IEnumerator CambiarElSueloSeVeYSeQueda()
        {
            yield return CargarYEmpezar();

            EventBus.Publish(new InteriorEntered("", "Tu casa"));
            yield return null;
            yield return null;

            var interior = Object.FindFirstObjectByType<Art.World.InteriorView>();
            Assert.IsNotNull(interior.CurrentRoom, "no hay habitación montada");

            var suelo = GameObject.Find("suelo");
            Assert.IsNotNull(suelo, "el suelo no se dibujó");

            // De serie: el suelo más barato del catálogo, vinilo imitación madera
            // (#C8A882), que es del mismo color claro que tuvo siempre la cabaña.
            Assert.AreEqual(MaterialDe("C8A882"),
                            suelo.GetComponent<MeshRenderer>().sharedMaterial.name,
                            "el suelo de serie no es el acabado por defecto del catálogo");

            var economy = ServiceRegistry.Get<IEconomyService>();
            const string damero = "floor_suelo_de_baldosa_damero"; // #FFFFFF en el catálogo
            economy.Inventory.Add(damero, 1);

            var panel = new FurnishPanel();
            panel.Show();

            Assert.IsTrue(panel.TryApplyFinish(damero),
                          "el panel no aplicó el suelo teniendo la unidad en la mochila");
            yield return null;

            var room = interior.CurrentRoom;
            for (int y = 0; y < room.Height; y++)
                for (int x = 0; x < room.Width; x++)
                    Assert.AreEqual(damero, room.FloorAt(new GridCoord(x, y)),
                                    $"la casilla ({x},{y}) no quedó con el suelo aplicado");

            var repintado = GameObject.Find("suelo");
            Assert.AreEqual(MaterialDe("FFFFFF"),
                            repintado.GetComponent<MeshRenderer>().sharedMaterial.name,
                            "se aplicó el suelo pero la losa sigue pintada del color de serie");
            Assert.AreEqual(0, economy.Inventory.CountOf(damero),
                            "aplicar el suelo no gastó la unidad");
        }
    }
}
