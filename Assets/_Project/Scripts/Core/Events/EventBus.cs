using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Core.Events
{
    /// <summary>
    /// El único sitio por el que los módulos se enteran de lo que pasa en los demás.
    /// Tipado por evento, sin cadenas de texto y sin reflexión en tiempo de ejecución.
    /// </summary>
    /// <remarks>
    /// Es estático a propósito: un bus por escena obliga a inyectarlo en todas partes
    /// y en un juego de una sola partida a la vez no aporta nada. A cambio hay que
    /// limpiarlo al entrar en modo juego, que es lo que hace <see cref="ResetOnPlay"/>.
    /// </remarks>
    public static class EventBus
    {
        private interface IChannel { void Clear(); }

        private sealed class Channel<T> : IChannel where T : struct
        {
            public readonly List<Action<T>> Handlers = new List<Action<T>>();

            /// <summary>
            /// Copia usada al publicar. Sin ella, un manejador que se desuscribe a sí
            /// mismo (cosa normal: "avísame una vez") corrompe la iteración.
            /// </summary>
            private Action<T>[] _snapshot = Array.Empty<Action<T>>();
            private bool _dirty = true;

            public void MarkDirty() => _dirty = true;

            public Action<T>[] Snapshot()
            {
                if (_dirty)
                {
                    _snapshot = Handlers.ToArray();
                    _dirty = false;
                }
                return _snapshot;
            }

            public void Clear()
            {
                Handlers.Clear();
                _snapshot = Array.Empty<Action<T>>();
                _dirty = false;
            }
        }

        private static readonly Dictionary<Type, IChannel> Channels = new Dictionary<Type, IChannel>(64);

        private static Channel<T> ChannelFor<T>() where T : struct
        {
            if (Channels.TryGetValue(typeof(T), out var existing)) return (Channel<T>)existing;
            var created = new Channel<T>();
            Channels[typeof(T)] = created;
            return created;
        }

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var channel = ChannelFor<T>();
            channel.Handlers.Add(handler);
            channel.MarkDirty();
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!Channels.TryGetValue(typeof(T), out var raw)) return;
            var channel = (Channel<T>)raw;
            if (channel.Handlers.Remove(handler)) channel.MarkDirty();
        }

        /// <summary>
        /// Avisa a todos los suscritos. Si uno peta, se registra y se sigue con los
        /// demás: un módulo roto no puede llevarse por delante a los otros doce.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
        {
            if (!Channels.TryGetValue(typeof(T), out var raw)) return;

            var handlers = ((Channel<T>)raw).Snapshot();
            for (int i = 0; i < handlers.Length; i++)
            {
                try { handlers[i](evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public static int HandlerCount<T>() where T : struct =>
            Channels.TryGetValue(typeof(T), out var raw) ? ((Channel<T>)raw).Handlers.Count : 0;

        public static void Clear()
        {
            foreach (var channel in Channels.Values) channel.Clear();
            Channels.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}
