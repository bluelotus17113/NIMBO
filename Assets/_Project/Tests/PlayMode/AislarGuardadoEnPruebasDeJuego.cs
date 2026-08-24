using System;
using System.IO;
using Nimbo.Core.Save;
using NUnit.Framework;
using UnityEngine;

// Sin espacio de nombres, para que cubra el ensamblado entero. Es gemelo del que
// hay en las pruebas de editor; son dos ensamblados distintos y NUnit los trata
// por separado, así que hacen falta los dos.

/// <summary>
/// Manda el guardado a una carpeta temporal mientras corren las pruebas de juego.
/// </summary>
/// <remarks>
/// Estas son las peligrosas de verdad: arrancan el juego entero, con su arranque,
/// su reloj y su autoguardado. Sin esto, una prueba de arranque que corra medio
/// minuto acaba escribiendo encima de la partida del jugador — no borrándola, que
/// se notaría, sino pisándola con una isla de tres habitantes recién sorteados.
/// </remarks>
[SetUpFixture]
public sealed class AislarGuardadoEnPruebasDeJuego
{
    private string _directory;

    [OneTimeSetUp]
    public void Redirigir()
    {
        _directory = Path.Combine(Path.GetTempPath(),
                                  "nimbo-juego-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_directory);
        SaveSystem.RedirectTo(_directory);

        Debug.Log($"Pruebas de juego: el guardado va a {_directory}, no a la carpeta del jugador.");
    }

    /// <summary>
    /// **No se restaura la carpeta de verdad, y es a propósito.**
    /// </summary>
    /// <remarks>
    /// Restaurarla es lo que rompía esto. <c>GameBootstrap</c> sobrevive entre escenas
    /// y guarda en <c>OnApplicationQuit</c>, que salta **después** de este
    /// <c>TearDown</c>: se devolvía la ruta a la carpeta del jugador y acto seguido el
    /// arranque escribía encima de su partida su isla de tres habitantes recién
    /// sorteados. La comprobación de antes y después del fichero lo enseñó a la
    /// primera; el registro no, porque el desvío sí se había anunciado.
    ///
    /// El proceso se muere en cuanto acaban las pruebas, así que dejar el desvío
    /// puesto no le estorba a nadie. Tampoco se borra la carpeta: si se borrara, ese
    /// guardado de despedida se quedaría sin sitio donde caer.
    /// </remarks>
    [OneTimeTearDown]
    public void Restaurar()
    {
        // Lo primero, echar al arranque. Sobrevive entre escenas y guarda en
        // OnApplicationQuit, que salta DESPUÉS de esto: si sigue vivo, lo último que
        // hace una tanda de pruebas es un guardado de despedida que nadie pidió. Esta
        // parte viene de la rama del rediseño y es buena.
        foreach (var bootstrap in
                 UnityEngine.Object.FindObjectsByType<Nimbo.Game.Bootstrap.GameBootstrap>(
                     FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(bootstrap.gameObject);

        // Lo que NO se hace, y aquí las dos ramas discrepaban: **no se devuelve la
        // carpeta del jugador ni se borra la temporal.** La otra versión restauraba y
        // limpiaba, apoyándose en que matar el arranque quita al único que escribiría
        // después. Puede que sea verdad hoy; el día que no lo sea, el error no avisa —
        // el fichero del jugador no desaparece, aparece con otra partida dentro—. Y en
        // esta casa ya se perdió una isla de treinta días este mes.
        //
        // Dejando el desvío puesto, cualquier escritura tardía que se nos haya escapado
        // cae en la carpeta temporal y no en la suya. El proceso se muere en cuanto
        // acaban las pruebas, así que no le estorba a nadie. Es la asimetría de siempre:
        // limpiar de más cuesta unos megas en /tmp, limpiar de menos cuesta la partida.
        Debug.Log($"Pruebas: el guardado se queda apuntando a {_directory}. " +
                  "La carpeta del jugador no se toca.");
    }
}
