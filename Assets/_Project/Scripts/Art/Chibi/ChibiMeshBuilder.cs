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
            var torso = MeshShapes.Cylinder(20, 0.44f, 0.5f, 1f);
            clothes.Add((torso, Matrix4x4.TRS(
                new Vector3(0f, bodyCentre, 0f), Quaternion.identity,
                new Vector3(bodyWidth, bodyHeight, bodyWidth * 0.82f))));

            // --- brazos: cuelgan un poco abiertos, que es la pose de reposo ---
            float armLength = height * 0.24f;
            float armThick = height * 0.085f * girth;
            float shoulder = legHeight + bodyHeight * 0.88f;

            // Justo en el borde del torso: más afuera parecían alas, y más adentro
            // desaparecían dentro del cuerpo.
            float armX = bodyWidth * 0.5f + armThick * 0.1f;

            for (int side = -1; side <= 1; side += 2)
            {
                // 12 gajos y no 8: a este grosor, ocho caras planas se ven planas.
                var arm = MeshShapes.Cylinder(12, 0.5f, 0.44f, 1f);
                // Inclinados hacia dentro por abajo: un brazo en reposo cae hacia el
                // cuerpo, no se abre. Con el ángulo al revés parecían alas extendidas.
                clothes.Add((arm, Matrix4x4.TRS(
                    new Vector3(side * armX, shoulder - armLength * 0.45f, 0f),
                    Quaternion.Euler(0f, 0f, side * 5f),
                    new Vector3(armThick, armLength, armThick))));

                var hand = MeshShapes.Sphere(12, 10);
                skin.Add((hand, Matrix4x4.TRS(
                    new Vector3(side * (armX - armLength * 0.04f), shoulder - armLength * 0.92f, 0f),
                    Quaternion.identity, Vector3.one * armThick * 1.15f)));
            }

            // --- piernas ---
            float legThick = height * 0.085f * girth;
            float legX = bodyWidth * 0.26f;

            for (int side = -1; side <= 1; side += 2)
            {
                var leg = MeshShapes.Cylinder(12, 0.5f, 0.5f, 1f);
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
        /// El pelo: un casquete sobre el cráneo más las piezas que pida el peinado.
        /// </summary>
        /// <remarks>
        /// Las piezas salen de <see cref="HairStyles"/>, que es una tabla escrita a
        /// mano. Antes se deducían del número de estilo con condiciones sueltas
        /// —«si es par, flequillo; si pasa de ocho, moño»— y varios números daban
        /// exactamente el mismo pelo: había veinte peinados sobre el papel y bastantes
        /// menos en pantalla. Con la tabla cada entrada es una decisión y se puede
        /// comprobar que las cuarenta son distintas.
        /// </remarks>
        private static Mesh BuildHair(in AppearanceData appearance, float headCentre,
                                      float headWidth, float headHeight)
        {
            var parts = new List<(Mesh, Matrix4x4)>();
            var style = HairStyles.Get(appearance.HairStyle);

            AddCap(parts, style.Cap, headCentre, headWidth, headHeight);
            AddFringe(parts, style.Fringe, headCentre, headWidth, headHeight);
            AddBack(parts, style.Back, headCentre, headWidth, headHeight);
            AddSides(parts, style.Sides, headCentre, headWidth, headHeight);
            AddCrown(parts, style.Crown, headCentre, headWidth, headHeight);

            return MeshShapes.Combine(parts, "chibi_pelo");
        }

        /// <summary>
        /// El casquete. Es un casquete de verdad y no una esfera entera: la esfera
        /// completa envolvía también la cara y tapaba los ojos y la boca — parecía
        /// que todos llevaran pasamontañas.
        /// </summary>
        private static void AddCap(List<(Mesh, Matrix4x4)> parts, HairCap cap,
                                   float headCentre, float headWidth, float headHeight)
        {
            float coverage = cap switch
            {
                HairCap.Shaved => 0.34f,
                HairCap.Short => 0.44f,
                _ => 0.52f,
            };

            // El rapado además se pega al cráneo, o se queda flotando como un gorro.
            float hug = cap == HairCap.Shaved ? 1.005f : 1.04f;

            parts.Add((MeshShapes.SphericalCap(20, 10, coverage), Matrix4x4.TRS(
                new Vector3(0f, headCentre + headHeight * 0.02f, -headWidth * 0.06f),
                Quaternion.identity,
                new Vector3(headWidth * hug, headHeight * (hug - 0.01f), headWidth * (hug + 0.02f)))));
        }

        private static void AddFringe(List<(Mesh, Matrix4x4)> parts, HairFringe fringe,
                                      float headCentre, float headWidth, float headHeight)
        {
            if (fringe == HairFringe.None) return;

            // El flequillo de lado va girado y desplazado, no centrado: es lo único
            // que lo distingue del recto a la distancia a la que se mira la isla.
            float tilt = fringe == HairFringe.Side ? 14f : 0f;
            float offsetX = fringe == HairFringe.Side ? headWidth * 0.13f : 0f;
            float drop = fringe == HairFringe.Long ? 0.20f : 0.30f;
            float thickness = fringe == HairFringe.Long ? 0.42f : 0.30f;

            var mesh = MeshShapes.Sphere(14, 10, new Vector3(1f, thickness, 0.42f));
            parts.Add((mesh, Matrix4x4.TRS(
                new Vector3(offsetX, headCentre + headHeight * drop, headWidth * 0.34f),
                Quaternion.Euler(16f, 0f, tilt), Vector3.one * headWidth * 0.90f)));
        }

        private static void AddBack(List<(Mesh, Matrix4x4)> parts, HairBack back,
                                    float headCentre, float headWidth, float headHeight)
        {
            if (back == HairBack.None) return;

            float length = back == HairBack.Long ? 1.55f : 1.15f;
            float drop = back == HairBack.Long ? 0.42f : 0.18f;

            var mesh = MeshShapes.Sphere(14, 12, new Vector3(0.86f, length, 0.5f));
            parts.Add((mesh, Matrix4x4.TRS(
                new Vector3(0f, headCentre - headHeight * drop, -headWidth * 0.40f),
                Quaternion.identity, Vector3.one * headWidth * 0.82f)));
        }

        private static void AddSides(List<(Mesh, Matrix4x4)> parts, HairSides sides,
                                     float headCentre, float headWidth, float headHeight)
        {
            if (sides == HairSides.None) return;

            for (int side = -1; side <= 1; side += 2)
            {
                switch (sides)
                {
                    case HairSides.Tails:
                        parts.Add((MeshShapes.Sphere(12, 10, new Vector3(0.7f, 1.5f, 0.7f)),
                            Matrix4x4.TRS(
                                new Vector3(side * headWidth * 0.52f,
                                            headCentre - headHeight * 0.05f, -headWidth * 0.12f),
                                Quaternion.Euler(0f, 0f, side * 16f),
                                Vector3.one * headWidth * 0.42f)));
                        break;

                    case HairSides.Braids:
                        // Tres bolas en fila hacen una trenza a esta distancia, y se
                        // distinguen de una coleta lisa porque se ve el escalonado.
                        for (int link = 0; link < 3; link++)
                        {
                            float t = link / 2f;
                            parts.Add((MeshShapes.Sphere(10, 8),
                                Matrix4x4.TRS(
                                    new Vector3(side * headWidth * (0.48f + t * 0.06f),
                                                headCentre - headHeight * (0.05f + t * 0.42f),
                                                -headWidth * 0.10f),
                                    Quaternion.identity,
                                    Vector3.one * headWidth * (0.30f - t * 0.05f))));
                        }
                        break;

                    case HairSides.Curls:
                        for (int curl = 0; curl < 3; curl++)
                        {
                            float t = curl / 2f;
                            parts.Add((MeshShapes.Sphere(10, 8),
                                Matrix4x4.TRS(
                                    new Vector3(side * headWidth * 0.50f,
                                                headCentre + headHeight * (0.18f - t * 0.34f),
                                                -headWidth * (0.06f + t * 0.10f)),
                                    Quaternion.identity,
                                    Vector3.one * headWidth * 0.34f)));
                        }
                        break;
                }
            }
        }

        private static void AddCrown(List<(Mesh, Matrix4x4)> parts, HairCrown crown,
                                     float headCentre, float headWidth, float headHeight)
        {
            switch (crown)
            {
                case HairCrown.Bun:
                    parts.Add((MeshShapes.Sphere(12, 10), Matrix4x4.TRS(
                        new Vector3(0f, headCentre + headHeight * 0.52f, -headWidth * 0.16f),
                        Quaternion.identity, Vector3.one * headWidth * 0.42f)));
                    break;

                case HairCrown.DoubleBun:
                    for (int side = -1; side <= 1; side += 2)
                        parts.Add((MeshShapes.Sphere(12, 10), Matrix4x4.TRS(
                            new Vector3(side * headWidth * 0.36f,
                                        headCentre + headHeight * 0.46f, -headWidth * 0.10f),
                            Quaternion.identity, Vector3.one * headWidth * 0.34f)));
                    break;

                case HairCrown.Spike:
                    // Cinco púas de alturas distintas. Iguales parecían un peine.
                    for (int i = 0; i < 5; i++)
                    {
                        float t = i / 4f;
                        float height = 0.34f + Mathf.Sin(t * Mathf.PI) * 0.30f;
                        parts.Add((MeshShapes.Sphere(8, 6, new Vector3(0.45f, 1.8f, 0.45f)),
                            Matrix4x4.TRS(
                                new Vector3(0f, headCentre + headHeight * 0.48f,
                                            headWidth * (0.26f - t * 0.62f)),
                                Quaternion.Euler(-12f + t * 24f, 0f, 0f),
                                Vector3.one * headWidth * height)));
                    }
                    break;

                case HairCrown.Antenna:
                    // El mechón que no se deja peinar. Es lo que da cara de despistado.
                    parts.Add((MeshShapes.Sphere(8, 6, new Vector3(0.4f, 2.2f, 0.4f)),
                        Matrix4x4.TRS(
                            new Vector3(headWidth * 0.10f, headCentre + headHeight * 0.60f,
                                        -headWidth * 0.04f),
                            Quaternion.Euler(-28f, 0f, 18f),
                            Vector3.one * headWidth * 0.30f)));
                    break;
            }
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
