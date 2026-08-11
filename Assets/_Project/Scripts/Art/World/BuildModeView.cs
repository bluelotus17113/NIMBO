using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// El modo construcción: la aldea desde el aire, con rejilla y un fantasma que
    /// sigue al ratón.
    /// </summary>
    /// <remarks>
    /// La rejilla solo existe mientras se construye. Dibujarla siempre ensuciaría el
    /// prado el noventa y nueve por ciento del tiempo, y este juego se mira mucho.
    ///
    /// El fantasma se pinta verde o rojo según quepa, y **se calcula con la misma
    /// llamada que luego coloca de verdad** (<c>CanPlace</c>). Pintar con una regla y
    /// colocar con otra es cómo se acaba teniendo una casilla que se ve verde y no
    /// acepta el clic.
    /// </remarks>
    public sealed class BuildModeView : MonoBehaviour
    {
        [SerializeField] private float _aerialHeight = 175f;

        private IBuildService _build;
        private Camera _camera;
        private Transform _gridRoot;
        private GameObject _ghost;

        private bool _active;
        private string _selected;
        private int _cellX, _cellY;

        private static Mesh _cellMesh;
        private static Mesh _ghostMesh;

        /// <summary>Qué edificio se está colocando. Vacío para no colocar nada.</summary>
        public string Selected
        {
            get => _selected;
            set => _selected = value;
        }

        public bool Active => _active;

        private void OnEnable() => EventBus.Subscribe<BuildModeChanged>(OnBuildModeChanged);
        private void OnDisable() => EventBus.Unsubscribe<BuildModeChanged>(OnBuildModeChanged);

        private void OnBuildModeChanged(BuildModeChanged evt)
        {
            if (evt.Building) Enter(); else Leave();
        }

        private void Enter()
        {
            if (_active) return;
            if (!ServiceRegistry.TryGet(out _build)) return;

            _active = true;
            _camera = Camera.main;

            // La cámara de seguir al protagonista se aparta: aquí manda una vista
            // cenital fija sobre la aldea, que es donde se construye.
            if (_camera != null)
            {
                var island = _camera.GetComponent<CameraWork.IslandCamera>();
                if (island != null) island.enabled = false;

                _camera.transform.SetPositionAndRotation(
                    Archipelago.VillageCentre + Vector3.up * _aerialHeight,
                    Quaternion.Euler(90f, 0f, 0f));
            }

            BuildGridOverlay();
        }

        private void Leave()
        {
            if (!_active) return;
            _active = false;
            _selected = null;

            if (_gridRoot != null) Destroy(_gridRoot.gameObject);
            if (_ghost != null) Destroy(_ghost);

            if (_camera == null) return;
            var island = _camera.GetComponent<CameraWork.IslandCamera>();

            // Al volver, la cámara de juego se recoloca sola en su primer LateUpdate,
            // así que no hace falta devolverla a mano: basta con encenderla.
            if (island != null) island.enabled = true;
        }

        /// <summary>
        /// La rejilla, como losas finas sobre el prado.
        /// </summary>
        /// <remarks>
        /// Solo se dibujan las casillas donde de verdad se puede construir. Pintar la
        /// cuadrícula entera y luego decir que no en la mitad es enseñar un tablero
        /// que miente; así el sitio útil se ve de un vistazo.
        /// </remarks>
        private void BuildGridOverlay()
        {
            _gridRoot = new GameObject("Rejilla").transform;
            _gridRoot.SetParent(transform, worldPositionStays: false);

            var material = ToonPalette.Solid(new Color32(0xFF, 0xFF, 0xFF, 255));
            int max = BuildGrid.MaxCell;

            for (int x = -max; x < max; x++)
            for (int y = -max; y < max; y++)
            {
                if (!BuildGrid.InsideIsland(x, y)) continue;
                if (BuildGrid.OnReservedCentre(x, y)) continue;

                var cell = new GameObject($"c_{x}_{y}");
                cell.transform.SetParent(_gridRoot, worldPositionStays: false);
                cell.transform.localPosition = BuildGrid.CentreOf(x, y) + Vector3.up * 0.08f;

                cell.AddComponent<MeshFilter>().sharedMesh = CellMesh();
                cell.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private void Update()
        {
            if (!_active || _camera == null) return;

            // Arrastrar con el botón derecho mueve la vista por la isla; sin esto, una
            // aldea de doscientos metros no cabe entera en pantalla a una altura a la
            // que se distinga una casilla.
            if (Input.GetMouseButton(1))
            {
                var move = new Vector3(-Input.GetAxis("Mouse X"), 0f, -Input.GetAxis("Mouse Y")) * 2f;
                var next = _camera.transform.position + move;

                float limit = Archipelago.VillageRadius;
                next.x = Mathf.Clamp(next.x, -limit, limit);
                next.z = Mathf.Clamp(next.z, -limit, limit);
                _camera.transform.position = next;
            }

            if (string.IsNullOrEmpty(_selected)) { HideGhost(); return; }

            if (!TryCellUnderMouse(out _cellX, out _cellY)) { HideGhost(); return; }

            var rejection = _build.CanPlace(_selected, _cellX, _cellY);
            ShowGhost(_cellX, _cellY, rejection == BuildRejection.Ok);

            if (Input.GetMouseButtonDown(0) && rejection == BuildRejection.Ok)
                _build.Place(_selected, _cellX, _cellY);
        }

        /// <summary>Sobre qué casilla está el ratón.</summary>
        private bool TryCellUnderMouse(out int cellX, out int cellY)
        {
            cellX = cellY = 0;

            // Se cruza con el plano del suelo en vez de tirar un rayo físico: el prado
            // tiene colisionador, pero también lo tienen los edificios ya puestos, y
            // con un rayo físico el ratón dejaría de poder señalar la casilla que hay
            // justo detrás de una casa.
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return false;

            float t = -ray.origin.y / ray.direction.y;
            if (t <= 0f) return false;

            BuildGrid.CellAt(ray.origin + ray.direction * t, out cellX, out cellY);
            return true;
        }

        private void ShowGhost(int cellX, int cellY, bool valid)
        {
            if (_ghost == null)
            {
                _ghost = new GameObject("fantasma");
                _ghost.transform.SetParent(transform, worldPositionStays: false);
                _ghost.AddComponent<MeshFilter>().sharedMesh = GhostMesh();
                _ghost.AddComponent<MeshRenderer>();
            }

            _ghost.SetActive(true);

            // El fantasma se pone en el centro del cuadrado de dos por dos, no en la
            // esquina: si no, se ve media manzana desplazado de donde va a caer.
            float half = (BuildGrid.BuildingCells - 1) * BuildGrid.CellSize * 0.5f;
            _ghost.transform.position = BuildGrid.CentreOf(cellX, cellY)
                                      + new Vector3(half, 1.6f, half);

            _ghost.GetComponent<MeshRenderer>().sharedMaterial = ToonPalette.Solid(
                valid ? new Color32(0xB8, 0xE6, 0xC8, 255) : new Color32(0xF5, 0xC0, 0xCB, 255));
        }

        private void HideGhost()
        {
            if (_ghost != null) _ghost.SetActive(false);
        }

        private static Mesh CellMesh() =>
            _cellMesh ??= MeshShapes.Box(new Vector3(BuildGrid.CellSize * 0.88f, 0.04f,
                                                     BuildGrid.CellSize * 0.88f));

        private static Mesh GhostMesh()
        {
            float side = BuildGrid.CellSize * BuildGrid.BuildingCells * 0.9f;
            return _ghostMesh ??= MeshShapes.Box(new Vector3(side, 3.2f, side));
        }

        private void OnDestroy()
        {
            foreach (var mesh in new[] { _cellMesh, _ghostMesh })
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
            _cellMesh = _ghostMesh = null;
        }
    }
}
