using System;

namespace Nimbo.Data.World
{
    /// <summary>
    /// Un adorno puesto en la isla: qué es, en qué zona está y en qué postura.
    /// </summary>
    /// <remarks>
    /// Dato muerto, como todo lo que va dentro del guardado: sin lógica y sin nada
    /// calculado. Renombrar un campo de aquí rompe las partidas de quien ya juega,
    /// así que se piensa una vez y se deja.
    ///
    /// La posición son tres <c>float</c> sueltos y no un <c>Vector3</c> a propósito.
    /// <c>Vector3</c> serializa arrastrando <c>magnitude</c>, <c>normalized</c> y el
    /// resto de propiedades calculadas: engorda el fichero para nada y
    /// <c>normalized</c> devuelve otro <c>Vector3</c>, que es una recursión que el
    /// serializador tiene que cortar a mano. El servicio convierte en el borde.
    /// </remarks>
    [Serializable]
    public class DecorPlacement
    {
        /// <summary>Identificador de esta pieza concreta. Estable entre sesiones.</summary>
        public string PlacementId = "";

        /// <summary>Qué adorno del catálogo es.</summary>
        public string CatalogId = "";

        /// <summary>En qué zona está puesto.</summary>
        public string ZoneId = "";

        /// <summary>Posición dentro de la zona, no en el mundo.</summary>
        public float X;
        public float Y;
        public float Z;

        /// <summary>Giro sobre el eje vertical, en grados.</summary>
        public float Yaw;
    }
}
