using System;

namespace Nimbo.Data.Save
{
    /// <summary>
    /// Lo que la partida recuerda de un logro: cuánto lleva y si ya está.
    /// </summary>
    /// <remarks>
    /// Solo se guardan los logros que se han tocado alguna vez. Guardar los cincuenta
    /// desde el primer día llenaría el fichero de ceros y, peor, obligaría a migrar
    /// la partida cada vez que se añada uno nuevo: al no estar, sale a cero solo.
    ///
    /// Dato muerto, como todo lo que va dentro del guardado. El campo
    /// <see cref="Unlocked"/> es redundante con «Current >= Goal», y se guarda a
    /// propósito: el objetivo vive en el catálogo y el catálogo se retoca. Si un
    /// logro pasara de pedir 20 a pedir 30, quien ya lo tenía lo perdería.
    /// </remarks>
    [Serializable]
    public class AchievementRecord
    {
        public string AchievementId = "";
        public int Current;
        public bool Unlocked;

        /// <summary>Día de juego en que se consiguió. 0 si todavía no.</summary>
        public int UnlockedOnDay;
    }
}
