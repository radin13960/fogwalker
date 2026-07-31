using System;
using FogWalker.Core;
using UnityEngine;

namespace FogWalker.Gameplay.Interactions
{
    /// <summary>قرارداد هر چیز قابل تعامل (در، جعبه مهمات، کیت، آیتم مأموریت، نقطه خروج).</summary>
    public interface IInteractable
    {
        /// <summary>کلید متن راهنمای تعامل (در Localization).</summary>
        string PromptKey { get; }
        /// <summary>اجرا توسط بازیکن.</summary>
        void Interact(GameObject interactor);
        /// <summary>آیا الان قابل تعامل است؟</summary>
        bool CanInteract { get; }
    }

    /// <summary>
    /// اسکنر تعامل بازیکن: نزدیک‌ترین IInteractable در شعاع و کونه دید جلو → رویداد برای HUD + اجرا.
    /// با OverlapSphereNonAlloc دوره‌ای (هر ۰.۱۵ ثانیه) — بدون هزینه پرفریم.
    /// </summary>
    public sealed class PlayerInteractionScanner : MonoBehaviour
    {
        [SerializeField, Tooltip("فاصله تأخیر اسکن (ثانیه)")]
        private float scanInterval = 0.15f;

        private static readonly Collider[] Buffer = new Collider[12];
        private float _scanTimer;

        /// <summary>تعامل‌پذیر فعلی زیر نشان؛ null اگر هیچ.</summary>
        public IInteractable CurrentInteractable { get; private set; }
        /// <summary>تغییر هدف تعامل (برای HUD نشانگر).</summary>
        public event Action<IInteractable> OnFocusChanged;

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = scanInterval;
            Scan();
        }

        private void Scan()
        {
            float radius = 2.2f;
            var tuning = GetComponent<Player.PlayerController>()?.Tuning;
            if (tuning != null) radius = tuning.interactRadius;

            IInteractable best = null;
            float bestDist = float.MaxValue;
            Vector3 eye = transform.position + Vector3.up * 1.4f;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, Buffer, GameplayLayers.InteractableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var it = Buffer[i].GetComponentInParent<IInteractable>();
                if (it == null || !it.CanInteract) continue;
                float d = Vector3.Distance(eye, Buffer[i].transform.position);
                if (d < bestDist) { bestDist = d; best = it; }
            }

            if (!ReferenceEquals(best, CurrentInteractable))
            {
                CurrentInteractable = best;
                OnFocusChanged?.Invoke(best);
            }
        }

        /// <summary>اجرای تعامل روی هدف فعلی.</summary>
        public void TryInteract()
        {
            if (CurrentInteractable != null && CurrentInteractable.CanInteract)
                CurrentInteractable.Interact(gameObject);
        }
    }
}
