namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// Qué está haciendo un habitante ahora mismo. Se guarda con la partida porque
    /// alguien que se durmió a las tres de la mañana tiene que seguir dormido al
    /// volver a abrir el juego.
    /// </summary>
    public enum IslanderActivity
    {
        Idle = 0,
        Walking = 1,
        Sleeping = 2,
        Eating = 3,
        Bathing = 4,
        Socializing = 5,
        Playing = 6,
        Working = 7,
        Shopping = 8,
        Moping = 9,     // enfurruñado: ni se mueve ni habla con nadie
    }
}
