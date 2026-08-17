using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using UnityEngine;

namespace Nimbo.Simulation.Requests
{
    /// <summary>
    /// La cola de peticiones del jugador: quién pide qué, cuánto aguanta sin respuesta
    /// y qué pasa cuando el jugador contesta o cuando no contesta.
    /// </summary>
    public sealed class RequestService : IRequestService, IDisposable
    {
        private readonly IIslanderRegistry _registry;
        private readonly ISimulationService _simulation;
        private readonly RequestGenerator _generator;
        private readonly RequestConfig _config;
        private readonly GameClock _clock;
        private readonly IInventoryService _bag;
        private readonly IEconomyService _economy;

        private readonly List<IslanderRequest> _open = new List<IslanderRequest>();
        private Rng _rng = Rng.FromTime();

        /// <summary>Lo que suma al ánimo traerle algo, además de lo que da atenderle.</summary>
        /// <remarks>
        /// Traer una cosa cuesta salir a por ella; decir una cosa cuesta el rato. Que
        /// paguen igual sería decir que da lo mismo, y no da lo mismo.
        /// </remarks>
        private const float BroughtSomethingHappiness = 6f;

        /// <summary>Lo que suma o resta acertar —o no— con lo que le gusta.</summary>
        private const float LovedHappiness = 5f;
        private const float HatedHappiness = -4f;

        /// <param name="bag">La mochila. Sin ella, las peticiones que piden algo no se pueden pagar.</param>
        /// <param name="economy">La despensa y el catálogo: lo comprado no vive en la mochila.</param>
        public RequestService(IIslanderRegistry registry, ISimulationService simulation,
                              RequestGenerator generator, RequestConfig config, GameClock clock,
                              IInventoryService bag = null, IEconomyService economy = null)
        {
            _registry = registry;
            _simulation = simulation;
            _generator = generator;
            _config = config;
            _clock = clock;
            _bag = bag;
            _economy = economy;

            EventBus.Subscribe<HourPassed>(OnHourPassed);
        }

        public void Dispose() => EventBus.Unsubscribe<HourPassed>(OnHourPassed);

        public IReadOnlyList<IslanderRequest> Open => _open;
        public int OpenCount => _open.Count;

        private void OnHourPassed(HourPassed _)
        {
            ExpireOverdue();

            // Dos tiradas por hora, porque el diseño evalúa cada media hora y el reloj
            // solo avisa en punto. Dos tiradas independientes son exactamente eso.
            for (int pass = 0; pass < 2; pass++) GenerationPass();
        }

        private void GenerationPass()
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                int openForHim = CountOpenFor(islander.Id);

                if (!_generator.ShouldGenerate(islander, openForHim, ref _rng)) continue;
                if (!_generator.PickKind(islander, out var kind, ref _rng)) continue;

                var request = _generator.Build(islander, kind, _clock.ElapsedMinutes, ref _rng);

                // Una petición sobre alguien que no existe no se puede resolver, así que
                // no llega a nacer.
                if (NeedsTarget(kind) && string.IsNullOrEmpty(request.TargetId)) continue;

                _open.Add(request);
                EventBus.Publish(new RequestRaised(request));
            }
        }

        private static bool NeedsTarget(RequestKind kind) => kind is
            RequestKind.SocialIntro or RequestKind.Complaint or RequestKind.Reconcile or
            RequestKind.Confession or RequestKind.Activity or RequestKind.Material;

        /// <summary>
        /// Caduca lo que lleva demasiado tiempo esperando. Ignorar tiene coste de ánimo:
        /// es lo que convierte la cola en decisiones y no en una lista de tareas.
        /// </summary>
        private void ExpireOverdue()
        {
            long now = _clock.ElapsedMinutes;
            for (int i = _open.Count - 1; i >= 0; i--)
            {
                if (_open[i].ExpiresMinute > now) continue;

                var request = _open[i];
                _open.RemoveAt(i);

                _simulation.ApplyHappiness(request.IslanderId, -request.NeglectPenalty);
                _simulation.ShowEmotion(request.IslanderId, Emotion.Sad, 5f);
                EventBus.Publish(new RequestExpired(request.RequestId, request.IslanderId));
            }
        }

        private int CountOpenFor(string islanderId)
        {
            int count = 0;
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].IslanderId == islanderId) count++;
            return count;
        }

        // --- IRequestService -------------------------------------------------

        public bool TryGet(string requestId, out IslanderRequest request)
        {
            for (int i = 0; i < _open.Count; i++)
            {
                if (_open[i].RequestId != requestId) continue;
                request = _open[i];
                return true;
            }
            request = default;
            return false;
        }

        public IEnumerable<IslanderRequest> OpenFor(string islanderId)
        {
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].IslanderId == islanderId) yield return _open[i];
        }

        public bool Resolve(string requestId, string payloadId = null)
        {
            int index = IndexOf(requestId);
            if (index < 0) return false;

            var request = _open[index];
            var demand = DemandOf(request);

            // Se cobra **antes** de quitarla de la cola. Al revés, una petición que no
            // se puede pagar desaparecería igual: el jugador se quedaría sin ella y sin
            // el aviso, y el vecino sin lo que pedía.
            float tasteBonus = 0f;
            if (demand.WantsAnItem)
            {
                string paid = PayloadToTake(demand, payloadId);
                if (string.IsNullOrEmpty(paid)) return false;
                if (!TryTake(paid, demand.Quantity)) return false;

                tasteBonus = TasteBonus(request.IslanderId, paid);
            }

            _open.RemoveAt(index);

            _simulation.GrantExperience(request.IslanderId, request.ExperienceReward);
            _simulation.ApplyHappiness(request.IslanderId,
                HappinessGain(request.Priority)
                + (demand.WantsAnItem ? BroughtSomethingHappiness : 0f)
                + tasteBonus);
            _simulation.ShowEmotion(request.IslanderId, Emotion.Ecstatic, 5f);

            EventBus.Publish(new RequestResolved(request.RequestId, request.IslanderId, true));
            return true;
        }

        /// <summary>
        /// Con qué se paga: lo que el jugador ha elegido, si vale.
        /// </summary>
        /// <remarks>
        /// Un encargo de material no admite elección —pide eso y nada más— y por eso
        /// ignora lo que le pasen. Los demás sí: es lo que hace que valga la pena saber
        /// qué le gusta a cada uno.
        ///
        /// **No hay elección por defecto a propósito.** Coger «lo primero que valga» de
        /// la mochila cuando el jugador no ha elegido acaba gastándole el plato que
        /// guardaba para otro, y eso no se puede deshacer.
        /// </remarks>
        private string PayloadToTake(in RequestDemand demand, string payloadId)
        {
            if (!string.IsNullOrEmpty(demand.CatalogId)) return demand.CatalogId;
            if (string.IsNullOrEmpty(payloadId)) return null;

            return CategoryOf(payloadId) == demand.Category ? payloadId : null;
        }

        /// <summary>Acertar con lo que le encanta alegra más; darle lo que odia, menos.</summary>
        private float TasteBonus(string islanderId, string catalogId)
        {
            if (!_registry.TryGet(islanderId, out var islander) || islander.Tastes == null)
                return 0f;

            return islander.Tastes.OpinionOf(catalogId) switch
            {
                1 => LovedHappiness,
                -1 => HatedHappiness,
                _ => 0f,
            };
        }

        /// <summary>Atender algo urgente alegra más que atender un capricho.</summary>
        private static float HappinessGain(RequestPriority priority) => priority switch
        {
            RequestPriority.Critical => 18f,
            RequestPriority.High => 12f,
            RequestPriority.Normal => 8f,
            _ => 5f,
        };

        public void Refuse(string requestId)
        {
            int index = IndexOf(requestId);
            if (index < 0) return;

            var request = _open[index];
            _open.RemoveAt(index);

            // Decir que no a la cara duele más que dejar que caduque: el habitante se
            // ha enterado. Es medio castigo más que ignorarla.
            _simulation.ApplyHappiness(request.IslanderId, -request.NeglectPenalty * 1.5f);
            _simulation.ShowEmotion(request.IslanderId, Emotion.Sad, 6f);

            EventBus.Publish(new RequestResolved(request.RequestId, request.IslanderId, false));
        }

        public IslanderRequest Raise(string islanderId, RequestKind kind, string targetId = null)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return default;

            var request = _generator.Build(islander, kind, _clock.ElapsedMinutes, ref _rng);
            if (!string.IsNullOrEmpty(targetId)) request.TargetId = targetId;

            _open.Add(request);
            EventBus.Publish(new RequestRaised(request));
            return request;
        }

        private int IndexOf(string requestId)
        {
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].RequestId == requestId) return i;
            return -1;
        }

        // --- lo que cuesta, y con qué se puede pagar --------------------------

        public RequestDemand DemandOf(string requestId) =>
            TryGet(requestId, out var request) ? DemandOf(request) : RequestDemand.Nothing;

        /// <summary>
        /// Qué familia de objeto pide cada tipo de petición.
        /// </summary>
        /// <remarks>
        /// Las otras siete —consejo, favor, queja, presentación, plan, obra, confesión y
        /// paces— no cuestan objetos, y no es que falte hacerlo: lo que piden es que
        /// estés, y el rato de ir hasta allí ya lo has pagado. Ponerles precio en cosas
        /// convertiría una charla en una entrega.
        /// </remarks>
        private static RequestDemand DemandOf(in IslanderRequest request) => request.Kind switch
        {
            RequestKind.Material => new RequestDemand(
                ItemCategory.Material, request.TargetId, Mathf.Max(1, request.Amount)),

            RequestKind.Food => new RequestDemand(ItemCategory.Food, null, 1),
            RequestKind.Clothes => new RequestDemand(ItemCategory.Clothing, null, 1),
            RequestKind.Object => new RequestDemand(ItemCategory.Furniture, null, 1),

            _ => RequestDemand.Nothing,
        };

        /// <remarks>
        /// Devuelve una lista nueva cada vez y no un buffer reutilizado: quien pinta el
        /// tablón pregunta por todas las peticiones seguidas, y con un buffer compartido
        /// la lista de la primera cambiaría al preguntar por la segunda. Se llama al
        /// abrir una pantalla, no en cada fotograma.
        /// </remarks>
        public IReadOnlyList<string> OptionsFor(string requestId)
        {
            var options = new List<string>();
            if (!TryGet(requestId, out var request)) return options;

            var demand = DemandOf(request);
            if (!demand.WantsAnItem) return options;

            // Pide uno concreto: o llega o no llega, no hay nada que elegir.
            if (!string.IsNullOrEmpty(demand.CatalogId))
            {
                if (Owned(demand.CatalogId) >= demand.Quantity) options.Add(demand.CatalogId);
                return options;
            }

            CollectOwned(demand.Category, options);
            return options;
        }

        /// <summary>Todo lo que el jugador tiene de esa familia, sin repetir.</summary>
        private void CollectOwned(ItemCategory category, List<string> into)
        {
            if (_bag != null)
            {
                var slots = _bag.Slots;
                for (int i = 0; i < slots.Count; i++)
                    Consider(slots[i].CatalogId, slots[i].Quantity, category, into);
            }

            var pantry = _economy?.Inventory?.Stacks;
            if (pantry == null) return;

            for (int i = 0; i < pantry.Count; i++)
                Consider(pantry[i].CatalogId, pantry[i].Quantity, category, into);
        }

        private void Consider(string catalogId, int quantity, ItemCategory category,
                              List<string> into)
        {
            if (quantity <= 0 || string.IsNullOrEmpty(catalogId)) return;
            if (CategoryOf(catalogId) != category) return;
            if (into.Contains(catalogId)) return;   // la misma cosa puede estar en los dos sitios
            into.Add(catalogId);
        }

        private ItemCategory CategoryOf(string catalogId)
        {
            var item = _economy?.GetItem(catalogId);
            // Sin catálogo no se puede decir de qué familia es; devolver una cualquiera
            // haría que cualquier cosa valiese para cualquier petición.
            return item?.Category ?? (ItemCategory)(-1);
        }

        /// <summary>
        /// Cuántos tiene el jugador, sumando la mochila y la despensa.
        /// </summary>
        /// <remarks>
        /// Los dos sitios, y no uno: lo que se recoge cae en la mochila y lo que se
        /// compra va a la despensa. Mirando solo la mochila, el vecino que pide algo de
        /// comer rechazaría la comida que acabas de comprarle.
        /// </remarks>
        private int Owned(string catalogId) =>
            (_bag?.CountOf(catalogId) ?? 0) + (_economy?.Inventory?.CountOf(catalogId) ?? 0);

        /// <summary>
        /// Saca esa cantidad de donde esté. Todo o nada.
        /// </summary>
        /// <remarks>
        /// Primero la mochila y luego la despensa, porque lo que se lleva encima es lo
        /// que se ha traído para esto. Si la segunda mitad falla se devuelve la primera:
        /// cobrar medio encargo y no resolverlo le quitaría al jugador material a cambio
        /// de nada, que es el peor fallo que puede tener esto.
        /// </remarks>
        private bool TryTake(string catalogId, int quantity)
        {
            if (quantity <= 0 || Owned(catalogId) < quantity) return false;

            int fromBag = Mathf.Min(_bag?.CountOf(catalogId) ?? 0, quantity);
            if (fromBag > 0 && !_bag.TryTake(catalogId, fromBag)) return false;

            int rest = quantity - fromBag;
            if (rest > 0 && !(_economy?.Inventory?.Remove(catalogId, rest) ?? false))
            {
                // El hueco sigue libre: lo acabamos de vaciar nosotros.
                if (fromBag > 0) _bag.TryStore(catalogId, fromBag, out _);
                return false;
            }

            return true;
        }

        /// <summary>Restaura la cola al cargar partida.</summary>
        public void LoadFrom(IEnumerable<IslanderRequest> saved)
        {
            _open.Clear();
            foreach (var request in saved)
                if (request.IsOpen) _open.Add(request);
        }
    }
}
