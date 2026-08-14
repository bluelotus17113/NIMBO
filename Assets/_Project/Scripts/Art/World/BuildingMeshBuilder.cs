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
        public readonly Mesh Trim;   // puertas, postes y madera
        public readonly Mesh Glass;  // escaparates y ventanas

        public BuildingMeshes(Mesh walls, Mesh roof, Mesh trim, Mesh glass)
        {
            Walls = walls; Roof = roof; Trim = trim; Glass = glass;
        }
    }

    /// <summary>
    /// Construye los edificios de la isla a partir de para qué sirve cada zona.
    /// </summary>
    /// <remarks>
    /// Las formas son sencillas —cajas, conos y cilindros— porque desde la cámara de
    /// la isla lo que distingue una tienda de una casa es la silueta y el color del
    /// tejado. Pero «sencillas» no es «sin medidas»: los números están escritos en
    /// metros de verdad, contra el muñeco.
    ///
    /// Y ese fue el error que estuvo aquí desde el principio. Todo esto se dibujó
    /// cuando el juego era un mirador y no se andaba por la isla: la casa medía
    /// dieciséis metros de ancho y su puerta cuatro de alto, con vecinos de metro
    /// veinte. La aldea entera estaba hecha para gigantes y no se veía, porque desde
    /// ochenta metros y en cenital no hay con qué comparar. Se vio a la primera foto
    /// con un vecino plantado delante: le llegaba a media puerta.
    ///
    /// Referencia: un vecino mide entre 0,95 y 1,35 m. Una puerta, 1,72 m — vez y
    /// media el vecino, que es la proporción de Animal Crossing. Con dos metros la
    /// puerta ya le doblaba y la casa volvía a parecer de otra escala.
    /// </remarks>
    public static class BuildingMeshBuilder
    {
        /// <summary>Alto de una puerta, en metros. Todo lo demás se mide contra esto.</summary>
        private const float DoorHeight = 1.72f;

        public static BuildingMeshes Build(ZonePurpose purpose, float scale = 1f)
        {
            var walls = new List<(Mesh, Matrix4x4)>();
            var roof = new List<(Mesh, Matrix4x4)>();
            var trim = new List<(Mesh, Matrix4x4)>();
            var glass = new List<(Mesh, Matrix4x4)>();

            switch (purpose)
            {
                case ZonePurpose.Home:
                    BuildApartments(walls, roof, trim, glass, scale);
                    break;
                case ZonePurpose.Food:
                case ZonePurpose.Shopping:
                    BuildShop(walls, roof, trim, glass, scale);
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
                Combine(trim, "edificio_detalle"),
                Combine(glass, "edificio_cristal"));
        }

        /// <summary>
        /// Un bloque de cuatro viviendas: dos plantas, tejado a cuatro aguas y una
        /// puerta por vecino.
        /// </summary>
        private static void BuildApartments(List<(Mesh, Matrix4x4)> walls,
                                            List<(Mesh, Matrix4x4)> roof,
                                            List<(Mesh, Matrix4x4)> trim,
                                            List<(Mesh, Matrix4x4)> glass, float s)
        {
            const float Width = 7f, Depth = 4.5f, Storey = 2.15f;
            const float Height = Storey * 2f;
            float front = Depth * 0.5f;

            walls.Add((MeshShapes.Box(new Vector3(Width, Height, Depth) * s),
                Matrix4x4.Translate(new Vector3(0f, Height * 0.5f, 0f) * s)));

            // Zócalo de madera. Un edificio que arranca directamente del césped parece
            // pegado encima; con un escalón alrededor, se apoya.
            //
            // De madera y no en color de pared: la cara de arriba del zócalo mira al
            // sol de lleno, y en crema se quemaba a blanco puro. El edificio quedaba
            // con una falda deslumbrante que se veía antes que la fachada.
            trim.Add((MeshShapes.Box(new Vector3(Width + 0.45f, 0.36f, Depth + 0.45f) * s),
                Matrix4x4.Translate(new Vector3(0f, 0.18f, 0f) * s)));

            // El alero: una losa fina que sobresale bajo el tejado, y del color del
            // tejado. En color de pared se quemaba a blanco puro bajo el sol y dejaba
            // una franja deslumbrante entre la fachada y las tejas; siendo teja, es el
            // canto del tejado y hace la sombra que separa una cosa de la otra.
            roof.Add((MeshShapes.Box(new Vector3(Width + 0.95f, 0.2f, Depth + 0.95f) * s),
                Matrix4x4.Translate(new Vector3(0f, Height + 0.1f, 0f) * s)));

            // El tejado es un cono de cuatro lados: a esta distancia se lee igual que
            // uno a dos aguas y cuesta la mitad de vértices.
            roof.Add((MeshShapes.Cylinder(4, 0.02f, 0.5f, 1f), Matrix4x4.TRS(
                new Vector3(0f, Height + 1.35f, 0f) * s, Quaternion.Euler(0f, 45f, 0f),
                new Vector3(Width + 2f, 2.5f, Depth + 2f) * s)));

            // La chimenea, descentrada. Es el detalle que hace que el bloque no parezca
            // un cuartel: rompe la simetría sin costar nada.
            walls.Add((MeshShapes.Box(new Vector3(0.55f, 1.4f, 0.55f) * s),
                Matrix4x4.Translate(new Vector3(-Width * 0.28f, Height + 1.25f, -0.95f) * s)));

            // Dos puertas abajo y dos arriba, no cuatro en fila.
            //
            // Cuatro puertas en siete metros de fachada dan metro y medio por vivienda:
            // salían cuatro rectángulos marrones idénticos pegados unos a otros y el
            // bloque se leía como un motel de carretera. Y no se podía ensanchar el
            // edificio, porque la parcela de la rejilla son dos casillas de cuatro
            // metros y la fachada ya ocupa casi las dos enteras.
            //
            // Así que las cuatro viviendas se reparten en las dos plantas, con una
            // pasarela y una escalera exterior para llegar a las de arriba. Las de
            // abajo quedan a tres metros y pico una de otra, que ya es una casa, y la
            // escalera le da al bloque la silueta que le faltaba: era una caja lisa con
            // agujeros y ahora tiene delante y fondo.
            const float DoorSpread = 1.75f;
            const float Walkway = Storey + 0.1f;

            for (int i = 0; i < 2; i++)
            {
                float x = (i * 2f - 1f) * DoorSpread;

                Doorway(x, 0f);                 // planta baja
                Doorway(x, Walkway);            // planta alta, sobre la pasarela

                // Ventana al lado de cada puerta, en las dos plantas. Antes solo había
                // arriba y la planta baja era una tira de puertas sin nada más.
                Window(x + DoorSpread * 0.62f, 1.1f);
                Window(x + DoorSpread * 0.62f, Walkway + 1.1f);
            }

            Balcony();
            Staircase();

            // Marco, hoja y escalón de una entrada.
            void Doorway(float x, float y)
            {
                // Marco en color de pared alrededor de la puerta: sin él, la puerta era
                // un rectángulo oscuro pegado a la fachada y se leía como un agujero.
                walls.Add((MeshShapes.Box(new Vector3(1.14f, DoorHeight + 0.26f, 0.13f) * s),
                    Matrix4x4.Translate(new Vector3(x, y + (DoorHeight + 0.26f) * 0.5f, front + 0.05f) * s)));

                trim.Add((MeshShapes.Box(new Vector3(0.88f, DoorHeight, 0.15f) * s),
                    Matrix4x4.Translate(new Vector3(x, y + DoorHeight * 0.5f, front + 0.12f) * s)));

                // El escalón de entrada. Dice por dónde se entra mejor que la puerta.
                trim.Add((MeshShapes.Box(new Vector3(1.26f, 0.16f, 0.55f) * s),
                    Matrix4x4.Translate(new Vector3(x, y + 0.26f, front + 0.45f) * s)));
            }

            void Window(float x, float y)
            {
                glass.Add((MeshShapes.Box(new Vector3(0.72f, 0.78f, 0.11f) * s),
                    Matrix4x4.Translate(new Vector3(x, y + 0.4f, front + 0.08f) * s)));

                // El alféizar: sin él la ventana es un cristal pegado a la pared.
                walls.Add((MeshShapes.Box(new Vector3(0.9f, 0.13f, 0.26f) * s),
                    Matrix4x4.Translate(new Vector3(x, y - 0.07f, front + 0.11f) * s)));
            }

            // La pasarela de la planta alta, con su barandilla.
            void Balcony()
            {
                trim.Add((MeshShapes.Box(new Vector3(Width + 0.3f, 0.16f, 1.15f) * s),
                    Matrix4x4.Translate(new Vector3(0f, Walkway, front + 0.55f) * s)));

                // Pasamanos y balaustres. Sin barandilla la pasarela es una repisa, y
                // una repisa a dos metros con puertas encima no se lee como un piso.
                trim.Add((MeshShapes.Box(new Vector3(Width + 0.3f, 0.1f, 0.1f) * s),
                    Matrix4x4.Translate(new Vector3(0f, Walkway + 0.85f, front + 1.08f) * s)));

                for (int i = 0; i <= 7; i++)
                {
                    float x = (i / 7f - 0.5f) * (Width + 0.2f);
                    trim.Add((MeshShapes.Box(new Vector3(0.09f, 0.85f, 0.09f) * s),
                        Matrix4x4.Translate(new Vector3(x, Walkway + 0.45f, front + 1.08f) * s)));
                }
            }

            // La escalera exterior, pegada al costado derecho.
            void Staircase()
            {
                const int Steps = 7;
                const float Tread = 0.45f;

                float sideX = Width * 0.5f + 0.3f;   // pegada al canto de la pasarela
                float top = front + 1.05f;           // donde entrega, en el borde

                // Cada peldaño es un bloque macizo desde el suelo, no una losa a su
                // altura. Con losas sueltas, la subida de treinta y dos centímetros y
                // el grueso de catorce dejaban dieciocho de aire entre una y la
                // siguiente: se veía una escalera de tablas flotando en el vacío.
                //
                // Y sube alejándose de la fachada. Al revés, los peldaños de arriba
                // caían por detrás del plano de la fachada y quedaban enterrados en el
                // muro: la escalera empezaba en el suelo y desaparecía dentro de casa.
                for (int i = 0; i < Steps; i++)
                {
                    float height = Walkway * (i + 1f) / Steps;

                    trim.Add((MeshShapes.Box(new Vector3(1.1f, height, Tread) * s),
                        Matrix4x4.Translate(new Vector3(
                            sideX, height * 0.5f, top + (Steps - 1 - i) * Tread) * s)));
                }
            }
        }

        /// <summary>Una tienda: escaparate, toldo y cartel.</summary>
        private static void BuildShop(List<(Mesh, Matrix4x4)> walls,
                                      List<(Mesh, Matrix4x4)> roof,
                                      List<(Mesh, Matrix4x4)> trim,
                                      List<(Mesh, Matrix4x4)> glass, float s)
        {
            const float Width = 5.8f, Depth = 4.2f, Height = 3.3f;
            float front = Depth * 0.5f;

            walls.Add((MeshShapes.Box(new Vector3(Width, Height, Depth) * s),
                Matrix4x4.Translate(new Vector3(0f, Height * 0.5f, 0f) * s)));

            trim.Add((MeshShapes.Box(new Vector3(Width + 0.4f, 0.4f, Depth + 0.4f) * s),
                Matrix4x4.Translate(new Vector3(0f, 0.2f, 0f) * s)));

            // Una sola cornisa y una banda de tejado encima. Antes había cornisa,
            // azotea, toldo y cartel a alturas parecidas, y de frente se leían como un
            // montón de tablas apiladas donde no se distinguía qué era cada cosa.
            walls.Add((MeshShapes.Box(new Vector3(Width + 0.6f, 0.24f, Depth + 0.6f) * s),
                Matrix4x4.Translate(new Vector3(0f, Height + 0.12f, 0f) * s)));

            roof.Add((MeshShapes.Box(new Vector3(Width + 0.2f, 0.34f, Depth + 0.2f) * s),
                Matrix4x4.Translate(new Vector3(0f, Height + 0.41f, 0f) * s)));

            // El escaparate, a la izquierda, con su marco. Antes ocupaba nueve metros
            // de pared y tres y medio de alto: no era un escaparate, era un muro negro.
            walls.Add((MeshShapes.Box(new Vector3(3.9f, 1.9f, 0.1f) * s),
                Matrix4x4.Translate(new Vector3(-0.75f, 1.72f, front + 0.04f) * s)));

            glass.Add((MeshShapes.Box(new Vector3(3.5f, 1.5f, 0.12f) * s),
                Matrix4x4.Translate(new Vector3(-0.75f, 1.72f, front + 0.11f) * s)));

            // El travesaño parte el cristal en dos hojas: un vidrio de tres metros y
            // medio sin nada que lo cruce se lee como un hueco, no como una ventana.
            trim.Add((MeshShapes.Box(new Vector3(3.7f, 0.11f, 0.16f) * s),
                Matrix4x4.Translate(new Vector3(-0.75f, 1.72f, front + 0.12f) * s)));

            // Puerta a la derecha, con marco y escalón.
            walls.Add((MeshShapes.Box(new Vector3(1.24f, DoorHeight + 0.24f, 0.12f) * s),
                Matrix4x4.Translate(new Vector3(1.95f, (DoorHeight + 0.24f) * 0.5f, front + 0.04f) * s)));

            trim.Add((MeshShapes.Box(new Vector3(0.96f, DoorHeight, 0.15f) * s),
                Matrix4x4.Translate(new Vector3(1.95f, DoorHeight * 0.5f, front + 0.12f) * s)));

            trim.Add((MeshShapes.Box(new Vector3(1.4f, 0.16f, 0.6f) * s),
                Matrix4x4.Translate(new Vector3(1.95f, 0.25f, front + 0.46f) * s)));

            // El toldo va SOLO sobre el escaparate, no sobre la fachada entera: tapando
            // también la puerta, el edificio quedaba con una visera de lado a lado y no
            // se veía por dónde se entraba.
            var slope = Quaternion.Euler(-20f, 0f, 0f);
            var awningAt = new Vector3(-0.75f, 2.62f, front + 0.62f) * s;

            roof.Add((MeshShapes.Box(new Vector3(4.2f, 0.12f, 1.25f) * s),
                Matrix4x4.TRS(awningAt, slope, Vector3.one)));

            // El faldón cuelga del canto bajo del toldo, girado con él. Suelto y en
            // horizontal quedaba una tabla flotando delante de la tienda.
            roof.Add((MeshShapes.Box(new Vector3(4.2f, 0.3f, 0.1f) * s), Matrix4x4.TRS(
                awningAt + slope * (new Vector3(0f, -0.15f, 0.62f) * s), slope, Vector3.one)));

            // El cartel, sobre la puerta y pegado a la pared.
            roof.Add((MeshShapes.Box(new Vector3(1.5f, 0.6f, 0.14f) * s),
                Matrix4x4.Translate(new Vector3(1.95f, 2.66f, front + 0.06f) * s)));
        }

        /// <summary>El escenario: tarima con fondo y dos focos.</summary>
        private static void BuildStage(List<(Mesh, Matrix4x4)> walls,
                                       List<(Mesh, Matrix4x4)> roof,
                                       List<(Mesh, Matrix4x4)> trim, float s)
        {
            walls.Add((MeshShapes.Box(new Vector3(9f, 0.85f, 6f) * s),
                Matrix4x4.Translate(new Vector3(0f, 0.43f, 0f) * s)));

            walls.Add((MeshShapes.Box(new Vector3(9f, 4.4f, 0.5f) * s),
                Matrix4x4.Translate(new Vector3(0f, 2.6f, -2.75f) * s)));

            roof.Add((MeshShapes.Box(new Vector3(9.6f, 0.4f, 6.6f) * s),
                Matrix4x4.Translate(new Vector3(0f, 5f, -0.25f) * s)));

            for (int side = -1; side <= 1; side += 2)
            {
                walls.Add((MeshShapes.Cylinder(8, 0.5f, 0.5f, 1f), Matrix4x4.TRS(
                    new Vector3(side * 4.25f, 2.5f, 2.75f) * s, Quaternion.identity,
                    new Vector3(0.45f, 5f, 0.45f) * s)));

                trim.Add((MeshShapes.Sphere(10, 8), Matrix4x4.TRS(
                    new Vector3(side * 4.25f, 4.7f, 2.75f) * s, Quaternion.identity,
                    Vector3.one * 0.9f * s)));
            }
        }

        /// <summary>El parque: un estanque, un banco y un par de arbustos.</summary>
        private static void BuildPark(List<(Mesh, Matrix4x4)> walls,
                                      List<(Mesh, Matrix4x4)> roof,
                                      List<(Mesh, Matrix4x4)> trim, float s)
        {
            trim.Add((MeshShapes.Cylinder(20, 0.5f, 0.48f, 1f), Matrix4x4.TRS(
                new Vector3(0f, 0.1f, 0f) * s, Quaternion.identity,
                new Vector3(6.5f, 0.25f, 4.5f) * s)));

            walls.Add((MeshShapes.Box(new Vector3(3f, 0.22f, 0.9f) * s),
                Matrix4x4.Translate(new Vector3(4.5f, 0.72f, 2f) * s)));

            walls.Add((MeshShapes.Box(new Vector3(3f, 0.85f, 0.18f) * s),
                Matrix4x4.Translate(new Vector3(4.5f, 1.2f, 1.6f) * s)));

            for (int side = -1; side <= 1; side += 2)
                walls.Add((MeshShapes.Box(new Vector3(0.16f, 0.62f, 0.7f) * s),
                    Matrix4x4.Translate(new Vector3(4.5f + side * 1.3f, 0.31f, 2f) * s)));

            var bush = MeshShapes.Sphere(12, 9, new Vector3(1f, 0.8f, 1f));
            for (int i = 0; i < 5; i++)
            {
                float angle = i / 5f * Mathf.PI * 2f;
                roof.Add((bush, Matrix4x4.TRS(
                    new Vector3(Mathf.Cos(angle) * 6f, 0.7f, Mathf.Sin(angle) * 5f) * s,
                    Quaternion.identity, Vector3.one * 1.7f * s)));
            }
        }

        /// <summary>El embarcadero: pasarela hacia el vacío y un farol al final.</summary>
        private static void BuildDock(List<(Mesh, Matrix4x4)> walls,
                                      List<(Mesh, Matrix4x4)> roof,
                                      List<(Mesh, Matrix4x4)> trim, float s)
        {
            walls.Add((MeshShapes.Box(new Vector3(2.3f, 0.3f, 10f) * s),
                Matrix4x4.Translate(new Vector3(0f, 0.2f, 4f) * s)));

            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                walls.Add((MeshShapes.Cylinder(6, 0.5f, 0.5f, 1f), Matrix4x4.TRS(
                    new Vector3(side * 1f, 0.8f, 1f + i * 2.5f) * s, Quaternion.identity,
                    new Vector3(0.3f, 1.5f, 0.3f) * s)));
            }

            trim.Add((MeshShapes.Sphere(10, 8), Matrix4x4.TRS(
                new Vector3(0f, 2.1f, 8.5f) * s, Quaternion.identity, Vector3.one * 1f * s)));
        }

        /// <summary>La plaza: un suelo empedrado, un banco y el tablón de noticias.</summary>
        private static void BuildPlaza(List<(Mesh, Matrix4x4)> walls,
                                       List<(Mesh, Matrix4x4)> roof,
                                       List<(Mesh, Matrix4x4)> trim, float s)
        {
            trim.Add((MeshShapes.Cylinder(24, 0.5f, 0.5f, 1f), Matrix4x4.TRS(
                new Vector3(0f, 0.08f, 0f) * s, Quaternion.identity,
                new Vector3(14f, 0.2f, 14f) * s)));

            walls.Add((MeshShapes.Box(new Vector3(2.5f, 1.75f, 0.25f) * s),
                Matrix4x4.Translate(new Vector3(4.5f, 1.8f, 2f) * s)));

            for (int side = -1; side <= 1; side += 2)
            {
                walls.Add((MeshShapes.Cylinder(6, 0.5f, 0.5f, 1f), Matrix4x4.TRS(
                    new Vector3(4.5f + side * 0.9f, 0.6f, 2f) * s, Quaternion.identity,
                    new Vector3(0.25f, 1.2f, 0.25f) * s)));
            }

            walls.Add((MeshShapes.Box(new Vector3(3.5f, 0.22f, 1f) * s),
                Matrix4x4.Translate(new Vector3(-4.5f, 0.72f, 0f) * s)));
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
