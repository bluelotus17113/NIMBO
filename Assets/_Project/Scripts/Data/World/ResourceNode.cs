using System;
using System.Collections.Generic;

namespace Nimbo.Data.World
{
    /// <summary>
    /// Un nodo de recurso puesto en la isla: este árbol concreto, esta piedra.
    /// </summary>
    /// <remarks>
    /// Dato muerto. La posición son tres <c>float</c> y no un <c>Vector3</c> por lo
    /// mismo que en <c>DecorPlacement</c>: <c>Vector3</c> serializa arrastrando
    /// <c>magnitude</c> y <c>normalized</c>, que engorda el fichero para nada.
    ///
    /// Aquí la posición es **del mundo**, no de zona: los árboles no pertenecen a
    /// ninguna zona, están donde están.
    /// </remarks>
    [Serializable]
    public class ResourceNode
    {
        public string InstanceId = "";
        public string NodeId = "";

        public float X;
        public float Y;
        public float Z;

        /// <summary>Giro sobre el eje vertical. Para que no salgan todos calcados.</summary>
        public float Yaw;

        /// <summary>Golpes que le quedan. 0 significa agotado.</summary>
        public int HitsLeft;

        /// <summary>Día de juego en que vuelve. 0 si está vivo.</summary>
        public int RespawnOnDay;

        public bool IsDepleted => HitsLeft <= 0;
    }

    /// <summary>Todos los nodos de la isla, tal y como se guardan.</summary>
    [Serializable]
    public class GatheringState
    {
        /// <summary>Cierto una vez sembrada la isla, para no volver a sembrarla al cargar.</summary>
        public bool Seeded;

        public List<ResourceNode> Nodes = new List<ResourceNode>();
    }
}
