using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.World;
using Nimbo.UI.Chronicle;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Lo que el jugador **lee** en la crónica, no lo que el servicio guarda.
    /// </summary>
    /// <remarks>
    /// La lección de este proyecto es que la lógica en verde no dice nada de lo que se
    /// ve: los ciento veinte nodos de recurso existieron meses sin que los dibujara
    /// nadie y todas las pruebas pasaban. Aquí se comprueba el árbol de elementos que el
    /// panel construye de verdad — el orden, las cabeceras, la agrupación por días—
    /// porque eso es lo que aparece en pantalla.
    ///
    /// No hay captura de pantalla porque la interfaz es UI Toolkit y no se dibuja dentro
    /// de la textura de una cámara: leer el árbol es lo más cerca de mirar que se puede
    /// llegar sin ojos.
    ///
    /// Va en PlayMode y no en EditMode porque el ensamblado de pruebas de EditMode no ve
    /// <c>Nimbo.UI</c>.
    /// </remarks>
    public class CronicaVisibleTests
    {
        private ChroniclePanel _panel;
        private CronicaDePrueba _cronica;

        private IChronicleService _cronicaPrevia;
        private GameClock _relojPrevio;
        private bool _habiaCronica;
        private bool _habiaReloj;

        /// <summary>
        /// Presta el registro de servicios y lo devuelve como estaba.
        /// </summary>
        /// <remarks>
        /// **Nada de <c>ServiceRegistry.Clear()</c>.** El registro es global y en modo
        /// juego todas las pruebas comparten proceso: al borrarlo aquí, las de recursos
        /// se quedaron sin servicio de recolección y fallaron dos, porque el
        /// <c>GameBootstrap</c> que lo registra solo lo hace una vez —tiene su
        /// <c>_built</c>— y no vuelve a registrarlo al recargar la escena. Una prueba que
        /// tira el estado compartido rompe a las vecinas, y la que revienta no es la que
        /// tiene el fallo.
        /// </remarks>
        [SetUp]
        public void SetUp()
        {
            _habiaCronica = ServiceRegistry.TryGet(out _cronicaPrevia);
            _habiaReloj = ServiceRegistry.TryGet(out _relojPrevio);

            _cronica = new CronicaDePrueba();
            Poner<IChronicleService>(_cronica);
            _panel = new ChroniclePanel();
        }

        [TearDown]
        public void TearDown()
        {
            if (_habiaCronica) Poner(_cronicaPrevia);
            else ServiceRegistry.Unregister<IChronicleService>();

            if (_habiaReloj) Poner(_relojPrevio);
            else ServiceRegistry.Unregister<GameClock>();
        }

        /// <summary>Registra sin el aviso de «ya estaba registrado».</summary>
        private static void Poner<T>(T service) where T : class
        {
            ServiceRegistry.Unregister<T>();
            ServiceRegistry.Register(service);
        }

        /// <summary>Todos los textos del panel, en el orden en que se leen.</summary>
        private List<string> Textos()
        {
            var textos = new List<string>();
            Recorrer(_panel.Root, textos);
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }

        private void Reloj(int dia)
        {
            // El reloj cuenta desde el día 1, así que el día N empieza a los (N-1) días.
            Poner(new GameClock((dia - 1L) * GameClock.MinutesPerDay));
        }

        // ── el orden ─────────────────────────────────────────────────────────

        [Test]
        public void LoMasRecienteVaArriba()
        {
            _cronica.Anadir(1, "Ana y Leo se conocieron.");
            _cronica.Anadir(9, "Ana y Leo se han casado.");
            Reloj(9);

            _panel.Show();
            var textos = Textos();

            int casados = textos.IndexOf("Ana y Leo se han casado.");
            int conocieron = textos.IndexOf("Ana y Leo se conocieron.");

            Assert.That(casados, Is.GreaterThan(-1), "la línea de la boda no aparece");
            Assert.That(conocieron, Is.GreaterThan(-1));
            Assert.That(casados, Is.LessThan(conocieron),
                "una crónica que empieza por el día 1 obliga a bajar hasta el final para " +
                "saber qué pasó anoche, que es lo único que se viene a mirar");
        }

        // ── las cabeceras ────────────────────────────────────────────────────

        [Test]
        public void ElDiaDeHoySeLlamaHoy()
        {
            _cronica.Anadir(12, "Algo pasó hoy.");
            Reloj(12);

            _panel.Show();

            Assert.That(Textos(), Does.Contain("Hoy"),
                "leer «día 12» no le dice a nadie si eso fue esta mañana");
        }

        [Test]
        public void ElDiaAnteriorSeLlamaAyer()
        {
            _cronica.Anadir(11, "Algo pasó ayer.");
            Reloj(12);

            _panel.Show();

            Assert.That(Textos(), Does.Contain("Ayer"));
        }

        [Test]
        public void LoDeEstaSemanaSeCuentaEnDias()
        {
            _cronica.Anadir(9, "Algo pasó hace tres días.");
            Reloj(12);

            _panel.Show();

            Assert.That(Textos(), Does.Contain("Hace 3 días"));
        }

        [Test]
        public void LoViejoSeQuedaConSuNumeroDeDia()
        {
            _cronica.Anadir(2, "Algo pasó hace mucho.");
            Reloj(40);

            _panel.Show();

            Assert.That(Textos(), Does.Contain("Día 2"),
                "«hace 38 días» no ayuda; el número del día sí sitúa");
        }

        // ── la agrupación ────────────────────────────────────────────────────

        [Test]
        public void VariasCosasDelMismoDiaComparteUnaSolaCabecera()
        {
            _cronica.Anadir(5, "Primera cosa.");
            _cronica.Anadir(5, "Segunda cosa.");
            _cronica.Anadir(5, "Tercera cosa.");
            Reloj(5);

            var textos = Cuenta("Hoy");

            Assert.That(textos, Is.EqualTo(1),
                "tres líneas del mismo día llevan una cabecera, no tres");
        }

        [Test]
        public void CadaDiaDistintoLlevaSuCabecera()
        {
            _cronica.Anadir(4, "Lo de anteayer.");
            _cronica.Anadir(5, "Lo de ayer.");
            _cronica.Anadir(6, "Lo de hoy.");
            Reloj(6);

            _panel.Show();
            var textos = Textos();

            Assert.That(textos, Does.Contain("Hoy"));
            Assert.That(textos, Does.Contain("Ayer"));
            Assert.That(textos, Does.Contain("Hace 2 días"));
        }

        private int Cuenta(string texto)
        {
            _panel.Show();
            int total = 0;
            foreach (var t in Textos())
                if (t == texto) total++;
            return total;
        }

        // ── los casos vacíos, que son los que se ven el primer día ───────────

        [Test]
        public void UnaCronicaVaciaLoDiceConPalabras()
        {
            Reloj(1);
            _panel.Show();

            var textos = Textos();
            bool loDice = false;
            foreach (var t in textos)
                if (t.Contains("Todavía no ha pasado nada")) loDice = true;

            Assert.That(loDice, Is.True,
                "el primer día la crónica está vacía y eso hay que contarlo, no dejar un hueco");
        }

        [Test]
        public void SinServicioNoRevienta()
        {
            ServiceRegistry.Unregister<IChronicleService>();

            Assert.DoesNotThrow(() => _panel.Show());
            Assert.That(_panel.IsShowing, Is.True);
        }

        [Test]
        public void ArrancaCerrada()
        {
            Assert.That(_panel.IsShowing, Is.False,
                "la crónica se abre porque el jugador quiere leerla, no sola");
        }

        [Test]
        public void AbrirYCerrarFunciona()
        {
            _cronica.Anadir(1, "Algo.");
            Reloj(1);

            _panel.Show();
            Assert.That(_panel.IsShowing, Is.True);

            _panel.Hide();
            Assert.That(_panel.IsShowing, Is.False);
        }

        /// <summary>Una crónica de mentira, para no montar la isla entera.</summary>
        private sealed class CronicaDePrueba : IChronicleService
        {
            private readonly List<ChronicleEntry> _entries = new List<ChronicleEntry>();
            public IReadOnlyList<ChronicleEntry> Entries => _entries;
            public void Anadir(int day, string text) => _entries.Add(new ChronicleEntry(day, text));
        }
    }
}
