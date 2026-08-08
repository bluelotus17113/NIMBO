using Nimbo.Data.Islanders;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Necesidades, ánimo y progresión. Es la única forma de mover esos números:
    /// nadie escribe <c>islander.Needs.Hunger = 100</c> desde fuera de la simulación.
    /// </summary>
    /// <remarks>
    /// Se hace así porque cada cambio de necesidad puede cruzar un umbral, y cruzar
    /// un umbral tiene que publicar <c>NeedBandChanged</c>. Si el módulo de economía
    /// escribiera el número a mano, la cara del habitante no se enteraría.
    /// </remarks>
    public interface ISimulationService
    {
        /// <summary>Suma (o resta, si es negativo) a una necesidad, con avisos y recortes.</summary>
        void ApplyNeed(string islanderId, NeedKind need, float delta);

        void SetNeed(string islanderId, NeedKind need, float value);

        /// <summary>Mueve la felicidad de fondo. El rango útil de un solo suceso es ±15.</summary>
        void ApplyHappiness(string islanderId, float delta);

        /// <summary>Le pone esa cara durante unos segundos y luego vuelve a la suya.</summary>
        void ShowEmotion(string islanderId, Emotion emotion, float seconds = 4f);

        void GrantExperience(string islanderId, float amount);

        /// <summary>Congela el paso del tiempo para todos: menús modales, cinemáticas.</summary>
        void SetSimulationPaused(bool paused);
    }
}
