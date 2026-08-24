using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// Qué va a hacer hoy este vecino y a qué hora: su ritmo, no su vida entera.
    /// </summary>
    /// <remarks>
    /// El GDD §2.2 promete «ver la agenda del día» y no existía ninguna vista que la
    /// enseñara. La agenda la calcula <c>IslanderBrain</c> hora a hora, pero no guarda
    /// nada: lo estable —cuándo duerme y adónde va cuando no le urge nada— es lo que
    /// esta tarjeta proyecta (ver <see cref="AgendaProjection"/> para por qué).
    ///
    /// Va entre los gustos y el trabajo porque peticiones, gestos y gustos son cosas
    /// que el jugador puede hacerle al vecino, y eso va arriba; esto es contexto de su
    /// vida, como el trabajo y la casa, pero más vivo que ambos: «qué hará hoy» se lee
    /// antes que su empleo. La ficha ya tiene scroll, así que empujar las tarjetas de
    /// abajo un poco más no esconde nada — solo ordena.
    /// </remarks>
    public sealed class AgendaSection
    {
        private readonly Label _intro;
        private readonly VisualElement _rows;

        /// <summary>Lo que se pintó la última vez: persona y tramo activo.</summary>
        private string _signature;

        public VisualElement Root { get; }

        public AgendaSection()
        {
            Root = UiTheme.Card();
            Root.Add(UiTheme.Title("Su día"));

            _intro = UiTheme.Body("—", soft: true);
            _intro.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_intro);

            _rows = new VisualElement();
            _rows.style.marginTop = 6;
            Root.Add(_rows);

            var caveat = UiTheme.Body(AgendaProjection.Caveat, soft: true);
            caveat.style.marginTop = 4;
            caveat.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(caveat);
        }

        public void Refresh(string islanderId)
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry) ||
                !registry.TryGet(islanderId, out var islander))
            {
                Root.style.display = DisplayStyle.None;
                return;
            }
            Root.style.display = DisplayStyle.Flex;

            // Sin reloj registrado no hay «ahora» que marcar; -1 no cae en ningún tramo.
            int hourNow = ServiceRegistry.TryGet<GameClock>(out var clock) ? clock.Hour : -1;

            var blocks = AgendaProjection.Day(islander.Personality, islander.Home.HasHome);

            // La firma lleva qué tramo está pasando y qué dice la proyección: es lo
            // único que puede cambiar con el reloj o con los datos del vecino, y así
            // el refresco de 0,4 s no repinta la tarjeta cada vez — solo cuando cambia
            // la persona, su carácter o el tramo día/noche.
            int active = IndexOf(blocks, hourNow);
            string firma = $"{islanderId}|{active}|{AgendaProjection.Intro(islander.Personality)}";
            if (firma == _signature) return;
            _signature = firma;

            _intro.text = AgendaProjection.Intro(islander.Personality);

            _rows.Clear();
            for (int i = 0; i < blocks.Count; i++)
            {
                bool now = i == active;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                var hours = UiTheme.Body(HoursOf(blocks[i]), soft: !now);
                hours.style.width = 60;
                row.Add(hours);

                // El tramo en curso va tinta entera y con «· ahora»: una agenda donde
                // no se vea dónde estás tú dentro es un horario de trenes.
                var text = UiTheme.Body(blocks[i].Text + (now ? "  ·  ahora" : ""), soft: !now);
                text.style.whiteSpace = WhiteSpace.Normal;
                row.Add(text);

                _rows.Add(row);
            }
        }

        private static int IndexOf(System.Collections.Generic.List<AgendaBlock> blocks, int hour)
        {
            for (int i = 0; i < blocks.Count; i++)
                if (blocks[i].Includes(hour)) return i;
            return -1;
        }

        /// <summary>«07–23», «23–07»: el envolvimiento del sueño se cuenta tal cual.</summary>
        private static string HoursOf(AgendaBlock block) =>
            $"{block.StartHour:00}–{block.EndHour:00}";
    }
}
