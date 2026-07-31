using System.Collections;
using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.Gameplay.Interactions;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Player;
using FogWalker.Gameplay.Weapons;
using FogWalker.Localization;
using FogWalker.Settings;
using FogWalker.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FogWalker.UI.HUD
{
    /// <summary>نقطه اتکای نشانگر هدف در صحنه (با id از ObjectiveDef.markerAnchorId).</summary>
    public sealed class ObjectiveAnchor : MonoBehaviour
    {
        public string anchorId = "anchor1";
    }

    /// <summary>
    /// HUD کامل بازی: سلامت، مهمات، سلاح/نارنجک، Crosshair پویا، متن هدف + مارکر سه‌بعدی،
    /// نشانگر تعامل/کاور، آسیب جهت‌دار، Hit Marker، Toast، و پنل‌های Pause/Death/Complete.
    /// سیم‌کشی توسط SetupFactory انجام می‌شود؛ فیلدها null-safe.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        [Header("سلامت")]
        [SerializeField] private Image healthFill;
        [SerializeField] private CanvasGroup damageVignette;

        [Header("سلاح")]
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text grenadeText;
        [SerializeField] private CanvasGroup crosshairGroup;
        [SerializeField] private RectTransform crosshairTop;
        [SerializeField] private RectTransform crosshairBottom;
        [SerializeField] private RectTransform crosshairLeft;
        [SerializeField] private RectTransform crosshairRight;

        [Header("هدف و نشانگر")]
        [SerializeField] private TMP_Text objectiveTitleText;
        [SerializeField] private TMP_Text objectiveProgressText;
        [SerializeField] private RectTransform objectiveMarker;
        [SerializeField] private TMP_Text toastText;

        [Header] [SerializeField, Tooltip("نشانگر تعامل (کلید متن پویا)")]
        private LocalizedText interactPrompt;
        [SerializeField] private GameObject interactPromptRoot;
        [SerializeField] private GameObject coverPromptRoot;
        [SerializeField] private GameObject hitmarker;

        [Header("آسیب جهت‌دار (بالا/راست/پایین/چپ)")]
        [SerializeField] private CanvasGroup[] directionIndicators = new CanvasGroup[4];

        [Header("پنل‌ها")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private TMP_Text completeStatsText;
        [SerializeField] private TMP_Text completeRewardText;

        [Header("گروه‌های کنترل لمسی (چپ/راست)")]
        [SerializeField] private RectTransform leftControlsGroup;
        [SerializeField] private RectTransform rightControlsGroup;
        [SerializeField] private CanvasGroup controlsCanvasGroup;

        // مراجع زمان‌اجرا
        private PlayerController _player;
        private PlayerCombatController _combat;
        private HealthComponent _playerHealth;
        private PlayerInteractionScanner _interaction;
        private MissionManager _mission;
        private LocalizationManager _loc;
        private SettingsManager _settings;
        private Camera _cam;
        private Transform _currentAnchor;
        private Vector2 _leftDefaultPos, _rightDefaultPos;
        private WeaponController _listeningWeapon;
        private Coroutine _toastRoutine;

        private void Awake()
        {
            ServiceLocator.Register(this); // سرویس سطح-مرحله؛ در OnDestroy پاک می‌شود
            if (leftControlsGroup != null) _leftDefaultPos = leftControlsGroup.anchoredPosition;
            if (rightControlsGroup != null) _rightDefaultPos = rightControlsGroup.anchoredPosition;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _loc);
            ServiceLocator.TryGet(out _settings);
            _mission = ServiceLocator.TryGet<MissionManager>();
            _cam = Camera.main;

            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                _playerHealth = _player.GetComponent<HealthComponent>();
                _combat = _player.GetComponent<PlayerCombatController>();
                _interaction = _player.GetComponent<PlayerInteractionScanner>();
                if (_interaction != null) _interaction.OnFocusChanged += HandleInteractFocus;

                var cover = _player.GetComponent<CoverController>();
                if (coverPromptRoot != null) coverPromptRoot.SetActive(false);

                var inventory = _player.GetComponent<WeaponInventory>();
                if (inventory != null) inventory.OnWeaponChanged += HandleWeaponChanged;

                var grenades = _player.GetComponent<GrenadeThrower>();
                if (grenades != null) grenades.OnCountChanged += HandleGrenades;
            }

            if (_playerHealth != null) _playerHealth.OnDamaged += HandlePlayerDamaged;

            DamageEvents.OnDamaged += HandleAnyDamaged;

            if (_mission != null)
                _mission.OnObjectiveUpdated += HandleObjective;

            if (ServiceLocator.TryGet(out CheckpointManager ck))
                ck.OnCheckpointCaptured += HandleCheckpoint;

            ApplyControlSettings();
            if (_settings != null) _settings.OnSettingsChanged += ApplyControlSettings;

            if (hitmarker != null) hitmarker.SetActive(false);
            ShowPause(false); ShowDeath(false); ShowLevelComplete(false, default);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<HUDController>();
            DamageEvents.OnDamaged -= HandleAnyDamaged;
            if (_settings != null) _settings.OnSettingsChanged -= ApplyControlSettings;
            if (_mission != null) _mission.OnObjectiveUpdated -= HandleObjective;
            if (_playerHealth != null) _playerHealth.OnDamaged -= HandlePlayerDamaged;
            if (_interaction != null) _interaction.OnFocusChanged -= HandleInteractFocus;
            if (ServiceLocator.TryGet(out CheckpointManager ck) && ck != null)
                ck.OnCheckpointCaptured -= HandleCheckpoint;
        }

        private void Update()
        {
            UpdateHealth();
            UpdateCrosshair();
            UpdateObjectiveMarker();
            UpdateCoverPrompt();
        }

        // ---------- سلامت ----------

        private void UpdateHealth()
        {
            if (_playerHealth == null || healthFill == null) return;
            healthFill.fillAmount = _playerHealth.Normalized;
        }

        private void HandlePlayerDamaged(DamageInfo info, float remain)
        {
            // وینیت کلی
            if (damageVignette != null)
            {
                StopCoroutine(nameof(VignettePulseRoutine));
                StartCoroutine(nameof(VignettePulseRoutine));
            }
            // نشانگر جهت حمله نسبت به دوربین بازیکن
            if (info.Instigator != null && directionIndicators != null)
                ShowDirectionIndicator(info.HitPoint - _player.transform.position);
        }

        private IEnumerator VignettePulseRoutine()
        {
            damageVignette.alpha = 0.7f;
            while (damageVignette.alpha > 0.01f)
            {
                damageVignette.alpha = Mathf.Lerp(damageVignette.alpha, 0f, Time.deltaTime * 3f);
                yield return null;
            }
            damageVignette.alpha = 0f;
        }

        private void ShowDirectionIndicator(Vector3 worldDirFromPlayer)
        {
            if (_cam == null || directionIndicators == null) return;
            Vector3 local = _cam.transform.InverseTransformDirection(worldDirFromPlayer);
            // 0=بالا 1=راست 2=پایین 3=چپ (بر اساس صفحه)
            int index = Mathf.Abs(local.x) > Mathf.Abs(local.y)
                ? (local.x > 0f ? 1 : 3)
                : (local.y > 0f ? 0 : 2);
            if (index < directionIndicators.Length && directionIndicators[index] != null)
                StartCoroutine(DirectionFlashRoutine(directionIndicators[index]));
        }

        private IEnumerator DirectionFlashRoutine(CanvasGroup group)
        {
            group.alpha = 1f;
            yield return new WaitForSeconds(0.8f);
            group.alpha = 0f;
        }

        // ---------- سلاح ----------

        private void HandleWeaponChanged(WeaponController weapon)
        {
            if (_listeningWeapon != null) _listeningWeapon.OnAmmoChanged -= HandleAmmo;
            _listeningWeapon = weapon;
            if (weapon == null) return;

            weapon.OnAmmoChanged += HandleAmmo;
            HandleAmmo(weapon.AmmoInMag, weapon.ReserveAmmo);

            if (weaponNameText != null && _loc != null)
                weaponNameText.text = _loc.UseBuiltInRtlFix && _loc.CurrentLanguage == "fa"
                    ? PersianTextUtility.Fix(_loc.GetText(weapon.Data.displayNameKey))
                    : _loc.GetText(weapon.Data.displayNameKey);
        }

        private void HandleAmmo(int mag, int reserve)
        {
            if (ammoText != null)
                ammoText.text = $"{PersianTextUtility.ToPersianDigits(mag.ToString())} / {PersianTextUtility.ToPersianDigits(reserve.ToString())}";
        }

        private void HandleGrenades(int count)
        {
            if (grenadeText != null)
                grenadeText.text = PersianTextUtility.ToPersianDigits(count.ToString());
        }

        // ---------- Crosshair پویا ----------

        private void UpdateCrosshair()
        {
            if (crosshairGroup == null || _combat == null) return;
            float spreadDeg = _combat.CurrentSpreadDegrees;
            float pixels = 12f + spreadDeg * 9f; // تبدیل تقریبی درجه→فاصله پیکسلی
            if (crosshairTop != null) crosshairTop.anchoredPosition = new Vector2(0f, pixels);
            if (crosshairBottom != null) crosshairBottom.anchoredPosition = new Vector2(0f, -pixels);
            if (crosshairLeft != null) crosshairLeft.anchoredPosition = new Vector2(-pixels, 0f);
            if (crosshairRight != null) crosshairRight.anchoredPosition = new Vector2(pixels, 0f);
        }

        private void HandleAnyDamaged(Component target, DamageInfo info)
        {
            // Hit Marker وقتی بازیکن زد (Instigator سلاح بازیکن) و هدف زنده/مرده دشمن است
            if (info.Instigator is WeaponController && !(target is PlayerController) && hitmarker != null)
            {
                StopCoroutine(nameof(HitmarkerRoutine));
                StartCoroutine(nameof(HitmarkerRoutine));
            }
        }

        private IEnumerator HitmarkerRoutine()
        {
            hitmarker.SetActive(true);
            yield return new WaitForSeconds(0.09f);
            hitmarker.SetActive(false);
        }

        // ---------- هدف ----------

        private void HandleObjective(ObjectiveDef def, float progress)
        {
            if (def == null) return;

            if (objectiveTitleText != null && _loc != null)
            {
                string t = _loc.GetText(def.titleKey);
                if (_loc.UseBuiltInRtlFix && _loc.CurrentLanguage == "fa") t = PersianTextUtility.Fix(t);
                objectiveTitleText.text = t;
            }

            // پیشرفت عددی (Collect: x/N — Defend: زمان باقی)
            if (objectiveProgressText != null)
            {
                string p = string.Empty;
                if (progress >= 0f)
                {
                    if (def.type == ObjectiveType.Collect)
                        p = $"{PersianTextUtility.ToPersianDigits(((int)progress).ToString())}/{PersianTextUtility.ToPersianDigits(def.requiredCount.ToString())}";
                    else if (def.type == ObjectiveType.Defend)
                        p = PersianTextUtility.ToPersianDigits(Mathf.CeilToInt(def.timeSeconds - progress).ToString()) + " " + GetLoc("hud.defend");
                    else if (def.type == ObjectiveType.EliminateGroup)
                        p = $"{PersianTextUtility.ToPersianDigits(((int)progress).ToString())}/{PersianTextUtility.ToPersianDigits(def.requiredCount.ToString())}";
                }
                objectiveProgressText.text = p;
            }

            // مارکر سه‌بعدی
            _currentAnchor = null;
            if (def.showMarker && !string.IsNullOrEmpty(def.markerAnchorId))
            {
                var anchors = FindObjectsByType<ObjectiveAnchor>(FindObjectsSortMode.None);
                foreach (var a in anchors)
                    if (a.anchorId == def.markerAnchorId) { _currentAnchor = a.transform; break; }
            }
            if (objectiveMarker != null) objectiveMarker.gameObject.SetActive(_currentAnchor != null);
        }

        private void UpdateObjectiveMarker()
        {
            if (_currentAnchor == null || objectiveMarker == null || _cam == null) return;

            Vector3 screen = _cam.WorldToScreenPoint(_currentAnchor.position + Vector3.up * 1.5f);
            if (screen.z < 0f) // پشت دوربین — به لبه صفحه بچسبان
            {
                screen.x = screen.x < Screen.width * 0.5f ? Screen.width : 0f;
                screen.y = Mathf.Clamp(screen.y, 60f, Screen.height - 60f);
                screen.z = 0f;
            }
            screen.x = Mathf.Clamp(screen.x, 60f, Screen.width - 60f);
            screen.y = Mathf.Clamp(screen.y, 60f, Screen.height - 60f);
            objectiveMarker.position = screen;
        }

        private string GetLoc(string key) => _loc != null ? _loc.GetText(key) : key;

        // ---------- تعامل و کاور ----------

        private void HandleInteractFocus(Interactions.IInteractable target)
        {
            if (interactPromptRoot != null)
                interactPromptRoot.SetActive(target != null);
            // promptKey پویا از خود آبجکت — برای سادگی فعلاً کلید ثابت
        }

        private void UpdateCoverPrompt()
        {
            if (coverPromptRoot == null) return;
            bool show = false;
            if (_player != null)
            {
                var cover = _player.GetComponent<CoverController>();
                show = cover != null && cover.IsInCover;
            }
            if (coverPromptRoot.activeSelf != show) coverPromptRoot.SetActive(show);
        }

        // ---------- Toast / چک‌پوینت ----------

        private void HandleCheckpoint(string id)
        {
            ShowToast(GetLoc("hud.checkpoint_saved"));
        }

        /// <summary>نمایش پیام کوتاه وسط پایین صفحه.</summary>
        public void ShowToast(string message)
        {
            if (toastText == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            string m = message;
            if (_loc != null && _loc.UseBuiltInRtlFix && _loc.CurrentLanguage == "fa")
                m = PersianTextUtility.Fix(m);
            toastText.text = m;
            toastText.canvasRenderer.SetAlpha(1f);
            yield return new WaitForSeconds(1.8f);
            toastText.CrossFadeAlpha(0f, 0.6f, false);
            _toastRoutine = null;
        }

        // ---------- پنل‌های جریان بازی ----------

        /// <summary>نمایش/پنهان منوی Pause.</summary>
        public void ShowPause(bool show)
        {
            if (pausePanel != null) pausePanel.SetActive(show);
        }

        /// <summary>نمایش صفحه شکست.</summary>
        public void ShowDeath(bool show)
        {
            if (deathPanel != null) deathPanel.SetActive(show);
        }

        /// <summary>نمایش صفحه پایان مرحله با آمار.</summary>
        public void ShowLevelComplete(bool show, MissionStats stats)
        {
            if (completePanel == null) return;
            completePanel.SetActive(show);
            if (!show) return;

            if (completeStatsText != null)
            {
                string time = PersianTextUtility.ToPersianDigits($"{(int)(stats.TimeSeconds / 60):00}:{(int)(stats.TimeSeconds % 60):00}");
                string acc = PersianTextUtility.ToPersianDigits(Mathf.RoundToInt(stats.Accuracy * 100f).ToString()) + "٪";
                string kills = PersianTextUtility.ToPersianDigits(stats.Kills.ToString());
                completeStatsText.text =
                    GetLoc("complete.time") + ": " + time + "\n" +
                    GetLoc("complete.accuracy") + ": " + acc + "\n" +
                    GetLoc("complete.kills") + ": " + kills + "\n" +
                    GetLoc("complete.objectives") + ": " +
                    PersianTextUtility.ToPersianDigits(stats.ObjectivesDone.ToString()) + "/" +
                    PersianTextUtility.ToPersianDigits(stats.ObjectivesTotal.ToString());

                if (_loc != null && _loc.UseBuiltInRtlFix && _loc.CurrentLanguage == "fa")
                    completeStatsText.text = PersianTextUtility.Fix(completeStatsText.text);
            }

            if (completeRewardText != null)
            {
                completeRewardText.gameObject.SetActive(!string.IsNullOrEmpty(stats.UnlockedNextLevelId));
            }
        }

        // ---------- تنظیمات کنترل لمسی ----------

        /// <summary>اعمال مقیاس، شفافیت و چیدمان چپ‌دست از تنظیمات کاربر.</summary>
        public void ApplyControlSettings()
        {
            if (_settings == null || _settings.Data == null) return;
            var data = _settings.Data;

            float scale = data.controlScale;
            if (leftControlsGroup != null) leftControlsGroup.localScale = Vector3.one * scale;
            if (rightControlsGroup != null) rightControlsGroup.localScale = Vector3.one * scale;

            if (controlsCanvasGroup != null)
                controlsCanvasGroup.alpha = data.controlOpacity;

            // چپ‌دست: جابه‌جایی جای گروه‌های چپ/راست
            if (data.leftHanded)
            {
                if (leftControlsGroup != null) leftControlsGroup.anchoredPosition = MirrorX(_rightDefaultPos);
                if (rightControlsGroup != null) rightControlsGroup.anchoredPosition = MirrorX(_leftDefaultPos);
            }
            else
            {
                if (leftControlsGroup != null) leftControlsGroup.anchoredPosition = _leftDefaultPos;
                if (rightControlsGroup != null) rightControlsGroup.anchoredPosition = _rightDefaultPos;
            }
        }

        private static Vector2 MirrorX(Vector2 v) => new Vector2(-v.x, v.y);
    }
}
