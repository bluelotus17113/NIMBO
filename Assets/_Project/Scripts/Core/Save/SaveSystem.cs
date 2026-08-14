using System;
using System.IO;
using Newtonsoft.Json;
using Nimbo.Core.Events;
using Nimbo.Data.Save;
using UnityEngine;

namespace Nimbo.Core.Save
{
    /// <summary>
    /// Lee y escribe la partida. Nada más: no decide cuándo guardar, solo cómo.
    /// </summary>
    /// <remarks>
    /// La escritura es atómica — fichero temporal y luego <c>File.Replace</c> — y viene
    /// de un incidente real en otro proyecto: un guardado a medias con el fichero bueno
    /// abierto en escritura se llevó por delante una partida entera. Con esto, si el
    /// juego se cierra a mitad de guardado, lo peor que pasa es que se pierda la última
    /// partida guardada, nunca la anterior.
    /// </remarks>
    public static class SaveSystem
    {
        public const string SaveFileName = "partida.json";
        public const string BackupFileName = "partida.bak.json";
        public const string ArchiveFileName = "partida.anterior.json";

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        private static string _directoryOverride;

        /// <summary>
        /// Dónde se apunta el desvío para que sobreviva a una recarga de dominio.
        /// </summary>
        /// <remarks>
        /// Un campo estático no basta, y esto costó tres partidas. Entrar en modo juego
        /// recarga el dominio de scripts y **pone a cero todos los estáticos**: el
        /// aislamiento de las pruebas de juego desviaba el guardado en su
        /// <c>OneTimeSetUp</c> —lo dejaba escrito en el registro, «el guardado va a
        /// /tmp/...»— y la recarga se lo llevaba por delante antes de la primera
        /// prueba. Desde ahí, cada arranque de prueba guardaba encima de la partida del
        /// jugador con su isla de tres habitantes recién sorteados.
        ///
        /// Una variable de entorno del proceso sí sobrevive a la recarga. Y de regalo
        /// deja lanzar el juego compilado contra una carpeta de usar y tirar:
        /// <c>NIMBO_SAVE_DIR=/tmp/prueba ./IslaNimbo</c>.
        /// </remarks>
        private const string DirectoryVariable = "NIMBO_SAVE_DIR";

        /// <summary>
        /// Dónde vive la partida. Normalmente la carpeta de datos del jugador.
        /// </summary>
        public static string SaveDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(_directoryOverride)) return _directoryOverride;

                var fromEnvironment = Environment.GetEnvironmentVariable(DirectoryVariable);
                if (!string.IsNullOrEmpty(fromEnvironment)) return fromEnvironment;

                return Application.persistentDataPath;
            }
        }

        /// <summary>
        /// Manda el guardado a otra carpeta. Es SOLO para las pruebas y existe por un
        /// motivo caro de aprender: en el editor, <c>persistentDataPath</c> es la misma
        /// carpeta que usa el juego compilado, así que una prueba que borraba «su»
        /// fichero de guardado estaba borrando la partida de verdad de quien estuviera
        /// jugando. Pasó, y se perdió una isla con tres habitantes dentro.
        /// </summary>
        /// <remarks>
        /// No basta con acordarse de llamarla: la que la llama de verdad es
        /// <c>AislarGuardadoEnPruebas</c>, un <c>SetUpFixture</c> que corre antes que
        /// cualquier prueba del ensamblado, para que también proteja a las que escriba
        /// alguien que no haya leído esto.
        /// </remarks>
        public static void RedirectTo(string directory)
        {
            _directoryOverride = directory;

            // Y en la variable de entorno, que es la que aguanta la recarga de dominio
            // al entrar en modo juego. Sin esto el desvío dura hasta la primera prueba.
            Environment.SetEnvironmentVariable(DirectoryVariable, directory);
        }

        /// <summary>Vuelve a la carpeta del jugador.</summary>
        public static void UseDefaultDirectory()
        {
            _directoryOverride = null;
            Environment.SetEnvironmentVariable(DirectoryVariable, null);
        }

        public static string SavePath => Path.Combine(SaveDirectory, SaveFileName);
        public static string BackupPath => Path.Combine(SaveDirectory, BackupFileName);

        public static string ArchivePath => Path.Combine(SaveDirectory, ArchiveFileName);

        public static bool SaveExists => File.Exists(SavePath);

        /// <summary>
        /// Aparta la partida actual antes de empezar otra. Devuelve false si no había
        /// nada que apartar o si no se pudo.
        /// </summary>
        /// <remarks>
        /// El respaldo de <see cref="Write"/> no vale para esto: es el de la escritura
        /// anterior, así que en cuanto la isla nueva autoguarde una sola vez, el
        /// respaldo pasa a ser de la isla nueva y la vieja ya no está en ningún sitio.
        /// Esta copia es aparte y nadie la sobrescribe salvo otro «empezar de nuevo».
        ///
        /// El juego no la lee nunca. Está para que quien haya borrado una isla de
        /// treinta días por pulsar mal pueda recuperarla renombrando un fichero.
        /// </remarks>
        public static bool Archive()
        {
            if (!File.Exists(SavePath)) return false;

            try
            {
                File.Copy(SavePath, ArchivePath, overwrite: true);
                Debug.Log($"Partida anterior guardada en {ArchivePath}");
                return true;
            }
            catch (Exception e)
            {
                // No se aborta la partida nueva por esto: es una red de seguridad,
                // no un requisito. Pero se dice, para que no parezca que hay copia.
                Debug.LogWarning($"SaveSystem: no se pudo apartar la partida anterior — {e.Message}");
                return false;
            }
        }

        /// <summary>Escribe la partida. Devuelve false y deja el fichero anterior intacto si falla.</summary>
        public static bool Write(SaveGame save)
        {
            if (save == null)
            {
                Debug.LogError("SaveSystem: no se guarda una partida nula");
                return false;
            }

            save.SavedUtc = DateTime.UtcNow.ToString("O");
            save.Version = SaveGame.CurrentVersion;

            string temp = SavePath + ".tmp";
            try
            {
                Directory.CreateDirectory(SaveDirectory);
                File.WriteAllText(temp, JsonConvert.SerializeObject(save, Settings));

                if (File.Exists(SavePath))
                {
                    // Replace deja la copia anterior como respaldo en el mismo paso.
                    File.Replace(temp, SavePath, BackupPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temp, SavePath);
                }

                EventBus.Publish(new GameSaved(SavePath));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: no se pudo guardar en {SavePath}");
                Debug.LogException(e);
                TryDelete(temp);
                return false;
            }
        }

        /// <summary>
        /// Carga la partida. Si el fichero está corrupto prueba con el respaldo antes
        /// de rendirse; si tampoco, devuelve null y no borra nada.
        /// </summary>
        public static SaveGame Read()
        {
            var loaded = TryRead(SavePath);
            if (loaded != null) return loaded;

            if (File.Exists(BackupPath))
            {
                Debug.LogWarning("SaveSystem: la partida no se pudo leer, intentando con el respaldo");
                loaded = TryRead(BackupPath);
                if (loaded != null) return loaded;
            }

            return null;
        }

        private static SaveGame TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var save = JsonConvert.DeserializeObject<SaveGame>(File.ReadAllText(path), Settings);
                if (save == null) return null;

                if (save.Version > SaveGame.CurrentVersion)
                {
                    Debug.LogError($"SaveSystem: {path} es de la versión {save.Version} y este juego " +
                                   $"entiende hasta la {SaveGame.CurrentVersion}. No se abre.");
                    return null;
                }

                foreach (var islander in save.Islanders) islander.Relationships?.RebuildIndex();
                return save;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: {path} no se pudo leer");
                Debug.LogException(e);
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) { Debug.LogWarning($"SaveSystem: no se pudo borrar {path}: {e.Message}"); }
        }
    }
}
