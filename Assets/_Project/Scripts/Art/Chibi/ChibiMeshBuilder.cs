using System.Collections.Generic;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.Chibi
{
    /// <summary>Las mallas de un habitante, separadas por el material que les toca.</summary>
    public readonly struct ChibiMeshes
    {
        public readonly Mesh Skin;
        public readonly Mesh Clothes;
        public readonly Mesh Hair;
        public readonly Mesh Face;
        public readonly float Height;

        public ChibiMeshes(Mesh skin, Mesh clothes, Mesh hair, Mesh face, float height)
        {
            Skin = skin; Clothes = clothes; Hair = hair; Face = face; Height = height;
        }
    }

    /// <summary>
    /// Construye el cuerpo de un habitante a partir de su <see cref="AppearanceData"/>.
    /// </summary>
    /// <remarks>
    /// Proporciones chibi: la cabeza se lleva un 42 % de la altura. No es una
    /// licencia estética, es lo que hace que la cara se lea desde la cámara cenital
    /// de la isla — con proporciones realistas, a esa distancia no se distingue quién
    /// es quién, y el juego entero va de reconocer a la gente.
    ///
    /// Se devuelven cuatro mallas y no una porque cada una lleva su material: piel,
    /// ropa, pelo y la cara, que es la única con textura.
    /// </remarks>
    public static class ChibiMeshBuilder
    {
        private const float HeadShare = 0.42f;

        public static ChibiMeshes Build(in AppearanceData appearance)
        {
            float height = Mathf.Lerp(0.95f, 1.35f, appearance.BodyHeight);
            float girth = Mathf.Lerp(0.78f, 1.22f, appearance.BodyBuild);

            float headSize = height * HeadShare;
            float headWidth = headSize * Mathf.Lerp(0.9f, 1.12f, appearance.HeadWidth);
            float headHeight = headSize * Mathf.Lerp(0.92f, 1.1f, appearance.HeadHeight);

            float bodyHeight = height * 0.30f;
            float legHeight = height * 0.24f;
            float bodyWidth = height * 0.30f * girth;

            float legTop = legHeight;
            float bodyCentre = legHeight + bodyHeight * 0.5f;
            float headCentre = legHeight + bodyHeight + headHeight * 0.44f;

            var skin = new List<(Mesh, Matrix4x4)>();
            var clothes = new List<(Mesh, Matrix4x4)>();

            // --- cabeza ---
            var headMesh = MeshShapes.Sphere(20, 16);
            skin.Add((headMesh, Matrix4x4.TRS(
                new Vector3(0f, headCentre, 0f), Quaternion.identity,
                new Vector3(headWidth, headHeight, headWidth * 0.94f))));

            // --- cuerpo, con la ropa por fuera ---
            var torso = MeshShapes.Cylinder(14, 0.42f, 0.5f, 1f);
            clothes.Add((torso, Matrix4x4.TRS(
                new Vector3(0f, bodyCentre, 0f), Quaternion.identity,
                new Vector3(bodyWidth, bodyHeight, bodyWidth * 0.82f))));

            // --- brazos: cuelgan un poco abiertos, que es la pose de reposo ---
            float armLength = height * 0.22f;
            float armThick = height * 0.075f * girth;
            float shoulder = legHeight + bodyHeight * 0.86f;
            float armX = bodyWidth * 0.52f;

            for (int side = -1; side <= 1; side += 2)
            {
                var arm = MeshShapes.Cylinder(8, 0.5f, 0.42f, 1f);
                clothes.Add((arm, Matrix4x4.TRS(
                    new Vector3(side * armX, shoulder - armLength * 0.42f, 0f),
                    Quaternion.Euler(0f, 0f, side * -9f),
                    new Vector3(armThick, armLength, armThick))));

                var hand = MeshShapes.Sphere(10, 8);
                skin.Add((hand, Matrix4x4.TRS(
                    new Vector3(side * (armX + armLength * 0.07f), shoulder - armLength * 0.94f, 0f),
                    Quaternion.identity, Vector3.one * armThick * 1.25f)));
            }

            // --- piernas ---
            float legThick = height * 0.085f * girth;
            float legX = bodyWidth * 0.26f;

            for (int side = -1; side <= 1; side += 2)
            {
                var leg = MeshShapes.Cylinder(8, 0.5f, 0.5f, 1f);
                skin.Add((leg, Matrix4x4.TRS(
                    new Vector3(side * legX, legTop * 0.5f, 0f), Quaternion.identity,
                    new Vector3(legThick, legHeight, legThick))));

                var foot = MeshShapes.Sphere(10, 8, new Vector3(1f, 0.62f, 1.5f));
                clothes.Add((foot, Matrix4x4.TRS(
                    new Vector3(side * legX, legThick * 0.34f, legThick * 0.22f),
                    Quaternion.identity, Vector3.one * legThick * 1.5f)));
            }

            var hair = BuildHair(appearance, headCentre, headWidth, headHeight);
            var face = BuildFaceQuad(headCentre, headWidth, headHeight);

            return new ChibiMeshes(
                MeshShapes.Combine(skin, "chibi_piel"),
                MeshShapes.Combine(clothes, "chibi_ropa"),
                hair,
                face,
                height);
        }

        /// <summary>
        /// El pelo: un casquete sobre el cráneo más los mechones que pida el estilo.
        /// Veinte peinados salen de combinar casquete, flequillo, coleta y moño.
        /// </summary>
        private static Mesh BuildHair(in AppearanceData appearance, float headCentre,
                                      float headWidth, float headHeight)
        {
            var parts = new List<(Mesh, Matrix4x4)>();
            int style = appearance.HairStyle;

            // Casquete: una esfera un pelín mayor que la cabeza, subida para que
            // asome por arriba y por detrás pero deje la cara libre.
            var cap = MeshShapes.Sphere(18, 14);
            parts.Add((cap, Matrix4x4.TRS(
                new Vector3(0f, headCentre + headHeight * 0.055f, -headWidth * 0.02f),
                Quaternion.identity,
                new Vector3(headWidth * 1.035f, headHeight * 1.02f, headWidth * 1.02f))));

            if (style % 4 >= 1) // flequillo
            {
                var fringe = MeshShapes.Sphere(14, 10, new Vector3(1f, 0.42f, 0.55f));
                parts.Add((fringe, Matrix4x4.TRS(
                    new Vector3(0f, headCentre + headHeight * 0.22f, headWidth * 0.40f),
                    Quaternion.Euler(14f, 0f, 0f), Vector3.one * headWidth * 0.92f)));
            }

            if (style % 4 >= 2) // melena por detrás
            {
                var back = MeshShapes.Sphere(14, 12, new Vector3(0.92f, 1.25f, 0.5f));
                parts.Add((back, Matrix4x4.TRS(
                    new Vector3(0f, headCentre - headHeight * 0.22f, -headWidth * 0.32f),
                    Quaternion.identity, Vector3.one * headWidth * 0.85f)));
            }

            if (style >= 8 && style % 4 == 3) // coletas
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var tail = MeshShapes.Sphere(12, 10, new Vector3(0.7f, 1.5f, 0.7f));
                    parts.Add((tail, Matrix4x4.TRS(
                        new Vector3(side * headWidth * 0.52f, headCentre - headHeight * 0.05f,
                                    -headWidth * 0.12f),
                        Quaternion.Euler(0f, 0f, side * 16f), Vector3.one * headWidth * 0.42f)));
                }
            }

            if (style >= 14) // moño
            {
                var bun = MeshShapes.Sphere(12, 10);
                parts.Add((bun, Matrix4x4.TRS(
                    new Vector3(0f, headCentre + headHeight * 0.52f, -headWidth * 0.16f),
                    Quaternion.identity, Vector3.one * headWidth * 0.42f)));
            }

            return MeshShapes.Combine(parts, "chibi_pelo");
        }

        /// <summary>
        /// La cara va en un plano curvado pegado al frente del cráneo, no pintada
        /// sobre la esfera. Así la textura no se estira por los polos de la esfera,
        /// que es lo que le pasa a un ojo dibujado en una UV esférica.
        /// </summary>
        private static Mesh BuildFaceQuad(float headCentre, float headWidth, float headHeight)
        {
            const int columns = 8, rows = 8;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[columns * rows * 6];

            float halfW = headWidth * 0.40f;
            float halfH = headHeight * 0.34f;
            float radius = headWidth * 0.5f;

            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= columns; x++)
            {
                float u = (float)x / columns, v = (float)y / rows;
                float px = Mathf.Lerp(-halfW, halfW, u);
                float py = Mathf.Lerp(-halfH, halfH, v);

                // Se curva hacia dentro por los bordes para seguir el cráneo, y se
                // separa un pelín para que no pelee con la esfera en el z-buffer.
                float bulge = Mathf.Sqrt(Mathf.Max(0f, radius * radius - px * px - py * py * 0.6f));
                int i = y * (columns + 1) + x;
                vertices[i] = new Vector3(px, headCentre + py, bulge + 0.004f);
                uv[i] = new Vector2(u, v);
            }

            int t = 0;
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int a = y * (columns + 1) + x;
                int b = a + columns + 1;
                triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
            }

            var mesh = new Mesh { name = "chibi_cara" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
