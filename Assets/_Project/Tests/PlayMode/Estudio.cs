using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// El plató vacío donde posan las herramientas de captura.
    /// </summary>
    /// <remarks>
    /// Crear una escena no descarga las que ya estaban: quedan cargadas, y lo que
    /// hay dentro sigue saliendo en la foto. Lanzadas de una pasada, el retrato de
    /// los muñecos salió con la pared de un edificio de fondo, porque antes había
    /// pasado por aquí la herramienta que carga la isla entera.
    ///
    /// No se descargan las otras escenas —el corredor de pruebas vive en una de
    /// ellas— y tampoco se apagan sus objetos: apagar las raíces de la isla se
    /// llevó por delante al AudioListener con el sonido sonando, y el editor
    /// murió con un fallo de FMOD. Se apaga solo lo que sale en la foto, que son
    /// las mallas y las luces; lo demás sigue vivo y no se entera.
    ///
    /// Y se buscan por objeto, no recorriendo escenas: la isla cuelga del objeto
    /// que sobrevive a los cambios de escena, y ese no está en ninguna de las que
    /// enumera SceneManager. Recorriendo escenas no se apagaba nada y el tronco
    /// del árbol seguía saliendo detrás de los muñecos.
    /// </remarks>
    public static class Estudio
    {
        public static Scene Nuevo(string nombre)
        {
            var escena = SceneManager.CreateScene(nombre);
            SceneManager.SetActiveScene(escena);

            foreach (var malla in Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (malla.gameObject.scene != escena) malla.enabled = false;

            foreach (var luz in Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (luz.gameObject.scene != escena) luz.enabled = false;

            return escena;
        }

        /// <summary>El sol de la isla, sobre el plató.</summary>
        public static Light Sol()
        {
            return Art.World.IslandLighting.Apply(new GameObject("sol").AddComponent<Light>());
        }
    }
}
