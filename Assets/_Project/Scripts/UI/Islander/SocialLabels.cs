using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Social;
using UnityEngine;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// Las palabras y los colores con que la ficha nombra cómo están dos personas.
    /// </summary>
    /// <remarks>
    /// Vivían dentro de <see cref="IslanderPanel"/> hasta que el mapa de la isla
    /// («Quién anda con quién») necesitó escribir exactamente lo mismo. Es el caso de
    /// <c>RequestRow</c> otra vez: dos copias de una etiqueta se separan al primer
    /// cambio, y entonces una tarjeta dice «casados» y la de al lado «prometidos» de
    /// la misma pareja.
    /// </remarks>
    public static class SocialLabels
    {
        /// <summary>Una sola etiqueta por relación: manda lo más fuerte que esté pasando.</summary>
        public static string StatusOf(in RelationshipRecord record) => record.Romance switch
        {
            RomanceStage.Married => "casados",
            RomanceStage.Engaged => "prometidos",
            RomanceStage.Dating => "saliendo",
            RomanceStage.Crush => "le gusta",
            // Se declaró y falta la respuesta. Sin esta case, la ficha caía al bloque
            // de amistad y alguien recién declarado salía como «se conocen».
            RomanceStage.Confessed => "pendiente de respuesta",
            RomanceStage.Separated => "rotos",
            _ => record.Conflict switch
            {
                ConflictStage.Feud => "enemistados",
                ConflictStage.Quarrel => "reñidos",
                ConflictStage.Rivalry => "rivales",
                ConflictStage.Tension => "tirantes",
                _ => record.Friendship switch
                {
                    FriendshipStage.BestFriend => "inseparables",
                    FriendshipStage.CloseFriend => "buenos amigos",
                    FriendshipStage.Friend => "amigos",
                    _ => "se conocen",
                },
            },
        };

        public static Color StatusColor(in RelationshipRecord record)
        {
            if (record.Romance is RomanceStage.Married or RomanceStage.Engaged
                or RomanceStage.Dating or RomanceStage.Crush or RomanceStage.Confessed)
                return UiTheme.Mood;
            // Por gravedad y no por el número del enum: la rivalidad se añadió al final
            // para no renumerar las partidas guardadas, así que comparar con `>=` la
            // pintaría más grave que una enemistad.
            if (record.Conflict.IsSerious()) return UiTheme.Critical;
            if (record.Conflict == ConflictStage.Rivalry) return UiTheme.PeachDeep;
            if (record.Conflict == ConflictStage.Tension) return UiTheme.Low;
            if (record.Friendship >= FriendshipStage.Friend) return UiTheme.Social;
            return UiTheme.InkSoft;
        }

        /// <summary>
        /// Para ordenar listas de relaciones: primero lo asentado, luego lo abierto.
        /// </summary>
        /// <remarks>
        /// El orden del enum no sirve —<see cref="ConflictStages.Severity"/> existe
        /// precisamente porque la rivalidad se coló al final sin renumerar—, y mezclar
        /// romance y bronca en un solo criterio es decidir dos veces lo mismo. Aquí va
        /// el orden en que se lee una historia: bodas arriba, riñas debajo.
        /// </remarks>
        public static int RankFor(in RelationshipRecord record) => record.Romance switch
        {
            RomanceStage.Married => 0,
            RomanceStage.Engaged => 1,
            RomanceStage.Dating => 2,
            RomanceStage.Crush => 3,
            RomanceStage.Confessed => 4,
            RomanceStage.Separated => 5,
            _ => record.Conflict switch
            {
                ConflictStage.Feud => 6,
                ConflictStage.Quarrel => 7,
                ConflictStage.Rivalry => 8,
                _ => 9,
            },
        };

        /// <summary>Cómo se llama el protagonista en la lista de un vecino.</summary>
        public static string PlayerName()
        {
            if (!ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var player)) return "Tú";

            string name = player.State?.DisplayName;
            return string.IsNullOrEmpty(name) ? "Tú" : $"{name} (tú)";
        }
    }
}
