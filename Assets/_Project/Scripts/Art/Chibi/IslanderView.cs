using Nimbo.Art.Materials;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.Chibi
{
    /// <summary>
    /// El muñeco de un habitante en la isla: su malla, su cara y cómo se mueve.
    /// </summary>
    /// <remarks>
    /// No lee la simulación por su cuenta; le dicen desde fuera adónde ir y qué cara
    /// poner. Así el arte se puede quitar entero y el juego sigue corriendo, que es
    /// lo que permitió construir todas las mecánicas antes que los gráficos.
    ///
    /// La ropa equipada es la única excepción, y es al revés: la mira él solo, dos
    /// veces por segundo y con una comparación de cadenas, porque el dato vive en
    /// <see cref="IslanderData.EquippedOutfit"/> y puede cambiar desde tres sitios
    /// —regalo, armario, la costumbre del vecino— sin que ninguno conozca al arte.
    /// </remarks>
    public sealed class IslanderView : MonoBehaviour
    {
        /// <summary>Cada cuánto se mira si el vecino se ha cambiado de ropa.</summary>
        private const float OutfitCheckEvery = 0.5f;

        private ChibiFaceTexture _face;
        private Material _faceMaterial;
        private Transform _body;

        private Vector3 _target;
        private float _walkSpeed = 1.4f;
        private float _bobPhase;
        private float _height = 1.1f;

        // ── la ropa puesta ──
        private IslanderData _data;
        private string _wornId;
        private float _sinceOutfitCheck;
        private Mesh _skinMesh, _ropaMesh, _extraMesh;
        private Transform _skinPart, _ropaPart, _extraPart;

        public string IslanderId { get; private set; }

        public static IslanderView Create(IslanderData islander, float walkSpeed, Transform parent)
        {
            var go = new GameObject($"Habitante_{islander.Identity.DisplayName}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var view = go.AddComponent<IslanderView>();
            view.Build(islander, walkSpeed);
            return view;
        }

        private void Build(IslanderData islander, float walkSpeed)
        {
            IslanderId = islander.Id;
            _data = islander;
            _walkSpeed = walkSpeed;
            _bobPhase = Random.value * Mathf.PI * 2f;

            var look = LookOf(islander);
            _wornId = islander.EquippedOutfit;

            var meshes = ChibiMeshBuilder.Build(islander.Appearance, look);
            _height = meshes.Height;

            var bodyGo = new GameObject("cuerpo");
            _body = bodyGo.transform;
            _body.SetParent(transform, worldPositionStays: false);

            _skinPart = AddPart(_body, "piel", meshes.Skin,
                                ToonPalette.Solid(islander.Appearance.SkinTone));
            _skinMesh = meshes.Skin;

            _ropaPart = AddPart(_body, "ropa", meshes.Clothes,
                                ToonPalette.Solid(look.HasGarment
                                                      ? look.Garment
                                                      : OutfitColor(islander)));
            _ropaMesh = meshes.Clothes;

            AddPart(_body, "pelo", meshes.Hair, ToonPalette.Solid(islander.Appearance.HairColor));

            if (meshes.Extra != null)
                _extraPart = AddPart(_body, "prenda", meshes.Extra,
                                     ToonPalette.Solid(look.PieceColor));
            _extraMesh = meshes.Extra;

            _face = new ChibiFaceTexture(islander.Appearance, islander.Identity.DisplayName);
            _faceMaterial = ToonPalette.Textured(_face.Texture);
            AddPart(_body, "cara", meshes.Face, _faceMaterial);
        }

        /// <summary>
        /// El color de la ropa sale del id del habitante: es estable entre partidas y
        /// distinto para cada uno, sin tener que guardarlo.
        /// </summary>
        private static Color OutfitColor(IslanderData islander)
        {
            var rng = Core.Util.Rng.FromSeed(islander.Id + "-ropa");
            return Color.HSVToRGB(rng.NextFloat(), rng.Range(0.35f, 0.62f), rng.Range(0.78f, 0.96f));
        }

        /// <summary>
        /// Se llama dos veces por segundo desde <see cref="Update"/> y también a mano
        /// desde las pruebas. Si el vecino se ha puesto otra cosa, el muñeco cambia.
        /// </summary>
        public void RefreshOutfitIfChanged()
        {
            if (_data == null || _wornId == _data.EquippedOutfit) return;

            _wornId = _data.EquippedOutfit;
            ApplyOutfit();
        }

        private void ApplyOutfit()
        {
            var look = LookOf(_data);
            var body = ChibiMeshBuilder.BuildBody(_data.Appearance, look);

            // Las mallas viejas nadie las suelta por nosotros: aquí es donde si no
            // se destruyen se acumulan en memoria cada cambio de ropa.
            SwapMesh(_skinPart, body.Skin, ref _skinMesh,
                     ToonPalette.Solid(_data.Appearance.SkinTone));
            SwapMesh(_ropaPart, body.Clothes, ref _ropaMesh,
                     ToonPalette.Solid(look.HasGarment ? look.Garment : OutfitColor(_data)));

            if (body.Extra != null)
            {
                var material = ToonPalette.Solid(look.PieceColor);
                if (_extraPart == null)
                {
                    _extraPart = AddPart(_body, "prenda", body.Extra, material);
                    // La prenda va entre la ropa y el pelo: encima del cuerpo, bajo
                    // el flequillo, que un sombrero no tapa el peinado.
                    _extraPart.SetSiblingIndex(_ropaPart.GetSiblingIndex() + 1);
                }
                else
                {
                    SwapMesh(_extraPart, body.Extra, ref _extraMesh, material);
                }
            }
            else if (_extraPart != null)
            {
                Destroy(_extraMesh);
                Destroy(_extraPart.gameObject);
                _extraPart = null;
                _extraMesh = null;
            }
        }

        /// <summary>Cambia la malla de una pieza ya creada y suelta la anterior.</summary>
        private static void SwapMesh(Transform part, Mesh mesh, ref Mesh previous,
                                     Material material)
        {
            if (part == null) return;

            if (previous != null) Destroy(previous);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            previous = mesh;
        }

        /// <summary>
        /// El look de lo que lleva puesto. Se busca entre la ropa del catálogo y no
        /// con <c>GetItem</c> porque ese registra error cuando el id no existe —y un
        /// id viejo en una partida guardada no debería llenar el registro de rojo—.
        /// </summary>
        private static OutfitLook LookOf(IslanderData islander)
        {
            var id = islander.EquippedOutfit;
            if (string.IsNullOrEmpty(id)) return OutfitLook.Bare();
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return OutfitLook.Bare();

            foreach (var item in economy.ItemsOfCategory(ItemCategory.Clothing))
                if (item.CatalogId == id)
                    return OutfitLook.From(item);

            return OutfitLook.Bare();
        }

        private static Transform AddPart(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go.transform;
        }

        public void SetEmotion(Emotion emotion) => _face.Draw(emotion);

        public void PlaceAt(Vector3 position)
        {
            transform.position = position;
            _target = position;
        }

        public void WalkTo(Vector3 position) => _target = position;

        /// <summary>
        /// Los bultos que hay que rodear. Los pone el mundo, que es quien los levanta.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<World.Obstacle> Obstacles { get; set; }

        public bool IsWalking => (transform.position - _target).sqrMagnitude > 0.04f;

        private void Update()
        {
            float dt = Time.deltaTime;

            // Vestirse no corre por eventos: con mirarlo dos veces por segundo basta
            // y sobra, y así ningún módulo de simulación tiene que conocer al arte.
            _sinceOutfitCheck += dt;
            if (_sinceOutfitCheck >= OutfitCheckEvery)
            {
                _sinceOutfitCheck = 0f;
                RefreshOutfitIfChanged();
            }

            if (IsWalking)
            {
                // La dirección la decide el rodeo, no la línea recta: antes cruzaban
                // por dentro de las tiendas, que es lo único de la isla que se veía
                // claramente mal.
                var step = World.WalkAround.Steer(transform.position, _target, Obstacles);
                if (step.sqrMagnitude < 0.0001f) step = (_target - transform.position).normalized;

                transform.position += step * (_walkSpeed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(step), 8f * dt);

                // Balanceo al andar: sube y baja y se inclina un poco. Es lo que
                // separa a un muñeco que camina de una caja que se desliza.
                _bobPhase += dt * _walkSpeed * 7f;
                _body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bobPhase)) * _height * 0.045f, 0f);
                _body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_bobPhase * 0.5f) * 4.5f);
            }
            else
            {
                // Parado también respira, o parece que el juego se ha colgado.
                _bobPhase += dt * 1.6f;
                _body.localPosition = new Vector3(0f, Mathf.Sin(_bobPhase) * _height * 0.008f, 0f);
                _body.localRotation = Quaternion.Slerp(_body.localRotation,
                                                       Quaternion.identity, 6f * dt);
            }
        }

        private void OnDestroy()
        {
            if (_faceMaterial != null) Destroy(_faceMaterial);
            if (_face?.Texture != null) Destroy(_face.Texture);

            // Las mallas del cuerpo son suyas y nadie más las comparte.
            if (_skinMesh != null) Destroy(_skinMesh);
            if (_ropaMesh != null) Destroy(_ropaMesh);
            if (_extraMesh != null) Destroy(_extraMesh);
        }
    }
}
