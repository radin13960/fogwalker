using System;
using FogWalker.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FogWalker.Controls
{
    /// <summary>
    /// دروازه واحد ورودی بازی بر پایه Input System جدید.
    /// نقشه "Gameplay" فقط در حالت Playing فعال است (توسط GameStateManager) تا منو/Pause ورودی ناخواسته نگیرد.
    /// نقشه "UI" همیشه فعال است. کنترل‌های لمسی روی صفحه (فاز ۲) با OnScreenControl به همین Actionها متصل می‌شوند.
    /// </summary>
    public sealed class InputManager : MonoBehaviour
    {
        [Header("دارایی ورودی")]
        [SerializeField, Tooltip("فایل Input Actions اصلی (Settings/Input/GameInput.inputactions)")]
        private InputActionAsset actionsAsset;

        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string uiMapName = "UI";

        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;
        private InputAction _pauseAction;

        /// <summary>رویداد فشردن دکمه Pause (در فاز ۲ به PauseManager متصل می‌شود).</summary>
        public event Action OnPauseRequested;

        /// <summary>وضعیت فعلی نقشه گیم‌پلی.</summary>
        public bool GameplayInputEnabled => _gameplayMap != null && _gameplayMap.enabled;

        private void Awake()
        {
            if (actionsAsset == null)
            {
                GameLog.Error("[Input] فایل InputActionAsset اختصاص داده نشده است!");
                enabled = false;
                return;
            }

            _gameplayMap = actionsAsset.FindActionMap(gameplayMapName, false);
            if (_gameplayMap == null)
                GameLog.Error($"[Input] نقشه '{gameplayMapName}' در Asset پیدا نشد!");

            _uiMap = actionsAsset.FindActionMap(uiMapName, false);
            if (_uiMap == null)
                GameLog.Warn($"[Input] نقشه '{uiMapName}' در Asset پیدا نشد (اختیاری).");

            if (_gameplayMap != null)
                _pauseAction = _gameplayMap.FindAction("Pause", false);
        }

        private InputAction _uiCancelAction;

        private void OnEnable()
        {
            _uiMap?.Enable();
            _gameplayMap?.Enable(); // بلافاصله بعد، GameStateManager طبق حالت ممکن استش غیرفعال کند

            if (_pauseAction != null)
                _pauseAction.performed += HandlePause;

            // نقشه UI همیشه فعال است؛ Cancel هم می‌تواند Pause را باز/بسته کند (مثلاً خروج از Pause با Escape)
            if (_uiMap != null)
            {
                _uiCancelAction = _uiMap.FindAction("Cancel", false);
                if (_uiCancelAction != null) _uiCancelAction.performed += HandlePause;
            }
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
                _pauseAction.performed -= HandlePause;
            if (_uiCancelAction != null)
                _uiCancelAction.performed -= HandlePause;

            _gameplayMap?.Disable();
            _uiMap?.Disable();
        }

        /// <summary>روشن/خاموش‌کردن ورودی گیم‌پلی؛ توسط GameStateManager با هر تغییر حالت صدا زده می‌شود.</summary>
        public void SetGameplayInputEnabled(bool enabledNow)
        {
            if (_gameplayMap == null) return;
            if (enabledNow) _gameplayMap.Enable();
            else _gameplayMap.Disable();
        }

        /// <summary>دسترسی به یک Action با نام (جهت اشتراک اسکریپت‌های گیم‌پلی؛ نتیجه را کش کنید).</summary>
        public InputAction GetAction(string actionName, bool onlyGameplayMap = false)
        {
            if (onlyGameplayMap)
                return _gameplayMap?.FindAction(actionName, false);
            return actionsAsset != null ? actionsAsset.FindAction(actionName, false) : null;
        }

        private void HandlePause(InputAction.CallbackContext context) => OnPauseRequested?.Invoke();

        /// <summary>درخواست Pause از UI لمسی (بدون عبور از Actionها).</summary>
        public void RequestPause() => OnPauseRequested?.Invoke();
    }
}
