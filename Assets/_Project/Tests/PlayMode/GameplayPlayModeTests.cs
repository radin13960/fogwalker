using System.Collections;
using FogWalker.Gameplay.Combat;
using FogWalker.Optimization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FogWalker.Tests.PlayMode
{
    /// <summary>
    /// تست‌های PlayMode زیرسیستم‌های حیاتی: HealthComponent و ریست Pool.
    /// (سناریوهای کامل مرحله — شروع/ادامه/مرگ/چک‌پوینت/پایان سه مرحله — در چک‌لیست دستی Docs/07 هستند چون به کامل بودن پروژه ساخته‌شده بستگی دارند.)
    /// </summary>
    public class GameplayPlayModeTests
    {
        [UnityTest]
        public IEnumerator HealthComponent_Damage_Death_Events_Fire()
        {
            var go = new GameObject("Target");
            var health = go.AddComponent<HealthComponent>();
            health.Initialize(100f, 0f, 3f);

            bool damaged = false;
            bool died = false;
            health.OnDamaged += (_, remain) => damaged = true;
            health.OnDied += _ => died = true;

            yield return null; // یک فریم برای بوت

            health.TakeDamage(new DamageInfo { Amount = 40f, Type = DamageType.Bullet });
            Assert.IsTrue(damaged);
            Assert.AreEqual(60f, health.CurrentHealth);
            Assert.IsTrue(health.IsAlive);

            health.TakeDamage(new DamageInfo { Amount = 60f, Type = DamageType.Bullet });
            Assert.IsTrue(died);
            Assert.IsFalse(health.IsAlive);

            // آسیب به جسد نباید اثری داشته باشد (رویداد جدیدی هم صدا نمی‌زند)
            health.TakeDamage(new DamageInfo { Amount = 50f });
            Assert.AreEqual(0f, health.CurrentHealth);

            Object.Destroy(go);
            yield break;
        }

        [UnityTest]
        public IEnumerator HealthComponent_Heal_ClampsToMax()
        {
            var go = new GameObject("Target");
            var health = go.AddComponent<HealthComponent>();
            health.Initialize(100f, 0f, 0f);
            yield return null;

            health.TakeDamage(new DamageInfo { Amount = 30f });
            float healed = health.Heal(999f);
            Assert.AreEqual(30f, healed, 0.01f);
            Assert.AreEqual(100f, health.CurrentHealth);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator PoolManager_Spawn_SamePrefab_ReusesInstances()
        {
            var serviceRoot = new GameObject("PoolSvc");
            var pool = serviceRoot.AddComponent<PoolManager>();
            yield return null;

            var prefab = new GameObject("Pref");
            prefab.SetActive(false);

            var a = pool.Spawn(prefab, Vector3.zero, Quaternion.identity);
            var b = pool.Spawn(prefab, Vector3.one, Quaternion.identity);
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotSame(a, b);

            pool.Despawn(a);
            var c = pool.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Assert.AreSame(a, c, "باید نمونه Despawn‌شده بازیافت شود، نه Instantiate تازه.");

            Object.Destroy(serviceRoot);
            Object.Destroy(prefab);
            Object.Destroy(b);
            Object.Destroy(c);
        }

        [UnityTest]
        public IEnumerator Hitbox_MultipliesDamage_AndMarksHeadshot()
        {
            var root = new GameObject("Root");
            var health = root.AddComponent<HealthComponent>();
            health.Initialize(100f, 0f, 0f);
            var box = root.AddComponent<BoxCollider>();
            var hitbox = root.AddComponent<Hitbox>();
            yield return null;

            // ضریب ۱ پیش‌فرض
            hitbox.TakeDamage(new DamageInfo { Amount = 10f });
            Assert.AreEqual(90f, health.CurrentHealth);
            Assert.IsTrue(health.IsAlive);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ExplosionUtility_DamagesMultipleTargets_WithFalloff()
        {
            var center = Vector3.zero;

            GameObject Mk(string name, Vector3 pos)
            {
                var g = new GameObject(name);
                g.transform.position = pos;
                g.AddComponent<HealthComponent>().Initialize(100f, 0f, 0f);
                g.layer = FogWalker.Core.GameplayLayers.Hitbox;
                var c = g.AddComponent<SphereCollider>();
                c.radius = 0.5f;
                return g;
            }

            var near = Mk("Near", center + Vector3.right * 1f);
            var far = Mk("Far", center + Vector3.right * 4.5f);
            var outOf = Mk("Out", center + Vector3.right * 20f);
            yield return null;

            int hits = ExplosionUtility.DealAreaDamage(center, 5f, 100f, DamageType.Explosion, null, 1 << FogWalker.Core.GameplayLayers.Hitbox);

            Assert.AreEqual(2, hits, "داخل شعاع: ۲ هدف.");
            Assert.Less(near.GetComponent<HealthComponent>().CurrentHealth, 100f);
            Assert.Greater(near.GetComponent<HealthComponent>().CurrentHealth, 0f);
            Assert.AreEqual(100f, outOf.GetComponent<HealthComponent>().CurrentHealth, "خارج شعاع نباید آسیب ببیند.");

            Object.Destroy(near); Object.Destroy(far); Object.Destroy(outOf);
        }
    }
}
