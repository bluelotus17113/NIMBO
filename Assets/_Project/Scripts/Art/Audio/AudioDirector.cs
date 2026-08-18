using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Audio;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Settings;
using Nimbo.Core.Time;
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
    ///
    /// La música tiene tres humores (<see cref="MusicMood"/>) y se pasa de uno a otro
    /// **cruzando**, nunca cortando. Son dos fuentes de música justo por eso: con una
    /// sola, cambiar de clip es un silencio de un frame, y un silencio en el fondo se
    /// oye como un fallo aunque dure nada.
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioDirector : MonoBehaviour
    {
        /// <summary>Lo que tarda en pasarse de un humor a otro.</summary>
        /// <remarks>
        /// Dos segundos y medio: bastante para que no se note el corte, poco para que
        /// al empezar una fiesta la música llegue con ella y no un rato después.
        /// </remarks>
        public const float CrossfadeSeconds = 2.5f;

        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _voiceVolume = 0.7f;

        private AudioSource _music;
        private AudioSource _musicFading;
        private AudioSource _sfx;
        private AudioSource _voice;

        private readonly Dictionary<MusicMood, AudioClip> _ambiences = new(3);
        private AudioClip _currentVoice;

        private Coroutine _crossfade;
        private bool _eventRunning;
        private int _hour = 12;

        /// <summary>Qué está sonando de fondo ahora mismo.</summary>
        public MusicMood CurrentMood { get; private set; } = MusicMood.Calm;

        private void Awake()
        {
            // Lo elegido en los ajustes manda sobre lo puesto en el inspector: el
            // inspector es el valor de fábrica, y el jugador ya dijo lo suyo.
            _musicVolume = AudioPrefs.Get(AudioChannel.Music);
            _sfxVolume = AudioPrefs.Get(AudioChannel.Sfx);
            _voiceVolume = AudioPrefs.Get(AudioChannel.Voice);

            _music = GetComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.volume = _musicVolume;

            _musicFading = gameObject.AddComponent<AudioSource>();
            _musicFading.loop = true;
            _musicFading.playOnAwake = false;
            _musicFading.volume = 0f;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = _sfxVolume;

            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.volume = _voiceVolume;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<VolumeChanged>(OnVolumeChanged);
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<CoinsChanged>(OnCoins);
            EventBus.Subscribe<IslanderLeveledUp>(OnLevelUp);
            EventBus.Subscribe<BuildingUnlocked>(OnUnlock);
            EventBus.Subscribe<RequestRaised>(OnRequestRaised);
            EventBus.Subscribe<RequestResolved>(OnRequestResolved);
            EventBus.Subscribe<EmotionShown>(OnEmotion);

            // Lo que mueve la música. La hora es lo que se nota todos los días; la
            // fiesta es lo que se nota de vez en cuando.
            EventBus.Subscribe<HourPassed>(OnHourPassed);
            EventBus.Subscribe<VillageEventStarted>(OnVillageEventStarted);
            EventBus.Subscribe<VillageEventEnded>(OnVillageEventEnded);

            // Los verbos del jugador. Faltaban todos: el sonido escuchaba lo que hacía
            // la isla y no lo que hacían tus manos, así que talar, cosechar, craftear y
            // dormir no sonaban.
            EventBus.Subscribe<NodeHit>(OnNodeHit);
            EventBus.Subscribe<NodeGathered>(OnNodeGathered);
            EventBus.Subscribe<CropHarvested>(OnCropHarvested);
            EventBus.Subscribe<ItemCrafted>(OnItemCrafted);
            EventBus.Subscribe<Slept>(OnSlept);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<VolumeChanged>(OnVolumeChanged);
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<CoinsChanged>(OnCoins);
            EventBus.Unsubscribe<IslanderLeveledUp>(OnLevelUp);
            EventBus.Unsubscribe<BuildingUnlocked>(OnUnlock);
            EventBus.Unsubscribe<RequestRaised>(OnRequestRaised);
            EventBus.Unsubscribe<RequestResolved>(OnRequestResolved);
            EventBus.Unsubscribe<EmotionShown>(OnEmotion);

            EventBus.Unsubscribe<HourPassed>(OnHourPassed);
            EventBus.Unsubscribe<VillageEventStarted>(OnVillageEventStarted);
            EventBus.Unsubscribe<VillageEventEnded>(OnVillageEventEnded);

            EventBus.Unsubscribe<NodeHit>(OnNodeHit);
            EventBus.Unsubscribe<NodeGathered>(OnNodeGathered);
            EventBus.Unsubscribe<CropHarvested>(OnCropHarvested);
            EventBus.Unsubscribe<ItemCrafted>(OnItemCrafted);
            EventBus.Unsubscribe<Slept>(OnSlept);
        }

        private void OnDestroy()
        {
            foreach (var clip in _ambiences.Values)
                if (clip != null) Destroy(clip);
            _ambiences.Clear();

            if (_currentVoice != null) Destroy(_currentVoice);
            SoundBank.Clear();
        }

        /// <summary>
        /// El jugador ha movido un deslizador. Se aplica en el acto y sonando: bajar
        /// la música y no oír el cambio hasta la siguiente pista es lo que hace que
        /// uno la baje de más.
        /// </summary>
        private void OnVolumeChanged(VolumeChanged evt)
        {
            switch (evt.Channel)
            {
                case AudioChannel.Music: SetMusicVolume(evt.Value); break;
                case AudioChannel.Sfx:   SetSfxVolume(evt.Value); break;
                case AudioChannel.Voice: SetVoiceVolume(evt.Value); break;
            }
        }

        /// <summary>
        /// Al cargar se construyen los tres fondos de una vez, no el que toca ahora.
        /// </summary>
        /// <remarks>
        /// Sintetizar treinta y dos segundos son unos setecientos mil senos, y eso es
        /// un frame perdido. Hacerlo aquí no se ve —ya se está cargando la partida—;
        /// hacerlo al empezar la fiesta se vería justo cuando el jugador está mirando.
        ///
        /// Y se empieza en el humor que toque por la hora, no en calma: cargar una
        /// partida guardada a las once de la noche y oír el fondo de mediodía es la
        /// misma incoherencia que había antes, solo que en el primer minuto.
        /// </remarks>
        private void OnGameLoaded(GameLoaded _)
        {
            // Volver al menú y cargar otra partida pasa por aquí otra vez, y la semilla
            // sale del primer habitante: son fondos distintos. Sin tirar los viejos se
            // quedarían tres clips de tres megas colgando por cada carga.
            if (_crossfade != null) { StopCoroutine(_crossfade); _crossfade = null; }
            foreach (var stale in _ambiences.Values)
                if (stale != null) Destroy(stale);
            _ambiences.Clear();

            _musicFading.Stop();
            _musicFading.clip = null;
            _musicFading.volume = 0f;

            uint seed = AmbienceSeed();
            foreach (MusicMood mood in System.Enum.GetValues(typeof(MusicMood)))
                _ambiences[mood] = SoundBank.BuildAmbience(seed, mood);

            _hour = ServiceRegistry.TryGet<GameClock>(out var clock) ? clock.Hour : _hour;
            _eventRunning = ServiceRegistry.TryGet<IVillageEvents>(out var events)
                         && !string.IsNullOrEmpty(events.ActiveEventId);

            CurrentMood = MusicMoods.For(_hour, _eventRunning);
            _music.clip = _ambiences[CurrentMood];
            _music.volume = _musicVolume;
            _music.Play();
        }

        private void OnHourPassed(HourPassed evt)
        {
            _hour = evt.Hour;
            RefreshMood();
        }

        private void OnVillageEventStarted(VillageEventStarted evt)
        {
            _eventRunning = true;
            RefreshMood();
        }

        private void OnVillageEventEnded(VillageEventEnded evt)
        {
            _eventRunning = false;
            RefreshMood();
        }

        private void RefreshMood()
        {
            var wanted = MusicMoods.For(_hour, _eventRunning);
            if (wanted == CurrentMood) return;

            CurrentMood = wanted;

            // Si todavía no hay fondos —el aviso llegó antes de cargar la partida—, no
            // hay nada que cruzar: el humor queda apuntado y lo recoge OnGameLoaded.
            if (!_ambiences.TryGetValue(wanted, out var clip) || clip == null) return;

            if (_crossfade != null) StopCoroutine(_crossfade);
            _crossfade = StartCoroutine(CrossfadeTo(clip));
        }

        /// <summary>
        /// Cambia el fondo cruzando las dos fuentes.
        /// </summary>
        /// <remarks>
        /// Las dos rampas son lineales, así que el volumen total no se hunde por el
        /// medio. Con dos desvanecidos en curva —los que «suenan mejor» de uno en
        /// uno— el centro baja y parece que la música se va un segundo.
        ///
        /// El intercambio de fuentes se hace **al empezar** y no al terminar, y eso
        /// importa cuando un cruce interrumpe a otro: si una fiesta empieza justo en
        /// el amanecer, la que se estaba desvaneciendo pasa a ser la saliente desde el
        /// volumen que tuviera. Haciéndolo al final, la saliente volvería de golpe al
        /// volumen entero, que es un salto que se oye.
        /// </remarks>
        private IEnumerator CrossfadeTo(AudioClip clip)
        {
            (_music, _musicFading) = (_musicFading, _music);

            _music.clip = clip;
            _music.volume = 0f;
            _music.time = 0f;
            _music.Play();

            float desde = _musicFading.volume;

            for (float t = 0f; t < CrossfadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / CrossfadeSeconds);
                _music.volume = _musicVolume * k;
                _musicFading.volume = desde * (1f - k);
                yield return null;
            }

            _music.volume = _musicVolume;
            _musicFading.Stop();
            _musicFading.volume = 0f;
            _crossfade = null;
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

        /// <summary>
        /// El golpe suena a lo que estás golpeando, no a «acción realizada».
        /// </summary>
        /// <remarks>
        /// El material sale del catálogo, que es quien sabe si eso es un árbol o una
        /// roca. Podría haber ido en el propio aviso, pero entonces el que decide
        /// tendría que saber a qué suena cada cosa, y eso es asunto del sonido.
        /// </remarks>
        private void OnNodeHit(NodeHit evt) => Play(ImpactFor(evt.NodeId));

        private void OnNodeGathered(NodeGathered _) => Play(Sfx.Harvest);
        private void OnCropHarvested(CropHarvested _) => Play(Sfx.Harvest);
        private void OnItemCrafted(ItemCrafted _) => Play(Sfx.Craft);
        private void OnSlept(Slept _) => Play(Sfx.Sleep);

        private static Sfx ImpactFor(string nodeId)
        {
            if (!ServiceRegistry.TryGet<IGatheringService>(out var gathering)) return Sfx.Pick;
            if (!gathering.TryGetDefinition(nodeId, out var definition)) return Sfx.Pick;

            return definition.Kind switch
            {
                NodeKind.Tree => Sfx.Chop,
                NodeKind.Rock => Sfx.Mine,
                _ => Sfx.Pick,
            };
        }

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

        /// <summary>
        /// El deslizador de música. En mitad de un cruce no toca las fuentes: las dos
        /// rampas ya se calculan sobre <c>_musicVolume</c> y lo recogen al frame
        /// siguiente. Escribirlas aquí además pondría la saliente al volumen entero.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_crossfade == null) _music.volume = _musicVolume;
        }
        public void SetSfxVolume(float volume) => _sfxVolume = Mathf.Clamp01(volume);
        public void SetVoiceVolume(float volume) => _voice.volume = _voiceVolume = Mathf.Clamp01(volume);
    }
}
