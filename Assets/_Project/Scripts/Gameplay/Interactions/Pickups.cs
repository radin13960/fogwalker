using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Player;
using FogWalker.Gameplay.Weapons;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Interactions
{
    /// <summary>
    /// پایه Pickupهای قابل تعامل: لایه Interactable + روشن/خاموش و رویداد.
    /// </summary>
    public abstract class PickupBase : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("کلید راهنمای UI")]
        protected string promptKey = "hud.interact";

        /// <inheritdoc/>
        public string PromptKey => promptKey;
        /// <inheritdoc/>
        public virtual bool CanInteract => isActiveAndEnabled;

        protected virtual void Awake()
        {
            gameObject.layer = GameplayLayers.Interactable;
        }

        /// <inheritdoc/>
        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            if (ApplyTo(interactor))
            {
                OnCollected();
                gameObject.SetActive(false);
            }
        }

        /// <summary>اثر روی بازیکن؛ true = مصرف شد.</summary>
        protected abstract bool ApplyTo(GameObject interactor);

        /// <summary>نقطه گسترش: صدا/افکت.</summary>
        protected virtual void OnCollected()
        {
            Audio.AudioManager.PlaySfxShielded("sfx.pickup", transform.position);
        }
    }

    /// <summary>جعبه مهمات: افزودن ذخیره به همه سلاح‌ها، مقیاس با سختی.</summary>
    public sealed class AmmoPickup : PickupBase
    {
        [SerializeField] private int reserveAmount = 45;

        protected override bool ApplyTo(GameObject interactor)
        {
            var inv = interactor.GetComponentInChildren<WeaponInventory>();
            if (inv == null || inv.Active == null) return false;

            float mult = DifficultyContext.Current != null ? DifficultyContext.Current.ammoPickupMultiplier : 1f;
            int amount = Mathf.RoundToInt(reserveAmount * mult);
            inv.Active.AddReserveAmmo(amount);
            return true;
        }
    }

    /// <summary>کیت درمانی: شفای دستی (بازتولید خودکار نداریم → این منبع اصلی HP است).</summary>
    public sealed class MedkitPickup : PickupBase
    {
        [SerializeField] private float healAmount = 45f;

        protected override bool ApplyTo(GameObject interactor)
        {
            var health = interactor.GetComponentInParent<HealthComponent>();
            if (health == null || !health.IsAlive || health.Normalized >= 0.999f) return false;

            float mult = DifficultyContext.Current != null ? DifficultyContext.Current.medkitPickupMultiplier : 1f;
            return health.Heal(healAmount * mult) > 0.01f;
        }
    }

    /// <summary>جعبه نارنجک: +N با سقف.</summary>
    public sealed class GrenadePickup : PickupBase
    {
        [SerializeField] private int amount = 2;

        protected override bool ApplyTo(GameObject interactor)
        {
            var thrower = interactor.GetComponentInChildren<GrenadeThrower>();
            if (thrower == null) return false;
            thrower.Add(amount);
            return true;
        }
    }

    /// <summary>سلاح روی زمین: افزودن به اینونتوری (یا مهمات در صورت تکرار).</summary>
    public sealed class WeaponPickup : PickupBase
    {
        [SerializeField] private WeaponDataSO weaponData;
        [SerializeField] private GameObject visualPrefab;

        protected override bool ApplyTo(GameObject interactor)
        {
            var inv = interactor.GetComponentInChildren<WeaponInventory>();
            if (inv == null || weaponData == null)
            {
                GameLog.Warn("[WeaponPickup] اینونتوری/داده موجود نیست.");
                return false;
            }
            return inv.AddWeapon(weaponData, visualPrefab) != null;
        }
    }

    /// <summary>آیتم مأموریت (سرنخ/منبع انرژی): به ObjectiveTracker خبر می‌دهد.</summary>
    public sealed class ObjectiveItemPickup : PickupBase
    {
        [SerializeField, Tooltip("id هدف در MissionDataSO")]
        private string objectiveId;

        protected override bool ApplyTo(GameObject interactor)
        {
            if (ServiceLocator.TryGet(out MissionManager mission))
            {
                mission.NotifyPickup(objectiveId);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// تعامل مأموریتی با id (نجات غیرنظامی، اهرم برق، باز کردن دروازه): رویداد InteractionEvents را با id می‌دهد
    /// و بعد از موفقیت مصرف می‌شود. (برای اهداف ObjectiveType.Interact)
    /// </summary>
    public sealed class ObjectiveInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "obj4";
        [SerializeField] private string promptKey = "hud.interact";

        /// <inheritdoc/>
        public string PromptKey => promptKey;
        /// <inheritdoc/>
        public bool CanInteract => isActiveAndEnabled;

        private void Awake()
        {
            gameObject.layer = GameplayLayers.Interactable;
        }

        /// <inheritdoc/>
        public void Interact(GameObject interactor)
        {
            InteractionEvents.Raise(id);
            Audio.AudioManager.PlaySfxShielded("sfx.objective.done", transform.position);
            gameObject.SetActive(false);
        }
    }

    /// <summary>نقطه خروج/استخراج: رسیدن = تکمیل هدف Reach.</summary>
    public sealed class ExtractionZone : MonoBehaviour
    {
        [SerializeField] private string objectiveId = "extract";

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (ServiceLocator.TryGet(out MissionManager mission))
                mission.NotifyReach(objectiveId);
        }
    }

    /// <summary>
    /// در ساده نیمه‌باز: چرخش روی لولا هنگام تعامل؛ برای فضاهای مغازه/دروازه.
    /// </summary>
    public sealed class DoorOpenable : MonoBehaviour, IInteractable
    {
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float openSpeed = 3f;
        [SerializeField] private string promptKey = "hud.interact";

        private bool _isOpen;
        private float _blend;
        private Quaternion _closedRot;

        public string PromptKey => promptKey;
        public bool CanInteract => true;

        private void Awake()
        {
            gameObject.layer = GameplayLayers.Interactable;
            _closedRot = transform.localRotation;
        }

        public void Interact(GameObject interactor)
        {
            _isOpen = !_isOpen;
            Audio.AudioManager.PlaySfxShielded(_isOpen ? "sfx.door.open" : "sfx.door.close", transform.position);
        }

        private void Update()
        {
            float target = _isOpen ? 1f : 0f;
            _blend = Mathf.MoveTowards(_blend, target, Time.deltaTime * openSpeed);
            transform.localRotation = _closedRot * Quaternion.Euler(0f, openAngle * _blend, 0f);
        }
    }
}
