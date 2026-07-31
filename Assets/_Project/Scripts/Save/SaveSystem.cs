using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Save
{
    /// <summary>رابط سرویس ذخیره برای تست‌پذیری و تزریق.</summary>
    public interface ISaveSystem
    {
        /// <summary>داده فعلی (همیشه معتبر؛ هرگز null نیست).</summary>
        SaveData Data { get; }
        /// <summary>پس از هر ذخیره موفق.</summary>
        event Action OnSaved;
        void Load();
        void Save();
        /// <summary>حذف کامل Save (تنظیمات + پیشرفت) و ساخت فایل پیش‌فرض.</summary>
        void ResetAll();
        /// <summary>شروع بازی جدید: نگه‌داشتن تنظیمات، صفرکردن پیشرفت با درجه سختی انتخابی.</summary>
        void ResetProgress(int difficultyIndex);
    }

    /// <summary>
    /// سیستم ذخیره‌سازی کاملاً آفلاین:
    /// JSON در persistentDataPath، پاکت نسخه‌بندی‌شده با Checksum(SHA256) برای تشخیص خرابی/دست‌کاری،
    /// فایل پشتیبان .bak، و قرنطینه فایل خراب برای دیباگ. کلاس خالص C# است تا EditMode قابل تست باشد.
    /// </summary>
    public sealed class SaveSystem : ISaveSystem
    {
        [Serializable]
        private sealed class SaveEnvelope
        {
            public int schemaVersion;
            public string checksum;
            public string payload; // JSONِ SaveData
        }

        private readonly string _savePath;
        private readonly string _backupPath;

        public SaveData Data { get; private set; }
        public event Action OnSaved;

        /// <summary>مسیر فایل اصلی (برای تست و دیباگ).</summary>
        public string SaveFilePath => _savePath;
        /// <summary>مسیر فایل پشتیبان (برای تست).</summary>
        public string BackupFilePath => _backupPath;

        /// <param name="baseDirectory">برای تست واحد مسیر موقت بدهید؛ null = مسیر پیش‌فرض یونیتی.</param>
        public SaveSystem(string baseDirectory = null)
        {
            string root = string.IsNullOrEmpty(baseDirectory) ? Application.persistentDataPath : baseDirectory;
            _savePath = Path.Combine(root, "save.json");
            _backupPath = Path.Combine(root, "save.bak");
            Data = new SaveData();
        }

        /// <summary>
        /// خواندن Save با راهبرد سه‌لایه: فایل اصلی → پشتیبان → پیش‌فرض امن. در هیچ حالتی استثنا به بیرون نمی‌رود.
        /// </summary>
        public void Load()
        {
            SaveData loaded = TryRead(_savePath, out bool mainWasCorrupt);
            if (loaded != null)
            {
                Data = loaded;
                GameLog.Info("[Save] Save اصلی بارگذاری شد.");
                return;
            }

            if (mainWasCorrupt)
                QuarantineFile(_savePath);

            loaded = TryRead(_backupPath, out bool backupWasCorrupt);
            if (loaded != null)
            {
                Data = loaded;
                GameLog.Warn("[Save] Save اصلی خراب بود؛ از نسخه پشتیبان بازیابی شد.");
                return;
            }

            if (backupWasCorrupt)
                QuarantineFile(_backupPath);

            GameLog.Warn("[Save] هیچ Save سالمی یافت نشد؛ ساخت Save پیش‌فرض امن.");
            Data = new SaveData();
        }

        /// <summary>نوشتن امن: tmp → نسخه‌گرفتن bak از فعلی → جایگزینی.</summary>
        public void Save()
        {
            try
            {
                Data.schemaVersion = SaveData.CurrentSchemaVersion;
                string payload = JsonUtility.ToJson(Data, true);
                var envelope = new SaveEnvelope
                {
                    schemaVersion = SaveData.CurrentSchemaVersion,
                    checksum = ComputeChecksum(payload),
                    payload = payload
                };
                string envelopeJson = JsonUtility.ToJson(envelope, true);

                string tempPath = _savePath + ".tmp";
                File.WriteAllText(tempPath, envelopeJson);

                if (File.Exists(_savePath))
                    File.Copy(_savePath, _backupPath, true);

                File.Copy(tempPath, _savePath, true);
                File.Delete(tempPath);

                OnSaved?.Invoke();
            }
            catch (Exception ex)
            {
                // ذخیره ناموفق هرگز نباید بازی را بشکند؛ فقط گزارش مهم.
                GameLog.Error($"[Save] خطا در ذخیره: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ResetAll()
        {
            Data = new SaveData();
            Save();
            GameLog.Info("[Save] Save کامل بازنشانی شد.");
        }

        /// <inheritdoc/>
        public void ResetProgress(int difficultyIndex)
        {
            Data.progress = new ProgressData
            {
                difficulty = Mathf.Clamp(difficultyIndex, 0, 2),
                hasSave = true
            };
            Data.stats = new StatsData();
            Save();
            GameLog.Info($"[Save] بازی جدید با سختی {difficultyIndex} آغاز شد.");
        }

        // ---------- داخلی ----------

        private SaveData TryRead(string path, out bool wasCorrupt)
        {
            wasCorrupt = false;
            try
            {
                if (!File.Exists(path))
                    return null;

                string envelopeJson = File.ReadAllText(path);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(envelopeJson);
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum))
                {
                    wasCorrupt = true;
                    return null;
                }

                if (envelope.schemaVersion > SaveData.CurrentSchemaVersion)
                {
                    // فایل از نسخه جدیدتر بازی است؛ قاطعش می‌کنیم ولی کل بازی را نمی‌شکنیم.
                    GameLog.Warn($"[Save] نسخه Save ({envelope.schemaVersion}) از نسخه بازی جدیدتر است.");
                    wasCorrupt = true;
                    return null;
                }

                if (!string.Equals(envelope.checksum, ComputeChecksum(envelope.payload), StringComparison.Ordinal))
                {
                    wasCorrupt = true; // دست‌کاری یا خرابی بیت
                    return null;
                }

                var data = JsonUtility.FromJson<SaveData>(envelope.payload);
                if (data == null)
                {
                    wasCorrupt = true;
                    return null;
                }

                if (data.schemaVersion < SaveData.CurrentSchemaVersion)
                    data = Migrate(data);

                return data;
            }
            catch (Exception ex)
            {
                GameLog.Warn($"[Save] خطا در خواندن '{path}': {ex.Message}");
                wasCorrupt = true;
                return null;
            }
        }

        /// <summary>مهاجرت نسخه‌های قدیمی به نسخه فعلی. برای هر افزایش نسخه یک case بافزایید.</summary>
        private static SaveData Migrate(SaveData old)
        {
            // نسخه ۱ فعلی است؛ الگوی آینده:
            // if (old.schemaVersion == 1) { ... old.schemaVersion = 2; }
            old.schemaVersion = SaveData.CurrentSchemaVersion;
            return old;
        }

        /// <summary>Checksum سبک SHA256 روی payload (Obfuscation کافی برای تشخیص خرابی/دست‌کاری).</summary>
        public static string ComputeChecksum(string payload)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }

        private static void QuarantineFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                string quarantine = path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Move(path, quarantine);
                GameLog.Warn($"[Save] فایل خراب قرنطینه شد: {quarantine}");
            }
            catch (Exception ex)
            {
                GameLog.Warn($"[Save] قرنطینه فایل ناموفق: {ex.Message}");
            }
        }
    }
}
