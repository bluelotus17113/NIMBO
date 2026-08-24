using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Nimbo.UI.Menu;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// La cadena entera de la escala, contra un documento vivo: deslizador →
    /// preferencias → PanelSettings.
    /// </summary>
    /// <remarks>
    /// El encargo pide probar que la escala llega de verdad al panel, no que el
    /// deslizador se mueva. Aquí hay un UIDocument de verdad con un PanelSettings
    /// propio de laboratorio —misma mecánica que el compartido:
    /// <c>ScaleWithScreenSize</c>, 1920×1080, match 1— y se comprueba que mover el
    /// deslizador cambia SU referencia. Y que cambiar la resolución con el panel
    /// abierto no lo tira, que es donde este tipo de ajustes rompe cosas.
    ///
    /// Las claves se apuntan antes y se devuelven después, por la misma razón que
    /// en las pruebas de editor: las preferencias del jugador no se tocan.
    /// </remarks>
    public class EscalaEnElPanelTests
    {
        private PanelSettings _settings;
        private UIDocument _document;
        private OptionsPanel _opciones;

        private bool _teniaModo, _teniaTamano, _teniaEscala;
        private int _modo, _ancho, _alto;
        private float _escala;

        [UnitySetUp]
        public IEnumerator Montar()
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

            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _settings.referenceResolution = new Vector2Int(1920, 1080);
            _settings.match = 1f;

            var go = new GameObject("documento-de-prueba");
            _document = go.AddComponent<UIDocument>();
            _document.panelSettings = _settings;

            // Construir el panel dispara ApplySavedOnce, que resuelve ESTE
            // documento y aplica la escala guardada: con las claves limpias es un
            // no-op, así que la referencia queda exactamente como la pusimos.
            _opciones = new OptionsPanel(() => { });
            _document.rootVisualElement.Add(_opciones.Root);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            if (_document != null) Object.Destroy(_document.gameObject);
            if (_settings != null) Object.Destroy(_settings);

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
            yield break;
        }

        [UnityTest]
        public IEnumerator MoverElDeslizadorDeEscalaCambiaLaReferenciaDelPanel()
        {
            var slider = _opciones.Root.Query<Slider>(name: "escala-interfaz").First();
            Assert.IsNotNull(slider, "el panel no trae el deslizador de escala");

            slider.value = 1.25f; // mismo camino que un arrastre: el callback real

            yield return null;

            Assert.AreEqual(new Vector2Int(1536, 864), _settings.referenceResolution,
                "el deslizador se movió pero la escala no llegó al PanelSettings: " +
                "promete y no cumple");
        }

        [UnityTest]
        public IEnumerator CambiarLaResolucionConElPanelAbiertoNoLoTira()
        {
            var slider = _opciones.Root.Query<Slider>(name: "escala-interfaz").First();
            slider.value = 1.25f;
            yield return null;

            var modo = _opciones.Root.Query<DropdownField>(name: "modo-ventana").First();
            Assert.IsNotNull(modo, "falta el control de modo de ventana");

            modo.index = 1; // pasar a ventana: aquí Unity reconstruye el layout

            yield return null;
            yield return null;

            Assert.IsNotNull(_opciones.Root.panel,
                "el panel quedó fuera de todo panel tras cambiar la resolución");
            Assert.That(_opciones.Root.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "el panel quedó oculto tras cambiar la resolución");
            Assert.AreEqual(FullScreenMode.Windowed, DisplayPrefs.Mode,
                "el modo cambiado con el panel abierto no llegó a las preferencias");
            Assert.AreEqual(new Vector2Int(1536, 864), _settings.referenceResolution,
                "el cambio de resolución pisó la escala ya aplicada");
        }
    }
}
