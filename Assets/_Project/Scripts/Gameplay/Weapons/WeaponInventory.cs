using System;
using System.Collections;
using System.Collections.Generic;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.Weapons
{
    /// <summary>
    /// مدیریت فهرست سلاح‌های بازیکن، تعویض با تأخیر واقعی (بدون خطای مهمات)،
    /// و افزودن سلاح جدید از Pickup. سلاح فعال فعلی را به کنترلر مبارزه معرفی می‌کند.
    /// </summary>
    public sealed class WeaponInventory : MonoBehaviour
    {
        [Header("محل نصب سلاح‌ها (دست/کمر بند)")]
        [SerializeField] private Transform weaponSocket;

        private readonly List<WeaponController> _weapons = new List<WeaponController>(4);
        private Camera _ownerCamera;
        private int _activeIndex = -1;

        /// <summary>سلاح فعال؛ null تا قبل از افزودن اولین سلاح.</summary>
        public WeaponController Active { get; private set; }
        /// <summary>تعداد سلاح‌های حمل‌شده.</summary>
        public int Count => _weapons.Count;
        /// <summary>در جریان تعویض؟</summary>
        public bool IsSwitching { get; private set; }

        /// <summary>پس از تعویض موفق (برای HUD/انیمیشن).</summary>
        public event Action<WeaponController> OnWeaponChanged;

        /// <summary>آماده‌سازی با دوربین بازیکن (برای Raycast شلیک).</summary>
        public void Initialize(Camera playerCamera)
        {
            _ownerCamera = playerCamera;
        }

        /// <summary>
        /// افزودن سلاح با داده؛ اگر قبلاً داشتیم، فقط مهمات ذخیره‌ش می‌شود (رفتار Pickup استاندارد).
        /// </summary>
        public WeaponController AddWeapon(WeaponDataSO data, GameObject visualPrefab, Transform muzzleOverride = null)
        {
            if (data == null) { GameLog.Error("[Inventory] WeaponData null است!"); return null; }

            // تکراری؟ → فقط مهمات
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i].Data == data)
                {
                    _weapons[i].AddReserveAmmo(data.reserveStart / 2);
                    if (Active == null) SwitchTo(i);
                    return _weapons[i];
                }
            }

            if (weaponSocket == null)
            {
                GameLog.Error("[Inventory] weaponSocket سیم نشده است!");
                return null;
            }

            GameObject visual = visualPrefab != null
                ? Instantiate(visualPrefab, weaponSocket)
                : CreatePlaceholderVisual(data, weaponSocket);
            visual.name = "W_" + data.weaponId;
            visual.SetActive(false);

            Transform muzzle = muzzleOverride;
            if (muzzle == null)
            {
                var mz = new GameObject("Muzzle").transform;
                mz.SetParent(visual.transform, false);
                mz.localPosition = new Vector3(0f, 0f, 0.5f);
                muzzle = mz;
            }

            var controller = visual.AddComponent<WeaponController>();
            controller.Initialize(data, muzzle, _ownerCamera);

            _weapons.Add(controller);
            if (Active == null) SwitchTo(_weapons.Count - 1);
            return controller;
        }

        /// <summary>تعویض به اندیس مشخص (با تأخیر switchTime و قفل شلیک).</summary>
        public void SwitchTo(int index)
        {
            if (index < 0 || index >= _weapons.Count) return;
            if (index == _activeIndex || IsSwitching) return;
            StartCoroutine(SwitchRoutine(index));
        }

        /// <summary>سلاح بعدی/قبلی چرخه‌ای.</summary>
        public void Cycle(int direction)
        {
            if (_weapons.Count <= 1) return;
            int next = (_activeIndex + direction + _weapons.Count) % _weapons.Count;
            SwitchTo(next);
        }

        private IEnumerator SwitchRoutine(int index)
        {
            IsSwitching = true;

            if (Active != null)
            {
                Active.SetSwitching(true);
                Active.gameObject.SetActive(false);
                Active.SetSwitching(false);
            }

            // تأخیر واقعی تعویض (anim-ready): از داده سلاح مقصد
            float delay = _weapons[index].Data != null ? _weapons[index].Data.switchTime : 0.3f;
            float t = 0f;
            while (t < delay) { t += Time.deltaTime; yield return null; }

            _activeIndex = index;
            Active = _weapons[index];
            Active.gameObject.SetActive(true);

            IsSwitching = false;
            OnWeaponChanged?.Invoke(Active);
        }

        /// <summary>ریست مهمات همه سلاح‌ها (شروع مرحله/چک‌پوینت).</summary>
        public void ResetAllAmmo()
        {
            for (int i = 0; i < _weapons.Count; i++)
                _weapons[i].ResetAmmo();
        }

        /// <summary>پری‌فب Placeholder شفاف برای زمانی که مدل نهایی سلاح هنوز نیامده است.</summary>
        private static GameObject CreatePlaceholderVisual(WeaponDataSO data, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = new Vector3(0.12f, -0.08f, 0.35f);
            cube.transform.localScale = new Vector3(0.06f, 0.1f, 0.55f);
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.sharedMaterial.color = new Color(0.22f, 0.23f, 0.26f, 1f);
            }
            return cube;
        }
    }
}
