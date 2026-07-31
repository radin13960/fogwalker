using System;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Core
{
    /// <summary>حالت‌های اصلی چرخه عمر بازی.</summary>
    public enum GameState
    {
        Bootstrap,
        MainMenu,
        Loading,
        Playing,
        Paused,
        Cutscene,
        PlayerDead,
        LevelComplete
    }

    /// <summary>
    /// ماشین‌حالت مرکزی بازی. کلاس خالص C# (بدون MonoBehaviour) تا در EditMode قابل تست باشد.
    /// مسئول: اعتبارسنجی Transitionها، کنترل timeScale/AudioListener و روشن/خاموش‌کردن ورودی گیم‌پلی.
    /// </summary>
    public sealed class GameStateManager
    {
        // جدول Transitionهای مجاز؛ هر مسیری خارج از این، رد می‌شود تا تداخل Pause/Cutscene/Death غیرممکن شود.
        private static readonly (GameState from, GameState to)[] AllowedTransitions =
        {
            (GameState.Bootstrap,     GameState.MainMenu),
            (GameState.Bootstrap,     GameState.Loading),
            (GameState.MainMenu,      GameState.Loading),
            (GameState.Loading,       GameState.Playing),
            (GameState.Loading,       GameState.MainMenu),
            (GameState.Playing,       GameState.Paused),
            (GameState.Paused,        GameState.Playing),
            (GameState.Paused,        GameState.Loading),   // خروج به منو/ری‌استارت از Pause
            (GameState.Playing,       GameState.Cutscene),
            (GameState.Cutscene,      GameState.Playing),
            (GameState.Playing,       GameState.PlayerDead),
            (GameState.PlayerDead,    GameState.Loading),   // ادامه از چک‌پوینت
            (GameState.PlayerDead,    GameState.MainMenu),
            (GameState.Playing,       GameState.LevelComplete),
            (GameState.LevelComplete, GameState.Loading),
            (GameState.LevelComplete, GameState.MainMenu),
        };

        private readonly Action<bool> _gameplayInputGate;

        /// <summary>حالت فعلی بازی.</summary>
        public GameState Current { get; private set; } = GameState.Bootstrap;

        /// <summary>(حالت قبلی، حالت جدید) — فقط پس از Transition موفق فراخوانی می‌شود.</summary>
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary>آیا ورودی گیم‌پلی باید فعال باشد؟</summary>
        public bool IsGameplayActive => Current == GameState.Playing;

        /// <param name="gameplayInputGate">تابع کنترل نقشه ورودی گیم‌پلی (به InputManager تزریق می‌شود؛ در تست می‌توان جایگزین کرد).</param>
        public GameStateManager(Action<bool> gameplayInputGate = null)
        {
            _gameplayInputGate = gameplayInputGate;
        }

        /// <summary>
        /// تلاش برای تغییر حالت. اگر Transition مجاز نباشد هشدار می‌دهد و false برمی‌گرداند؛ هیچ اثر جانبی‌ای رخ نمی‌دهد.
        /// </summary>
        public bool SetState(GameState next)
        {
            if (next == Current)
                return false;

            if (!IsTransitionAllowed(Current, next))
            {
                GameLog.Warn($"[State] Transition نامعتبر رد شد: {Current} -> {next}");
                return false;
            }

            GameState previous = Current;
            Current = next;
            ApplySideEffects(next);
            GameLog.Info($"[State] {previous} -> {next}");
            OnStateChanged?.Invoke(previous, next);
            return true;
        }

        /// <summary>آیا Transition بین دو حالت مجاز است؟ (Public برای تست و ابزارها)</summary>
        public static bool IsTransitionAllowed(GameState from, GameState to)
        {
            for (int i = 0; i < AllowedTransitions.Length; i++)
                if (AllowedTransitions[i].from == from && AllowedTransitions[i].to == to)
                    return true;
            return false;
        }

        /// <summary>کنترل زمان، صدا و ورودی بر اساس حالت جدید.</summary>
        private void ApplySideEffects(GameState state)
        {
            // timeScale فقط در Pause صفر می‌شود؛ خروج از هر حالت، مقدار ۱ را برمی‌گرداند و مانع گیر کردن زمان می‌شود.
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
            AudioListener.pause = state == GameState.Paused;

            // ورودی گیم‌پلی فقط در حالت Playing فعال است؛ یعنی منو/کات‌سین/مرگ هیچ ورودی ناخواسته‌ای دریافت نمی‌کنند.
            _gameplayInputGate?.Invoke(state == GameState.Playing);
        }
    }
}
