using System;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Island.Zones
{
    /// <summary>Qué hace falta para que una zona abra. Todo lo que esté puesto se exige a la vez.</summary>
    [Serializable]
    public struct UnlockCondition
    {
        [Tooltip("Habitantes que tiene que haber en la isla. 0 = sin requisito.")]
        public int MinResidents;

        [Tooltip("Nivel que tiene que haber alcanzado al menos un habitante. 0 = sin requisito.")]
        public int MinAnyLevel;

        [Tooltip("Marca de partida que tiene que existir. Vacío = sin requisito.")]
        public string RequiredFlag;

        public static UnlockCondition Always => new UnlockCondition();

        public bool IsAlways => MinResidents == 0 && MinAnyLevel == 0 &&
                                string.IsNullOrEmpty(RequiredFlag);
    }

    /// <summary>
    /// Una zona de la isla: dónde está, para qué sirve y cuándo abre.
    /// </summary>
    /// <remarks>
    /// Las posiciones son las del plano de 200×200 del GDD y viven aquí, en datos, no
    /// repartidas por la escena. Así <c>Nimbo.Art</c> puede construir la isla entera a
    /// partir de esta lista y la simulación puede correr sin escena ninguna.
    /// </remarks>
    [Serializable]
    public struct ZoneDefinition
    {
        public string ZoneId;
        public string DisplayName;
        public ZonePurpose Purpose;

        /// <summary>Centro de la zona en metros, con el origen en el centro de la isla.</summary>
        public Vector3 Center;

        /// <summary>Radio en metros dentro del que se puede plantar a un habitante.</summary>
        public float Radius;

        public UnlockCondition Unlock;

        /// <summary>Apartamentos que aporta esta zona. 0 si no es residencial.</summary>
        public int HousingUnits;

        /// <summary>Tienda que hay aquí, o vacío. Lo lee la economía para su rotación.</summary>
        public string ShopId;
    }
}
