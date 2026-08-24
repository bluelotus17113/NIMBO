using Nimbo.UI.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que los ajustes de pantalla existan de verdad: que se guarden, que lleguen
    /// a un PanelSettings real y que el panel los pinte.
    /// </summary>
    /// <remarks>
    /// Un ajuste que se pierde al salir es peor que no tenerlo, y uno que solo
    /// mueve el deslizador sin cambiar nada es peor todavía: promete y no cumple.
    /// Aquí están las dos mitades en editor; la cadena entera contra un UIDocument
    /// vivo está en PlayMode (<c>EscalaEnElPanelTests</c>).
    ///
    /// Las claves se apuntan antes y se devuelven después: PlayerPrefs del editor
    /// es donde ya viven los volúmenes, y una prueba no tiene derecho a dejarle al
    /// jugador una escala a 1,25 que él nunca pidió.
    /// </remarks>
    public class AjustesDePantallaTests
    {
        private bool _teniaModo, _teniaTamano, _teniaEscala;
        private int _modo, _ancho, _alto;
        private float _escala;

        [SetUp]
        public void ApuntarPrevios()
        {
            _teniaModo = PlayerPrefs.HasKey("nimbo.pantalla.modo");
            _modo = PlayerPrefs.GetInt("nimbo.pantalla.modo", 0);
            _teniaTamano = PlayerPrefs.HasKey("nimbo.pantalla.ancho");
            _ancho = PlayerPrefs.GetInt("nimbo.pantalla.ancho", 0);
            _alto = PlayerPrefs.GetInt("nimbo.pantalla.alto", 0);
            _teniaEscala = PlayerPrefs.HasKey("nimbo.pantalla.escala");
            _escala = PlayerPrefs.GetFloat("nimbo.pantalla.escala", 1f);

            PlayerPrefs.DeleteKey("nimbo.pantalla.modo");
            PlayerPrefs.DeleteKey("nimbo.pantalla.ancho");
            PlayerPrefs.DeleteKey("nimbo.pantalla.alto");
            PlayerPrefs.DeleteKey("nimbo.pantalla.escala");
        }

        [TearDown]
        public void DevolverPrevios()
        {
            if (_teniaModo) PlayerPrefs.SetInt("nimbo.pantalla.modo", _modo);
            else PlayerPrefs.DeleteKey("nimbo.pantalla.modo");

            if (_teniaTamano)
            {
                PlayerPrefs.SetInt("nimbo.pantalla.ancho", _ancho);
                PlayerPrefs.SetInt("nimbo.pantalla.alto", _alto);
            }
            else
            {
                PlayerPrefs.DeleteKey("nimbo.pantalla.ancho");
                PlayerPrefs.DeleteKey("nimbo.pantalla.alto");
            }

            if (_teniaEscala) PlayerPrefs.SetFloat("nimbo.pantalla.escala", _escala);
            else PlayerPrefs.DeleteKey("nimbo.pantalla.escala");

            PlayerPrefs.Save();
        }

        [Test]
        public void UnAjusteCambiadoSobreviveAGuardarYVolverALeer()
        {
            DisplayPrefs.Set(1280, 720, FullScreenMode.Windowed);
            DisplayPrefs.SetScale(1.25f);
            DisplayPrefs.Flush();

            // «Volver a leer» es literal: los accesos leen de PlayerPrefs en cada
            // llamada, igual que AudioPrefs, así que un proceso recién arrancado
            // vería exactamente esto.
            Assert.AreEqual(FullScreenMode.Windowed, DisplayPrefs.Mode,
                "el modo de ventana no sobrevivió al guardado");
            Assert.AreEqual(1280, DisplayPrefs.Width,
                "el ancho no sobrevivió al guardado");
            Assert.AreEqual(720, DisplayPrefs.Height,
                "el alto no sobrevivió al guardado");
            Assert.AreEqual(1.25f, DisplayPrefs.Scale, 0.0001f,
                "la escala no sobrevivió al guardado");
        }

        [Test]
        public void LaEscalaSeQuedaDentroDelRangoUtil()
        {
            DisplayPrefs.SetScale(9f);
            Assert.AreEqual(DisplayPrefs.MaxScale, DisplayPrefs.Scale, 0.0001f,
                "una escala desbocada por arriba se coló sin recortar");

            DisplayPrefs.SetScale(0.01f);
            Assert.AreEqual(DisplayPrefs.MinScale, DisplayPrefs.Scale, 0.0001f,
                "una escala mínima ridícula se coló sin recortar");
        }

        [Test]
        public void LaEscalaDivideLaReferenciaDeUnPanelDeVerdad()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 1f;

            try
            {
                DisplayPrefs.SetScale(1.25f);

                DisplayPrefs.ApplyUiScale(settings);
                Assert.AreEqual(new Vector2Int(1536, 864), settings.referenceResolution,
                    "escala 1,25 sobre 1920×1080 debería pedir una pantalla lógica " +
                    "de 1536×864: la referencia no se dividió");

                // Aplicar dos veces no puede componer (1,25 × 1,25 = 1,56): la base
                // original se tiene que recordar aunque el panel ya esté dividido.
                DisplayPrefs.ApplyUiScale(settings);
                Assert.AreEqual(new Vector2Int(1536, 864), settings.referenceResolution,
                    "aplicar dos veces compuso la escala: la base no se estaba recordando");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ElPanelEnseaLosTresControlesDePantalla()
        {
            var opciones = new OptionsPanel(() => { });

            Assert.IsNotNull(opciones.Root.Query<Slider>(name: "escala-interfaz").First(),
                "sin deslizador de escala no hay respuesta para quien no lee las fuentes pequeñas");
            Assert.IsNotNull(opciones.Root.Query<DropdownField>(name: "modo-ventana").First(),
                "falta el control de modo de ventana");
            Assert.IsNotNull(opciones.Root.Query<DropdownField>(name: "resolucion").First(),
                "falta el control de resolución");
        }
    }
}
