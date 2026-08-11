using System;
using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;

namespace Nimbo.Simulation.Progression
{
    /// <summary>
    /// Cuenta los logros suscribiéndose a los eventos que ya existen. Nadie sabe que
    /// este servicio existe: él escucha y apunta, y el resto del juego sigue igual.
    /// </summary>
    /// <remarks>
    /// La regla de oro: <see cref="Advance"/> y <see cref="Record"/> solo devuelven
    /// cierto y publican <see cref="AchievementUnlocked"/> la primera vez que se
    /// alcanza el objetivo. Después devuelven falso siempre — sin esto, un logro que
    /// se desbloquea al llegar a 30 días pagaría cada día del resto de la partida.
    ///
    /// Solo se guardan los logros que se han tocado. Si un identificador no está en
    /// el catálogo se ignora en silencio, para que quitar un logro no rompa las
    /// partidas viejas.
    /// </remarks>
    public sealed class AchievementService : IAchievementService, IDisposable
    {
        readonly AchievementCatalog _catalog;
        readonly SaveGame _save;
        readonly GameClock _clock;

        // ── estado ──────────────────────────────────────────────────────────

        int _unlockedCount;
        bool _dirtyCount;

        // ── constructores ────────────────────────────────────────────────────

        public AchievementService(AchievementCatalog catalog, SaveGame save, GameClock clock)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            // contar los que ya venían desbloqueados de la partida
            for (int i = 0; i < _save.Achievements.Count; i++)
                if (_save.Achievements[i].Unlocked)
                    _unlockedCount++;

            Subscribe();
        }

        // ── IAchievementService ──────────────────────────────────────────────

        public IReadOnlyList<AchievementDefinition> Catalog => _catalog.Catalog;

        public bool TryGetDefinition(string achievementId, out AchievementDefinition definition)
            => _catalog.TryGetDefinition(achievementId, out definition);

        public AchievementProgress ProgressOf(string achievementId)
        {
            if (!_catalog.TryGetDefinition(achievementId, out var def))
                return new AchievementProgress(achievementId, 0, 1, false);

            var record = FindRecord(achievementId);
            if (record == null)
                return new AchievementProgress(achievementId, 0, def.Goal, false);

            return new AchievementProgress(achievementId, record.Current, def.Goal, record.Unlocked);
        }

        public int UnlockedCount
        {
            get
            {
                if (_dirtyCount) RecountUnlocked();
                return _unlockedCount;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Solo paga la primera vez que se alcanza el objetivo. Después de
        /// desbloqueado devuelve falso siempre — si no, sería dinero infinito.
        /// </remarks>
        public bool Advance(string achievementId, int amount = 1)
        {
            if (amount <= 0) return false;
            if (!_catalog.TryGetDefinition(achievementId, out var def)) return false;

            var record = GetOrCreateRecord(achievementId);
            if (record.Unlocked) return false; // ya se consiguió, no vuelve a pagar

            record.Current += amount;
            return MaybeUnlock(def, record);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Solo deja el valor si es mayor que el que había — es para máximos, no
        /// para sumas — y solo paga la primera vez que se alcanza el objetivo.
        /// </remarks>
        public bool Record(string achievementId, int value)
        {
            if (!_catalog.TryGetDefinition(achievementId, out var def)) return false;

            var record = GetOrCreateRecord(achievementId);
            if (record.Unlocked) return false; // ya se consiguió, no vuelve a pagar

            if (value <= record.Current) return false; // no es un nuevo máximo
            record.Current = value;
            return MaybeUnlock(def, record);
        }

        // ── lógica interna ───────────────────────────────────────────────────

        /// <summary>
        /// Comprueba si el contador ha llegado al objetivo y, si sí, lo marca
        /// desbloqueado, publica el evento y cuenta. Devuelve cierto solo si lo
        /// acaba de desbloquear.
        /// </summary>
        bool MaybeUnlock(AchievementDefinition def, AchievementRecord record)
        {
            if (record.Current < def.Goal) return false;

            record.Unlocked = true;
            record.UnlockedOnDay = _clock.Day;
            _unlockedCount++;
            _dirtyCount = false;

            EventBus.Publish(new AchievementUnlocked(def.AchievementId, def.Reward));
            return true;
        }

        /// <summary>
        /// Busca el registro en el guardado. Si no está y el id existe en el
        /// catálogo, lo crea y lo añade. Si el id no está en el catálogo,
        /// devuelve null — así los ids huérfanos de partidas viejas no crean
        /// registros.
        /// </summary>
        AchievementRecord GetOrCreateRecord(string achievementId)
        {
            var record = FindRecord(achievementId);
            if (record != null) return record;

            // solo crea registro si el id existe en el catálogo
            if (!_catalog.TryGetDefinition(achievementId, out _)) return null;

            record = new AchievementRecord { AchievementId = achievementId };
            _save.Achievements.Add(record);
            return record;
        }

        AchievementRecord FindRecord(string achievementId)
        {
            for (int i = 0; i < _save.Achievements.Count; i++)
                if (_save.Achievements[i].AchievementId == achievementId)
                    return _save.Achievements[i];
            return null;
        }

        void RecountUnlocked()
        {
            _unlockedCount = 0;
            for (int i = 0; i < _save.Achievements.Count; i++)
                if (_save.Achievements[i].Unlocked)
                    _unlockedCount++;
            _dirtyCount = false;
        }

        // ── suscripciones a eventos ──────────────────────────────────────────
        //
        // Cada método se suscribe a UN evento y desde dentro llama a Advance o
        // Record sobre los logros que le tocan. La gracia de este patrón es que
        // añadir un logro nuevo es tocar solo este fichero: el módulo que lanza
        // el evento no sabe que existen los logros.

        void Subscribe()
        {
            EventBus.Subscribe<DayPassed>(OnDayPassed);
            EventBus.Subscribe<IslanderLeveledUp>(OnIslanderLeveledUp);
            EventBus.Subscribe<IslanderCreated>(OnIslanderCreated);
            EventBus.Subscribe<IslanderMovedIn>(OnIslanderMovedIn);
            EventBus.Subscribe<IslanderLeft>(OnIslanderLeft);
            EventBus.Subscribe<FriendshipStageChanged>(OnFriendshipStageChanged);
            EventBus.Subscribe<RomanceStageChanged>(OnRomanceStageChanged);
            EventBus.Subscribe<BabyBorn>(OnBabyBorn);
            EventBus.Subscribe<RequestResolved>(OnRequestResolved);
            EventBus.Subscribe<RoomEdited>(OnRoomEdited);
            EventBus.Subscribe<ItemAcquired>(OnItemAcquired);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<ItemGifted>(OnItemGifted);
            EventBus.Subscribe<EmotionShown>(OnEmotionShown);
            EventBus.Subscribe<HappinessChanged>(OnHappinessChanged);
            EventBus.Subscribe<BuildingUnlocked>(OnBuildingUnlocked);
            EventBus.Subscribe<DecorPlaced>(OnDecorPlaced);
        }

        // ── manejadores ──────────────────────────────────────────────────────

        void OnDayPassed(DayPassed e)
        {
            Advance("logro_primer_amanecer");
            Advance("logro_semana_viva");
            Advance("logro_un_mes_en_la_isla");
            Advance("logro_centenario");
        }

        void OnIslanderLeveledUp(IslanderLeveledUp e)
        {
            Record("logro_nivel_cinco", e.NewLevel);
            Record("logro_nivel_diez", e.NewLevel);
        }

        void OnIslanderCreated(IslanderCreated e) => Advance("logro_caras_nuevas");

        void OnIslanderMovedIn(IslanderMovedIn e) => Advance("logro_isla_poblada");

        // ── social ───────────────────────────────────────────────────────────

        void OnFriendshipStageChanged(FriendshipStageChanged e)
        {
            Advance("logro_primer_amigo");
            Advance("logro_amistades");
            Advance("logro_tejedor_social");
        }

        void OnRomanceStageChanged(RomanceStageChanged e)
        {
            Advance("logro_primer_flechazo");
            Advance("logro_mariposas");
            Advance("logro_casanova");
        }

        void OnBabyBorn(BabyBorn e)
        {
            Advance("logro_primer_bebe");
            Advance("logro_guarderia");
        }

        void OnRequestResolved(RequestResolved e)
        {
            if (e.Satisfied) Advance("logro_cumplidor");
        }

        // ── hogar ────────────────────────────────────────────────────────────

        void OnRoomEdited(RoomEdited e)
        {
            Advance("logro_primer_toque");
            Advance("logro_decorador");
            Advance("logro_arquitecto");
        }

        void OnItemAcquired(ItemAcquired e)
        {
            Advance("logro_primer_objeto", e.Quantity);
            Advance("logro_coleccionista", e.Quantity);
            Advance("logro_acaparador", e.Quantity);
        }

        // ── dinero ───────────────────────────────────────────────────────────

        void OnCoinsChanged(CoinsChanged e)
        {
            if (e.Delta > 0)
            {
                Advance("logro_primer_ingreso");
                Advance("logro_jornalero");
            }
            if (e.Delta < 0)
            {
                Advance("logro_gastos");
                Advance("logro_derrochador");
            }
            Record("logro_ahorros", (int)Math.Min(e.Total, int.MaxValue));
            Record("logro_fortuna", (int)Math.Min(e.Total, int.MaxValue));
        }

        // ── juego ────────────────────────────────────────────────────────────

        void OnItemGifted(ItemGifted e)
        {
            Advance("logro_primer_regalo");
            Advance("logro_generoso");
            Advance("logro_rey_regalos");

            // odd: regalos que el isleño odia
            if (e.Opinion < 0) Advance("logro_mal_regalo");
        }

        void OnEmotionShown(EmotionShown e)
        {
            Advance("logro_emocion");
            Advance("logro_teatro_isla");

            // odd: isleños que se duermen donde no deben
            if (e.Emotion == Emotion.Sleepy) Advance("logro_dormilon");
        }

        void OnHappinessChanged(HappinessChanged e)
        {
            Record("logro_felicidad_contagiosa", (int)e.To);
        }

        // ── isla ─────────────────────────────────────────────────────────────

        void OnBuildingUnlocked(BuildingUnlocked e)
        {
            Advance("logro_primer_edificio");
            Advance("logro_expansion");
            Advance("logro_isla_completa");
        }

        void OnDecorPlaced(DecorPlaced e)
        {
            Advance("logro_primer_adorno");
            Advance("logro_isla_hermosa");
            Advance("logro_isla_de_ensueno");
        }

        // ── rarezas ──────────────────────────────────────────────────────────

        void OnIslanderLeft(IslanderLeft e) => Advance("logro_abandono");

        // ── IDisposable ──────────────────────────────────────────────────────
        //
        // Quitarse de todos los eventos al morir no es una cortesía: el
        // EventBus guarda delegados, y un servicio muerto que sigue suscrito
        // revienta al volver al menú y recargar la escena.

        bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            EventBus.Unsubscribe<DayPassed>(OnDayPassed);
            EventBus.Unsubscribe<IslanderLeveledUp>(OnIslanderLeveledUp);
            EventBus.Unsubscribe<IslanderCreated>(OnIslanderCreated);
            EventBus.Unsubscribe<IslanderMovedIn>(OnIslanderMovedIn);
            EventBus.Unsubscribe<IslanderLeft>(OnIslanderLeft);
            EventBus.Unsubscribe<FriendshipStageChanged>(OnFriendshipStageChanged);
            EventBus.Unsubscribe<RomanceStageChanged>(OnRomanceStageChanged);
            EventBus.Unsubscribe<BabyBorn>(OnBabyBorn);
            EventBus.Unsubscribe<RequestResolved>(OnRequestResolved);
            EventBus.Unsubscribe<RoomEdited>(OnRoomEdited);
            EventBus.Unsubscribe<ItemAcquired>(OnItemAcquired);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<ItemGifted>(OnItemGifted);
            EventBus.Unsubscribe<EmotionShown>(OnEmotionShown);
            EventBus.Unsubscribe<HappinessChanged>(OnHappinessChanged);
            EventBus.Unsubscribe<BuildingUnlocked>(OnBuildingUnlocked);
            EventBus.Unsubscribe<DecorPlaced>(OnDecorPlaced);
        }
    }
}
