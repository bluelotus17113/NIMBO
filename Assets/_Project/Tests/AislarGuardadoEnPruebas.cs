using System;
using System.IO;
using Nimbo.Core.Save;
using NUnit.Framework;
using UnityEngine;

// A propósito SIN espacio de nombres: un SetUpFixture fuera de todo espacio de
// nombres se aplica al ensamblado entero, así que cubre también las pruebas que
// escriba mañana alguien que no se haya leído esto.

/// <summary>
/// Manda el guardado a una carpeta temporal mientras corren las pruebas.
/// </summary>
/// <remarks>
/// Esto no es una comodidad, es una reparación. En el editor,
/// <c>Application.persistentDataPath</c> apunta a la MISMA carpeta que usa el juego
/// compilado. Varias pruebas borraban «su» fichero de guardado en el TearDown para
/// dejar limpio, y lo que borraban era la partida de verdad: pasar la batería
/// entera fulminaba la isla del jugador sin decir nada. Se perdió una con tres
/// habitantes y catorce horas de juego dentro.
///
/// Acordarse de aislar en cada prueba no vale como solución, porque el día que a
/// alguien se le olvide vuelve a pasar y encima en silencio. Esto corre una vez
/// antes de la primera prueba del ensamblado y lo deja imposible.
/// </remarks>
[SetUpFixture]
public sealed class AislarGuardadoEnPruebas
{
    private string _directory;

    [OneTimeSetUp]
    public void Redirigir()
    {
        _directory = Path.Combine(Path.GetTempPath(),
                                  "nimbo-pruebas-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_directory);
        SaveSystem.RedirectTo(_directory);

        Debug.Log($"Pruebas: el guardado va a {_directory}, no a la carpeta del jugador.");
    }

    [OneTimeTearDown]
    public void Restaurar()
    {
        SaveSystem.UseDefaultDirectory();

        // Si el borrado falla da igual: es una carpeta temporal y el sistema la
        // barre solo. Lo que no puede fallar es la línea de arriba.
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (Exception e) { Debug.LogWarning($"Pruebas: no se pudo limpiar {_directory}: {e.Message}"); }
    }
}
