using System;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Player;

namespace Nimbo.Player
{
    /// <summary>
    /// Va apuntando cómo se comporta el protagonista y lo decae con los días.
    /// </summary>
    /// <remarks>
    /// Casi todo se lo cuentan desde fuera con <see cref="Note"/>, porque medir dónde
    /// estás o si corres es cosa de quien te mueve. Lo que sí escucha por su cuenta es
    /// lo que ya llega por evento: craftear y colocar adornos, que son las dos formas
    /// más claras de decir si eres de los prácticos o de los soñadores.
    /// </remarks>
    public sealed class ConductService : IConductService, IDisposable
    {
        private readonly PlayerState _player;

        private readonly Action<DayPassed> _onDay;
        private readonly Action<ItemCrafted> _onCrafted;
        private readonly Action<DecorPlaced> _onDecor;

        public ConductService(PlayerState player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));

            _onDay = _ => _player.Conduct.Decay();
            _onCrafted = OnCrafted;
            _onDecor = _ => Note(PersonalityAxis.Outlook, towardPositive: true);

            EventBus.Subscribe(_onDay);
            EventBus.Subscribe(_onCrafted);
            EventBus.Subscribe(_onDecor);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe(_onDay);
            EventBus.Unsubscribe(_onCrafted);
            EventBus.Unsubscribe(_onDecor);
        }

        public void Note(PersonalityAxis axis, bool towardPositive, float weight = 1f) =>
            _player.Conduct.Note(axis, towardPositive, weight);

        public PersonalityProfile Profile => _player.Conduct.AsProfile();

        public float ConfidenceOf(PersonalityAxis axis) => _player.Conduct.ConfidenceOf(axis);

        /// <summary>
        /// Lo que fabricas dice bastante de ti.
        /// </summary>
        /// <remarks>
        /// Un adorno o un cuadro son gestos gratuitos: no dan de comer y se hacen porque
        /// sí. Una azada o una pared son gestos de renta. Los dos lados están al alcance
        /// en la misma sesión, que es lo que hace que la proporción signifique algo — si
        /// uno de los dos fuera contenido tardío, el eje mediría el avance en vez del
        /// carácter.
        ///
        /// La comida no cuenta para ninguno: se cocina por hambre y por regalar, y
        /// meterla en un lado sería decir que quien cocina es soñador o práctico por el
        /// hecho de cenar.
        /// </remarks>
        private void OnCrafted(ItemCrafted evt)
        {
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return;

            var item = economy.GetItem(evt.OutputId);
            if (item == null) return;

            switch (item.Category)
            {
                case ItemCategory.Decoration:
                case ItemCategory.Gift:
                    Note(PersonalityAxis.Outlook, towardPositive: true);
                    break;

                case ItemCategory.Tool:
                case ItemCategory.Material:
                    Note(PersonalityAxis.Outlook, towardPositive: false);
                    break;

                // Los muebles caen del lado soñador salvo los que son pura utilidad.
                // Es una raya en la arena, pero al lado de no medirlos es mejor: la
                // mitad del catálogo de crafteo son muebles.
                case ItemCategory.Furniture:
                    Note(PersonalityAxis.Outlook, towardPositive: true, weight: 0.5f);
                    break;
            }
        }
    }
}
