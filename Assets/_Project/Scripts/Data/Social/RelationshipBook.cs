using System;
using System.Collections.Generic;

namespace Nimbo.Data.Social
{
    /// <summary>
    /// La agenda de un habitante: qué siente por cada uno de los demás.
    /// </summary>
    /// <remarks>
    /// Es una lista y no un diccionario porque Unity no serializa diccionarios y
    /// porque una isla llena son decenas de habitantes, no miles: la búsqueda lineal
    /// no se nota. El índice se reconstruye al cargar, no se guarda.
    /// </remarks>
    [Serializable]
    public class RelationshipBook
    {
        public List<RelationshipRecord> Records = new List<RelationshipRecord>();

        [NonSerialized] private Dictionary<string, int> _index;

        public int Count => Records.Count;

        public void RebuildIndex()
        {
            _index ??= new Dictionary<string, int>(Records.Count);
            _index.Clear();
            for (int i = 0; i < Records.Count; i++) _index[Records[i].OtherId] = i;
        }

        private int IndexOf(string otherId)
        {
            if (_index == null) RebuildIndex();
            return _index.TryGetValue(otherId, out int i) ? i : -1;
        }

        public bool TryGet(string otherId, out RelationshipRecord record)
        {
            int i = IndexOf(otherId);
            if (i < 0) { record = default; return false; }
            record = Records[i];
            return true;
        }

        /// <summary>Devuelve la ficha con ese habitante, creándola en blanco si no existía.</summary>
        public RelationshipRecord GetOrCreate(string otherId)
        {
            int i = IndexOf(otherId);
            if (i >= 0) return Records[i];

            var fresh = RelationshipRecord.NewWith(otherId);
            Records.Add(fresh);
            _index ??= new Dictionary<string, int>();
            _index[otherId] = Records.Count - 1;
            return fresh;
        }

        public void Set(in RelationshipRecord record)
        {
            int i = IndexOf(record.OtherId);
            if (i >= 0) { Records[i] = record; return; }

            Records.Add(record);
            _index ??= new Dictionary<string, int>();
            _index[record.OtherId] = Records.Count - 1;
        }

        public void Remove(string otherId)
        {
            int i = IndexOf(otherId);
            if (i < 0) return;
            Records.RemoveAt(i);
            RebuildIndex();
        }
    }
}
