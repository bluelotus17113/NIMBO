using Nimbo.Core.Services.Contracts;

namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Contrato común para los tres minijuegos.
    /// </summary>
    /// <remarks>
    /// <c>Start</c> siembra y prepara. <c>Step</c> procesa una entrada del jugador
    /// (el significado del entero depende de cada juego). <c>Finish</c> cierra y
    /// devuelve el resultado. <c>IsOver</c> es true desde que se cumple la condición
    /// de victoria o derrota hasta que se llama a <c>Finish</c>.
    /// </remarks>
    public interface IMinigame
    {
        void Start(uint seed, int difficulty);
        void Step(int input);
        MinigameResult Finish();
        bool IsOver { get; }
    }
}
