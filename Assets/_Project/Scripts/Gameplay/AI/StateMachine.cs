using System;
using System.Collections.Generic;
using FogWalker.Utilities;

namespace FogWalker.Gameplay.AI
{
    /// <summary>
    /// ماشین‌حالت سبک و ماژولار (قابل استفاده مجدد برای AI و غیره).
    /// هر حالت سه Delegate دارد: Enter/Tick/Exit — بدون منطق پراکنده، بدون تخصیص در Update.
    /// </summary>
    /// <typeparam name="TState">Enum حالت‌ها</typeparam>
    public sealed class StateMachine<TState> where TState : struct, Enum
    {
        /// <summary>دست‌lers حالت.</summary>
        public sealed class StateHandlers
        {
            public Action OnEnter;
            public Action OnTick;
            public Action OnExit;
        }

        private readonly Dictionary<TState, StateHandlers> _states = new Dictionary<TState, StateHandlers>(16);

        /// <summary>حالت فعلی.</summary>
        public TState Current { get; private set; }
        /// <summary>زمان سپری‌شده در حالت فعلی.</summary>
        public float TimeInState { get; private set; }
        /// <summary>(قبلی، جدید) پس از تغییر موفق.</summary>
        public event Action<TState, TState> OnStateChanged;

        private bool _started;
        private bool _logTransitions;

        /// <summary>ثبت یک حالت.</summary>
        public StateMachine<TState> Add(TState state, Action onEnter = null, Action onTick = null, Action onExit = null)
        {
            _states[state] = new StateHandlers { OnEnter = onEnter, OnTick = onTick, OnExit = onExit };
            return this;
        }

        /// <summary>فعال‌سازی لاگ توسعه برای Transitionها.</summary>
        public StateMachine<TState> WithLogging(bool enabled) { _logTransitions = enabled; return this; }

        /// <summary>شروع از حالت مشخص.</summary>
        public void Start(TState initial)
        {
            Current = initial;
            _started = true;
            TimeInState = 0f;
            Tick(onEnter: true);
        }

        /// <summary>تغییر حالت؛ نادیده اگر همان بود.</summary>
        public void Change(TState next)
        {
            if (!_started) { Start(next); return; }
            if (EqualityComparer<TState>.Default.Equals(next, Current)) return;

            if (_states.TryGetValue(Current, out StateHandlers oldH)) oldH.OnExit?.Invoke();
            TState previous = Current;
            Current = next;
            TimeInState = 0f;
            if (_logTransitions) GameLog.Info($"[FSM] {previous} → {next}");
            if (_states.TryGetValue(next, out StateHandlers newH)) newH.OnEnter?.Invoke();
            OnStateChanged?.Invoke(previous, next);
        }

        /// <summary>اجرای Tick حالت فعلی (از Update صاحب صدا زده شود).</summary>
        public void Tick(bool onEnter = false)
        {
            if (!_started) return;
            if (!onEnter) TimeInState += UnityEngine.Time.deltaTime;
            if (_states.TryGetValue(Current, out StateHandlers h)) h.OnTick?.Invoke();
        }

        private void TickStateTickOnEnterIfNeeded() { }
    }
}
