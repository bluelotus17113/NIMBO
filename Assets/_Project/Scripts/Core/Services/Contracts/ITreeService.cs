namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Lo que el Árbol Nimbo ofrece una vez al día.
    /// </summary>
    /// <remarks>
    /// Vive en Core y no en Nimbo.Island porque quien lo llama es la vista
    /// (<c>Nimbo.Art</c>), y Nimbo.Art no ve Nimbo.Island: es el motivo de existencia
    /// del resto de contratos de esta carpeta.
    ///
    /// El servicio estuvo entero, construido y registrado durante meses sin un solo
    /// llamador —un grep de <c>TryTalk</c> fuera de su fichero daba cero resultados—
    /// porque le faltaba justo esto: una puerta que la vista pudiera abrir.
    /// </remarks>
    public interface ITreeService
    {
        /// <summary>Queda por recoger lo de hoy.</summary>
        bool CanTalkToday { get; }

        /// <summary>
        /// Habla con el árbol. Falso si lo de hoy ya se recogió; el texto viene igual,
        /// porque un árbol que no dice nada se lee como un árbol roto.
        /// </summary>
        bool TryTalk(out string text);
    }
}
