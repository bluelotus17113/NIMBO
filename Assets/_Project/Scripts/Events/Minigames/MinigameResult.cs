namespace Nimbo.Events.Minigames
{
    /// <summary>
    /// Lo que devuelve un minijuego al terminar. Son solo números: quien lo llama
    /// decide si los aplica a la partida o los ignora.
    /// </summary>
    public readonly struct MinigameResult
    {
        public readonly int Points;
        public readonly int Successes;
        public readonly int Failures;
        public readonly int Coins;
        public readonly int Experience;
        public readonly bool IsVictory;
        public readonly int Difficulty;

        public MinigameResult(int points, int successes, int failures,
                              int coins, int experience, bool isVictory, int difficulty)
        {
            Points = points;
            Successes = successes;
            Failures = failures;
            Coins = coins;
            Experience = experience;
            IsVictory = isVictory;
            Difficulty = difficulty;
        }
    }
}
