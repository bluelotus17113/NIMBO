using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>Las piezas de un edificio, separadas por el material que les toca.</summary>
    public readonly struct BuildingMeshes
    {
        public readonly Mesh Walls;
        public readonly Mesh Roof;
        public readonly Mesh Trim;   // puertas, ventanas y detalles

        public BuildingMeshes(Mesh walls, Mesh roof, Mesh trim)
        {
            Walls = walls; Roof = roof; Trim = trim;
        }
    }

    /// <summary>
    /// Construye los edificios de la isla a partir de para qué sirve cada zona.
    /// </summary>
    /// <remarks>
    /// Las formas son deliberadamente sencillas — cajas, conos y cilindros — porque a
    /// la distancia de la cámara lo que distingue una tienda de una casa es la
    /// silueta y el color del tejado, no el detalle. Un edificio con moldura tallada
    /// se ve exactamente igual que una caja desde ochenta metros.
    /// </remarks>
    public static class BuildingMeshBuilder
    {
        public static BuildingMeshes Build(ZonePurpose purpose, float scale = 1f)
        {
            var walls = new List<(Mesh, Matrix4x4)>();
            var roof = new List<(Mesh, Matrix4x4)>();
            var trim = new List<(Mesh, Matrix4x4)>();

            switch (purpose)
            {
                case ZonePurpose.Home:
                    BuildApartments(walls, roof, trim, scale);
                    break;
                case ZonePurpose.Food:
                case ZonePurpose.Shopping:
                    BuildShop(walls, roof, trim, scale);
                    break;
                case ZonePurpose.Leisure:
                    BuildStage(walls, roof, trim, scale);
                    break;
                case ZonePurpose.Nature:
                    BuildPark(walls, roof, trim, scale);
                    break;
                case ZonePurpose.Civic:
                    BuildDock(walls, roof, trim, scale);
                    break;
                default:
                    BuildPlaza(walls, roof, trim, scale);
                    break;
            }

            return new BuildingMeshes(
                Combine(walls, "edificio_muros"),
                Combine(roof, "edificio_tejado"),
                Combine(trim, "edificio_detalle"));
        }

        /// <summary>Un bloque de cuatro apartamentos: dos plantas con tejado a dos aguas.</summary>
        private static void BuildApartments(List<(Mesh, Matrix4x4)> walls,
                                            List<(Mesh, Matrix4x4)> roof,
                                            List<(Mesh, Matrix4x4)> trim, float s)
        {
            var body = MeshShapes.Box(new Vector3(16f, 11f, 11f) * s);
            walls.Add((body, Matrix4x4.Translate(new Vector3(0f, 5.5f * s, 0f))));

            // El tejado es un cono de cuatro lados: a esta distancia se lee igual que
            // uno a dos aguas y cuesta la mitad de vértices.
            var cap = MeshShapes.Cylinder(4, 0.02f, 0.5f, 1f);
            roof.Add((cap, Matrix4x4.TRS(
                new Vector3(0f, 14f * s, 0f), Quaternion.Euler(0f, 45f, 0f),
                new Vector3(22f, 6f, 22f) * s)));

            // Cuatro puertas, una por vivienda.
            var door = MeshShapes.Box(new Vector3(2.2f, 4f, 0.4f) * s);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 3.8f * s;
                trim.Add((door, Matrix4x4.Translate(new Vector3(x, 2f * s, 5.6f * s))));
            }

            var window = MeshShapes.Box(new Vector3(2f, 2f, 0.3f) * s);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 3.8f * s;
                trim.Add((window, Matrix4x4.Translate(new Vector3(x, 8.5f * s, 5.6f * s))));
            }
        }

        /// <summary>Una tienda: local ancho, toldo y escaparate.</summary>
        private static void BuildShop(List<(Mesh, Matrix4x4)> walls,
                                      List<(Mesh, Matrix4x4)> roof,
                                      List<(Mesh, Matrix4x4)> trim, float s)
        {
            var body = MeshShapes.Box(new Vector3(13f, 8f, 10f) * s);
            walls.Add((body, Matrix4x4.Translate(new Vector3(0f, 4f * s, 0f))));

            var flatRoof = MeshShapes.Box(new Vector3(14.5f, 1.2f, 11.5f) * s);
            roof.Add((flatRoof, Matrix4x4.Translate(new Vector3(0f, 8.6f * s, 0f))));

            // El toldo inclinado es lo que dice «esto es una tienda» de un vistazo.
            var awning = MeshShapes.Box(new Vector3(13f, 0.5f, 4.5f) * s);
            roof.Add((awning, Matrix4x4.TRS(
                new Vector3(0f, 6.2f * s, 6.5f * s), Quaternion.Euler(-18f, 0f, 0f),
                Vector3.one)));

            var glass = MeshShapes.Box(new Vector3(9f, 3.6f, 0.3f) * s);
            trim.Add((glass, Matrix4x4.Translate(new Vector3(0f, 3.4f * s, 5.1f * s))));

            var door = MeshShapes.Box(new Vector3(2.4f, 4.4f, 0.4f) * s);
            trim.Add((door, Matrix4x4.Translate(new Vector3(4.8f * s, 2.2f * s, 5.2f * s))));
        }

        /// <summary>El escenario: tarima con fondo y dos focos.</summary>
        private static void BuildStage(List<(Mesh, Matrix4x4)> walls,
                                       List<(Mesh, Matrix4x4)> roof,
                                       List<(Mesh, Matrix4x4)> trim, float s)
        {
            var deck = MeshShapes.Box(new Vector3(18f, 1.8f, 12f) * s);
            walls.Add((deck, Matrix4x4.Translate(new Vector3(0f, 0.9f * s, 0f))));

            var back = MeshShapes.Box(new Vector3(18f, 9f, 1f) * s);
            walls.Add((back, Matrix4x4.Translate(new Vector3(0f, 5.4f * s, -5.5f * s))));

            var canopy = MeshShapes.Box(new Vector3(19f, 0.8f, 13f) * s);
            roof.Add((canopy, Matrix4x4.Translate(new Vector3(0f, 10f * s, -0.5f * s))));

            for (int side = -1; side <= 1; side += 2)
            {
                var post = MeshShapes.Cylinder(8, 0.5f, 0.5f, 1f);
                walls.Add((post, Matrix4x4.TRS(
                    new Vector3(side * 8.5f * s, 5f * s, 5.5f * s), Quaternion.identity,
                    new Vector3(0.9f, 10f, 0.9f) * s)));

                var lamp = MeshShapes.Sphere(10, 8);
                trim.Add((lamp, Matrix4x4.TRS(
                    new Vector3(side * 8.5f * s, 9.4f * s, 5.5f * s), Quaternion.identity,
                    Vector3.one * 1.8f * s)));
            }
        }

        /// <summary>El parque: un estanque, un banco y un par de arbustos.</summary>
        private static void BuildPark(List<(Mesh, Matrix4x4)> walls,
                                      List<(Mesh, Matrix4x4)> roof,
                                      List<(Mesh, Matrix4x4)> trim, float s)
        {
            var pond = MeshShapes.Cylinder(20, 0.5f, 0.48f, 1f);
            trim.Add((pond, Matrix4x4.TRS(
                new Vector3(0f, 0.2f * s, 0f), Quaternion.identity,
                new Vector3(13f, 0.5f, 9f) * s)));

            var seat = MeshShapes.Box(new Vector3(6f, 0.5f, 1.8f) * s);
            walls.Add((seat, Matrix4x4.Translate(new Vector3(9f * s, 1.6f * s, 4f * s))));

            var backrest = MeshShapes.Box(new Vector3(6f, 2f, 0.4f) * s);
            walls.Add((backrest, Matrix4x4.Translate(new Vector3(9f * s, 2.6f * s, 3.2f * s))));

            var bush = MeshShapes.Sphere(12, 9, new Vector3(1f, 0.8f, 1f));
            for (int i = 0; i < 5; i++)
            {
                float angle = i / 5f * Mathf.PI * 2f;
                roof.Add((bush, Matrix4x4.TRS(
                    new Vector3(Mathf.Cos(angle) * 12f * s, 1.6f * s, Mathf.Sin(angle) * 10f * s),
                    Quaternion.identity, Vector3.one * 3.6f * s)));
            }
        }

        /// <summary>El embarcadero: pasarela hacia el vacío y un farol al final.</summary>
        private static void BuildDock(List<(Mesh, Matrix4x4)> walls,
                                      List<(Mesh, Matrix4x4)> roof,
                                      List<(Mesh, Matrix4x4)> trim, float s)
        {
            var walkway = MeshShapes.Box(new Vector3(4.5f, 0.6f, 20f) * s);
            walls.Add((walkway, Matrix4x4.Translate(new Vector3(0f, 0.4f * s, 8f * s))));

            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                var post = MeshShapes.Cylinder(6, 0.5f, 0.5f, 1f);
                walls.Add((post, Matrix4x4.TRS(
                    new Vector3(side * 2f * s, 1.6f * s, (2f + i * 5f) * s),
                    Quaternion.identity, new Vector3(0.6f, 3f, 0.6f) * s)));
            }

            var lantern = MeshShapes.Sphere(10, 8);
            trim.Add((lantern, Matrix4x4.TRS(
                new Vector3(0f, 4.2f * s, 17f * s), Quaternion.identity, Vector3.one * 2f * s)));
        }

        /// <summary>La plaza: un suelo empedrado, un banco y el tablón de noticias.</summary>
        private static void BuildPlaza(List<(Mesh, Matrix4x4)> walls,
                                       List<(Mesh, Matrix4x4)> roof,
                                       List<(Mesh, Matrix4x4)> trim, float s)
        {
            var floor = MeshShapes.Cylinder(24, 0.5f, 0.5f, 1f);
            trim.Add((floor, Matrix4x4.TRS(
                new Vector3(0f, 0.15f * s, 0f), Quaternion.identity,
                new Vector3(24f, 0.4f, 24f) * s)));

            var board = MeshShapes.Box(new Vector3(5f, 3.5f, 0.5f) * s);
            walls.Add((board, Matrix4x4.Translate(new Vector3(9f * s, 3.6f * s, 4f * s))));

            for (int side = -1; side <= 1; side += 2)
            {
                var post = MeshShapes.Cylinder(6, 0.5f, 0.5f, 1f);
                walls.Add((post, Matrix4x4.TRS(
                    new Vector3((9f + side * 1.8f) * s, 1.2f * s, 4f * s), Quaternion.identity,
                    new Vector3(0.5f, 2.4f, 0.5f) * s)));
            }

            var seat = MeshShapes.Box(new Vector3(7f, 0.5f, 2f) * s);
            walls.Add((seat, Matrix4x4.Translate(new Vector3(-9f * s, 1.5f * s, 0f))));
        }

        /// <summary>El color de tejado de cada tipo de zona: es lo que las distingue de lejos.</summary>
        public static Color RoofColor(ZonePurpose purpose) => purpose switch
        {
            ZonePurpose.Home => new Color32(0xD9, 0x72, 0x62, 255),      // teja
            ZonePurpose.Food => new Color32(0xE8, 0xA0, 0x5C, 255),      // toldo naranja
            ZonePurpose.Shopping => new Color32(0x7A, 0xB8, 0xD6, 255),  // toldo azul
            ZonePurpose.Leisure => new Color32(0xC0, 0x8A, 0xD0, 255),   // lona morada
            ZonePurpose.Nature => new Color32(0x63, 0xB0, 0x5A, 255),    // arbustos
            ZonePurpose.Civic => new Color32(0x9B, 0x8A, 0x7A, 255),
            _ => new Color32(0xE0, 0xCF, 0xA8, 255),
        };

        /// <summary>Combina, o devuelve null si esa parte no tiene nada.</summary>
        private static Mesh Combine(List<(Mesh, Matrix4x4)> parts, string name) =>
            parts.Count == 0 ? null : MeshShapes.Combine(parts, name);
    }
}
