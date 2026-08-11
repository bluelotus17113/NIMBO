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
