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

    [OneTimeTearDown]
    public void Restaurar()
    {
        // El arranque se va antes de devolver la carpeta buena. Su OnApplicationQuit
        // guarda al cerrar, y eso ocurre DESPUÉS de esto: con la carpeta ya restaurada,
        // lo último que hacía una tanda de pruebas era escribir su isla de mentira
        // encima de la del jugador. Redirigir el guardado no bastaba, y no se veía
        // venir porque el fichero no desaparece: aparece con otra partida dentro.
        foreach (var bootstrap in
                 UnityEngine.Object.FindObjectsByType<Nimbo.Game.Bootstrap.GameBootstrap>(
                     FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(bootstrap.gameObject);

        SaveSystem.UseDefaultDirectory();

        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (Exception e) { Debug.LogWarning($"Pruebas: no se pudo limpiar {_directory}: {e.Message}"); }
    }
}
