using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Island.Decor
{
    /// <summary>
    /// Implementación de <see cref="IDecorService"/>: coloca, mueve y quita adornos
    /// de las zonas comunes de la isla.
    /// </summary>
    /// <remarks>
    /// El servicio decide y guarda; no dibuja nada. Publica <c>DecorPlaced</c>,
    /// <c>DecorRemoved</c> y <c>DecorMoved</c>, y quien pinta la isla se entera por
    /// ahí.
    ///
    /// No guarda en disco: de eso ya se encarga el autoguardado. Escribe directo en
    /// <c>save.Island.Decor</c>.
    /// </remarks>
    public class DecorService : IDecorService
    {
        // ── constantes públicas ─────────────────────────────────────────────

        /// <summary>Radio de una zona en metros. Más allá de esto no se puede colocar.</summary>
        public const float ZoneRadius = 12f;

        public int CapacityPerZone => 20;

        // ── estado interno ──────────────────────────────────────────────────

        readonly DecorCatalog _catalog;
        readonly SaveGame _save;
        readonly IIslandService _island;

        List<DecorPlacement> _decor => _save?.Island?.Decor;

        // ── constructores ───────────────────────────────────────────────────

        /// <summary>Constructor de producción.</summary>
        public DecorService(DecorCatalog catalog, SaveGame save, IIslandService island)
        {
            _catalog = catalog;
            _save = save;
            _island = island;
        }

        // ── IDecorService ───────────────────────────────────────────────────

        public IReadOnlyList<DecorDefinition> Catalog => _catalog.All;

        public bool TryGetDefinition(string catalogId, out DecorDefinition definition)
        {
            return _catalog.TryGetDefinition(catalogId, out definition);
        }

        public IReadOnlyList<DecorPlacement> Placed
        {
            get
            {
                if (_decor == null) return System.Array.Empty<DecorPlacement>();
                return _decor.AsReadOnly();
            }
        }

        public IEnumerable<DecorPlacement> InZone(string zoneId)
        {
            if (_decor == null) yield break;
            for (int i = 0; i < _decor.Count; i++)
                if (_decor[i].ZoneId == zoneId)
                    yield return _decor[i];
        }

        // ── CanPlace ────────────────────────────────────────────────────────

        public DecorRejection CanPlace(string catalogId, string zoneId,
                                       Vector3 localPosition)
        {
            // las comprobaciones van en el orden exacto que pide el contrato
            // para que la interfaz de usuario pueda parar en el primer motivo

            // 1. ¿existe ese adorno?
            if (!_catalog.TryGetDefinition(catalogId, out var def))
                return DecorRejection.UnknownItem;

            // 2. ¿la zona está abierta?
            if (_island != null && !_island.IsUnlocked(zoneId))
                return DecorRejection.ZoneLocked;

            // 3. ¿está dentro del radio de la zona?
            float sqrMag = localPosition.x * localPosition.x
                         + localPosition.y * localPosition.y
                         + localPosition.z * localPosition.z;
            if (sqrMag > ZoneRadius * ZoneRadius)
                return DecorRejection.OutsideZone;

            // 4. ¿se solapa con otro adorno ya puesto?
            if (_decor != null)
            {
                float combined = def.Footprint;
                for (int i = 0; i < _decor.Count; i++)
                {
                    var other = _decor[i];
                    if (other.ZoneId != zoneId) continue;
                    if (!_catalog.TryGetDefinition(other.CatalogId, out var otherDef))
                        continue;

                    float minDist = combined + otherDef.Footprint;
                    float dx = localPosition.x - other.X;
                    float dy = localPosition.y - other.Y;
                    float dz = localPosition.z - other.Z;
                    if (dx * dx + dy * dy + dz * dz < minDist * minDist)
                        return DecorRejection.Overlaps;
                }
            }

            // 5. ¿la zona ya está llena?
            if (_decor != null)
            {
                int count = 0;
                for (int i = 0; i < _decor.Count; i++)
                    if (_decor[i].ZoneId == zoneId) count++;
                if (count >= CapacityPerZone)
                    return DecorRejection.TooMany;
            }

            return DecorRejection.Ok;
        }

        // ── Place ───────────────────────────────────────────────────────────

        public string Place(string catalogId, string zoneId, Vector3 localPosition,
                            float yaw)
        {
            if (_decor == null) return "";

            if (CanPlace(catalogId, zoneId, localPosition) != DecorRejection.Ok)
                return "";

            string placementId = NextPlacementId(zoneId, catalogId);

            var placement = new DecorPlacement
            {
                PlacementId = placementId,
                CatalogId = catalogId,
                ZoneId = zoneId,
                X = localPosition.x,
                Y = localPosition.y,
                Z = localPosition.z,
                Yaw = yaw,
            };

            _decor.Add(placement);

            EventBus.Publish(new DecorPlaced(placementId, catalogId, zoneId));
            return placementId;
        }

        // ── Move ────────────────────────────────────────────────────────────

        public bool Move(string placementId, Vector3 localPosition, float yaw)
        {
            if (_decor == null) return false;

            int idx = IndexOf(placementId);
            if (idx < 0) return false;

            var placement = _decor[idx];

            // comprobar todo, pero sin contarse a sí mismo en el solape:
            // al mover, la pieza no cuenta contra sí misma o no se puede mover nunca
            if (!_catalog.TryGetDefinition(placement.CatalogId, out var def))
                return false;

            if (_island != null && !_island.IsUnlocked(placement.ZoneId))
                return false;

            float sqrMag = localPosition.x * localPosition.x
                         + localPosition.y * localPosition.y
                         + localPosition.z * localPosition.z;
            if (sqrMag > ZoneRadius * ZoneRadius)
                return false;

            // solape contra los demás (saltándose a sí mismo)
            for (int i = 0; i < _decor.Count; i++)
            {
                if (i == idx) continue;
                var other = _decor[i];
                if (other.ZoneId != placement.ZoneId) continue;
                if (!_catalog.TryGetDefinition(other.CatalogId, out var otherDef))
                    continue;

                float minDist = def.Footprint + otherDef.Footprint;
                float dx = localPosition.x - other.X;
                float dy = localPosition.y - other.Y;
                float dz = localPosition.z - other.Z;
                if (dx * dx + dy * dy + dz * dz < minDist * minDist)
                    return false;
            }

            placement.X = localPosition.x;
            placement.Y = localPosition.y;
            placement.Z = localPosition.z;
            placement.Yaw = yaw;

            EventBus.Publish(new DecorMoved(placementId));
            return true;
        }

        // ── Remove ──────────────────────────────────────────────────────────

        public bool Remove(string placementId)
        {
            if (_decor == null) return false;

            int idx = IndexOf(placementId);
            if (idx < 0) return false;

            _decor.RemoveAt(idx);
            EventBus.Publish(new DecorRemoved(placementId));
            return true;
        }

        // ── CharmOf ─────────────────────────────────────────────────────────

        public float CharmOf(string zoneId)
        {
            if (_decor == null) return 0f;

            // contar cuántas veces aparece cada catalogId en la zona
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < _decor.Count; i++)
            {
                if (_decor[i].ZoneId != zoneId) continue;
                string cid = _decor[i].CatalogId;
                counts.TryGetValue(cid, out int c);
                counts[cid] = c + 1;
            }

            float total = 0f;
            foreach (var kv in counts)
            {
                if (!_catalog.TryGetDefinition(kv.Key, out var def)) continue;

                // primer ejemplar rinde al 100%, el segundo al 50%,
                // el tercero al 25%, y del cuarto en adelante nada
                float multiplier = kv.Value switch
                {
                    0 => 0f,
                    1 => 1f,
                    2 => 1f + 0.5f,       // 1 + 1/2
                    3 => 1f + 0.5f + 0.25f, // 1 + 1/2 + 1/4
                    _ => 1f + 0.5f + 0.25f, // del cuarto en adelante no suma más
                };

                total += def.Charm * multiplier;
            }

            return Mathf.Min(total, 1f);
        }

        // ── auxiliares ──────────────────────────────────────────────────────

        int IndexOf(string placementId)
        {
            for (int i = 0; i < _decor.Count; i++)
                if (_decor[i].PlacementId == placementId) return i;
            return -1;
        }

        /// <summary>
        /// Genera un identificador estable: <c>zona_catalogId_n</c> con n el número
        /// más alto visto para ese prefijo más uno. Así sobrevive a borrar piezas del
        /// medio sin repetir un identificador que ya estuvo en uso.
        /// </summary>
        string NextPlacementId(string zoneId, string catalogId)
        {
            string prefix = $"{zoneId}_{catalogId}_";
            int maxN = -1;
            for (int i = 0; i < _decor.Count; i++)
            {
                string id = _decor[i].PlacementId;
                if (id.StartsWith(prefix))
                {
                    string suffix = id.Substring(prefix.Length);
                    if (int.TryParse(suffix, out int n) && n > maxN)
                        maxN = n;
                }
            }
            return $"{prefix}{maxN + 1}";
        }
    }
}
