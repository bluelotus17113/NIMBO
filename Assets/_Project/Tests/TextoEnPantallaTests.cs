using System;
using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Requests;
using Nimbo.Data.Save;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.UI;
using Nimbo.UI.Menu;
using Nimbo.UI.Player;
using Nimbo.UI.Requests;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Nimbo.Tests
{
    /// <summary>
    /// El texto que el jugador lee: plurales, plazos y nombres de objetos.
    /// </summary>
    /// <remarks>
    /// Nació de cuatro defectos medidos en pantalla: «hace 1 minutos», un plazo que
    /// decía «menos de una hora» quedando 89 minutos, el mismo objeto llamado de tres
    /// formas según la pantalla, y dos títulos iguales uno detrás de otro. Son los
    /// casos frontera que los habrían cazado antes de llegar al jugador.
    /// </remarks>
    public class TextoEnPantallaTests
    {
        private IEconomyService _economiaPrevia;
        private IInventoryService _inventarioPrevio;
        private bool _habiaEconomia;
        private bool _habiaInventario;

        [SetUp]
        public void SetUp()
        {
            // El registro es global y todas las pruebas comparten proceso: se guarda
            // lo que hubiera y se devuelve al salir, como en TablonVisibleTests.
            _habiaEconomia = ServiceRegistry.TryGet(out _economiaPrevia);
            _habiaInventario = ServiceRegistry.TryGet(out _inventarioPrevio);

            var economia = new EconomyService(new ItemCatalog(), new SaveGame());
            ServiceRegistry.Unregister<IEconomyService>();
            ServiceRegistry.Register<IEconomyService>(economia);
        }

        [TearDown]
        public void TearDown()
        {
            ServiceRegistry.Unregister<IEconomyService>();
            if (_habiaEconomia) ServiceRegistry.Register(_economiaPrevia);

            ServiceRegistry.Unregister<IInventoryService>();
            if (_habiaInventario) ServiceRegistry.Register(_inventarioPrevio);
        }

        // ── el plural, decidido en un sitio ──────────────────────────────────

        [Test]
        public void ElPluralSeDecideEnUnSitio()
        {
            Assert.That(UiTheme.Plural(1, "minuto"), Is.EqualTo("minuto"),
                "el singular era lo que se escribía mal a mano");
            Assert.That(UiTheme.Plural(2, "minuto"), Is.EqualTo("minutos"));
            Assert.That(UiTheme.Plural(0, "minuto"), Is.EqualTo("minutos"));
            Assert.That(UiTheme.Plural(1, "día", "días"), Is.EqualTo("día"),
                "los que no hacen plural en -s llevan las dos formas");
            Assert.That(UiTheme.Plural(7, "día", "días"), Is.EqualTo("días"));
        }

        [Test]
        public void LaAntiguedadDeLaPartidaNuncaDiceUnMinutos()
        {
            var ahora = DateTime.UtcNow;

            Assert.That(SaveSummary.Ago(ahora.AddSeconds(-30).ToString("o")),
                Is.EqualTo("hace un momento"));
            Assert.That(SaveSummary.Ago(ahora.AddMinutes(-5).ToString("o")),
                Is.EqualTo("hace 5 minutos"));
            Assert.That(SaveSummary.Ago(ahora.AddMinutes(-45).ToString("o")),
                Is.EqualTo("hace 45 minutos"));
            Assert.That(SaveSummary.Ago(ahora.AddMinutes(-75).ToString("o")),
                Is.EqualTo("hace una hora"));
            Assert.That(SaveSummary.Ago(ahora.AddHours(-5).ToString("o")),
                Is.EqualTo("hace 5 horas"));
            Assert.That(SaveSummary.Ago(ahora.AddHours(-30).ToString("o")),
                Is.EqualTo("ayer"));
            Assert.That(SaveSummary.Ago(ahora.AddDays(-3).ToString("o")),
                Is.EqualTo("hace 3 días"));

            // Y si alguien afloja mañana la guarda de «un momento» a un minuto,
            // el plural aguanta solo: para eso está el ayudante y no el if suelto.
            Assert.That(SaveSummary.Ago(""), Is.Empty, "sin fecha no se inventa tiempo");
        }

        // ── el plazo, que compara minutos antes de redondear ─────────────────

        [Test]
        public void LosCasosFronteraDelPlazoDicenLaVerdad()
        {
            Assert.That(UiTheme.Plazo(0), Is.EqualTo("Se le ha pasado el momento."));
            Assert.That(UiTheme.Plazo(-3), Is.EqualTo("Se le ha pasado el momento."));

            Assert.That(UiTheme.Plazo(1), Is.EqualTo("Queda menos de una hora."));
            Assert.That(UiTheme.Plazo(59), Is.EqualTo("Queda menos de una hora."));

            // Aquí vivía la mentira: con 61-89 minutos decía «menos de una hora»
            // porque redondeaba a horas ANTES de comparar.
            Assert.That(UiTheme.Plazo(60), Is.EqualTo("Queda sobre una hora."));
            Assert.That(UiTheme.Plazo(61), Is.EqualTo("Queda sobre una hora."));
            Assert.That(UiTheme.Plazo(89), Is.EqualTo("Queda sobre una hora."));

            Assert.That(UiTheme.Plazo(90), Is.EqualTo("Quedan unas 2 horas."));
            Assert.That(UiTheme.Plazo(200), Is.EqualTo("Quedan unas 3 horas."));
        }

        // ── un solo nombre para cada objeto ──────────────────────────────────

        [Test]
        public void LasTresFormasDeNombrarSalenDeLaMismaFuente()
        {
            var catalogo = new ItemCatalog();

            for (int i = 0; i < catalogo.All.Count; i++)
            {
                var item = catalogo.All[i];
                string display = ItemNames.Display(item.CatalogId);

                Assert.That(display, Is.EqualTo(item.DisplayName),
                    $"Display no es el catálogo en {item.CatalogId}");
                Assert.That(ItemNames.Of(item.CatalogId),
                    Is.EqualTo(display.ToLowerInvariant()),
                    $"Of y Display discrepan en {item.CatalogId}");

                string corto = ItemNames.Short(item.CatalogId);
                Assert.That(display, Does.StartWith(corto),
                    $"Short no sale de Display en {item.CatalogId}: «{corto}» vs «{display}»");
            }
        }

        [Test]
        public void SinCatalogoNingunIdentificadorCrudoLlegaALaPantalla()
        {
            // Sin economía registrada, como al arrancar o en TablonVisibleTests:
            // el recorte del identificador sigue siendo mejor que un id crudo.
            ServiceRegistry.Unregister<IEconomyService>();

            Assert.That(ItemNames.Of("food_galleta"), Is.EqualTo("galleta"));
            Assert.That(ItemNames.Display("mat_madera"), Is.EqualTo("madera"));
            Assert.That(ItemNames.Short("comida_pastel_de_nubes"), Is.EqualTo("pastel"));
        }

        /// <remarks>
        /// La prueba que pide el encargo entera: las tres pantallas del defecto —
        /// hotbar, mochila y tablón— pintando el mismo objeto a la vez. Antes salían
        /// «regadera», «Regadera» y «regadera» desde tres recortadores distintos.
        /// </remarks>
        [Test]
        public void LaMismaRegaderaSeLlamaIgualEnLasTresPantallas()
        {
            ServiceRegistry.Unregister<IInventoryService>();
            ServiceRegistry.Register<IInventoryService>(
                new MochilaDePrueba(new ItemStack("tool_regadera", 1)));

            string hotbar = null, mochila = null, tablon = null;

            var barra = new HotbarView();
            barra.Rebuild();
            foreach (string texto in Textos(barra.Root))
                if (texto == "Regadera") { hotbar = texto; break; }

            var bolsa = new BagPanel();
            bolsa.Show();
            foreach (string texto in Textos(bolsa.Root))
                if (texto == "Regadera") { mochila = texto; break; }

            var peticiones = new TablonDePrueba();
            peticiones.Poner("bea", RequestKind.Material, "tool_regadera", 1, "Se me rompió.");
            var fila = RequestRow.Build(peticiones.Open[0], peticiones, null, "Bea");
            foreach (string texto in Textos(fila))
                if (texto.Contains("regadera")) { tablon = texto; break; }

            Assert.That(hotbar, Is.EqualTo("Regadera"),
                "el hotbar debe usar ItemNames.Short, no su propio recorte del id");
            Assert.That(mochila, Is.EqualTo("Regadera"),
                "la mochila debe usar ItemNames.Display, no su propia copia");
            Assert.That(tablon, Is.Not.Null.And.Contains("regadera"),
                "el tablón nombra dentro de una frase, en minúsculas, pero del mismo nombre");
        }

        // ── ayudas ───────────────────────────────────────────────────────────

        private static List<string> Textos(VisualElement raiz)
        {
            var textos = new List<string>();
            Recorrer(raiz, textos);
            return textos;
        }

        private static void Recorrer(VisualElement elemento, List<string> textos)
        {
            if (elemento is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);
            else if (elemento is Button boton && !string.IsNullOrEmpty(boton.text))
                textos.Add(boton.text);

            for (int i = 0; i < elemento.childCount; i++)
                Recorrer(elemento[i], textos);
        }

        /// <summary>Una mochila de mentira: lo justo para pintar huecos.</summary>
        private sealed class MochilaDePrueba : IInventoryService
        {
            private readonly ItemStack[] _huecos;

            public MochilaDePrueba(params ItemStack[] huecos)
            {
                _huecos = huecos;
                SlotCount = huecos.Length;
                HotbarSize = 10;
                SelectedSlot = 0;
            }

            public int SlotCount { get; }
            public int SelectedSlot { get; }
            public int HotbarSize { get; }

            // El hotbar recorre HotbarSize huecos aunque la mochila sea más pequeña:
            // fuera de rango hay vacío, como en una mochila de verdad.
            public ItemStack At(int slot) =>
                slot >= 0 && slot < _huecos.Length ? _huecos[slot] : default;
            public ItemStack InHand => At(SelectedSlot);
            public ToolKind ToolInHand => ToolKind.None;
            public int ToolTierInHand => 1;
            public IReadOnlyList<ItemStack> Slots => _huecos;

            public void Select(int slot) { }
            public int CountOf(string catalogId) => 0;
            public bool TryTake(string catalogId, int quantity = 1) => false;
            public bool TryConsumeSelected(int quantity = 1) => false;
            public void Swap(int a, int b) { }
            public bool Drop(int slot) => false;
            public int StackLimitOf(string catalogId) => 99;
            public StoreResult TryStore(string catalogId, int quantity, out int leftover)
            {
                leftover = quantity;
                return StoreResult.UnknownItem;
            }
        }

        /// <summary>Un tablón de mentira, mínimo: una petición y qué pide.</summary>
        private sealed class TablonDePrueba : IRequestService
        {
            private readonly List<IslanderRequest> _abiertas = new List<IslanderRequest>();

            public void Poner(string islanderId, RequestKind kind, string targetId,
                              int amount, string line)
            {
                _abiertas.Add(new IslanderRequest
                {
                    RequestId = $"pet-{_abiertas.Count}",
                    IslanderId = islanderId,
                    Kind = kind,
                    Priority = RequestPriority.Normal,
                    TargetId = targetId,
                    Amount = amount,
                    Line = line,
                    ExpiresMinute = 10_000,
                });
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
                    if (_abiertas[i].IslanderId == islanderId)
                        yield return _abiertas[i];
            }

            public RequestDemand DemandOf(string requestId) =>
                TryGet(requestId, out var request)
                    ? new RequestDemand(ItemCategory.Material, request.TargetId, request.Amount)
                    : RequestDemand.Nothing;

            public IReadOnlyList<string> OptionsFor(string requestId)
            {
                var opciones = new List<string>();
                var demanda = DemandOf(requestId);
                if (demanda.WantsAnItem && !string.IsNullOrEmpty(demanda.CatalogId))
                    opciones.Add(demanda.CatalogId);
                return opciones;
            }

            public bool Resolve(string requestId, string payloadId = null) => true;
            public void Refuse(string requestId) { }
            public IslanderRequest Raise(string islanderId, RequestKind kind,
                                         string targetId = null) => default;
        }
    }
}
