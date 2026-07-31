using System.Collections.Generic;
using System.IO;
using FogWalker.Audio;
using FogWalker.Controls;
using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.Gameplay.AI;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Interactions;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Player;
using FogWalker.Gameplay.Weapons;
using FogWalker.Localization;
using FogWalker.Optimization;
using FogWalker.UI;
using FogWalker.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// بخش Prefabها: FX، Pickupها، Systems، Player، Enemy، HUD و آیتم انتخاب مرحله.
    /// </summary>
    public static partial class SetupFactory
    {
        private const string PrefabsRoot = GenRoot + "/Prefabs";

        // ---------- FX & Tracer ----------

        public static void BuildFxPrefabs()
        {
            EnsureDir(PrefabsRoot + "/VFX");

            var impact = MakeFxPrefab("FX_Impact", new Color(1f, 0.65f, 0.25f, 0.9f), 0.09f, 0.15f, 0.5f);
            MakeFxPrefab("FX_MuzzleFlash", new Color(1f, 0.9f, 0.45f, 0.95f), 0.05f, 0.25f, 0.55f);
            MakeFxPrefab("FX_Explosion", new Color(1f, 0.45f, 0.1f, 0.9f), 0.45f, 1.6f, 7f);

            // Tracer با LineRenderer
            GameObject tracer = new GameObject("FX_Tracer");
            var line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.02f; line.endWidth = 0.02f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.4f, 0.85f));
            AssetDatabase.CreateAsset(mat, TmpGenPath + "/Gen_TracerMat.mat");
            line.sharedMaterial = mat;
            tracer.AddComponent<PooledTracer>();
            SavePrefab(tracer, PrefabsRoot + "/VFX/FX_Tracer.prefab");

            // ImpactLibrary
            var impactLib = GetOrCreate<ImpactLibrarySO>(GenRoot + "/ScriptableObjects/Combat_ImpactLibrary.asset");
            EnsureDir(GenRoot + "/ScriptableObjects");
            impactLib.defaultPrefab = impact;
            EditorUtility.SetDirty(impactLib);

            // پیکربندی مرجع‌های سلاح/نارنجک بعد از ساخت FX
            var flashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/VFX/FX_MuzzleFlash.prefab");
            var tracerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/VFX/FX_Tracer.prefab");
            foreach (string wname in new[] { "WD_Pistol", "WD_AssaultRifle", "WD_SMG", "WD_Shotgun", "WD_DMR" })
            {
                var w = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(GenRoot + "/ScriptableObjects/Weapons/" + wname + ".asset");
                if (w == null) continue;
                w.muzzleFlashPrefab = flashPrefab;
                w.tracerPrefab = tracerPrefab;
                EditorUtility.SetDirty(w);
            }

            var grenade = AssetDatabase.LoadAssetAtPath<GrenadeDataSO>(GenRoot + "/ScriptableObjects/Weapons/GD_Grenade.asset");
            if (grenade != null)
            {
                grenade.explosionFxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/VFX/FX_Explosion.prefab");
                grenade.projectilePrefab = BuildGrenadeProjectilePrefab();
                EditorUtility.SetDirty(grenade);
            }
        }

        private static GameObject MakeFxPrefab(string name, Color color, float life, float startScale, float endScale)
        {
            GameObject go = new GameObject(name);
            var fx = go.AddComponent<PooledFX>();
            SetField(fx, "lifetime", life);
            SetField(fx, "startScale", startScale);
            SetField(fx, "endScale", endScale);
            SetField(fx, "billboard", true);

            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(go.transform, false);
            var sr = spriteChild.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite != null ? CircleSprite : AssetDatabase.LoadAssetAtPath<Sprite>(TmpGenPath + "/Gen_Circle.png");
            sr.color = color;

            return SavePrefab(go, PrefabsRoot + "/VFX/" + name + ".prefab");
        }

        private static GameObject BuildGrenadeProjectilePrefab()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * 0.22f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            go.AddComponent<GrenadeProjectile>();
            return SavePrefab(go, PrefabsRoot + "/Weapons/GrenadeProjectile.prefab");
        }

        // ---------- Pickups Prefabs ----------

        public static void BuildPickupPrefabs()
        {
            EnsureDir(PrefabsRoot + "/Environment/Pickups");
            MakePickupPrefab<AmmoPickup>("Pickup_Ammo", new Color(0.25f, 0.55f, 0.9f));
            MakePickupPrefab<MedkitPickup>("Pickup_Medkit", new Color(0.2f, 0.85f, 0.35f));
            MakePickupPrefab<GrenadePickup>("Pickup_Grenade", new Color(0.55f, 0.7f, 0.3f));
            MakePickupPrefab<WeaponPickup>("Pickup_Weapon", new Color(0.7f, 0.7f, 0.75f));
            MakePickupPrefab<ObjectiveItemPickup>("Pickup_ObjectiveItem", new Color(0.95f, 0.75f, 0.2f), 0.45f);
            MakePickupPrefab<ObjectiveInteractable>("Interactable_Objective", new Color(0.3f, 0.8f, 0.9f), 0.6f);
        }

        private static void MakePickupPrefab<T>(string name, Color color, float size = 0.35f) where T : Component
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = Vector3.one * size;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            go.GetComponent<Renderer>().material.color = color;
            go.AddComponent<T>();
            SavePrefab(go, PrefabsRoot + "/Environment/Pickups/" + name + ".prefab");
        }

        // ---------- Systems Prefab ----------

        public static void BuildSystemsPrefab()
        {
            GameObject systems = new GameObject("Systems");

            var input = systems.AddComponent<FogWalker.Controls.InputManager>();
            var actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(GenRoot + "/Settings/Input/GameInput.inputactions");
            SetField(input, "actionsAsset", actionsAsset);

            var settings = systems.AddComponent<FogWalker.Settings.SettingsManager>(); // audioMixer دستی (سند ۰۲)

            var quality = systems.AddComponent<QualityManager>();
            SetField(quality, "performanceProfile", AssetDatabase.LoadAssetAtPath<QualityProfileSO>(GenRoot + "/ScriptableObjects/Quality/QP_Performance.asset"));
            SetField(quality, "balancedProfile", AssetDatabase.LoadAssetAtPath<QualityProfileSO>(GenRoot + "/ScriptableObjects/Quality/QP_Balanced.asset"));
            SetField(quality, "highProfile", AssetDatabase.LoadAssetAtPath<QualityProfileSO>(GenRoot + "/ScriptableObjects/Quality/QP_High.asset"));

            var loc = systems.AddComponent<LocalizationManager>();
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTableSO>(GenRoot + "/ScriptableObjects/Localization/LocTable_FA.asset");
            SetField(loc, "tables", new List<LocalizationTableSO> { table });
            SetField(loc, "useBuiltInRtlFix", true);

            var loader = systems.AddComponent<SceneLoader>();
            SetField(loader, "catalog", AssetDatabase.LoadAssetAtPath<SceneCatalog>(GenRoot + "/ScriptableObjects/Scenes/SceneCatalog_Main.asset"));
            SetField(loader, "minLoadingSeconds", 0.8f);

            systems.AddComponent<PoolManager>();
            systems.AddComponent<AdaptiveQualityMonitor>();

            var audio = systems.AddComponent<AudioManager>();
            SetField(audio, "sfxLibrary", AssetDatabase.LoadAssetAtPath<SfxLibrarySO>(GenRoot + "/ScriptableObjects/Audio/SfxLibrary.asset"));
            SetField(audio, "musicLibrary", AssetDatabase.LoadAssetAtPath<MusicLibrarySO>(GenRoot + "/ScriptableObjects/Audio/MusicLibrary.asset"));

            // UI مدیر + صفحه بارگذاری
            var uiHost = new GameObject("UI");
            uiHost.transform.SetParent(systems.transform, false);
            var ui = uiHost.AddComponent<UIManager>();

            var lsGo = new GameObject("LoadingScreen", typeof(RectTransform));
            lsGo.transform.SetParent(uiHost.transform, false);
            var canvas = lsGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = lsGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            lsGo.AddComponent<GraphicRaycaster>();
            var group = lsGo.AddComponent<CanvasGroup>();

            var bgRt = NewRect("Background", lsGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            NewImage(bgRt, new Color(0.04f, 0.04f, 0.06f, 0.92f));

            NewText("Hint", bgRt, new Vector2(0, 120f), new Vector2(700, 60), "loading.hint", 28f, UiColors.TextDim);

            var barBg = NewRect("Bar", bgRt, new Vector2(0, 0), new Vector2(520, 22));
            NewImage(barBg, new Color(0.15f, 0.16f, 0.2f, 1f));
            var fillArea = NewRect("Fill Area", barBg, Vector2.zero, new Vector2(520, 22));
            var fill = NewRect("Fill", fillArea, Vector2.zero, new Vector2(520, 22), Vector2.zero, Vector2.one);
            NewImage(fill, UiColors.Accent);
            var sliderComp = barBg.gameObject.AddComponent<Slider>();
            sliderComp.fillRect = fill;
            sliderComp.minValue = 0f; sliderComp.maxValue = 1f; sliderComp.value = 0f;
            sliderComp.interactable = false;
            sliderComp.transition = Selectable.Transition.None;

            var percentRt = NewRect("Percent", bgRt, new Vector2(0, -70f), new Vector2(220, 60));
            var percentTxt = percentRt.gameObject.AddComponent<TextMeshProUGUI>();
            percentTxt.alignment = TextAlignmentOptions.Center; percentTxt.fontSize = 40f; percentTxt.color = UiColors.Accent;
            if (ResolveFont() != null) percentTxt.font = ResolveFont();

            var ls = lsGo.AddComponent<LoadingScreen>();
            SetField(ls, "group", group);
            SetField(ls, "progressBar", sliderComp);
            SetField(ls, "percentText", percentTxt);

            SetField(ui, "loadingScreen", ls);

            SavePrefab(systems, PrefabsRoot + "/UI/Systems.prefab");
        }

        // ---------- Player Prefab ----------

        public static void BuildPlayerPrefab()
        {
            GameObject player = new GameObject("Player");
            player.layer = GameplayLayers.Player;
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 50f; cc.stepOffset = 0.4f;

            player.AddComponent<HealthComponent>();
            var hb = player.AddComponent<Hitbox>();
            SetField(hb, "damageMultiplier", 1f);

            var tuning = AssetDatabase.LoadAssetAtPath<PlayerTuningSO>(GenRoot + "/ScriptableObjects/Player/PlayerTuning_Main.asset");
            var pc = player.AddComponent<PlayerController>();
            SetField(pc, "tuning", tuning);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(player.transform, false);
            var cam = cameraGo.AddComponent<Camera>();
            cam.fieldOfView = 65f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 220f;
            cameraGo.AddComponent<AudioListener>();

            var pivotGo = new GameObject("Pivot");
            pivotGo.transform.SetParent(player.transform, false);
            pivotGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var camCtrl = player.AddComponent<PlayerCameraController>();
            SetField(camCtrl, "targetCamera", cam);
            SetField(camCtrl, "pivot", pivotGo.transform);

            var socketGo = new GameObject("WeaponSocket");
            socketGo.transform.SetParent(pivotGo.transform, false);
            socketGo.transform.localPosition = new Vector3(0.35f, -0.25f, 0.4f);

            var inv = player.AddComponent<WeaponInventory>();
            SetField(inv, "weaponSocket", socketGo.transform);

            var grenades = player.AddComponent<GrenadeThrower>();
            SetField(grenades, "data", AssetDatabase.LoadAssetAtPath<GrenadeDataSO>(GenRoot + "/ScriptableObjects/Weapons/GD_Grenade.asset"));

            player.AddComponent<PlayerCombatController>();
            player.AddComponent<CoverController>();
            player.AddComponent<PlayerInteractionScanner>();
            player.AddComponent<GameplayInputSource>();

            // ویژوال Placeholder کپسول
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().material.color = new Color(0.25f, 0.45f, 0.75f);

            SavePrefab(player, PrefabsRoot + "/Player/Player.prefab");
        }

        // ---------- Enemy Prefab ----------

        public static void BuildEnemyPrefab()
        {
            GameObject enemy = new GameObject("Enemy");
            enemy.layer = GameplayLayers.Enemy;

            var agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.4f; agent.height = 1.8f;

            enemy.AddComponent<HealthComponent>();
            enemy.AddComponent<EnemyMotor>();
            enemy.AddComponent<EnemyPerception>();
            enemy.AddComponent<EnemyCombat>();
            enemy.AddComponent<EnemyBrain>();

            // تنه (Hitbox×1)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f);
            body.layer = GameplayLayers.Hitbox;
            var hb1 = body.AddComponent<Hitbox>(); SetField(hb1, "damageMultiplier", 1f);
            var bodyRenderer = body.GetComponent<Renderer>();

            // سر (Hitbox×2)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.layer = GameplayLayers.Hitbox;
            var hb2 = head.AddComponent<Hitbox>(); SetField(hb2, "damageMultiplier", 2f);

            var vis = enemy.AddComponent<EnemyVisualizer>();
            SetField(vis, "bodyRenderer", bodyRenderer);

            SavePrefab(enemy, PrefabsRoot + "/Enemies/Enemy.prefab");
        }

        // ---------- HUD Prefab ----------

        public static void BuildHudPrefab()
        {
            GameObject hudGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hudGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            hudGo.AddComponent<GraphicRaycaster>();

            var safe = NewRect("SafeArea", hudGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<FogWalker.UI.Common.SafeAreaFitter>();

            // ناحیه نگاه (پایین‌ترین لایه، شفاف ولی Raycastable)
            var lookRt = NewRect("LookArea", safe, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            lookRt.offsetMin = Vector2.zero; lookRt.offsetMax = Vector2.zero;
            var lookImg = NewImage(lookRt, new Color(1f, 1f, 1f, 0f));
            lookRt.gameObject.AddComponent<LookTouchArea>();

            var hud = hudGo.AddComponent<HUDController>();

            BuildHudBars(safe, hud);
            BuildHudWeapon(safe, hud);
            BuildHudObjectives(safe, hud);
            BuildHudFeedbackLayer(safe, hud);
            BuildHudTouchControls(safe, hud);
            BuildHudPanels(safe, hud);

            SavePrefab(hudGo, PrefabsRoot + "/UI/HUD.prefab");
        }

        private static void BuildHudBars(RectTransform safe, HUDController hud)
        {
            // سلامت بالا-چپ
            var hbBg = NewRect("HealthBar", safe, new Vector2(230f, -70f), new Vector2(360, 22),
                new Vector2(0f, 1f), new Vector2(0f, 1f));
            NewImage(hbBg, UiColors.HealthBg);
            var fill = NewRect("Fill", hbBg, Vector2.zero, new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(1f, 1f));
            var fillImg = NewImage(fill, UiColors.Health);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            SetField(hud, "healthFill", fillImg);

            // هدف بالا-وسط
            var objTitle = NewText("ObjectiveTitle", safe, new Vector2(0f, -50f), new Vector2(900, 44),
                "hud.checkpoint_saved", 26f, UiColors.Text, true);
            objTitle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            objTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            SetField(hud, "objectiveTitleText", objTitle);

            var objProgress = NewText("ObjectiveProgress", safe, new Vector2(0f, -92f), new Vector2(400, 34),
                "-", 22f, UiColors.Accent, false);
            objProgress.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            objProgress.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            SetField(hud, "objectiveProgressText", objProgress);
        }

        private static void BuildHudWeapon(RectTransform safe, HUDController hud)
        {
            var ammo = NewText("AmmoText", safe, new Vector2(-280f, 90f), new Vector2(300, 56),
                "15 / 60", 40f, UiColors.Text, false, TextAlignmentOptions.Right);
            ammo.rectTransform.anchorMin = new Vector2(1f, 0f);
            ammo.rectTransform.anchorMax = new Vector2(1f, 0f);
            SetField(hud, "ammoText", ammo);

            var weaponName = NewText("WeaponName", safe, new Vector2(-280f, 45f), new Vector2(300, 36),
                "-", 24f, UiColors.TextDim, false, TextAlignmentOptions.Right);
            weaponName.rectTransform.anchorMin = new Vector2(1f, 0f);
            weaponName.rectTransform.anchorMax = new Vector2(1f, 0f);
            SetField(hud, "weaponNameText", weaponName);

            var grenade = NewText("GrenadeText", safe, new Vector2(-110f, 45f), new Vector2(80, 36),
                "2", 26f, UiColors.Accent, false);
            grenade.rectTransform.anchorMin = new Vector2(1f, 0f);
            grenade.rectTransform.anchorMax = new Vector2(1f, 0f);
            SetField(hud, "grenadeText", grenade);

            // Crosshair مرکزی: 4 خط
            var chGroup = NewRect("Crosshair", safe, Vector2.zero, new Vector2(0, 0));
            var chCanvas = chGroup.gameObject.AddComponent<CanvasGroup>();
            RectTransform[] dirs = new RectTransform[4];
            string[] names = { "Top", "Bottom", "Left", "Right" };
            for (int i = 0; i < 4; i++)
            {
                var line = NewRect(names[i], chGroup, Vector2.zero, new Vector2(3, 14));
                NewImage(line, new Color(1f, 1f, 1f, 0.85f));
                dirs[i] = line;
            }
            SetField(hud, "crosshairGroup", chCanvas);
            SetField(hud, "crosshairTop", dirs[0]);
            SetField(hud, "crosshairBottom", dirs[1]);
            SetField(hud, "crosshairLeft", dirs[2]);
            SetField(hud, "crosshairRight", dirs[3]);

            // Hitmarker
            var hm = NewRect("Hitmarker", safe, Vector2.zero, new Vector2(30, 30));
            NewImage(hm, UiColors.Accent, CircleSprite);
            hm.gameObject.SetActive(false);
            SetField(hud, "hitmarker", hm.gameObject);
        }

        private static void BuildHudObjectives(RectTransform safe, HUDController hud)
        {
            // نشانگر سه‌بعدی هدف
            var marker = NewRect("ObjectiveMarker", safe, Vector2.zero, new Vector2(50, 50));
            NewImage(marker, UiColors.Accent, CircleSprite, raycast: false);
            marker.gameObject.SetActive(false);
            SetField(hud, "objectiveMarker", marker);

            // Toast
            var toast = NewText("Toast", safe, new Vector2(0f, 240f), new Vector2(700, 48),
                "-", 26f, UiColors.Accent, false);
            SetField(hud, "toastText", toast);

            // تعامل
            var interact = NewRect("InteractPrompt", safe, new Vector2(0f, -200f), new Vector2(360, 60));
            NewImage(interact, UiColors.TouchBtn);
            NewText("Label", interact, Vector2.zero, new Vector2(340, 50), "hud.interact", 26f, UiColors.Text);
            var promptRoot = interact.gameObject;
            var lt = interact.GetComponentInChildren<LocalizedText>();
            promptRoot.SetActive(false);
            SetField(hud, "interactPromptRoot", promptRoot);
            SetField(hud, "interactPrompt", lt);

            // کاور
            var cover = NewRect("CoverPrompt", safe, new Vector2(0f, 180f), new Vector2(360, 60));
            NewImage(cover, UiColors.TouchBtn);
            NewText("Label", cover, Vector2.zero, new Vector2(340, 50), "hud.cover", 26f, UiColors.Text);
            cover.gameObject.SetActive(false);
            SetField(hud, "coverPromptRoot", cover.gameObject);
        }

        private static void BuildHudFeedbackLayer(RectTransform safe, HUDController hud)
        {
            // وینیت آسیب: Image قرمز کل صفحه
            var vigRt = NewRect("DamageVignette", safe, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            vigRt.offsetMin = Vector2.zero; vigRt.offsetMax = Vector2.zero;
            NewImage(vigRt, new Color(0.8f, 0.05f, 0.05f, 0.35f), null, raycast: false);
            var vigGroup = vigRt.gameObject.AddComponent<CanvasGroup>();
            vigGroup.alpha = 0f;
            SetField(hud, "damageVignette", vigGroup);

            // نشانگرهای جهت حمله (بالا/راست/پایین/چپ)
            var dirs = new CanvasGroup[4];
            Vector2[] pos = { new Vector2(0, 380), new Vector2(760, 0), new Vector2(0, -420), new Vector2(-760, 0) };
            Vector2[] size = { new Vector2(700, 120), new Vector2(120, 700), new Vector2(700, 120), new Vector2(120, 700) };
            for (int i = 0; i < 4; i++)
            {
                var rt = NewRect("HitDir" + i, safe, pos[i], size[i]);
                NewImage(rt, new Color(0.9f, 0.1f, 0.1f, 0.4f), null, raycast: false);
                var g = rt.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0f;
                dirs[i] = g;
            }
            SetField(hud, "directionIndicators", dirs);
        }

        // دکمه‌های لمسی + منوها در بخش Scenes ادامه دارد
    }
}
