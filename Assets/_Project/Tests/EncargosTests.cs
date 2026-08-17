using System;
using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Requests;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que atender a un vecino cueste lo que pide.
    /// </summary>
    /// <remarks>
    /// Antes de esto, «Ayudarle» era un botón: repartía ánimo, experiencia y monedas a
    /// cambio de un clic, sin mirar siquiera si el jugador tenía lo que le estaban
    /// pidiendo. Con doce vecinos pidiendo cosas todo el día, eso no es un bucle de
    /// juego: es una máquina de nimbos con forma de conversación.
    ///
    /// Lo que se comprueba aquí es sobre todo lo que **no** debe pasar: que no se cobre
    /// sin resolver, que no se resuelva sin cobrar, y que lo que no vale no sirva de
    /// pago.
    /// </remarks>
    public class EncargosTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registro;
        private SimulationService _simulacion;
        private EconomyService _economia;
        private InventoryService _mochila;
        private RequestService _peticiones;
        private IslanderData _vecino;

        /// <summary>Lo único que suelta la isla de estas pruebas.</summary>
        private const string Madera = "mat_madera";
        private const string Piedra = "mat_piedra";

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registro = new IslanderRegistry();

            var personalidades = PersonalityRoster.CreateService();
            var catalogo = new ItemCatalog();

            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var requestConfig = ScriptableObject.CreateInstance<RequestConfig>();

            _simulacion = new SimulationService(_registro, personalidades, needsConfig);
            _economia = new EconomyService(catalogo, _save);
            _mochila = new InventoryService(_save.Player, _economia);

            var comidas = new List<string>();
            for (int i = 0; i < catalogo.All.Count; i++)
                if (catalogo.All[i].Category == ItemCategory.Food)
                    comidas.Add(catalogo.All[i].CatalogId);

            var fabrica = new IslanderFactory(_registro, personalidades, _clock, comidas);

            var generador = new RequestGenerator(_registro, personalidades, requestConfig,
                                                 new IslaDeMaderaYPiedra(), _economia);
            _peticiones = new RequestService(_registro, _simulacion, generador, requestConfig,
                                             _clock, _mochila, _economia);

            ServiceRegistry.Register<IIslanderRegistry>(_registro);
            ServiceRegistry.Register<ISimulationService>(_simulacion);

            _vecino = fabrica.CreateRandom("vecino");
            _registro.Add(_vecino);
            _vecino.Mood.Happiness = 50f;
        }

        [TearDown]
        public void TearDown()
        {
            _peticiones?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── el encargo de material ───────────────────────────────────────────

        [Test]
        public void UnEncargoNaceConCantidadYConAlgoQueLaIslaSuelte()
        {
            var encargo = Encargar();

            Assert.That(encargo.Amount, Is.GreaterThan(0), "un encargo sin cantidad no pide nada");
            Assert.That(encargo.TargetId, Is.EqualTo(Madera).Or.EqualTo(Piedra),
                "solo se puede pedir lo que hay de dónde sacar");
        }

        [Test]
        public void SinElMaterialNoSeResuelve()
        {
            var encargo = Encargar();

            Assert.That(_peticiones.Resolve(encargo.RequestId), Is.False,
                "atender un encargo sin traer nada era el botón gratis de antes");
            Assert.That(_peticiones.OpenCount, Is.EqualTo(1),
                "y la petición se queda abierta: el vecino sigue esperando");
        }

        [Test]
        public void ConElMaterialSeResuelveYSeGasta()
        {
            var encargo = Encargar();
            Llevar(encargo.TargetId, encargo.Amount + 4);

            Assert.That(_peticiones.Resolve(encargo.RequestId, encargo.TargetId), Is.True);
            Assert.That(_mochila.CountOf(encargo.TargetId), Is.EqualTo(4),
                "se gasta lo que pedía, ni más ni menos");
            Assert.That(_peticiones.OpenCount, Is.Zero);
        }

        [Test]
        public void SiFaltaUnoNoSeGastaNinguno()
        {
            var encargo = Encargar();
            Llevar(encargo.TargetId, encargo.Amount - 1);

            Assert.That(_peticiones.Resolve(encargo.RequestId, encargo.TargetId), Is.False);
            Assert.That(_mochila.CountOf(encargo.TargetId), Is.EqualTo(encargo.Amount - 1),
                "cobrar medio encargo y no resolverlo deja al jugador sin material y sin nada a cambio");
        }

        [Test]
        public void CobraDeLaMochilaYDeLaDespensa()
        {
            var encargo = Encargar();

            // La mitad recogida y la mitad comprada. El jugador guarda en dos sitios sin
            // haberlo decidido: lo que recoge cae en la mochila y lo que compra en la
            // despensa. Mirando solo uno, el encargo diría que falta lo que sí tiene.
            int enMochila = encargo.Amount / 2;
            Llevar(encargo.TargetId, enMochila);
            _save.Inventory.Add(encargo.TargetId, encargo.Amount - enMochila);

            Assert.That(_peticiones.Resolve(encargo.RequestId, encargo.TargetId), Is.True);
            Assert.That(_mochila.CountOf(encargo.TargetId), Is.Zero);
            Assert.That(_save.Inventory.CountOf(encargo.TargetId), Is.Zero);
        }

        [Test]
        public void PrimeroSeGastaLoQueLlevasEncima()
        {
            var encargo = Encargar();
            Llevar(encargo.TargetId, encargo.Amount);
            _save.Inventory.Add(encargo.TargetId, 10);

            _peticiones.Resolve(encargo.RequestId, encargo.TargetId);

            Assert.That(_mochila.CountOf(encargo.TargetId), Is.Zero,
                "lo que llevas encima es lo que has traído para esto");
            Assert.That(_save.Inventory.CountOf(encargo.TargetId), Is.EqualTo(10),
                "la despensa solo se toca cuando la mochila no llega");
        }

        [Test]
        public void UnEncargoPagaMasQueVenderElMaterial()
        {
            var encargo = Encargar();
            int enCrudo = _economia.GetItem(encargo.TargetId).Price * encargo.Amount;

            Assert.That(encargo.CoinReward, Is.GreaterThan(enCrudo),
                "si dárselo a un vecino rentase menos que venderlo en el cajón, nadie " +
                "volvería a leer el tablón después de hacer la cuenta una vez");
        }

        [Test]
        public void SinIslaDeDondeSacarloNoHayEncargos()
        {
            var config = ScriptableObject.CreateInstance<RequestConfig>();
            var personalidades = PersonalityRoster.CreateService();

            // Sin servicio de recolección: nada que pedir, y sobre todo nada que caduque
            // restando ánimo por un recado imposible.
            var generador = new RequestGenerator(_registro, personalidades, config);
            var rng = new Core.Util.Rng(1234u);

            for (int i = 0; i < 200; i++)
            {
                Assert.That(generador.PickKind(_vecino, out var tipo, ref rng)
                            && tipo == RequestKind.Material, Is.False,
                    "un encargo que no se puede cumplir solo sirve para restar ánimo");
            }
        }

        // ── lo que vale cualquiera de su familia ─────────────────────────────

        [Test]
        public void LoQueNoValeNoSirveDePago()
        {
            var hambre = _peticiones.Raise(_vecino.Id, RequestKind.Food);
            Llevar(Madera, 5);

            Assert.That(_peticiones.Resolve(hambre.RequestId, Madera), Is.False,
                "quien pide de comer no se conforma con un tablón");
            Assert.That(_mochila.CountOf(Madera), Is.EqualTo(5), "y no se le cobra igualmente");
        }

        [Test]
        public void SinElegirNadaNoSeGastaNadaPorSuCuenta()
        {
            var hambre = _peticiones.Raise(_vecino.Id, RequestKind.Food);
            Llevar(UnaComida(), 3);

            Assert.That(_peticiones.Resolve(hambre.RequestId), Is.False,
                "coger «lo primero que valga» acaba gastando el plato que guardabas para otro");
        }

        [Test]
        public void LasOpcionesSalenDeLosDosSitios()
        {
            var hambre = _peticiones.Raise(_vecino.Id, RequestKind.Food);

            string recogida = UnaComida();
            string comprada = OtraComida(recogida);
            Llevar(recogida, 1);
            _save.Inventory.Add(comprada, 1);

            var opciones = _peticiones.OptionsFor(hambre.RequestId);

            Assert.That(opciones, Does.Contain(recogida));
            Assert.That(opciones, Does.Contain(comprada));
        }

        [Test]
        public void SinNadaQueDarNoHayOpciones()
        {
            var hambre = _peticiones.Raise(_vecino.Id, RequestKind.Food);

            Assert.That(_peticiones.OptionsFor(hambre.RequestId), Is.Empty);
        }

        [Test]
        public void DarleLoQueLeEncantaAlegraMas()
        {
            string amada = UnaComida();
            string cualquiera = OtraComida(amada);
            _vecino.Tastes.LovedFoods.Clear();
            _vecino.Tastes.HatedFoods.Clear();
            _vecino.Tastes.LovedFoods.Add(amada);

            Llevar(amada, 1);
            Llevar(cualquiera, 1);

            float neutra = Alegria(() =>
                _peticiones.Resolve(_peticiones.Raise(_vecino.Id, RequestKind.Food).RequestId,
                                    cualquiera));

            float favorita = Alegria(() =>
                _peticiones.Resolve(_peticiones.Raise(_vecino.Id, RequestKind.Food).RequestId,
                                    amada));

            Assert.That(favorita, Is.GreaterThan(neutra),
                "saber qué le gusta a cada uno tiene que servir para algo");
        }

        // ── lo que sigue costando solo tu rato ───────────────────────────────

        [Test]
        public void PedirConsejoNoCuestaObjetos()
        {
            var consejo = _peticiones.Raise(_vecino.Id, RequestKind.Advice);

            Assert.That(_peticiones.DemandOf(consejo.RequestId).WantsAnItem, Is.False);
            Assert.That(_peticiones.Resolve(consejo.RequestId), Is.True,
                "lo que pide es que estés, y el rato de ir hasta allí ya lo has pagado");
        }

        [Test]
        public void TraerAlgoAlegraMasQueSoloEscuchar()
        {
            var encargo = Encargar();
            Llevar(encargo.TargetId, encargo.Amount);
            float trayendo = Alegria(() =>
                _peticiones.Resolve(encargo.RequestId, encargo.TargetId));

            float escuchando = Alegria(() =>
                _peticiones.Resolve(_peticiones.Raise(_vecino.Id, RequestKind.Favor).RequestId));

            Assert.That(trayendo, Is.GreaterThan(escuchando),
                "traer una cosa cuesta salir a por ella; que paguen igual sería decir que da lo mismo");
        }

        [Test]
        public void UnaPeticionQueNoExisteNoPideNada()
        {
            Assert.That(_peticiones.DemandOf("no-existe").WantsAnItem, Is.False);
            Assert.That(_peticiones.OptionsFor("no-existe"), Is.Empty);
            Assert.That(_peticiones.Resolve("no-existe"), Is.False);
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        private IslanderRequest Encargar() =>
            _peticiones.Raise(_vecino.Id, RequestKind.Material);

        private void Llevar(string catalogId, int cantidad)
        {
            var resultado = _mochila.TryStore(catalogId, cantidad, out int fuera);
            Assert.That(resultado, Is.Not.EqualTo(StoreResult.UnknownItem),
                $"el catálogo no conoce {catalogId}");
            Assert.That(fuera, Is.Zero, "no cabía en la mochila de la prueba");
        }

        /// <summary>Cuánto ánimo le movió eso, empezando siempre desde el mismo sitio.</summary>
        private float Alegria(Action accion)
        {
            _vecino.Mood.Happiness = 50f;
            accion();
            return _vecino.Mood.Happiness - 50f;
        }

        private string UnaComida() => Comidas()[0];

        private string OtraComida(string distinta)
        {
            var todas = Comidas();
            for (int i = 0; i < todas.Count; i++)
                if (todas[i] != distinta) return todas[i];
            throw new InvalidOperationException("el catálogo solo tiene una comida");
        }

        private List<string> Comidas()
        {
            var ids = new List<string>();
            foreach (var item in _economia.ItemsOfCategory(ItemCategory.Food))
                ids.Add(item.CatalogId);
            return ids;
        }

        /// <summary>
        /// Una isla que solo suelta madera y piedra.
        /// </summary>
        /// <remarks>
        /// El generador solo le pregunta qué sueltan los nodos, así que el resto del
        /// contrato no hace falta. Fijar los dos materiales es lo que deja comprobar que
        /// no se pide nada que no esté ahí.
        /// </remarks>
        private sealed class IslaDeMaderaYPiedra : IGatheringService
        {
            private static readonly NodeDefinition[] Nodos =
            {
                new NodeDefinition("node_roble", "Roble", NodeKind.Tree, ToolKind.Axe,
                                   Madera, 3, 5, 5, 7),
                new NodeDefinition("node_pena", "Peña", NodeKind.Rock, ToolKind.Pickaxe,
                                   Piedra, 2, 4, 4, 5),
            };

            public IReadOnlyList<NodeDefinition> Catalog => Nodos;

            public bool TryGetDefinition(string nodeId, out NodeDefinition definition)
            {
                for (int i = 0; i < Nodos.Length; i++)
                {
                    if (Nodos[i].NodeId != nodeId) continue;
                    definition = Nodos[i];
                    return true;
                }
                definition = default;
                return false;
            }

            public IReadOnlyList<ResourceNode> Nodes => Array.Empty<ResourceNode>();

            public bool TryGetNode(string instanceId, out ResourceNode node)
            {
                node = null;
                return false;
            }

            public GatherResult Gather(string instanceId, ToolKind tool, out int dropped)
            {
                dropped = 0;
                return GatherResult.UnknownNode;
            }

            public void AdvanceDay() { }

            public int ClearAround(Vector3 centre, float radius) => 0;
        }
    }
}
