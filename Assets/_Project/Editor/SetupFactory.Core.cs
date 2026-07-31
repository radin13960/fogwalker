using System.Collections.Generic;
using System.IO;
using FogWalker.Audio;
using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.Gameplay.AI;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Weapons;
using FogWalker.Optimization;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// کارخانه ساخت خودکار پروژه (بخش Core): تنظیمات Player، لایه‌ها، میکسر، فونت فارسی،
    /// پری‌فب Systems/Player/Enemy/FX/Pickups و Assetهای داده (سلاح‌ها، آرکی‌تایپ‌ها، نارنجک، کتابخانه صدا).
    /// با «Setup > 0 - ساخت کامل» همه مراحل یک‌جا اجرا می‌شوند.
    /// </summary>
    public static partial class SetupFactory
    {
        private const string GenRoot = "Assets/_Project";
        private const string TmpGenPath = GenRoot + "/Art/Textures/Gen";

        // ---------- منوی اصلی ----------

        [MenuItem("FogWalker/Setup/0 - 🚀 ساخت کامل پروژه (همه مراحل یک‌جا)", false, 0)]
        public static void BuildEverything()
        {
            try
            {
                ProjectSetupUtility.CreateFolderStructure();
                ProjectSetupUtility.CreateScriptableObjects();
                ApplyPlayerSettings();
                EnsureLayersAndTags();
                TryCreatePersianFontAsset();
                BuildGeneratedSprites();
                BuildAudioLibraries();
                BuildWeaponAssets();
                BuildEnemyArchetypeAssets();
                BuildGrenadeAsset();
                BuildFxPrefabs();
                BuildPickupPrefabs();
                BuildSystemsPrefab();
                BuildPlayerPrefab();
                BuildEnemyPrefab();
                BuildHudPrefab();
                BuildLevelSelectItemPrefab();
                BuildAllScenes();
                AddScenesToBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("✅ [FogWalker] ساخت کامل پروژه انجام شد. از صحنه Bootstrap بازی را شروع کنید یا Build بگیرید.");
                EditorUtility.DisplayDialog("FogWalker", "ساخت کامل انجام شد!\nاز Bootstrap بازی را اجرا کنید.", "عالی");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ [FogWalker] خطا در ساخت: " + ex.Message + "\n" + ex.StackTrace);
                EditorUtility.DisplayDialog("FogWalker", "خطا: " + ex.Message, "باشه");
            }
        }

        [MenuItem("FogWalker/Setup/3 - تنظیمات Player و لایه‌ها", false, 20)]
        public static void ApplyPlayerSettingsMenu() { ApplyPlayerSettings(); EnsureLayersAndTags(); }

        [MenuItem("FogWalker/Setup/4 - ساخت پری‌فب‌ها و داده‌ها", false, 21)]
        public static void BuildAssetsMenu()
        {
            ProjectSetupUtility.CreateFolderStructure();
            ProjectSetupUtility.CreateScriptableObjects();
            EnsureLayersAndTags();
            TryCreatePersianFontAsset();
            BuildGeneratedSprites();
            BuildAudioLibraries();
            BuildWeaponAssets();
            BuildEnemyArchetypeAssets();
            BuildGrenadeAsset();
            BuildFxPrefabs();
            BuildPickupPrefabs();
            BuildSystemsPrefab();
            BuildPlayerPrefab();
            BuildEnemyPrefab();
            BuildHudPrefab();
            BuildLevelSelectItemPrefab();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[FogWalker] پری‌فب‌ها و داده‌ها ساخته شد.");
        }

        [MenuItem("FogWalker/Setup/5 - ساخت صحنه‌ها + Build Settings", false, 22)]
        public static void BuildScenesMenu() { BuildAllScenes(); AddScenesToBuildSettings(); AssetDatabase.SaveAssets(); }

        // ---------- تنظیمات Player ----------

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Mehnord Studio";
            PlayerSettings.productName = "FogWalker";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidApiLevel.Level29;
            PlayerSettings.Android.targetSdkVersion = AndroidApiLevel.Auto;
            PlayerSettings.SetGraphicsAPIs(NamedBuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;

            // قفل Landscape
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Input System جدید (مقدار 1)
            var playerSettingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/PlayerSettings.asset")[0];
            var so = new SerializedObject(playerSettingsAsset);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null) { prop.intValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); }

            Debug.Log("[FogWalker] تنظیمات Player اعمال شد (IL2CPP/ARM64/API29/Vulkan/Landscape).");
        }

        // ---------- لایه‌ها و تگ‌ها ----------

        public static void EnsureLayersAndTags()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) { Debug.LogWarning("[FogWalker] TagManager.asset پیدا نشد."); return; }
            var so = new SerializedObject(assets[0]);
            var layersProp = so.FindProperty("layers");
            string[] names = { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                               "Player", "Enemy", "Environment", "Hitbox", "Interactable", "Cover" };
            for (int i = 6; i <= 11; i++)
            {
                var element = layersProp.GetArrayElementAtIndex(i);
                element.stringValue = names[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[FogWalker] لایه‌ها تنظیم شدند: Player/Enemy/Environment/Hitbox/Interactable/Cover.");
        }

        // ---------- فونت فارسی (TMP) ----------

        public static void TryCreatePersianFontAsset()
        {
            string fontsDir = GenRoot + "/Art/UI/Fonts";
            if (!Directory.Exists(fontsDir)) return;

            // هر TTF/OTF رها شده پیدا شود
            string[] files = Directory.GetFiles(fontsDir, "*.ttf");
            if (files.Length == 0) files = Directory.GetFiles(fontsDir, "*.otf");
            if (files.Length == 0)
            {
                Debug.LogWarning("[FogWalker] فونت فارسی در " + fontsDir + " نیست؛ متن فارسی مستلزم افزودن فونت (مثل Vazirmatn) و اجرای مجدد Setup است.");
                return;
            }

            string fontPath = files[0];
            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) return;

            string outPath = fontsDir + "/" + Path.GetFileNameWithoutExtension(fontPath) + "_TMP.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(outPath);
            if (existing == null)
            {
                var fa = TMPro.TMP_FontAsset.CreateFontAsset(font);
                if (fa != null)
                {
                    AssetDatabase.CreateAsset(fa, outPath);
                    Debug.Log("[FogWalker] TMP Font Asset فارسی ساخته شد: " + outPath);
                }
            }

            // تنظیم به‌عنوان پیش‌فرض TMP
            var settingsGuids = AssetDatabase.FindAssets("t:TMP_Settings");
            foreach (var guid in settingsGuids)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings == null) continue;
                var soo = new SerializedObject(settings);
                var prop = soo.FindProperty("m_defaultFontAsset") ?? soo.FindProperty("m_defaultFontAssetUuid");
                // fallback: مستقیم property‌های شناخته‌شده TMP
                prop = soo.FindProperty("m_defaultFontAsset");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(outPath);
                    prop.objectReferenceValue = fontAsset;
                    soo.ApplyModifiedPropertiesWithoutUndo();
                }
                break;
            }
        }

        // ---------- اسپرایت‌های تولیدی (دایره برای FX/UI) ----------

        public static Sprite CircleSprite;
        public static Sprite SquareSprite;

        public static void BuildGeneratedSprites()
        {
            EnsureDir(TmpGenPath);
            CircleSprite = CreateCircleSprite(TmpGenPath + "/Gen_Circle.png", 64);
            SquareSprite = CreateSquareSprite(TmpGenPath + "/Gen_Square.png", 8);
        }

        private static Sprite CreateCircleSprite(string pngPath, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.48f; Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float a = Mathf.Clamp01((r - d) / (size * 0.08f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        private static Sprite CreateSquareSprite(string pngPath, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        // ---------- کتابخانه‌های صدا ----------

        public static void BuildAudioLibraries()
        {
            var sfx = GetOrCreate<SfxLibrarySO>(GenRoot + "/ScriptableObjects/Audio/SfxLibrary.asset");
            if (sfx.entries == null || sfx.entries.Length == 0)
            {
                var keys = new List<SfxEntry>();
                foreach (var k in new[]
                    { "sfx.fire.pistol","sfx.fire.ar","sfx.fire.smg","sfx.fire.shotgun","sfx.fire.dmr",
                      "sfx.reload","sfx.empty","sfx.pickup","sfx.objective.done","sfx.explosion",
                      "sfx.grenade.throw","sfx.enemy.fire","sfx.door.open","sfx.door.close",
                      "sfx.player.hurt","sfx.checkpoint","sfx.ui.click","sfx.steps" })
                    keys.Add(new SfxEntry { key = k, volume = 0.9f });
                sfx.entries = keys.ToArray();
                EditorUtility.SetDirty(sfx);
            }

            var music = GetOrCreate<MusicLibrarySO>(GenRoot + "/ScriptableObjects/Audio/MusicLibrary.asset");
            if (music.entries == null || music.entries.Length == 0)
            {
                music.entries = new[]
                {
                    new MusicEntry { mood = MusicMood.Exploration, volume = 0.55f },
                    new MusicEntry { mood = MusicMood.Tension, volume = 0.65f },
                    new MusicEntry { mood = MusicMood.Combat, volume = 0.8f },
                    new MusicEntry { mood = MusicMood.Victory, volume = 0.75f },
                };
                EditorUtility.SetDirty(music);
            }
        }

        // ---------- داده سلاح‌ها ----------

        public static void BuildWeaponAssets()
        {
            string dir = GenRoot + "/ScriptableObjects/Weapons";
            EnsureDir(dir);

            GetOrCreateWith(dir + "/WD_Pistol.asset", (WeaponDataSO d) =>
            {
                d.weaponId = "pistol"; d.type = WeaponType.Pistol; d.displayNameKey = "weapon.pistol";
                d.damagePerBullet = 25f; d.fireMode = FireMode.Semi; d.roundsPerMinute = 320f;
                d.magazineSize = 12; d.reserveStart = 60; d.reserveMax = 120; d.reloadTime = 1.1f; d.switchTime = 0.3f;
                d.falloffStart = 12f; d.falloffEnd = 35f; d.falloffMinMultiplier = 0.5f; d.maxRange = 100f;
                d.spreadHip = 2.2f; d.spreadAim = 0.45f; d.spreadMoveAdd = 0.9f; d.spreadHeatPerShot = 0.3f; d.spreadHeatRecovery = 3.5f;
                d.recoilPitch = 0.9f; d.recoilYawRandom = 0.3f; d.recoilRecovery = 9f;
                d.aimFovMultiplier = 0.85f; d.pellets = 1;
                d.fireSfxKey = "sfx.fire.pistol";
            });

            GetOrCreateWith(dir + "/WD_AssaultRifle.asset", (WeaponDataSO d) =>
            {
                d.weaponId = "ar"; d.type = WeaponType.AssaultRifle; d.displayNameKey = "weapon.ar";
                d.damagePerBullet = 18f; d.fireMode = FireMode.Auto; d.roundsPerMinute = 620f;
                d.magazineSize = 30; d.reserveStart = 90; d.reserveMax = 210; d.reloadTime = 1.6f; d.switchTime = 0.4f;
                d.falloffStart = 20f; d.falloffEnd = 60f; d.falloffMinMultiplier = 0.55f; d.maxRange = 140f;
                d.spreadHip = 2.8f; d.spreadAim = 0.6f; d.spreadMoveAdd = 1.1f; d.spreadHeatPerShot = 0.22f; d.spreadHeatRecovery = 2.8f;
                d.recoilPitch = 0.55f; d.recoilYawRandom = 0.4f; d.recoilRecovery = 8f;
                d.aimFovMultiplier = 0.8f; d.pellets = 1;
                d.fireSfxKey = "sfx.fire.ar";
            });

            GetOrCreateWith(dir + "/WD_SMG.asset", (WeaponDataSO d) =>
            {
                d.weaponId = "smg"; d.type = WeaponType.SMG; d.displayNameKey = "weapon.smg";
                d.damagePerBullet = 14f; d.fireMode = FireMode.Auto; d.roundsPerMinute = 770f;
                d.magazineSize = 40; d.reserveStart = 120; d.reserveMax = 280; d.reloadTime = 1.9f; d.switchTime = 0.35f;
                d.falloffStart = 10f; d.falloffEnd = 35f; d.falloffMinMultiplier = 0.4f; d.maxRange = 100f;
                d.spreadHip = 3.4f; d.spreadAim = 1.0f; d.spreadMoveAdd = 0.9f; d.spreadHeatPerShot = 0.18f; d.spreadHeatRecovery = 3.2f;
                d.recoilPitch = 0.75f; d.recoilYawRandom = 0.5f; d.recoilRecovery = 8f;
                d.aimFovMultiplier = 0.85f; d.pellets = 1;
                d.fireSfxKey = "sfx.fire.smg";
            });

            GetOrCreateWith(dir + "/WD_Shotgun.asset", (WeaponDataSO d) =>
            {
                d.weaponId = "shotgun"; d.type = WeaponType.Shotgun; d.displayNameKey = "weapon.shotgun";
                d.damagePerBullet = 12f; d.pellets = 8; d.fireMode = FireMode.Semi; d.roundsPerMinute = 70f;
                d.magazineSize = 6; d.reserveStart = 24; d.reserveMax = 48; d.reloadTime = 2.4f; d.switchTime = 0.45f;
                d.falloffStart = 5f; d.falloffEnd = 18f; d.falloffMinMultiplier = 0.25f; d.maxRange = 45f;
                d.spreadHip = 6f; d.spreadAim = 4.5f; d.spreadMoveAdd = 0.5f; d.spreadHeatPerShot = 0.5f; d.spreadHeatRecovery = 2.2f;
                d.recoilPitch = 2.2f; d.recoilYawRandom = 0.8f; d.recoilRecovery = 7f;
                d.aimFovMultiplier = 0.9f;
                d.fireSfxKey = "sfx.fire.shotgun";
            });

            GetOrCreateWith(dir + "/WD_DMR.asset", (WeaponDataSO d) =>
            {
                d.weaponId = "dmr"; d.type = WeaponType.DMR; d.displayNameKey = "weapon.dmr";
                d.damagePerBullet = 65f; d.fireMode = FireMode.Semi; d.roundsPerMinute = 120f;
                d.magazineSize = 10; d.reserveStart = 30; d.reserveMax = 60; d.reloadTime = 1.8f; d.switchTime = 0.5f;
                d.falloffStart = 40f; d.falloffEnd = 90f; d.falloffMinMultiplier = 0.7f; d.maxRange = 220f;
                d.spreadHip = 2.0f; d.spreadAim = 0.08f; d.spreadMoveAdd = 1.5f; d.spreadHeatPerShot = 0.6f; d.spreadHeatRecovery = 1.8f;
                d.recoilPitch = 1.8f; d.recoilYawRandom = 0.5f; d.recoilRecovery = 6f;
                d.aimFovMultiplier = 0.45f; d.pellets = 1;
                d.fireSfxKey = "sfx.fire.dmr";
            });
        }

        // ---------- آرکی‌تایپ دشمن ----------

        public static void BuildEnemyArchetypeAssets()
        {
            string dir = GenRoot + "/ScriptableObjects/Enemies";
            EnsureDir(dir);

            GetOrCreateWith(dir + "/EA_Rifleman.asset", (EnemyArchetypeDataSO a) =>
            {
                a.archetype = EnemyArchetype.Rifleman; a.health = 55f;
                a.walkSpeed = 2.3f; a.runSpeed = 4.6f; a.preferredRange = new Vector2(10f, 20f);
                a.viewDistance = 26f; a.fieldOfViewAngle = 115f; a.awarenessFillTime = 1f;
                a.damagePerShot = 7f; a.roundsPerMinute = 250f; a.baseAccuracy = 0.55f;
                a.burstMin = 2; a.burstMax = 4; a.burstPause = 0.9f; a.reactionTime = 0.55f;
                a.coverPreference = 0.75f; a.flankChance = 0.12f; a.retreatBelowHealth = 0.25f; a.canUseCover = true;
            });

            GetOrCreateWith(dir + "/EA_Rusher.asset", (EnemyArchetypeDataSO a) =>
            {
                a.archetype = EnemyArchetype.Rusher; a.health = 35f;
                a.walkSpeed = 3.2f; a.runSpeed = 6.2f; a.preferredRange = new Vector2(2.5f, 8f);
                a.viewDistance = 24f; a.fieldOfViewAngle = 125f; a.awarenessFillTime = 0.8f;
                a.damagePerShot = 5f; a.roundsPerMinute = 320f; a.baseAccuracy = 0.38f;
                a.burstMin = 3; a.burstMax = 6; a.burstPause = 0.6f; a.reactionTime = 0.35f;
                a.coverPreference = 0.1f; a.flankChance = 0.6f; a.retreatBelowHealth = 0f; a.canUseCover = false;
            });

            GetOrCreateWith(dir + "/EA_Heavy.asset", (EnemyArchetypeDataSO a) =>
            {
                a.archetype = EnemyArchetype.Heavy; a.health = 150f;
                a.walkSpeed = 1.7f; a.runSpeed = 3.1f; a.preferredRange = new Vector2(8f, 16f);
                a.viewDistance = 28f; a.fieldOfViewAngle = 120f; a.awarenessFillTime = 1.1f;
                a.damagePerShot = 11f; a.roundsPerMinute = 170f; a.baseAccuracy = 0.5f;
                a.burstMin = 4; a.burstMax = 8; a.burstPause = 1.2f; a.reactionTime = 0.7f;
                a.coverPreference = 0.35f; a.flankChance = 0f; a.retreatBelowHealth = 0f; a.canUseCover = true;
                a.hearingRadiusMultiplier = 1.1f;
            });
        }

        // ---------- نارنجک ----------

        public static void BuildGrenadeAsset()
        {
            string dir = GenRoot + "/ScriptableObjects/Weapons";
            EnsureDir(dir);
            GetOrCreateWith(dir + "/GD_Grenade.asset", (GrenadeDataSO g) =>
            {
                g.damage = 95f; g.radius = 4.5f; g.fuseSeconds = 2.2f;
                g.throwSpeed = 12f; g.upForce = 3.5f; g.throwCooldown = 1.2f;
                g.explosionSfxKey = "sfx.explosion";
            });
        }

        // ---------- کمکی‌های عمومی ----------

        public static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                EnsureDir(Path.GetDirectoryName(path));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        /// <summary>ساخت یا بازکردن Asset و اعمال مقادیر مرجع فقط روی نسخه تازه‌ساخته‌شده (ویرایش دستی کاربر حفظ می‌شود).</summary>
        public static void GetOrCreateWith<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            bool isNew = AssetDatabase.LoadAssetAtPath<T>(path) == null;
            var asset = GetOrCreate<T>(path);
            if (isNew) configure(asset);
            EditorUtility.SetDirty(asset);
        }

        /// <summary>تنظیم فیلد سریالایز خصوصی از بیرون (فقط Editor).</summary>
        public static void SetField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); EditorUtility.SetDirty(target); }
            else Debug.LogWarning($"[SetupFactory] فیلد '{fieldName}' در {target.GetType().Name} پیدا نشد.");
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureDir(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
