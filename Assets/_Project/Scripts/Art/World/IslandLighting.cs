using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// La luz de la isla: el sol y de qué color se ve lo que está a la sombra.
    /// </summary>
    /// <remarks>
    /// Está aquí, y no dentro del que monta la escena, porque las herramientas de
    /// captura montan su propio plató y tienen que alumbrarlo igual que el juego.
    /// Cuando cada una llevaba su copia de los números, el plató enseñaba una luz
    /// que la isla ya no tenía y las fotos dejaban de servir para decidir nada.
    ///
    /// El sol es flojo a propósito. Estaba a 1,35 sobre un ambiente de 0,7, y una
    /// pared crema —que es casi blanca de partida— saturaba en cuanto le daba:
    /// la fachada de la residencial salía como una hoja en blanco, sin ventanas,
    /// sin marcos y sin alféizares. Con 0,65 la suma se queda justo por debajo de
    /// uno y el crema vuelve a ser crema. Lo que se pierde de contraste se
    /// recupera bajando el relleno, no subiendo el sol.
    /// </remarks>
    public static class IslandLighting
    {
        /// <summary>Un mediodía de lado: alarga las sombras sin tumbarlas.</summary>
        public static readonly Quaternion SunRotation = Quaternion.Euler(48f, -35f, 0f);

        public static readonly Color SunColor = new(1f, 0.96f, 0.88f);

        public static readonly float SunIntensity = 0.65f;

        /// <summary>Deja el sol como lo tiene la isla. Devuelve la misma luz.</summary>
        public static Light Apply(Light light)
        {
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = SunIntensity;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = SunRotation;

            ApplyAmbient();
            ApplyHaze();
            return light;
        }

        /// <summary>
        /// La bruma del fondo: lo lejano se va destiñendo hacia el color del cielo.
        /// </summary>
        /// <remarks>
        /// Es lo que hace que una isla flotante se lea como una isla flotante y no
        /// como una maqueta recortada contra un fondo liso.
        ///
        /// La escala es la de la cámara de ojos (5,5 m), no la del mirador. Arrancaba
        /// a 60 m pensada para mirar la isla desde 150, y desde la tercera persona no
        /// se activaba jamás: hasta la orilla lejana de la aldea —radio 100 m,
        /// Archipelago.VillageRadius— solo cogía un 18 % y el prado se leía nítido de
        /// orilla a orilla. Con el inicio a 30 m el campo de juego (<30 m) sale limpio
        /// y la bruma aparece justo donde acaba: un 30 % en la orilla lejana. El
        /// cierre a 260 m es el compromiso con las referencias de orientación: el
        /// puente y la isla vecina (120–210 medidos desde el centro de la aldea)
        /// quedan entre un 39 % y un 78 % de destiñe — silueta y masa, no fachadas.
        /// Lo que se pierde por bajarlo: ver la superficie de la vecina desde la
        /// aldea, y el panorama clásico sale con más bruma que antes (~47 % en su
        /// centro frente al 36 %). Si eso molesta, el número a mover es el cierre,
        /// no el inicio.
        /// </remarks>
        public static void ApplyHaze()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.90f, 0.96f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 260f;
        }

        /// <summary>
        /// El relleno, y es el que decide de qué color se ve una pared a la sombra.
        /// </summary>
        /// <remarks>
        /// El rebote de abajo es cálido porque el prado devuelve verde y tierra, no
        /// azul: con el ecuador y el suelo en gris azulado, una fachada crema en
        /// sombra se veía gris marengo y el edificio parecía de hormigón.
        /// </remarks>
        public static void ApplyAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.69f, 0.79f);
            RenderSettings.ambientEquatorColor = new Color(0.63f, 0.61f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.44f, 0.43f, 0.36f);
        }
    }
}
