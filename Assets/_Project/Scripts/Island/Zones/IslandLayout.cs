using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Island.Zones
{
    /// <summary>
    /// El plano de la primera isla: las diez zonas del GDD con su sitio y su condición
    /// de apertura.
    /// </summary>
    /// <remarks>
    /// Está en código y no en un ScriptableObject porque es el plano de UNA isla
    /// concreta y hay exactamente una en la v1: meterlo en un asset añade un fichero
    /// binario que dos agentes no pueden editar a la vez, y no gana nada.
    /// Las islas siguientes sí irán en datos, cuando existan y haya varias.
    /// </remarks>
    public static class IslandLayout
    {
        public const float IslandRadius = 100f;   // los 200×200 metros del GDD
        public const int MaxResidents = 12;

        public static ZoneDefinition[] FirstIsland() => new[]
        {
            new ZoneDefinition
            {
                ZoneId = "zona_plaza", DisplayName = "Plaza central",
                Purpose = ZonePurpose.Social,
                Center = new Vector3(0f, 0f, 0f), Radius = 18f,
                Unlock = UnlockCondition.Always,
            },
            new ZoneDefinition
            {
                ZoneId = "zona_residencial_a", DisplayName = "Residencial A",
                Purpose = ZonePurpose.Home,
                Center = new Vector3(-42f, 0f, 28f), Radius = 14f,
                Unlock = new UnlockCondition { MinResidents = 1 },
                HousingUnits = 4,
            },
            new ZoneDefinition
            {
                ZoneId = "zona_tienda_comida", DisplayName = "Tienda de comida",
                Purpose = ZonePurpose.Food,
                Center = new Vector3(34f, 0f, 22f), Radius = 10f,
                Unlock = new UnlockCondition { MinResidents = 1 },
                ShopId = "tienda_comida",
            },
            new ZoneDefinition
            {
                ZoneId = "zona_residencial_b", DisplayName = "Residencial B",
                Purpose = ZonePurpose.Home,
                Center = new Vector3(-52f, 0f, -14f), Radius = 14f,
                Unlock = new UnlockCondition { MinResidents = 3 },
                HousingUnits = 4,
            },
            new ZoneDefinition
            {
                ZoneId = "zona_tienda_muebles", DisplayName = "Tienda de muebles",
                Purpose = ZonePurpose.Shopping,
                Center = new Vector3(44f, 0f, -8f), Radius = 11f,
                Unlock = new UnlockCondition { MinResidents = 3 },
                ShopId = "tienda_muebles",
            },
            new ZoneDefinition
            {
                ZoneId = "zona_tienda_ropa", DisplayName = "Tienda de ropa",
                Purpose = ZonePurpose.Shopping,
                Center = new Vector3(30f, 0f, -36f), Radius = 10f,
                Unlock = new UnlockCondition { MinResidents = 5 },
                ShopId = "tienda_ropa",
            },
            new ZoneDefinition
            {
                ZoneId = "zona_parque", DisplayName = "Parque",
                Purpose = ZonePurpose.Nature,
                Center = new Vector3(-8f, 0f, -44f), Radius = 20f,
                Unlock = new UnlockCondition { MinResidents = 5 },
            },
            new ZoneDefinition
            {
                ZoneId = "zona_residencial_c", DisplayName = "Residencial C",
                Purpose = ZonePurpose.Home,
                Center = new Vector3(-38f, 0f, -56f), Radius = 14f,
                Unlock = new UnlockCondition { MinResidents = 8 },
                HousingUnits = 4,
            },
            new ZoneDefinition
            {
                ZoneId = "zona_escenario", DisplayName = "Escenario",
                Purpose = ZonePurpose.Leisure,
                Center = new Vector3(6f, 0f, 52f), Radius = 16f,
                Unlock = new UnlockCondition { MinResidents = 8, MinAnyLevel = 10 },
            },
            new ZoneDefinition
            {
                ZoneId = "zona_embarcadero", DisplayName = "Embarcadero",
                Purpose = ZonePurpose.Civic,
                Center = new Vector3(66f, 0f, 46f), Radius = 12f,
                Unlock = new UnlockCondition
                {
                    MinResidents = 12,
                    RequiredFlag = "evento_puente_aparece",
                },
            },
        };
    }
}
