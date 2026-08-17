using System;
using System.Collections.Generic;

namespace Nimbo.Data.Player
{
    /// <summary>
    /// Las cinco vías por las que sube el protagonista.
    /// </summary>
    /// <remarks>
    /// Cinco y no una barra, y esa es la decisión de diseño entera. Con una sola, una
    /// tarde de hachazos paga los desbloqueos sociales y al revés, y el juego se
    /// convierte en «haz lo que más experiencia dé», que es justo la optimización que
    /// este proyecto no quiere. Separadas, quien solo quiere convivir desbloquea lo
    /// suyo sin tocar una azada, y quien solo quiere granja no se queda atascado por no
    /// hablar con nadie.
    ///
    /// Con su número escrito: estos valores acaban en las partidas guardadas.
    /// </remarks>
    public enum SkillKind
    {
        Farming = 0,      // Cultivo
        Gathering = 1,    // Recolección
        Crafting = 2,     // Oficio
        Social = 3,       // Convivencia
        Village = 4,      // Aldea
    }

    /// <summary>Una vía: hasta dónde ha llegado y cuánto lleva del tramo actual.</summary>
    [Serializable]
    public struct SkillProgress
    {
        public int Level;

        /// <summary>Experiencia acumulada **dentro** del nivel actual, no en total.</summary>
        public float Xp;
    }

    /// <summary>
    /// Las cinco vías del protagonista, guardadas con el resto de la partida.
    /// </summary>
    /// <remarks>
    /// Una lista y no cinco campos: así añadir una sexta vía algún día no obliga a
    /// tocar el guardado. Se rellena sola al leerla, porque una partida vieja no la
    /// trae y quedarse sin vías al cargar dejaría al protagonista a nivel cero de todo.
    /// </remarks>
    [Serializable]
    public class SkillSet
    {
        public List<SkillProgress> Lines = new List<SkillProgress>();

        public const int MaxLevel = 10;

        /// <summary>Cuántas vías hay. Si algún día son seis, esto sigue valiendo.</summary>
        public static int Count => Enum.GetValues(typeof(SkillKind)).Length;

        /// <summary>La experiencia que hace falta para pasar de ese nivel al siguiente.</summary>
        /// <remarks>
        /// Sale de la curva del diseño, que da la experiencia **total** de cada nivel
        /// como <c>40·nivel²</c>: lo de aquí es la diferencia entre un escalón y el
        /// siguiente. De 1 a 2 cuestan 120 y de 9 a 10 cuestan 760, y llegar al tope
        /// son las cuatro mil de la tabla.
        ///
        /// El mismo coeficiente para las cinco vías: con una curva por vía habría que
        /// reequilibrarlas cada vez que se toca lo que paga una acción, y no se acuerda
        /// nadie.
        /// </remarks>
        public static float XpForLevel(int level) => 40f * (2 * level + 1);

        /// <summary>La experiencia total acumulada para llegar a ese nivel desde cero.</summary>
        public static float TotalXpForLevel(int level) => 40f * level * level - 40f;

        public SkillProgress this[SkillKind kind]
        {
            get { Fill(); return Lines[(int)kind]; }
            set { Fill(); Lines[(int)kind] = value; }
        }

        public int LevelOf(SkillKind kind) => this[kind].Level;

        /// <summary>
        /// El nivel de aldeano: la media de las cinco, hacia abajo.
        /// </summary>
        /// <remarks>
        /// No se gana: es un resumen. Sale en el HUD porque hace falta un número que
        /// diga «voy por aquí» sin abrir nada, pero no desbloquea nada por su cuenta —
        /// si lo hiciera, volveríamos a tener una barra única con cinco disfraces.
        /// </remarks>
        public int VillagerLevel
        {
            get
            {
                Fill();
                int total = 0;
                for (int i = 0; i < Lines.Count; i++) total += Lines[i].Level;
                return total / Lines.Count;
            }
        }

        /// <summary>Nivel 1 en todo lo que falte. Una partida vieja entra por aquí.</summary>
        private void Fill()
        {
            while (Lines.Count < Count)
                Lines.Add(new SkillProgress { Level = 1, Xp = 0f });
        }
    }
}
