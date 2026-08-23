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

        /// <summary>
        /// La pieza de la prenda equipada que lleva color propio —capucha, capa,
        /// sombrero—. Null si no toca ninguna: no se crea ni el GameObject.
        /// </summary>
        public readonly Mesh Extra;
        public readonly float Height;

        public ChibiMeshes(Mesh skin, Mesh clothes, Mesh hair, Mesh face, float height,
                           Mesh extra = null)
        {
            Skin = skin; Clothes = clothes; Hair = hair; Face = face; Extra = extra;
            Height = height;
        }
    }

    /// <summary>Solo el cuerpo: piel, ropa y prenda, sin pelo ni cara.</summary>
    /// <remarks>
    /// Existe para vestir a alguien que ya está en escena sin rehacerle la cara ni
    /// el pelo, que es lo caro (textura incluida) y lo que no cambia nunca.
    /// </remarks>
    public readonly struct ChibiBodyMeshes
    {
        public readonly Mesh Skin;
        public readonly Mesh Clothes;
        public readonly Mesh Extra;

        public ChibiBodyMeshes(Mesh skin, Mesh clothes, Mesh extra)
        {
            Skin = skin; Clothes = clothes; Extra = extra;
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
    /// Se devuelven hasta cinco mallas y no una porque cada una lleva su material:
    /// piel, ropa, pelo, la cara —la única con textura— y la prenda equipada, que
    /// solo existe cuando lo que se lleva pide pieza propia.
    /// </remarks>
    public static class ChibiMeshBuilder
    {
        private const float HeadShare = 0.42f;

        public static ChibiMeshes Build(in AppearanceData appearance) =>
            Build(appearance, OutfitLook.Bare());

        /// <summary>
        /// El cuerpo completo con la prenda equipada puesta.
        /// </summary>
        /// <remarks>
        /// La prenda no es una malla importada: es el color de la ropa y, si la familia
        /// lo pide, una pieza más generada. Con un pantalón las piernas pasan de la
        /// malla de piel a la de ropa —dejan de ser piernas desnudas—; con un vestido
        /// la campana sustituye a la cadera y las piernas se quedan en piel, que es
        /// como se lee «vestido» y no «pantalón ancho».
        /// </remarks>
        public static ChibiMeshes Build(in AppearanceData appearance, in OutfitLook look)
        {
            var body = BuildBody(appearance, look);

            var hair = BuildHair(appearance,
                                 HeadCentreOf(appearance, out var headWidth, out var headHeight),
                                 headWidth, headHeight);
            var face = BuildFaceQuad(HeadCentreOf(appearance, out headWidth, out headHeight),
                                     headWidth, headHeight);

            return new ChibiMeshes(body.Skin, body.Clothes, hair, face,
                                   HeightOf(appearance), body.Extra);
        }

        /// <summary>La altura total del muñeco para estos rasgos.</summary>
        private static float HeightOf(in AppearanceData appearance) =>
            Mathf.Lerp(0.95f, 1.35f, appearance.BodyHeight);

        /// <summary>Dónde queda el centro de la cabeza y cuánto mide.</summary>
        private static float HeadCentreOf(in AppearanceData appearance,
                                          out float headWidth, out float headHeight)
        {
            float headSize = HeightOf(appearance) * HeadShare;
            headWidth = headSize * Mathf.Lerp(0.9f, 1.12f, appearance.HeadWidth);
            headHeight = headSize * Mathf.Lerp(0.92f, 1.1f, appearance.HeadHeight);

            float height = HeightOf(appearance);
            return height * 0.24f + height * 0.30f + headHeight * 0.44f;
        }

        /// <summary>
        /// Piel, ropa y prenda de un cuerpo con esa prenda puesta. Es la mitad del
        /// muñeco que cambia al vestir: pelo y cara se quedan como estaban.
        /// </summary>
        public static ChibiBodyMeshes BuildBody(in AppearanceData appearance, in OutfitLook look)
        {
            float height = Mathf.Lerp(0.95f, 1.35f, appearance.BodyHeight);
            float girth = Mathf.Lerp(0.78f, 1.22f, appearance.BodyBuild);

            float headSize = height * HeadShare;
            float headWidth = headSize * Mathf.Lerp(0.9f, 1.12f, appearance.HeadWidth);
            float headHeight = headSize * Mathf.Lerp(0.92f, 1.1f, appearance.HeadHeight);

            float bodyHeight = height * 0.30f;
            float legHeight = height * 0.24f;
            float bodyWidth = height * 0.30f * girth;

            float torsoTop = legHeight + bodyHeight;
            float bodyCentre = legHeight + bodyHeight * 0.5f;
            float headCentre = torsoTop + headHeight * 0.44f;

            var skin = new List<(Mesh, Matrix4x4)>();
            var clothes = new List<(Mesh, Matrix4x4)>();
            var extra = new List<(Mesh, Matrix4x4)>();

            // Un sombrero no viste el cuerpo: quien solo lleva gorra sigue con su ropa
            // y sus piernas de siempre. Y un vestido deja las piernas en piel, que es
            // lo que lo separa de un pantalón a simple vista.
            bool esSombrero = look.Piece == GarmentPiece.HatBrimmed ||
                              look.Piece == GarmentPiece.HatBeanie;
            bool pantalon = look.HasGarment && !esSombrero && look.Piece != GarmentPiece.Skirt;

            // --- cabeza ---
            var headMesh = MeshShapes.Sphere(20, 16);
            skin.Add((headMesh, Matrix4x4.TRS(
                new Vector3(0f, headCentre, 0f), Quaternion.identity,
                new Vector3(headWidth, headHeight, headWidth * 0.94f))));

            // --- cuello ---
            // La cabeza es una esfera y el torso acaba en un círculo más estrecho, así
            // que entre los dos quedaba una muesca de aire: la cabeza no se apoyaba en
            // nada, flotaba encima. El cuello se mete dentro de las dos piezas y la
            // rellena. Se ve poquísimo y se nota muchísimo.
            skin.Add((MeshShapes.Capsule(14, 4, headHeight * 0.26f, headWidth * 0.125f),
                Matrix4x4.TRS(new Vector3(0f, torsoTop, -headWidth * 0.03f),
                              Quaternion.identity, Vector3.one)));

            // --- cuerpo, con la ropa por fuera ---
            var torso = MeshShapes.Cylinder(20, 0.46f, 0.5f, 1f);
            clothes.Add((torso, Matrix4x4.TRS(
                new Vector3(0f, bodyCentre, 0f), Quaternion.identity,
                new Vector3(bodyWidth, bodyHeight, bodyWidth * 0.82f))));

            // --- hombros ---
            // Un tronco de cono acaba en una tapa plana, y el brazo pegado al costado
            // parecía una manga cosida a una caja. Con la bola, el hombro es redondo y
            // el brazo nace de él en vez de estar apoyado al lado.
            //
            // La bola tiene que quedar POR DEBAJO de la barbilla. Puesta más arriba se
            // tragaba media cara: la boca quedaba a la altura del hombro y el muñeco
            // parecía hundido en su propia camiseta.
            // El borde de arriba se mete un diez por ciento dentro de la cabeza: así el
            // cuello queda escondido bajo la barbilla en vez de quedar a la vista como
            // un pedestal, y la boca —que está bastante más arriba— sigue libre.
            // Y redonda, no aplastada: achatada dejaba una meseta plana arriba con un
            // agujero para el cuello, y de perfil parecía que el muñeco llevara un
            // plato al cuello en vez de hombros.
            float shoulderHalf = bodyHeight * 0.33f;
            float shoulder = headCentre - headHeight * 0.44f - shoulderHalf;

            clothes.Add((MeshShapes.Sphere(20, 12), Matrix4x4.TRS(
                new Vector3(0f, shoulder, 0f), Quaternion.identity,
                new Vector3(bodyWidth * 0.99f, shoulderHalf * 2f, bodyWidth * 0.92f))));

            // --- brazos: cuelgan un poco abiertos, que es la pose de reposo ---
            float armLength = height * 0.24f;
            float armThick = height * 0.085f * girth;

            // Justo en el borde del torso: más afuera parecían alas, y más adentro
            // desaparecían dentro del cuerpo.
            float armX = bodyWidth * 0.5f + armThick * 0.14f;

            for (int side = -1; side <= 1; side += 2)
            {
                // La cápsula se construye ya con su tamaño y la matriz solo la mueve:
                // escalarla aplastaría las puntas redondas y volverían las bocas.
                var arm = MeshShapes.Capsule(12, 5, armLength, armThick * 0.5f);

                // Inclinados hacia dentro por abajo: un brazo en reposo cae hacia el
                // cuerpo, no se abre. Con el ángulo al revés parecían alas extendidas.
                var tilt = Quaternion.Euler(0f, 0f, side * 6f);
                var armCentre = new Vector3(side * armX, shoulder - armLength * 0.42f, 0f);
                clothes.Add((arm, Matrix4x4.TRS(armCentre, tilt, Vector3.one)));

                // La mano va en la punta del brazo, girada con él. Antes se colocaba
                // en vertical mientras el brazo iba inclinado, así que asomaba por un
                // lado de la manga.
                var hand = MeshShapes.Sphere(12, 10, new Vector3(1f, 0.92f, 1f));
                skin.Add((hand, Matrix4x4.TRS(
                    armCentre + tilt * new Vector3(0f, -armLength * 0.5f + armThick * 0.22f, 0f),
                    tilt, Vector3.one * armThick * 1.18f)));
            }

            // --- caderas o falda ---
            if (look.Piece == GarmentPiece.Skirt)
            {
                // La campana nace donde acababa la cadera y se abre hasta la rodilla
                // alta. El bajo dobla al radio del torso: es lo que hace que se lea
                // «falda» y no «pantalón ancho».
                clothes.Add((MeshShapes.Cylinder(20, 0.5f, 1f, 1f), Matrix4x4.TRS(
                    new Vector3(0f, legHeight * 0.72f, 0f), Quaternion.identity,
                    new Vector3(bodyWidth * 0.95f, legHeight * 0.85f, bodyWidth * 0.85f))));
            }
            else
            {
                // Un pantalón corto sobre el arranque de las piernas. Sin él salían dos
                // tubos de piel directamente del borde del torso, y el muñeco parecía
                // ir en camiseta y nada más.
                clothes.Add((MeshShapes.Cylinder(20, 0.5f, 0.54f, 1f), Matrix4x4.TRS(
                    new Vector3(0f, legHeight * 0.82f, 0f), Quaternion.identity,
                    new Vector3(bodyWidth * 0.88f, legHeight * 0.40f, bodyWidth * 0.78f))));
            }

            // --- piernas ---
            // Con pantalón dejan de ser piel: se dibujan en la malla de ropa y toman
            // su color. Es el cambio más barato que más se nota al vestir a alguien.
            float legThick = height * 0.085f * girth;
            float legX = bodyWidth * 0.26f;
            var piernas = pantalon ? clothes : skin;

            for (int side = -1; side <= 1; side += 2)
            {
                var leg = MeshShapes.Capsule(12, 5, legHeight, legThick * 0.5f);
                piernas.Add((leg, Matrix4x4.TRS(
                    new Vector3(side * legX, legHeight * 0.5f, 0f),
                    Quaternion.identity, Vector3.one)));

                // El zapato mira hacia delante: la esfera se estira en z y se adelanta
                // media suela, que es lo que da la puntera. Antes era una bolita
                // centrada y de perfil no se veía que hubiera pie.
                var foot = MeshShapes.Sphere(12, 8, new Vector3(1f, 0.7f, 1.62f));
                clothes.Add((foot, Matrix4x4.TRS(
                    new Vector3(side * legX, legThick * 0.36f, legThick * 0.34f),
                    Quaternion.identity, Vector3.one * legThick * 1.6f)));
            }

            // --- la pieza de la prenda, con color propio ---
            switch (look.Piece)
            {
                case GarmentPiece.Hood:
                    // Casquete más grande que el pelo y corrido hacia atrás: la cara
                    // queda fuera y desde detrás —que es desde donde se mira a un
                    // vecino— se lee capucha clara.
                    extra.Add((MeshShapes.SphericalCap(20, 10, 0.62f), Matrix4x4.TRS(
                        new Vector3(0f, headCentre + headHeight * 0.02f, -headWidth * 0.12f),
                        Quaternion.identity,
                        new Vector3(headWidth * 1.18f, headHeight * 1.16f,
                                    headWidth * 1.22f))));

                    // La gola al cuello, para que la capucha no parezca flotar.
                    extra.Add((MeshShapes.Cylinder(14, 0.9f, 1f, 1f), Matrix4x4.TRS(
                        new Vector3(0f, torsoTop - headHeight * 0.02f, -headWidth * 0.02f),
                        Quaternion.identity,
                        new Vector3(headWidth * 0.42f, headHeight * 0.18f,
                                    headWidth * 0.40f))));
                    break;

                case GarmentPiece.Cape:
                    // Una tabla a la espalda, ligeramente girada para que la base se
                    // abra. Desde la cámara, que los ve de espaldas casi siempre, es
                    // literalmente lo único que se ve de una capa.
                    extra.Add((MeshShapes.Box(new Vector3(bodyWidth * 1.05f,
                                                          bodyHeight * 1.35f, 0.07f)),
                               Matrix4x4.TRS(
                                   new Vector3(0f, shoulder - bodyHeight * 0.50f,
                                               -bodyWidth * 0.58f),
                                   Quaternion.Euler(6f, 0f, 0f), Vector3.one)));
                    break;

                case GarmentPiece.HatBrimmed:
                    // Ala de disco y copa de cilindro: dos piezas y se lee sombrero.
                    float ala = headCentre + headHeight * 0.50f;
                    extra.Add((MeshShapes.Cylinder(16, 1f, 1f, 1f), Matrix4x4.TRS(
                        new Vector3(0f, ala, 0f), Quaternion.identity,
                        new Vector3(headWidth * 1.02f, headHeight * 0.045f,
                                    headWidth * 1.02f))));
                    extra.Add((MeshShapes.Cylinder(14, 0.82f, 0.92f, 1f), Matrix4x4.TRS(
                        new Vector3(0f, ala + headHeight * 0.17f, 0f), Quaternion.identity,
                        new Vector3(headWidth * 0.56f, headHeight * 0.34f,
                                    headWidth * 0.56f))));
                    break;

                case GarmentPiece.HatBeanie:
                    // Gorro ceñido con pompón: el casquete del pelo pero más cerrado
                    // y una bola arriba, que es toda la personalidad de un gorro.
                    extra.Add((MeshShapes.SphericalCap(18, 8, 0.55f), Matrix4x4.TRS(
                        new Vector3(0f, headCentre + headHeight * 0.03f, -headWidth * 0.04f),
                        Quaternion.identity,
                        new Vector3(headWidth * 1.12f, headHeight * 1.10f,
                                    headWidth * 1.14f))));
                    extra.Add((MeshShapes.Sphere(10, 8), Matrix4x4.TRS(
                        new Vector3(0f, headCentre + headHeight * 0.60f, 0f),
                        Quaternion.identity, Vector3.one * headWidth * 0.15f)));
                    break;
            }

            return new ChibiBodyMeshes(
                MeshShapes.Combine(skin, "chibi_piel"),
                MeshShapes.Combine(clothes, "chibi_ropa"),
                extra.Count > 0 ? MeshShapes.Combine(extra, "chibi_prenda") : null);
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
                new Vector3(0f, headCentre - headHeight * drop, -headWidth * 0.33f),
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
                                new Vector3(side * headWidth * 0.46f,
                                            headCentre - headHeight * 0.02f, -headWidth * 0.10f),
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
                                    new Vector3(side * headWidth * (0.43f + t * 0.05f),
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
                                    new Vector3(side * headWidth * 0.44f,
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
                        new Vector3(0f, headCentre + headHeight * 0.44f, -headWidth * 0.10f),
                        Quaternion.identity, Vector3.one * headWidth * 0.46f)));
                    break;

                case HairCrown.DoubleBun:
                    for (int side = -1; side <= 1; side += 2)
                        parts.Add((MeshShapes.Sphere(12, 10), Matrix4x4.TRS(
                            new Vector3(side * headWidth * 0.36f,
                                        headCentre + headHeight * 0.40f, -headWidth * 0.08f),
                            Quaternion.identity, Vector3.one * headWidth * 0.36f)));
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
