using System;
using System.IO;
using FogWalker.Save;
using NUnit.Framework;

namespace FogWalker.Tests.EditMode
{
    /// <summary>
    /// تست داده‌های تنظیمات و پیشرفت: سلامت پیش‌فرض‌ها، پایداری درجه سختی، گردکردن FPS.
    /// </summary>
    public class SettingsDataTests
    {
        private string _dir;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "fw_settings_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }
            catch { }
        }

        [Test]
        public void Defaults_AreSane()
        {
            var data = new SaveData();

            Assert.AreEqual(1, data.settings.qualityLevel, "پیش‌فرض کیفیت باید Balanced (1) باشد.");
            Assert.Contains(data.settings.targetFps, new[] { 30, 45, 60 });
            Assert.IsTrue(data.settings.masterVolume >= 0f && data.settings.masterVolume <= 1f);
            Assert.IsTrue(data.settings.musicVolume >= 0f && data.settings.musicVolume <= 1f);
            Assert.IsTrue(data.settings.sfxVolume >= 0f && data.settings.sfxVolume <= 1f);
            Assert.AreEqual("fa", data.settings.language);
            Assert.AreEqual(1, data.progress.difficulty, "پیش‌فرض سختی باید عادی باشد.");
            Assert.IsFalse(data.progress.hasSave);
        }

        [Test]
        public void Difficulty_IsPersistedInProgress()
        {
            var system = new SaveSystem(_dir);
            system.Load();
            system.ResetProgress(0); // آسان

            var fresh = new SaveSystem(_dir);
            fresh.Load();

            Assert.AreEqual(0, fresh.Data.progress.difficulty);
            Assert.IsTrue(fresh.Data.progress.hasSave, "پس از شروع بازی جدید، ادامه بازی باید فعال شود.");
        }

        [Test]
        public void ResetProgress_KeepsSettings_ClearsProgress()
        {
            var system = new SaveSystem(_dir);
            system.Load();
            system.Data.settings.qualityLevel = 2;
            system.Data.progress.unlockedLevelIds.Add("level1");
            system.Data.progress.unlockedLevelIds.Add("level2");
            system.Save();

            system.ResetProgress(2);

            Assert.AreEqual(2, system.Data.settings.qualityLevel, "تنظیمات باید حفظ شوند.");
            Assert.AreEqual(0, system.Data.progress.unlockedLevelIds.Count, "مراحل بازشده باید پاک شوند.");
            Assert.AreEqual(2, system.Data.progress.difficulty);
        }
    }
}
