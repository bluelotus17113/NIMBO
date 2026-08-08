using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Core.Services
{
    /// <summary>
    /// Dónde encuentra un módulo a los demás. Se registran implementaciones contra
    /// interfaces de <c>Core/Services/Contracts</c>, nunca contra clases concretas.
    /// </summary>
    /// <remarks>
    /// Esto sustituye a los singletons. La diferencia práctica es que un test puede
    /// registrar un doble y que nadie escribe <c>Housing.Instance</c> desde Social,
    /// que es exactamente el acoplamiento que rompería el trabajo en paralelo.
    /// </remarks>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>(32);

        public static void Register<T>(T implementation) where T : class
        {
            if (implementation == null)
            {
                Debug.LogError($"ServiceRegistry: intento de registrar null para {typeof(T).Name}");
                return;
            }

            if (Services.ContainsKey(typeof(T)))
                Debug.LogWarning($"ServiceRegistry: {typeof(T).Name} ya estaba registrado, se reemplaza");

            Services[typeof(T)] = implementation;
        }

        public static void Unregister<T>() where T : class => Services.Remove(typeof(T));

        /// <summary>
        /// Devuelve el servicio, o null con un error en consola si nadie lo registró.
        /// Falla ruidosamente a propósito: un servicio que falta es un fallo de
        /// cableado en el arranque, no algo que un módulo deba apañar en silencio.
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service)) return (T)service;
            Debug.LogError($"ServiceRegistry: nadie ha registrado {typeof(T).Name}");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var raw)) { service = (T)raw; return true; }
            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class => Services.ContainsKey(typeof(T));

        public static void Clear() => Services.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}
