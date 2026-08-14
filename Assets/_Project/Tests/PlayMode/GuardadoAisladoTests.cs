using System.Collections;
using System.IO;
using Nimbo.Core.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Las pruebas no pueden tocar la partida de quien esté jugando.
    /// </summary>
    /// <remarks>
    /// Esta es la prueba que vigila al vigilante, y hace falta porque el vigilante ya
    /// ha fallado tres veces. <c>AislarGuardadoEnPruebasDeJuego</c> desvía el guardado
    /// a una carpeta temporal en su <c>OneTimeSetUp</c>, lo deja escrito en el registro
    /// —«el guardado va a /tmp/…»— y todo el mundo se queda tranquilo. Pero entrar en
    /// modo juego **recarga el dominio de scripts y pone a cero los estáticos**, así
    /// que el desvío moría antes de la primera prueba y cada arranque escribía encima
    /// de la partida del jugador. Se veía solo mirando la fecha del fichero, y nadie
    /// mira la fecha de un fichero.
    ///
    /// Lo que se comprueba aquí no es que el aviso salga: es que la carpeta a la que se
    /// escribe de verdad, ahora mismo y dentro de modo juego, no es la del jugador.
    /// </remarks>
    public class GuardadoAisladoTests
    {
        [UnityTest]
        public IEnumerator ElGuardadoNoApuntaALaCarpetaDelJugador()
        {
            // Dentro de modo juego, que es donde muere el desvío. Un [Test] normal
            // corre antes de la recarga y pasaría aunque estuviera roto.
            yield return null;

            Assert.That(SaveSystem.SaveDirectory, Is.Not.EqualTo(Application.persistentDataPath),
                        "el guardado de las pruebas apunta a la carpeta del jugador: " +
                        "una prueba de arranque va a escribir encima de su partida");
        }

        [UnityTest]
        public IEnumerator LoQueSeEscribeCaeEnLaCarpetaTemporal()
        {
            yield return null;

            // No basta con mirar la ruta: lo que cuenta es dónde acaba el fichero.
            var save = new Nimbo.Data.Save.SaveGame { ElapsedMinutes = 1234 };
            SaveSystem.Write(save);

            Assert.That(File.Exists(SaveSystem.SavePath), "no ha escrito nada");
            Assert.That(Path.GetDirectoryName(SaveSystem.SavePath),
                        Is.Not.EqualTo(Application.persistentDataPath),
                        "ha escrito en la carpeta del jugador");
        }
    }
}
