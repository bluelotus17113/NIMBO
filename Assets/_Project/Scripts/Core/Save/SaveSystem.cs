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

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public static string SaveDirectory => Application.persistentDataPath;
        public static string SavePath => Path.Combine(SaveDirectory, SaveFileName);
        public static string BackupPath => Path.Combine(SaveDirectory, BackupFileName);

        public static bool SaveExists => File.Exists(SavePath);

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
