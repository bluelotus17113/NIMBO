using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.UI.Requests;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Lo que el jugador **lee** en el tablón de encargos.
    /// </summary>
    /// <remarks>
    /// Va en modo juego y no en modo edición porque el ensamblado de pruebas de edición
    /// no ve <c>Nimbo.UI</c>. Se recorre el árbol de elementos en vez de mirar una
    /// captura: la interfaz es UI Toolkit y no se dibuja dentro de la textura de una
    /// cámara.
    ///
    /// Nada de <c>ServiceRegistry.Clear()</c>: el registro es global y todas las pruebas
    /// comparten proceso. Se guarda con <c>TryGet</c> y se devuelve con
    /// <c>Unregister</c>.
    /// </remarks>
    public class TablonVisibleTests
    {
        private RequestBoardPanel _panel;
        private TablonDePrueba _tablon;
        private CensoDePrueba _censo;

        private IRequestService _peticionesPrevias;
        private IIslanderRegistry _censoPrevio;
        private IEconomyService _economiaPrevia;
        private bool _habiaPeticiones;
        private bool _habiaCenso;
        private bool _habiaEconomia;

        /// <remarks>
        /// El catálogo se aparta a propósito. Estas pruebas inventan identificadores
        /// —<c>food_galleta</c>, <c>food_sopa</c>— para no atarse a lo que haya hoy en
        /// los JSON, y el catálogo de verdad escribe un error en la consola cuando le
        /// preguntan por algo que no conoce, cosa que el corredor de pruebas cuenta como
        /// fallo. Sin él, <see cref="Nimbo.UI.ItemNames"/> recorta el identificador, que
        /// es justo el camino que interesa comprobar aquí: que nunca salga un
        /// <c>food_galleta</c> en pantalla.
        /// </remarks>
        [SetUp]
        public void SetUp()
        {
            _habiaPeticiones = ServiceRegistry.TryGet(out _peticionesPrevias);
            _habiaCenso = ServiceRegistry.TryGet(out _censoPrevio);
            _habiaEconomia = ServiceRegistry.TryGet(out _economiaPrevia);

            _tablon = new TablonDePrueba();
            _censo = new CensoDePrueba();
            Poner<IRequestService>(_tablon);
            Poner<IIslanderRegistry>(_censo);
            ServiceRegistry.Unregister<IEconomyService>();

            _panel = new RequestBoardPanel();
        }

        [TearDown]
        public void TearDown()
        {
            if (_habiaPeticiones) Poner(_peticionesPrevias);
            else ServiceRegistry.Unregister<IRequestService>();

            if (_habiaCenso) Poner(_censoPrevio);
            else ServiceRegistry.Unregister<IIslanderRegistry>();

            if (_habiaEconomia) Poner(_economiaPrevia);
        }

        private static void Poner<T>(T service) where T : class
        {
            ServiceRegistry.Unregister<T>();
            ServiceRegistry.Register(service);
        }

        // ── lo que se ve ─────────────────────────────────────────────────────

        [Test]
        public void UnTablonVacioLoDiceConPalabras()
        {
            _panel.Show();

            Assert.That(Contiene("El tablón está vacío"), Is.True,
                "un panel en blanco parece roto; que hoy no haya nada es una noticia buena");
        }

        [Test]
        public void SaleQuienPideYQuePide()
        {
            _censo.Poner("bea", "Bea");
            _tablon.Poner("bea", RequestKind.Material, "mat_madera", 5, "Necesito tablas.");

            _panel.Show();

            Assert.That(Textos(), Does.Contain("Bea"),
                "en el tablón hay que saber de quién es cada encargo sin abrir su ficha");
            Assert.That(Contiene("Quiere 5"), Is.True);
            Assert.That(Contiene("Necesito tablas."), Is.True);
        }

        [Test]
        public void LoUrgentePrimero()
        {
            _censo.Poner("bea", "Bea");
            _censo.Poner("leo", "Leo");
            _tablon.Poner("bea", RequestKind.Object, null, 0, "Un capricho.",
                          RequestPriority.Low);
            _tablon.Poner("leo", RequestKind.Food, null, 0, "Me muero de hambre.",
                          RequestPriority.Critical);

            _panel.Show();
            var textos = Textos();

            Assert.That(textos.IndexOf("Leo"), Is.LessThan(textos.IndexOf("Bea")),
                "leer primero los caprichos y encontrar el hambre al final del scroll " +
                "es enterarse tarde de lo único que importaba");
        }

        [Test]
        public void SinPoderAtenderloDiceQueFalta()
        {
            _censo.Poner("bea", "Bea");
            _tablon.Poner("bea", RequestKind.Material, "mat_madera", 5, "Necesito tablas.");

            _panel.Show();

            Assert.That(Contiene("te falta"), Is.True,
                "esconder el botón no manda a nadie a la isla; decir qué falta, sí");
        }

        [Test]
        public void ConLoQueHaceFaltaSaleElBoton()
        {
            _censo.Poner("bea", "Bea");
            _tablon.Poner("bea", RequestKind.Material, "mat_madera", 5, "Necesito tablas.");
            _tablon.Tengo("mat_madera");

            _panel.Show();

            Assert.That(Botones().Exists(t => t.StartsWith("Darle")), Is.True,
                $"no hay con qué atenderlo. Botones: {string.Join(", ", Botones())}");
        }

        [Test]
        public void LoQueValeCualquieraSaleComoUnaListaParaElegir()
        {
            _censo.Poner("bea", "Bea");
            _tablon.Poner("bea", RequestKind.Food, null, 0, "Algo de comer.");
            _tablon.Tengo("food_galleta", "food_sopa");

            _panel.Show();
            var botones = Botones();

            Assert.That(botones, Does.Contain("galleta"));
            Assert.That(botones, Does.Contain("sopa"),
                "elige el jugador: coger «la primera que valga» gastaría lo que guardaba para otro");
        }

        // ── los casos de siempre ─────────────────────────────────────────────

        [Test]
        public void ArrancaCerrado()
        {
            Assert.That(_panel.IsShowing, Is.False);
        }

        [Test]
        public void AbrirYCerrarFunciona()
        {
            _panel.Show();
            Assert.That(_panel.IsShowing, Is.True);

            _panel.Hide();
            Assert.That(_panel.IsShowing, Is.False);
        }

        [Test]
        public void SinServicioNoRevienta()
        {
            ServiceRegistry.Unregister<IRequestService>();

            Assert.DoesNotThrow(() => _panel.Show());
            Assert.That(_panel.IsShowing, Is.True);
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        private List<string> Textos()
        {
            var textos = new List<string>();
            Recorrer(_panel.Root, textos, botonesSolo: false);
            return textos;
        }

        private List<string> Botones()
        {
            var textos = new List<string>();
            Recorrer(_panel.Root, textos, botonesSolo: true);
            return textos;
        }

        private bool Contiene(string fragmento)
        {
            foreach (var texto in Textos())
                if (texto.Contains(fragmento)) return true;
            return false;
        }

        private static void Recorrer(VisualElement element, List<string> textos, bool botonesSolo)
        {
            if (element is Button button)
            {
                if (!string.IsNullOrEmpty(button.text)) textos.Add(button.text);
            }
            else if (!botonesSolo && element is Label label && !string.IsNullOrEmpty(label.text))
            {
                textos.Add(label.text);
            }

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos, botonesSolo);
        }

        /// <summary>Una cola de peticiones de mentira, con lo que el jugador lleva encima.</summary>
        private sealed class TablonDePrueba : IRequestService
        {
            private readonly List<IslanderRequest> _abiertas = new List<IslanderRequest>();
            private readonly HashSet<string> _tengo = new HashSet<string>();

            public void Poner(string islanderId, RequestKind kind, string targetId, int amount,
                              string line, RequestPriority priority = RequestPriority.Normal)
            {
                _abiertas.Add(new IslanderRequest
                {
                    RequestId = $"pet-{_abiertas.Count}",
                    IslanderId = islanderId,
                    Kind = kind,
                    Priority = priority,
                    TargetId = targetId,
                    Amount = amount,
                    Line = line,
                    ExpiresMinute = 10_000,
                });
            }

            /// <summary>Lo que el jugador tiene a mano para pagar.</summary>
            public void Tengo(params string[] catalogIds)
            {
                foreach (var id in catalogIds) _tengo.Add(id);
            }

            public IReadOnlyList<IslanderRequest> Open => _abiertas;
            public int OpenCount => _abiertas.Count;

            public bool TryGet(string requestId, out IslanderRequest request)
            {
                for (int i = 0; i < _abiertas.Count; i++)
                {
                    if (_abiertas[i].RequestId != requestId) continue;
                    request = _abiertas[i];
                    return true;
                }
                request = default;
                return false;
            }

            public IEnumerable<IslanderRequest> OpenFor(string islanderId)
            {
                for (int i = 0; i < _abiertas.Count; i++)
                    if (_abiertas[i].IslanderId == islanderId) yield return _abiertas[i];
            }

            public RequestDemand DemandOf(string requestId)
            {
                if (!TryGet(requestId, out var request)) return RequestDemand.Nothing;

                return request.Kind switch
                {
                    RequestKind.Material => new RequestDemand(
                        ItemCategory.Material, request.TargetId, request.Amount),
                    RequestKind.Food => new RequestDemand(ItemCategory.Food, null, 1),
                    RequestKind.Object => new RequestDemand(ItemCategory.Furniture, null, 1),
                    _ => RequestDemand.Nothing,
                };
            }

            public IReadOnlyList<string> OptionsFor(string requestId)
            {
                var opciones = new List<string>();
                var demanda = DemandOf(requestId);
                if (!demanda.WantsAnItem) return opciones;

                if (!string.IsNullOrEmpty(demanda.CatalogId))
                {
                    if (_tengo.Contains(demanda.CatalogId)) opciones.Add(demanda.CatalogId);
                    return opciones;
                }

                foreach (var id in _tengo) opciones.Add(id);
                opciones.Sort(string.CompareOrdinal);
                return opciones;
            }

            public bool Resolve(string requestId, string payloadId = null) => true;
            public void Refuse(string requestId) { }

            public IslanderRequest Raise(string islanderId, RequestKind kind, string targetId = null)
                => default;
        }

        /// <summary>Un censo de mentira, solo para poner nombres a los identificadores.</summary>
        private sealed class CensoDePrueba : IIslanderRegistry
        {
            private readonly List<IslanderData> _todos = new List<IslanderData>();

            public void Poner(string id, string nombre)
            {
                _todos.Add(new IslanderData
                {
                    Identity = new IslanderIdentity { Id = id, DisplayName = nombre },
                });
            }

            public IReadOnlyList<IslanderData> All => _todos;
            public int Count => _todos.Count;

            public IslanderData Get(string islanderId) =>
                TryGet(islanderId, out var islander) ? islander : null;

            public bool TryGet(string islanderId, out IslanderData islander)
            {
                for (int i = 0; i < _todos.Count; i++)
                {
                    if (_todos[i].Identity.Id != islanderId) continue;
                    islander = _todos[i];
                    return true;
                }
                islander = null;
                return false;
            }

            public bool Exists(string islanderId) => TryGet(islanderId, out _);

            public IEnumerable<IslanderData> InZone(string zoneId) => _todos;

            public void Add(IslanderData islander) => _todos.Add(islander);

            public void Remove(string islanderId) =>
                _todos.RemoveAll(i => i.Identity.Id == islanderId);
        }
    }
}
