using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.Audio
{
    /// <summary>
    /// Pone el sonido del juego: el fondo, los efectos y las voces.
    /// </summary>
    /// <remarks>
    /// Escucha los mismos eventos que la interfaz, así que no hace falta que nadie
    /// «llame al sonido» al hacer algo: si una moneda cambia de manos, se oye.
    ///
    /// Se limita a una voz a la vez a propósito. Con doce habitantes reaccionando,
    /// oírlos a todos hablando encima es ruido; oír a uno es carácter.
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _voiceVolume = 0.7f;

        private AudioSource _music;
        private AudioSource _sfx;
        private AudioSource _voice;

        private AudioClip _ambience;
        private AudioClip _currentVoice;

        private void Awake()
        {
            _music = GetComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.volume = _musicVolume;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = _sfxVolume;

            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.volume = _voiceVolume;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<CoinsChanged>(OnCoins);
            EventBus.Subscribe<IslanderLeveledUp>(OnLevelUp);
            EventBus.Subscribe<BuildingUnlocked>(OnUnlock);
            EventBus.Subscribe<RequestRaised>(OnRequestRaised);
            EventBus.Subscribe<RequestResolved>(OnRequestResolved);
            EventBus.Subscribe<EmotionShown>(OnEmotion);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<CoinsChanged>(OnCoins);
            EventBus.Unsubscribe<IslanderLeveledUp>(OnLevelUp);
            EventBus.Unsubscribe<BuildingUnlocked>(OnUnlock);
            EventBus.Unsubscribe<RequestRaised>(OnRequestRaised);
            EventBus.Unsubscribe<RequestResolved>(OnRequestResolved);
            EventBus.Unsubscribe<EmotionShown>(OnEmotion);
        }

        private void OnDestroy()
        {
            if (_ambience != null) Destroy(_ambience);
            if (_currentVoice != null) Destroy(_currentVoice);
            SoundBank.Clear();
        }

        private void OnGameLoaded(GameLoaded _)
        {
            _ambience = SoundBank.BuildAmbience(AmbienceSeed());
            _music.clip = _ambience;
            _music.Play();
        }

        /// <summary>
        /// La semilla del ambiente sale del primer habitante de la isla: cada partida
        /// suena distinta y la tuya suena siempre igual.
        /// </summary>
        private static uint AmbienceSeed()
        {
            const uint Fallback = 7u;

            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return Fallback;
            if (registry.Count == 0) return Fallback;

            // Rng es un struct con estado: hay que tenerlo en una variable para que
            // avanzar la secuencia signifique algo. Sobre un temporal, no.
            var rng = Rng.FromSeed(registry.All[0].Id);
            return (uint)rng.Range(1, 1000000);
        }

        private void OnCoins(CoinsChanged evt)
        {
            if (evt.Delta > 0) Play(Sfx.Coin);
        }

        private void OnLevelUp(IslanderLeveledUp _) => Play(Sfx.Levelup);
        private void OnUnlock(BuildingUnlocked _) => Play(Sfx.Unlock);

        /// <summary>Cuando alguien pide algo, se le oye pedirlo. Es su frase, con su voz.</summary>
        private void OnRequestRaised(RequestRaised evt)
        {
            SpeakLine(evt.Request.IslanderId, evt.Request.Line);
        }

        private void OnRequestResolved(RequestResolved evt) =>
            Play(evt.Satisfied ? Sfx.Happy : Sfx.Sad);

        private void OnEmotion(EmotionShown evt)
        {
            // Solo las emociones fuertes suenan: si cada cara sonara, la isla sería
            // un guirigay constante.
            if (evt.Emotion is Emotion.Ecstatic or Emotion.Love) Play(Sfx.Happy);
            else if (evt.Emotion == Emotion.Angry) Play(Sfx.Deny);
        }

        public void Play(Sfx sfx)
        {
            var clip = SoundBank.Get(sfx);
            if (clip != null) _sfx.PlayOneShot(clip, _sfxVolume);
        }

        /// <summary>Hace hablar a un habitante. Si ya había uno hablando, lo corta.</summary>
        public void SpeakLine(string islanderId, string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;
            if (!registry.TryGet(islanderId, out var islander)) return;

            if (_currentVoice != null)
            {
                _voice.Stop();
                Destroy(_currentVoice);
            }

            _currentVoice = VoiceSynth.Speak(islander.Voice, line, islanderId);
            _voice.clip = _currentVoice;
            _voice.Play();
        }

        public void SetMusicVolume(float volume) => _music.volume = _musicVolume = Mathf.Clamp01(volume);
        public void SetSfxVolume(float volume) => _sfxVolume = Mathf.Clamp01(volume);
        public void SetVoiceVolume(float volume) => _voice.volume = _voiceVolume = Mathf.Clamp01(volume);
    }
}
