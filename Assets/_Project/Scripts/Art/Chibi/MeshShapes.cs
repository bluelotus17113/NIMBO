using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Art.Chibi
{
    /// <summary>
    /// Formas básicas generadas por código: esferas, cilindros, cajas y conos.
    /// </summary>
    /// <remarks>
    /// Se generan aquí en vez de usar <c>GameObject.CreatePrimitive</c> porque eso
    /// crea y destruye objetos de escena solo para robarles la malla, y en modo
    /// batch — que es como se construye todo esto — no siempre están disponibles.
    /// Con mallas propias, además, se controla el número de gajos: un habitante
    /// chibi no necesita la esfera de 500 triángulos de Unity.
    /// </remarks>
    public static class MeshShapes
    {
        /// <summary>Esfera UV. <paramref name="scale"/> permite achatarla o estirarla.</summary>
        public static Mesh Sphere(int segments = 16, int rings = 12, Vector3 scale = default)
        {
            if (scale == default) scale = Vector3.one;

            var vertices = new List<Vector3>((segments + 1) * (rings + 1));
            var normals = new List<Vector3>(vertices.Capacity);
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(segments * rings * 6);

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = (float)ring / rings;
                float phi = v * Mathf.PI;
                float y = Mathf.Cos(phi);
                float r = Mathf.Sin(phi);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = (float)seg / segments;
                    float theta = u * Mathf.PI * 2f;
                    var unit = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);

                    vertices.Add(Vector3.Scale(unit, scale) * 0.5f);
                    normals.Add(unit);
                    uv.Add(new Vector2(u, 1f - v));
                }
            }

            int stride = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            for (int seg = 0; seg < segments; seg++)
            {
                int a = ring * stride + seg;
                int b = a + stride;

                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }

            return Build("esfera", vertices, normals, uv, triangles);
        }

        /// <summary>
        /// Un casquete: la parte de arriba de una esfera, cerrado por abajo con un disco.
        /// </summary>
        /// <param name="coverage">
        /// Cuánta esfera se conserva, de 0 a 1. <c>0.5</c> es media esfera exacta;
        /// por encima empieza a bajar por los lados de la cabeza.
        /// </param>
        /// <remarks>
        /// Existe por el pelo. Con una esfera entera, el casquete envolvía también la
        /// cara y tapaba los ojos y la boca — todos los habitantes parecían llevar
        /// pasamontañas. Un casquete solo cubre cráneo y nuca, que es lo que hace el
        /// pelo de verdad.
        ///
        /// Cerrado por abajo a propósito. Desde que la cámara va a la altura de los
        /// ojos se ve mucho casquete desde abajo —el vecino de delante, el interior
        /// de una capucha— y un cacillo abierto enseña su borde en canto, como papel:
        /// la malla de un lado no tiene nada detrás. El disco reutiliza el último
        /// anillo como borde en vez de duplicarlo, así que cerrar cuesta
        /// <c>segments</c> triángulos y un vértice: 20 por casquete de pelo, frente
        /// a los 400 que ya traía la campana.
        /// </remarks>
        public static Mesh SphericalCap(int segments = 18, int rings = 10, float coverage = 0.55f)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            float maxPhi = Mathf.Clamp01(coverage) * Mathf.PI;

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = (float)ring / rings;
                float phi = v * maxPhi;
                float y = Mathf.Cos(phi);
                float r = Mathf.Sin(phi);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = (float)seg / segments;
                    float theta = u * Mathf.PI * 2f;
                    var unit = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);

                    vertices.Add(unit * 0.5f);
                    normals.Add(unit);
                    uv.Add(new Vector2(u, 1f - v));
                }
            }

            int stride = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            for (int seg = 0; seg < segments; seg++)
            {
                int a = ring * stride + seg;
                int b = a + stride;

                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }

            // El disco del fondo: un vértice central y un abanico contra el último
            // anillo, que hace de borde sin duplicar ni un vértice. Mismo orden que
            // la tapa inferior de Cylinder para que mire hacia fuera —hacia abajo—
            // y no se vea del revés desde abajo.
            int centro = vertices.Count;
            vertices.Add(new Vector3(0f, Mathf.Cos(maxPhi), 0f) * 0.5f);
            normals.Add(Vector3.down);
            uv.Add(new Vector2(0.5f, 0.5f));

            int borde = rings * stride;
            for (int seg = 0; seg < segments; seg++)
            {
                triangles.Add(centro);
                triangles.Add(borde + seg + 1);
                triangles.Add(borde + seg);
            }

            return Build("casquete", vertices, normals, uv, triangles);
        }

        /// <summary>
        /// Cápsula: un cilindro rematado en media esfera por arriba y por abajo.
        /// </summary>
        /// <param name="height">Alto total, puntas incluidas.</param>
        /// <remarks>
        /// Es lo que van a ser los brazos y las piernas. Un cilindro acaba en una tapa
        /// plana, y a esta escala esa tapa se lee como la boca de un tubo hueco: los
        /// brazos parecían mangas de papel con una bola de mano metida dentro.
        ///
        /// El ecuador se genera dos veces a propósito, una por cada media esfera. Con
        /// un solo anillo compartido, la pared del cilindro hereda la normal inclinada
        /// del casquete y se sombrea como si siguiera curvándose.
        /// </remarks>
        public static Mesh Capsule(int segments = 12, int rings = 6, float height = 1f,
                                   float radius = 0.25f)
        {
            radius = Mathf.Min(radius, height * 0.5f);
            float half = Mathf.Max(0f, height * 0.5f - radius);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            for (int hemi = 0; hemi < 2; hemi++)
            {
                float offset = hemi == 0 ? half : -half;

                for (int ring = 0; ring <= rings; ring++)
                {
                    // La de arriba va de 0° a 90°; la de abajo, de 90° a 180°.
                    float phi = (hemi + (float)ring / rings) * Mathf.PI * 0.5f;
                    float y = Mathf.Cos(phi);
                    float r = Mathf.Sin(phi);

                    for (int seg = 0; seg <= segments; seg++)
                    {
                        float u = (float)seg / segments;
                        float theta = u * Mathf.PI * 2f;
                        var unit = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);

                        vertices.Add(unit * radius + new Vector3(0f, offset, 0f));
                        normals.Add(unit);
                        uv.Add(new Vector2(u, (hemi + (float)ring / rings) * 0.5f));
                    }
                }
            }

            int stride = segments + 1;
            int rows = 2 * (rings + 1);

            for (int row = 0; row < rows - 1; row++)
            for (int seg = 0; seg < segments; seg++)
            {
                int a = row * stride + seg;
                int b = a + stride;

                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }

            return Build("capsula", vertices, normals, uv, triangles);
        }

        /// <summary>Cilindro con tapas, con el eje en Y y centrado en el origen.</summary>
        public static Mesh Cylinder(int segments = 14, float topRadius = 0.5f,
                                    float bottomRadius = 0.5f, float height = 1f)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            float half = height * 0.5f;

            for (int seg = 0; seg <= segments; seg++)
            {
                float u = (float)seg / segments;
                float theta = u * Mathf.PI * 2f;
                float cos = Mathf.Cos(theta), sin = Mathf.Sin(theta);

                vertices.Add(new Vector3(cos * topRadius, half, sin * topRadius));
                vertices.Add(new Vector3(cos * bottomRadius, -half, sin * bottomRadius));

                var side = new Vector3(cos, 0f, sin).normalized;
                normals.Add(side); normals.Add(side);
                uv.Add(new Vector2(u, 1f)); uv.Add(new Vector2(u, 0f));
            }

            for (int seg = 0; seg < segments; seg++)
            {
                int a = seg * 2;
                triangles.Add(a); triangles.Add(a + 1); triangles.Add(a + 2);
                triangles.Add(a + 2); triangles.Add(a + 1); triangles.Add(a + 3);
            }

            AddCap(vertices, normals, uv, triangles, segments, topRadius, half, Vector3.up);
            AddCap(vertices, normals, uv, triangles, segments, bottomRadius, -half, Vector3.down);

            return Build("cilindro", vertices, normals, uv, triangles);
        }

        private static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv,
                                   List<int> triangles, int segments, float radius, float y,
                                   Vector3 normal)
        {
            int center = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f));
            normals.Add(normal);
            uv.Add(new Vector2(0.5f, 0.5f));

            for (int seg = 0; seg <= segments; seg++)
            {
                float theta = (float)seg / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(theta), sin = Mathf.Sin(theta);
                vertices.Add(new Vector3(cos * radius, y, sin * radius));
                normals.Add(normal);
                uv.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
            }

            for (int seg = 0; seg < segments; seg++)
            {
                int a = center + 1 + seg;
                // Las tapas de arriba y de abajo giran al revés para que las dos
                // miren hacia fuera; si no, una de las dos se ve del revés.
                if (normal.y > 0f) { triangles.Add(center); triangles.Add(a); triangles.Add(a + 1); }
                else { triangles.Add(center); triangles.Add(a + 1); triangles.Add(a); }
            }
        }

        public static Mesh Box(Vector3 size)
        {
            var mesh = new Mesh { name = "caja" };
            Vector3 h = size * 0.5f;

            Vector3[] vertices =
            {
                new(-h.x, -h.y,  h.z), new( h.x, -h.y,  h.z), new( h.x,  h.y,  h.z), new(-h.x,  h.y,  h.z),
                new( h.x, -h.y, -h.z), new(-h.x, -h.y, -h.z), new(-h.x,  h.y, -h.z), new( h.x,  h.y, -h.z),
                new(-h.x,  h.y,  h.z), new( h.x,  h.y,  h.z), new( h.x,  h.y, -h.z), new(-h.x,  h.y, -h.z),
                new(-h.x, -h.y, -h.z), new( h.x, -h.y, -h.z), new( h.x, -h.y,  h.z), new(-h.x, -h.y,  h.z),
                new(-h.x, -h.y, -h.z), new(-h.x, -h.y,  h.z), new(-h.x,  h.y,  h.z), new(-h.x,  h.y, -h.z),
                new( h.x, -h.y,  h.z), new( h.x, -h.y, -h.z), new( h.x,  h.y, -h.z), new( h.x,  h.y,  h.z),
            };

            var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            {
                int v = face * 4, t = face * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }

            var uv = new Vector2[24];
            for (int face = 0; face < 6; face++)
            {
                int v = face * 4;
                uv[v] = new Vector2(0, 0); uv[v + 1] = new Vector2(1, 0);
                uv[v + 2] = new Vector2(1, 1); uv[v + 3] = new Vector2(0, 1);
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Une varias mallas en una sola, cada una con su transformación. Es lo que
        /// convierte a un habitante de nueve objetos en uno, y de nueve draw calls
        /// en uno.
        /// </summary>
        public static Mesh Combine(IReadOnlyList<(Mesh mesh, Matrix4x4 transform)> parts, string name)
        {
            var combines = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++)
                combines[i] = new CombineInstance { mesh = parts[i].mesh, transform = parts[i].transform };

            var combined = new Mesh { name = name };
            combined.CombineMeshes(combines, mergeSubMeshes: true, useMatrices: true);

            // NO se recalculan las normales: Sphere y Cylinder ya las traen suaves y
            // correctas, y RecalculateNormals las promedia por posición — mezclando
            // las del costado de un cilindro con las de su tapa. El resultado es que
            // brazos y piernas se sombrean como paneles planos en vez de como tubos.
            combined.RecalculateBounds();
            return combined;
        }

        private static Mesh Build(string name, List<Vector3> vertices, List<Vector3> normals,
                                  List<Vector2> uv, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
