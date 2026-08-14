using System.Collections.Generic;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Qué clase de nodo es. Decide con qué se recoge y cómo se dibuja.</summary>
    public enum NodeKind
    {
        Tree = 0,     // hacha
        Rock = 1,     // pico
        Bush = 2,     // a mano
        Herb = 3,     // a mano o guadaña
        Flower = 4,   // a mano
        Flotsam = 5,  // restos de nube: a mano, aparecen en el borde
    }

    /// <summary>Qué da un nodo al recogerlo y cuánto tarda en reponerse.</summary>
    public readonly struct NodeDefinition
    {
        public readonly string NodeId;
        public readonly string DisplayName;
        public readonly NodeKind Kind;

        /// <summary>Con qué hace falta darle. <c>None</c> se recoge a mano.</summary>
        public readonly ToolKind RequiredTool;

        /// <summary>Qué suelta, y cuánto.</summary>
        public readonly string DropId;
        public readonly int MinDrop;
        public readonly int MaxDrop;

        /// <summary>Golpes que aguanta antes de soltar. 1 para lo que se coge a mano.</summary>
        public readonly int Hits;

        /// <summary>Días de juego hasta que vuelve a estar. 0 = no vuelve.</summary>
        public readonly int RespawnDays;

        public NodeDefinition(string nodeId, string displayName, NodeKind kind,
                              ToolKind requiredTool, string dropId, int minDrop,
                              int maxDrop, int hits, int respawnDays)
        {
            NodeId = nodeId; DisplayName = displayName; Kind = kind;
            RequiredTool = requiredTool; DropId = dropId;
            MinDrop = minDrop; MaxDrop = maxDrop; Hits = hits; RespawnDays = respawnDays;
        }
    }

    /// <summary>Qué pasó al darle a un nodo.</summary>
    public enum GatherResult
    {
        Ok = 0,          // soltó algo
        Hit,             // le dio pero aún aguanta
        WrongTool,
        Depleted,        // ya estaba vacío, esperando reponerse
        UnknownNode,
        InventoryFull,
    }

    /// <summary>
    /// Lo que hay por la isla para recoger: árboles, piedras, hierbas, flores.
    /// </summary>
    /// <remarks>
    /// Los nodos se reponen solos con los días, y esa es la razón de que este juego
    /// pueda dejarse una semana sin que pase nada malo: la isla se rellena mientras no
    /// estás en vez de vaciarse.
    ///
    /// El servicio decide y guarda; no dibuja nada. Publica <c>NodeGathered</c> y
    /// <c>NodeRespawned</c>, y quien pinta la isla se entera por ahí.
    /// </remarks>
    public interface IGatheringService
    {
        IReadOnlyList<NodeDefinition> Catalog { get; }

        bool TryGetDefinition(string nodeId, out NodeDefinition definition);

        /// <summary>Todo lo que hay puesto por la isla, vivo o esperando reponerse.</summary>
        IReadOnlyList<ResourceNode> Nodes { get; }

        /// <summary>El nodo con ese identificador de instancia.</summary>
        bool TryGetNode(string instanceId, out ResourceNode node);

        /// <summary>
        /// Le da un golpe con esa herramienta. Si lo agota, mete lo que suelte en la
        /// mochila y lo deja esperando a reponerse.
        /// </summary>
        GatherResult Gather(string instanceId, ToolKind tool, out int dropped);

        /// <summary>
        /// Pasa un día: lo agotado que ya haya cumplido su espera vuelve.
        /// </summary>
        /// <remarks>
        /// Tiene que poder llamarse varias veces seguidas: al volver tras estar fuera,
        /// el juego adelanta varios días de golpe por este mismo camino.
        /// </remarks>
        void AdvanceDay();

        /// <summary>
        /// Aparta lo que haya en ese círculo. Devuelve cuántos se han movido.
        /// </summary>
        /// <remarks>
        /// Lo llama quien coloca un edificio. Los recursos se siembran esquivando los
        /// edificios que hay, pero los edificios se mueven después, así que hace falta
        /// una segunda respuesta para cuando le cae uno encima a un roble.
        ///
        /// Apartar y no borrar: la isla tiene un número de nodos y perder uno cada vez
        /// que se recoloca la aldea la iría dejando pelada sin que nadie se diera
        /// cuenta hasta que ya no hubiera de qué sacar madera.
        /// </remarks>
        int ClearAround(Vector3 centre, float radius);
    }
}
