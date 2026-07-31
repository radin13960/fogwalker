using System;
using System.IO;
using FogWalker.Save;
using NUnit.Framework;

namespace FogWalker.Tests.EditMode
{
    /// <summary>
    /// تست‌های واحد SaveSystem: Roundtrip، بازیابی از Backup، بازسازی امن هنگام خرابی، تشخیص دست‌کاری.
    /// هر تست در پوشه موقت جدا اجرا می‌شود و به فایل واقعی کاربر دست نمی‌زند.
    /// </summary>
    public class SaveSystemTests
    {
        private string _dir;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "fw_save_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }
            catch { /* بهترین تلاش */ }
        }

        [Test]
        public void Roundtrip_PreservesData()
        {
            var system = new SaveSystem(_dir);
            system.Load();
            system.Data.settings.qualityLevel = 2;
            system.Data.settings.targetFps = 45;
            system.Data.settings.leftHanded = true;
            system.Data.progress.difficulty = 2;
            system.Data.progress.lastLevelId = "level2";
            system.Data.progress.hasSave = true;
            system.Save();

            var fresh = new SaveSystem(_dir);
            fresh.Load();

            Assert.AreEqual(2, fresh.Data.settings.qualityLevel);
            Assert.AreEqual(45, fresh.Data.settings.targetFps);
            Assert.IsTrue(fresh.Data.settings.leftHanded);
            Assert.AreEqual(2, fresh.Data.progress.difficulty);
            Assert.AreEqual("level2", fresh.Data.progress.lastLevelId);
            Assert.IsTrue(fresh.Data.progress.hasSave);
        }

        [Test]
        public void CorruptMain_RecoversFromBackup()
        {
            var system = new SaveSystem(_dir);
            system.Load();
            system.Data.progress.difficulty = 0; // نسخه قدیمی (v1)
            system.Save();

            system.Data.progress.difficulty = 2; // نسخه جدید (v2) — بکاپ v1 را نگه می‌دارد
            system.Save();

            // خراب‌کردن عمدی فایل اصلی
            File.WriteAllText(system.SaveFilePath, "this is not valid json!");

            var fresh = new SaveSystem(_dir);
            fresh.Load();

            Assert.AreEqual(0, fresh.Data.progress.difficulty, "باید از فایل پشتیبان بازیابی شود.");
        }

        [Test]
        public void BothCorrupt_DefaultsAndQuarantine_WithoutCrash()
        {
            var system = new SaveSystem(_dir);
            File.WriteAllText(system.SaveFilePath, "garbage");
            File.WriteAllText(system.BackupFilePath, "also garbage");

            system.Load();

            // بازی نباید کرش کند و داده پیش‌فرض امن برگردد
            Assert.IsNotNull(system.Data);
            Assert.AreEqual(1, system.Data.settings.qualityLevel, "پیش‌فرض کیفیت باید Balanced باشد.");
            Assert.AreEqual(SaveData.CurrentSchemaVersion, system.Data.schemaVersion);

            // حداقل یک فایل قرنطینه باید ساخته شده باشد
            string[] quarantined = Directory.GetFiles(_dir, "*.corrupt-*");
            Assert.GreaterOrEqual(quarantined.Length, 1, "فایل خراب باید قرنطینه شود نه بی‌صدا نادیده گرفته شود.");
        }

        [Test]
        public void ChecksumTampering_IsRejected()
        {
            var system = new SaveSystem(_dir);
            system.Load();
            system.Data.totalKillsTamperTestHelper();
            system.Save();

            // دست‌کاری payload بدون اصلاح checksum
            string raw = File.ReadAllText(system.SaveFilePath);
            string tampered = raw.Replace("\"totalKills\": 0", "\"totalKills\": 99999");
            Assert.AreNotEqual(raw, tampered, "سناریوی تست باید متن را تغییر دهد.");
            File.WriteAllText(system.SaveFilePath, tampered);

            var fresh = new SaveSystem(_dir);
            fresh.Load();

            // چون checksum معتبر نیست، فایل دست‌کاری‌شده پذیرفته نمی‌شود؛ داده پیش‌فرض برمی‌گردد.
            Assert.AreEqual(0, fresh.Data.stats.totalKills);
        }

        [Test]
        public void ResetAll_CreatesCleanDefault()
        {
            var system = new SaveSystem(_dir);
            system.Load();
            system.Data.progress.hasSave = true;
            system.Data.settings.qualityLevel = 2;
            system.Save();

            system.ResetAll();

            Assert.IsFalse(system.Data.progress.hasSave);
            Assert.AreEqual(1, system.Data.settings.qualityLevel);

            var fresh = new SaveSystem(_dir);
            fresh.Load();
            Assert.IsFalse(fresh.Data.progress.hasSave);
        }
    }

    internal static class SaveDataTestExtensions
    {
        /// <summary>فقط برای خوانایی سناریوی تست دست‌کاری.</summary>
        public static void totalKillsTamperTestHelper(this SaveData data) => data.stats.totalKills = 0;
    }
}
