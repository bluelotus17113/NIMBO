using Nimbo.Art.Materials;
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
    /// </remarks>
    public sealed class IslanderView : MonoBehaviour
    {
        private ChibiFaceTexture _face;
        private Material _faceMaterial;
        private Transform _body;

        private Vector3 _target;
        private float _walkSpeed = 1.4f;
        private float _bobPhase;
        private float _height = 1.1f;

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
            _walkSpeed = walkSpeed;
            _bobPhase = Random.value * Mathf.PI * 2f;

            var meshes = ChibiMeshBuilder.Build(islander.Appearance);
            _height = meshes.Height;

            var bodyGo = new GameObject("cuerpo");
            _body = bodyGo.transform;
            _body.SetParent(transform, worldPositionStays: false);

            AddPart(_body, "piel", meshes.Skin, ToonPalette.Solid(islander.Appearance.SkinTone));
            AddPart(_body, "ropa", meshes.Clothes, ToonPalette.Solid(OutfitColor(islander)));
            AddPart(_body, "pelo", meshes.Hair, ToonPalette.Solid(islander.Appearance.HairColor));

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

        private static void AddPart(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
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
        }
    }
}
