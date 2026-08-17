using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Data.Player;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// La casa del protagonista: la cabaña, la hamaca, la mesa de trabajo y el cajón.
    /// </summary>
    /// <remarks>
    /// Todo lo de trabajar queda fuera, en el porche. No hay interior que recorrer, y
    /// fingir uno —una puerta que te teletransporta a una sala— sería trabajo grande
    /// para algo que en un juego de vista cenital casi no se mira. La cama es una
    /// hamaca por lo mismo: en una isla que flota no chirría, y evita tener que
    /// inventar un dormitorio.
    ///
    /// La cabaña sí es sólida: se rodea, como los edificios de los vecinos. Los tres
    /// muebles no, para que no te quedes atascado en tu propia mesa al acercarte.
    /// </remarks>
    public sealed class PlayerHomeView : MonoBehaviour
    {
        private Transform _root;

        private void OnEnable() => EventBus.Subscribe<GameLoaded>(OnGameLoaded);
        private void OnDisable() => EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);

        private void OnGameLoaded(GameLoaded _)
        {
            if (_root != null) return;

            _root = new GameObject("Casa del jugador").transform;
            _root.SetParent(transform, worldPositionStays: false);

            BuildCabin();
            BuildHammock();
            BuildBench();
            BuildShippingBox();
            BuildStove();
        }

        /// <summary>
        /// El fogón: piedra, un puchero y el fuego debajo.
        /// </summary>
        /// <remarks>
        /// Fuera y no dentro de la cabaña porque el interior es una escena aparte y la
        /// cocina tiene que verse desde el huerto: lo que se cocina sale de ahí al lado,
        /// y la cocina es lo que le da salida a la cosecha que no se vende.
        /// </remarks>
        private void BuildStove()
        {
            var stove = Child("fogon", PlayerHome.Stove);
            var stone = ToonPalette.Solid(ToonPalette.Rock);

            AddMesh(stove, "base", MeshShapes.Box(new Vector3(1.3f, 0.75f, 1.1f)), stone,
                    new Vector3(0f, 0.38f, 0f), solid: true);

            // La boca del fuego, en el frente y hacia abajo: es lo que dice que es un
            // fogón y no un poyete.
            AddMesh(stove, "boca", MeshShapes.Box(new Vector3(0.7f, 0.34f, 0.1f)),
                    ToonPalette.Solid(new Color32(0xF0, 0x8A, 0x4B, 255)),
                    new Vector3(0f, 0.3f, -0.56f));

            AddMesh(stove, "puchero", MeshShapes.Cylinder(9, 0.42f, 0.36f, 0.5f),
                    ToonPalette.Solid(new Color32(0x5E, 0x6B, 0x73, 255)),
                    new Vector3(0f, 1f, 0f));

            AddMesh(stove, "tapa", MeshShapes.Cylinder(9, 0.44f, 0.44f, 0.08f),
                    ToonPalette.Solid(new Color32(0x8C, 0x93, 0x9B, 255)),
                    new Vector3(0f, 1.29f, 0f));
        }

        private void BuildCabin()
        {
            var cabin = Child("cabaña", PlayerHome.Cabin);

            var walls = MeshShapes.Box(new Vector3(5.4f, 3.2f, 4.6f));
            AddMesh(cabin, "muros", walls, ToonPalette.Solid(ToonPalette.WallCream),
                    new Vector3(0f, 1.6f, 0f), solid: true);

            // Tejado a dos aguas de mentira: dos cajas inclinadas. A esta distancia de
            // cámara se lee igual que uno de verdad y no cuesta una malla nueva.
            var slope = MeshShapes.Box(new Vector3(3.2f, 0.28f, 4.9f));
            AddMesh(cabin, "tejado_a", slope, ToonPalette.Solid(ToonPalette.RoofRed),
                    new Vector3(-1.35f, 3.85f, 0f), rotation: Quaternion.Euler(0f, 0f, 32f));
            AddMesh(cabin, "tejado_b", slope, ToonPalette.Solid(ToonPalette.RoofRed),
                    new Vector3(1.35f, 3.85f, 0f), rotation: Quaternion.Euler(0f, 0f, -32f));

            // La puerta mira al huerto, al sur: es de donde vienes.
            AddMesh(cabin, "puerta", MeshShapes.Box(new Vector3(1.1f, 2f, 0.16f)),
                    ToonPalette.Solid(ToonPalette.TrunkBrown), new Vector3(0f, 1f, -2.35f));

            BuildCabinFaces(cabin);
        }

        /// <summary>
        /// Ventanas, chimenea y porche: que la cabaña tenga cuatro caras y no una.
        /// </summary>
        /// <remarks>
        /// Solo tenía puerta al sur. Las otras tres eran paredes lisas de suelo a
        /// tejado, y como la cabaña está en tu isla y se rodea andando, se pasa más
        /// tiempo mirándole la espalda que la cara. Una pared crema de cinco metros
        /// sin nada encima no parece una casa: parece que falta por terminar.
        ///
        /// Las ventanas van a la altura de los ojos de un vecino —un metro y medio, no
        /// dos— porque son la referencia con la que se lee el tamaño de todo lo demás.
        /// </remarks>
        private void BuildCabinFaces(Transform cabin)
        {
            var glass = ToonPalette.Solid(ToonPalette.Glass, smoothness: 0.35f);
            var frame = ToonPalette.Solid(ToonPalette.TrunkBrown);
            var stone = ToonPalette.Solid(ToonPalette.Rock);

            // Norte: la cara que se ve al volver del puente. Dos ventanas, porque una
            // sola en el centro de cinco metros deja la pared igual de vacía a los
            // lados.
            for (int i = -1; i <= 1; i += 2)
            {
                Window(new Vector3(i * 1.25f, 1.5f, 2.35f), new Vector3(1f, 0.9f, 0.14f));
                Sill(new Vector3(i * 1.25f, 0.99f, 2.4f), new Vector3(1.2f, 0.14f, 0.3f));
            }

            // Este y oeste: una por lado, y en el costado de la mesa de trabajo se ve
            // desde donde crafteas.
            for (int i = -1; i <= 1; i += 2)
            {
                Window(new Vector3(i * 2.75f, 1.5f, 0.4f), new Vector3(0.14f, 0.9f, 1.1f));
                Sill(new Vector3(i * 2.8f, 0.99f, 0.4f), new Vector3(0.3f, 0.14f, 1.3f));
            }

            // La chimenea de piedra, en el costado oeste y asomando por encima del
            // tejado. Es lo que dice «aquí vive alguien» desde lejos, y de piedra
            // porque toda la casa es madera y crema: sin un tercer material, el
            // volumen se pierde contra la pared.
            AddMesh(cabin, "chimenea", MeshShapes.Box(new Vector3(0.75f, 4.6f, 0.8f)), stone,
                    new Vector3(-2.5f, 2.3f, 1.2f));
            AddMesh(cabin, "chimenea_remate", MeshShapes.Box(new Vector3(0.95f, 0.22f, 1f)),
                    frame, new Vector3(-2.5f, 4.65f, 1.2f));

            // Un alero sobre la puerta, con sus dos postes. Además de dar sombra a la
            // entrada, es lo que hace que la fachada sur no sea otra pared con un
            // rectángulo marrón pegado.
            AddMesh(cabin, "alero", MeshShapes.Box(new Vector3(2.6f, 0.16f, 1.3f)),
                    ToonPalette.Solid(ToonPalette.RoofRed), new Vector3(0f, 2.35f, -3f));

            for (int i = -1; i <= 1; i += 2)
                AddMesh(cabin, "poste", MeshShapes.Cylinder(6, 0.09f, 0.11f, 2.35f), frame,
                        new Vector3(i * 1.15f, 1.17f, -3.5f));

            void Window(Vector3 at, Vector3 size) =>
                AddMesh(cabin, "ventana", MeshShapes.Box(size), glass, at);

            void Sill(Vector3 at, Vector3 size) =>
                AddMesh(cabin, "alfeizar", MeshShapes.Box(size), frame, at);
        }

        private void BuildHammock()
        {
            var hammock = Child("hamaca", PlayerHome.Hammock);
            var wood = ToonPalette.Solid(ToonPalette.TrunkBrown);

            AddMesh(hammock, "poste_a", MeshShapes.Cylinder(7, 0.12f, 0.14f, 1.7f), wood,
                    new Vector3(0f, 0.85f, -1.1f));
            AddMesh(hammock, "poste_b", MeshShapes.Cylinder(7, 0.12f, 0.14f, 1.7f), wood,
                    new Vector3(0f, 0.85f, 1.1f));

            // La tela cuelga: una caja fina y algo hundida en el centro basta para
            // que se lea como hamaca y no como una tabla entre dos palos.
            AddMesh(hammock, "tela", MeshShapes.Box(new Vector3(0.9f, 0.14f, 2.1f)),
                    ToonPalette.Solid(new Color32(0xE8, 0xB4, 0x9A, 255)),
                    new Vector3(0f, 0.95f, 0f));
        }

        private void BuildBench()
        {
            var bench = Child("mesa", PlayerHome.Bench);
            var wood = ToonPalette.Solid(new Color32(0xA9, 0x86, 0x63, 255));

            AddMesh(bench, "tablero", MeshShapes.Box(new Vector3(2.2f, 0.18f, 1.2f)), wood,
                    new Vector3(0f, 0.95f, 0f));

            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    AddMesh(bench, "pata", MeshShapes.Box(new Vector3(0.16f, 0.95f, 0.16f)),
                            wood, new Vector3(x * 0.95f, 0.47f, z * 0.45f));

            // Herramientas encima: es lo que la distingue de una mesa de comer.
            AddMesh(bench, "trastos", MeshShapes.Box(new Vector3(0.6f, 0.3f, 0.4f)),
                    ToonPalette.Solid(new Color32(0x8C, 0x93, 0x9B, 255)),
                    new Vector3(0.5f, 1.19f, 0f));
        }

        private void BuildShippingBox()
        {
            var box = Child("cajon", PlayerHome.ShippingBox);

            AddMesh(box, "caja", MeshShapes.Box(new Vector3(1.5f, 1f, 1.1f)),
                    ToonPalette.Solid(new Color32(0x9A, 0x7B, 0x5C, 255)),
                    new Vector3(0f, 0.5f, 0f));

            // Tapa levantada: se ve que está abierto y que se puede echar algo dentro.
            AddMesh(box, "tapa", MeshShapes.Box(new Vector3(1.5f, 0.12f, 1.1f)),
                    ToonPalette.Solid(ToonPalette.TrunkBrown),
                    new Vector3(0f, 1.15f, -0.45f), rotation: Quaternion.Euler(-42f, 0f, 0f));
        }

        private Transform Child(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, worldPositionStays: false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static void AddMesh(Transform parent, string name, Mesh mesh, Material material,
                                    Vector3 offset, Quaternion rotation = default,
                                    bool solid = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = offset;
            go.transform.localRotation = rotation == default ? Quaternion.identity : rotation;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            if (solid) go.AddComponent<BoxCollider>();
        }
    }
}
