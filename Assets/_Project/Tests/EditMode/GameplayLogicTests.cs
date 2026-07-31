using FogWalker.Core;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Weapons;
using FogWalker.Save;
using NUnit.Framework;
using UnityEngine;

namespace FogWalker.Tests.EditMode
{
    /// <summary>تست محاسبات خالص سلاح (بدون GameObject): Falloff، پراکندگی، مهمات Reload.</summary>
    public class WeaponMathTests
    {
        [Test]
        public void Falloff_BeforeStart_IsOne()
        {
            Assert.AreEqual(1f, WeaponMath.FalloffMultiplier(5f, 10f, 50f, 0.5f));
        }

        [Test]
        public void Falloff_AtEnd_IsMin()
        {
            Assert.AreEqual(0.5f, WeaponMath.FalloffMultiplier(50f, 10f, 50f, 0.5f));
        }

        [Test]
        public void Falloff_Midpoint_IsLinear()
        {
            float v = WeaponMath.FalloffMultiplier(30f, 10f, 50f, 0.5f);
            Assert.AreEqual(0.75f, v, 0.001f);
        }

        [Test]
        public void Falloff_AfterEnd_StaysAtMin()
        {
            Assert.AreEqual(0.5f, WeaponMath.FalloffMultiplier(200f, 10f, 50f, 0.5f));
        }

        [Test]
        public void ReloadTransfer_NeverTakesMoreThanNeeded()
        {
            // خشاب ۱۵تایی، ۱۰ تیر داخل، ذخیره ۲۰ → فقط ۵ منتقل می‌شود
            Assert.AreEqual(5, WeaponMath.ComputeReloadTransfer(15, 10, 20));
            // ذخیره ۳ → همان ۳ (کیس ناتمام)
            Assert.AreEqual(3, WeaponMath.ComputeReloadTransfer(15, 10, 3));
            // خشاب پر → صفر
            Assert.AreEqual(0, WeaponMath.ComputeReloadTransfer(15, 15, 20));
        }

        [Test]
        public void CanFire_BlockedWhenEmptyOrReloading()
        {
            Assert.IsFalse(WeaponMath.CanFire(0, false, false, 0f));
            Assert.IsFalse(WeaponMath.CanFire(5, true, false, 0f));
            Assert.IsFalse(WeaponMath.CanFire(5, false, false, 0.2f));
            Assert.IsTrue(WeaponMath.CanFire(5, false, false, 0f));
        }

        [Test]
        public void EffectiveSpread_ClampsAtMax()
        {
            Assert.AreEqual(8f, WeaponMath.EffectiveSpread(5f, 5f, 5f));
        }
    }

    /// <summary>تست ObjectiveTracker: ترتیب، Collect، Defend، بازیابی از چک‌پوینت.</summary>
    public class ObjectiveTrackerTests
    {
        private static ObjectiveDef Reach(string id) =>
            new ObjectiveDef { id = id, type = ObjectiveType.Reach };

        private static MissionDataSO MissionWith(params ObjectiveDef[] defs)
        {
            var m = ScriptableObject.CreateInstance<MissionDataSO>();
            m.levelId = "level1";
            m.objectives = defs;
            return m;
        }

        [Test]
        public void Sequence_WrongId_DoesNotAdvance()
        {
            var tracker = new ObjectiveTracker(MissionWith(Reach("a"), Reach("b")).objectives);
            Assert.IsFalse(tracker.NotifyReach("b"), "نباید از ترتیب بیرون بزند.");
            Assert.AreEqual(0, tracker.CurrentIndex);
            Assert.IsTrue(tracker.NotifyReach("a"));
            Assert.AreEqual(1, tracker.CurrentIndex);
        }

        [Test]
        public void Collect_RequiresCount()
        {
            var defs = new[] { new ObjectiveDef { id = "cells", type = ObjectiveType.Collect, requiredCount = 3 } };
            var tracker = new ObjectiveTracker(defs);
            tracker.NotifyPickup("cells");
            tracker.NotifyPickup("cells");
            Assert.IsFalse(tracker.IsComplete);
            tracker.NotifyPickup("cells");
            Assert.IsTrue(tracker.IsComplete);
        }

        [Test]
        public void Defend_CompletesByTimer()
        {
            var defs = new[] { new ObjectiveDef { id = "hold", type = ObjectiveType.Defend, timeSeconds = 2f } };
            var tracker = new ObjectiveTracker(defs);
            tracker.Tick(1.2f);
            Assert.IsFalse(tracker.IsComplete);
            tracker.Tick(1.2f);
            Assert.IsTrue(tracker.IsComplete);
        }

        [Test]
        public void Kill_GroupAdvancesByCount()
        {
            var defs = new[] { new ObjectiveDef { id = "fight", type = ObjectiveType.EliminateGroup, targetGroupId = "g1", requiredCount = 2 } };
            var tracker = new ObjectiveTracker(defs);
            Assert.IsFalse(tracker.NotifyKill("g2"), "کشتن گروه دیگر نباید بشمارد.");
            tracker.NotifyKill("g1");
            Assert.IsFalse(tracker.IsComplete);
            tracker.NotifyKill("g1");
            Assert.IsTrue(tracker.IsComplete);
        }

        [Test]
        public void RestoreBeforeIndex_SkipsCompletedObjectives()
        {
            var tracker = new ObjectiveTracker(MissionWith(Reach("a"), Reach("b"), Reach("c")).objectives);
            tracker.RestoreBeforeIndex(2);
            Assert.AreEqual(2, tracker.CurrentIndex);
            Assert.IsTrue(tracker.NotifyReach("c"));
            Assert.IsTrue(tracker.IsComplete);
        }

        [Test]
        public void Events_FireInOrder()
        {
            var defs = new[] { Reach("a"), Reach("b") };
            var tracker = new ObjectiveTracker(defs);
            int completed = 0;
            tracker.OnObjectiveCompleted += (_, __) => completed++;
            tracker.NotifyReach("a");
            tracker.NotifyReach("b");
            Assert.AreEqual(2, completed);
        }
    }

    /// <summary>تست بازشدن مرحله‌ها و ثبت آمار (خالص).</summary>
    public class ProgressUnlockerTests
    {
        private static SceneCatalog Catalog()
        {
            var c = ScriptableObject.CreateInstance<SceneCatalog>();
            c.levels = new[]
            {
                new SceneCatalog.LevelEntry { levelId = "l1", sceneName = "S1" },
                new SceneCatalog.LevelEntry { levelId = "l2", sceneName = "S2" },
                new SceneCatalog.LevelEntry { levelId = "l3", sceneName = "S3" },
            };
            return c;
        }

        [Test]
        public void UnlockNext_AddsFollowingLevel_OnlyOnce()
        {
            var progress = new ProgressData();
            var next = ProgressUnlocker.UnlockNextLevel(progress, Catalog(), "l1");
            Assert.AreEqual("l2", next);
            Assert.AreEqual(1, progress.unlockedLevelIds.Count);

            // تکرار نباید تکثیر کند
            ProgressUnlocker.UnlockNextLevel(progress, Catalog(), "l1");
            Assert.AreEqual(1, progress.unlockedLevelIds.Count);
        }

        [Test]
        public void UnlockNext_LastLevel_ReturnsNull()
        {
            var progress = new ProgressData();
            Assert.IsNull(ProgressUnlocker.UnlockNextLevel(progress, Catalog(), "l3"));
        }

        [Test]
        public void RecordCompletion_KeepsBestValues()
        {
            var stats = new StatsData();
            ProgressUnlocker.RecordCompletion(stats, "l1", new MissionStats { TimeSeconds = 300f, Accuracy = 0.6f, Kills = 10 });
            ProgressUnlocker.RecordCompletion(stats, "l1", new MissionStats { TimeSeconds = 250f, Accuracy = 0.5f, Kills = 5 });

            Assert.AreEqual(1, stats.levelRecords.Count);
            Assert.AreEqual(250f, stats.levelRecords[0].bestTimeSeconds);
            Assert.AreEqual(0.6f, stats.levelRecords[0].bestAccuracy);
            Assert.AreEqual(10, stats.levelRecords[0].bestKills);
            Assert.AreEqual(15, stats.totalKills);
        }

        [Test]
        public void ClearCheckpoint_ResetsOnlyMatchingLevel()
        {
            var progress = new ProgressData();
            progress.checkpointLevelId = "l1";
            progress.lastCheckpointId = "cp1";
            progress.lastObjectiveIndex = 2;

            ProgressUnlocker.ClearCheckpoint(progress, "l2");
            Assert.AreEqual("cp1", progress.lastCheckpointId, "مرحله نامطابق نباید پاک شود.");

            ProgressUnlocker.ClearCheckpoint(progress, "l1");
            Assert.AreEqual("", progress.lastCheckpointId);
            Assert.AreEqual(0, progress.lastObjectiveIndex);
        }
    }
}
