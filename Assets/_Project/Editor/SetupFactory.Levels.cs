using System.Collections.Generic;
using System.IO;
using FogWalker.Core;
using FogWalker.Gameplay;
using FogWalker.Gameplay.AI;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Interactions;
using FogWalker.Gameplay.Missions;
using FogWalker.Gameplay.Player;
using FogWalker.Gameplay.Weapons;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// سازنده سه مرحله از primitiveها با چیدمان پارامتری، کاور، SpawnZone، چک‌پوینت، اهداف،
    /// Pickupها، NavMesh و نور/مه. چیدمان با seed ثابت و قطعی است (اجرای مجدد = همان نتیجه).
    /// </summary>
    public static partial class SetupFactory
    {
        // ---------- Blueprint ----------

        private sealed class ZoneCfg
        {
            public string GroupId;
            public SpawnTrigger Trigger;
            public string ObjectiveId;
            public Vector3 Center;
            public List<(string archetypePath, int count)> Units = new List<(string, int)>();
        }

        private sealed class LevelBlueprint
        {
            public string LevelId;
            public string SceneName;
            public int Seed;
            public bool Fog;
            public string MissionTitle; //(کاتالوگ نام را از Localization می‌گیرد)
            public ObjectiveDef[] Objectives;
            public ZoneCfg[] Zones;
            public int EnergyPickups;      // برای Collect (مرحله ۲)
            public string InteractLeverId; // برای Interact (مرحله ۲ و ۳)
            public Vector3 InteractPos;
            public string CivilianId;      // برای مرحله ۱ (نجات غیرنظامی)
            public Vector3 CivilianPos;

            public static LevelBlueprint Level1() => new LevelBlueprint
            {
                LevelId = "level1",
                SceneName = "Level1_Boulevard",
                Seed = 101,
                Objectives = new[]
                {
                    Obj("obj1", ObjectiveType.Reach, "obj.l1.1", "a1"),
                    Obj("obj2", ObjectiveType.EliminateGroup, "obj.l1.2", "a2", requiredCount: 3, groupId: "g1"),
                    Obj("obj3", ObjectiveType.Reach, "obj.l1.3", "a3"),
                    Obj("obj4", ObjectiveType.Interact, "obj.l1.4", "a4"),
                    Obj("obj5", ObjectiveType.Reach, "obj.l1.5", "a5"),
                },
                Zones = new[]
                {
                    new ZoneCfg { GroupId = "g1", Trigger = SpawnTrigger.PlayerEnter, Center = new Vector3(0, 0, -2), Units = { ("EA_Rifleman", 3) } },
                    new ZoneCfg { GroupId = "g2", Trigger = SpawnTrigger.PlayerEnter, Center = new Vector3(0, 0, 36), Units = { ("EA_Rifleman", 2), ("EA_Rusher", 2) } },
                    new ZoneCfg { GroupId = "g3", Trigger = SpawnTrigger.OnObjectiveStart, ObjectiveId = "obj4", Center = new Vector3(0, 0, 52),
                        Units = { ("EA_Rifleman", 1), ("EA_Rusher", 2) } },
                },
                CivilianId = "obj4",
                CivilianPos = new Vector3(-8f, 0f, 48f),
            };

            public static LevelBlueprint Level2() => new LevelBlueprint
            {
                LevelId = "level2",
                SceneName = "Level2_Bazaar",
                Seed = 202,
                Objectives = new[]
                {
                    Obj("obj1", ObjectiveType.Collect, "obj.l2.1", "a1", requiredCount: 3),
                    Obj("obj2", ObjectiveType.Interact, "obj.l2.2", "a2"),
                    Obj("obj3", ObjectiveType.Defend, "obj.l2.3", "a3", defendSeconds: 40f),
                    Obj("obj4", ObjectiveType.Reach, "obj.l2.4", "a4"),
                },
                Zones = new[]
                {
                    new ZoneCfg { GroupId = "g1", Trigger = SpawnTrigger.PlayerEnter, Center = new Vector3(0, 0, 0), Units = { ("EA_Rifleman", 3), ("EA_Rusher", 1) } },
                    new ZoneCfg { GroupId = "g2", Trigger = SpawnTrigger.PlayerEnter, Center = new Vector3(0, 0, 28), Units = { ("EA_Rifleman", 2), ("EA_Rusher", 2) } },
                    new ZoneCfg { GroupId = "g3", Trigger = SpawnTrigger.OnObjectiveStart, ObjectiveId = "obj3", Center = new Vector3(0, 0, 42),
                        Units = { ("EA_Rifleman", 2), ("EA_Rusher", 2), ("EA_Heavy", 1) } },
                },
                EnergyPickups = 3,
                InteractLeverId = "obj2",
                InteractPos = new Vector3(9f, 0f, 20f),
            };

            public static LevelBlueprint Level3() => new LevelBlueprint
            {
                LevelId = "level3",
                SceneName = "Level3_Bridge",
                Seed = 303,
                Fog = true,
                Objectives = new[]
                {
                    Obj("obj1", ObjectiveType.Reach, "obj.l3.1", "a1"),
                    Obj("obj2", ObjectiveType.EliminateGroup, "obj.l3.2", "a2", requiredCount: 6, groupId: "g1"),
                    Obj("obj3", ObjectiveType.Interact, "obj.l3.3", "a3"),
                    Obj("obj4", ObjectiveType.Defend, "obj.l3.4", "a4", defendSeconds: 45f),
                },
                Zones = new[]
                {
                    new ZoneCfg { GroupId = "g1", Trigger = SpawnTrigger.PlayerEnter, Center = new Vector3(0, 0, 6), Units = { ("EA_Rifleman", 4), ("EA_Rusher", 2) } },
                    new ZoneCfg { GroupId = "g2", Trigger = SpawnTrigger.OnObjectiveStart, ObjectiveId = "obj3", Center = new Vector3(0, 0, 40),
                        Units = { ("EA_Rifleman", 2), ("EA_Heavy", 1) } },
                    new ZoneCfg { GroupId = "g3", Trigger = SpawnTrigger.OnObjectiveStart, ObjectiveId = "obj4", Center = new Vector3(0, 0, 50),
                        Units = { ("EA_Rifleman", 3), ("EA_Heavy", 2) } },
                },
                InteractLeverId = "obj3",
                InteractPos = new Vector3(-9f, 0f, 34f),
            };
        }

        private static ObjectiveDef Obj(string id, ObjectiveType type, string titleKey, string markerAnchorId,
            int requiredCount = 1, float defendSeconds = 0f, string groupId = null)
        {
            return new ObjectiveDef
            {
                id = id,
                type = type,
                titleKey = titleKey,
                requiredCount = requiredCount,
                timeSeconds = defendSeconds,
                targetGroupId = groupId,
                markerAnchorId = markerAnchorId,
                showMarker = true,
            };
        }

        // ---------- ساخت صحنه مرحله ----------

        private static void BuildLevelScene(LevelBlueprint bp)
        {
            System.Random rng = new System.Random(bp.Seed);
            var scene = NewEmptyScene();

            // نور و آسمان (غروب خیالی / مه)
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunGo.transform.rotation = Quaternion.Euler(bp.Fog ? 35f : 28f, -145f, 0f);
            sun.color = bp.Fog ? new Color(0.75f, 0.8f, 0.85f) : new Color(1f, 0.78f, 0.55f);
            sun.intensity = bp.Fog ? 0.9f : 1.05f;
            RenderSettings.ambientLight = bp.Fog ? new Color(0.35f, 0.4f, 0.45f) : new Color(0.42f, 0.38f, 0.38f);
            if (bp.Fog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.68f);
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.006f;
            }
            else
            {
                RenderSettings.fog = false;
            }

            // EventSystem برای HUD
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // محیط: محور و والدها
            var envRoot = new GameObject("Environment");
            var gameplayRoot = new GameObject("Gameplay");

            float streetHalfWidth = 7f;
            float streetStartZ = -46f;
            float streetEndZ = 78f;
            float streetLength = streetEndZ - streetStartZ;

            // زمین (خیابان)
            CreateEnvBox(envRoot.transform, "Ground", new Vector3(0, -0.15f, (streetStartZ + streetEndZ) / 2f),
                new Vector3(30f, 0.3f, streetLength + 20f), new Color(0.28f, 0.27f, 0.26f));

            // ساختمان‌های دو سوی خیابان (تنوع ارتفاع با seed)
            for (int i = 0; i < 11; i++)
            {
                float z = streetStartZ + 8f + i * 12f;
                foreach (int side in new[] { -1, 1 })
                {
                    // گاهی یک کوچه/خلأ فرعی (مسیر فرعی مرحله)
                    bool gap = rng.NextDouble() < 0.18;
                    if (gap) continue;
                    float h = 5f + (float)rng.NextDouble() * 7f;
                    float w = 9f + (float)rng.NextDouble() * 3f;
                    Color c = Color.Lerp(new Color(0.45f, 0.32f, 0.24f), new Color(0.35f, 0.35f, 0.4f), (float)rng.NextDouble());
                    CreateEnvBox(envRoot.transform, "Building", new Vector3(side * (streetHalfWidth + w * 0.5f + 1.2f), h / 2f, z),
                        new Vector3(w, h, 10f), c);
                }
            }

            // درختان بلوار (جعبه سبز ساده)
            for (int i = 0; i < 8; i++)
            {
                float z = streetStartZ + 14f + i * 13f;
                CreateEnvBox(envRoot.transform, "Tree", new Vector3(-streetHalfWidth - 0.8f, 1.5f, z),
                    new Vector3(0.4f, 3f, 0.4f), new Color(0.3f, 0.24f, 0.2f));
                CreateEnvBox(envRoot.transform, "Leaves", new Vector3(-streetHalfWidth - 0.8f, 3.4f, z),
                    new Vector3(1.8f, 1.6f, 1.8f), new Color(0.2f, 0.38f, 0.22f));
            }

            // کاورها/موانع در طول مسیر (فاصله منطقی + مسیر فرعی)
            float[] coverZs = { -38, -26, -14, -2, 12, 24, 33, 44, 56, 66 };
            foreach (float z in coverZs)
            {
                float x = -4f + (float)rng.NextDouble() * 8f;
                MakeCoverWall(envRoot.transform, new Vector3(x, 0, z + (float)(rng.NextDouble() * 4f - 2f)), (float)(rng.NextDouble() * 40f - 20f));
            }
            // خودروهای رهاشده (کاور)
            for (int i = 0; i < 4; i++)
            {
                float z = streetStartZ + 20f + i * 22f;
                float x = (i % 2 == 0 ? -1 : 1) * (3f + (float)rng.NextDouble() * 2f);
                var car = CreateEnvBox(envRoot.transform, "Car", new Vector3(x, 0.6f, z),
                    new Vector3(2f, 1.2f, 4.4f), Color.Lerp(Color.gray, new Color(0.5f, 0.3f, 0.15f), (float)rng.NextDouble()));
                var cp = new GameObject("CoverPoint").AddComponent<CoverPoint>();
                cp.transform.SetParent(car.transform, false);
                cp.transform.localPosition = new Vector3(x > 0 ? -1.4f : 1.4f, 0f, 0f);
                cp.forward = new Vector3(x > 0 ? -1f : 1f, 0f, 0f);
            }
            // اتوبوس رهاشده در مرحله ۳ (نقطه معروف طراحی)
            if (bp.LevelId == "level3")
            {
                CreateEnvBox(envRoot.transform, "Bus", new Vector3(2.5f, 1.4f, 30f), new Vector3(2.6f, 2.8f, 9f), new Color(0.55f, 0.45f, 0.3f));
            }

            // بازیکن
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/Player/Player.prefab");
            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            playerInstance.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, streetStartZ + 2f), Quaternion.Euler(0f, 0f, 0f));
            var playerComp = playerInstance.GetComponent<PlayerController>();

            // HUD
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/UI/HUD.prefab");
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);

            // نقاط حساس روی خط سیر
            float zA = -30f, zB = 6f, zC = 26f, zD = 46f, zE = 70f;
            string[] anchorIds = { "a1", "a2", "a3", "a4", "a5" };
            float[] anchorZs = { zA, zB + 4f, zC, zD, zE };
            for (int i = 0; i < anchorIds.Length; i++)
                MakeAnchor(gameplayRoot.transform, anchorIds[i], new Vector3(0f, 0.5f, anchorZs[i]));

            // اهداف بر اساس نوع
            foreach (var objDef in bp.Objectives)
            {
                float zOf = AnchorZOf(anchorIds, anchorZs, objDef.markerAnchorId);
                if (objDef.type == ObjectiveType.Reach)
                    MakeReachVolume(gameplayRoot.transform, objDef.id, new Vector3(0f, 1.5f, zOf), new Vector3(14f, 3f, 5f));
                if (objDef.type == ObjectiveType.Defend)
                    MakeAnchor(gameplayRoot.transform, objDef.markerAnchorId + "_zone", new Vector3(0, 0.5f, zOf));
            }

            // چک‌پوینت‌ها (میانه و پیش از نبرد آخر)
            MakeCheckpoint(gameplayRoot.transform, "cp1", new Vector3(0f, 1.5f, zC - 10f), new Vector3(14f, 3f, 4f));
            MakeCheckpoint(gameplayRoot.transform, "cp2", new Vector3(0f, 1.5f, zD + 4f), new Vector3(14f, 3f, 4f));

            // Pickupها (مهمات/سلامتی/نارنجک + سلاح‌های اضافی در مسیرهای فرعی)
            PlacePickup(scene, "Pickup_Ammo", new Vector3(-5.5f, 0.5f, -20f));
            PlacePickup(scene, "Pickup_Medkit", new Vector3(5.5f, 0.5f, 2f));
            PlacePickup(scene, "Pickup_Ammo", new Vector3(-5f, 0.5f, 30.5f));
            PlacePickup(scene, "Pickup_Medkit", new Vector3(4.5f, 0.5f, 52f));
            PlacePickup(scene, "Pickup_Grenade", new Vector3(0.5f, 0.5f, 15f));
            PlaceWeaponPickup(scene, "WD_Shotgun.asset", new Vector3(6f, 0.5f, -8f)); // مسیر فرعی کوتاه
            if (bp.LevelId != "level1")
            {
                PlaceWeaponPickup(scene, "WD_SMG.asset", new Vector3(-6f, 0.5f, 22f));
            }
            if (bp.LevelId == "level3")
            {
                PlaceWeaponPickup(scene, "WD_DMR.asset", new Vector3(6f, 0.5f, 42f));
                PlacePickup(scene, "Pickup_Grenade", new Vector3(-4.5f, 0.5f, 46f));
            }

            // آیتم‌های انرژی (مرحله ۲)
            if (bp.EnergyPickups > 0)
            {
                Vector3[] spots = { new Vector3(-8.5f, 0.5f, -18f), new Vector3(8.5f, 0.5f, 6f), new Vector3(-6f, 0.5f, 30f) };
                for (int i = 0; i < Mathf.Min(bp.EnergyPickups, spots.Length); i++)
                {
                    var item = PlacePickup(scene, "Pickup_ObjectiveItem", spots[i]);
                    SetField(item.GetComponent<ObjectiveItemPickup>(), "objectiveId", "obj1");
                }
            }

            // اهرم برق/گذر (Interact)
            if (!string.IsNullOrEmpty(bp.InteractLeverId))
            {
                var lever = PlacePickup(scene, "Interactable_Objective", bp.InteractPos);
                SetField(lever.GetComponent<ObjectiveInteractable>(), "id", bp.InteractLeverId);
                // دیوار جلوی گذر قبل از اهرم (باز می‌شود دستی پس از تعامل — برای نسخه اول: کاور جدید نمی‌سازیم)
                MakeAnchor(gameplayRoot.transform, "lever_anchor", bp.InteractPos);
            }

            // غیرنظامی (مرحله ۱)
            if (!string.IsNullOrEmpty(bp.CivilianId))
            {
                var civ = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                civ.name = "Civilian (Rescue)";
                civ.transform.position = bp.CivilianPos + Vector3.up * 0.9f;
                civ.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
                Object.DestroyImmediate(civ.GetComponent<Collider>());
                civ.GetComponent<Renderer>().material.color = new Color(0.95f, 0.85f, 0.6f);
                civ.AddComponent<BoxCollider>().isTrigger = true;
                var oi = civ.AddComponent<ObjectiveInteractable>();
                SetField(oi, "id", bp.CivilianId);
                civ.layer = GameplayLayers.Interactable;
            }

            // نقطه استخراج (با id آخرین Reach یا اختصاصی)
            ObjectiveDef lastReach = null;
            foreach (var o in bp.Objectives) if (o.type == ObjectiveType.Reach) lastReach = o;
            if (lastReach != null && bp.EnergyPickups == 0)
            {
                // Reachها خودشان Volume دارند؛ «استخراج» نهایی با exhale ناحیه بزرگ‌تر
                var extGo = new GameObject("ExtractionZone");
                extGo.transform.SetParent(gameplayRoot.transform, false);
                extGo.transform.position = new Vector3(0f, 1.5f, streetEndZ - 3f);
                var col = extGo.AddComponent<BoxCollider>();
                col.isTrigger = true; col.size = new Vector3(14f, 3f, 5f);
                var ext = extGo.AddComponent<ExtractionZone>();
                SetField(ext, "objectiveId", lastReach.id);
            }

            // ناحیه‌های Spawn
            foreach (var zone in bp.Zones)
                MakeSpawnZone(gameplayRoot.transform, zone);

            // سیستم‌های مرحله
            var sysGo = new GameObject("LevelSystems");
            var mission = CreateMissionAsset(bp);
            var mm = sysGo.AddComponent<MissionManager>();
            SetField(mm, "mission", mission);
            SetField(mm, "sceneCatalog", AssetDatabase.LoadAssetAtPath<SceneCatalog>(GenRoot + "/ScriptableObjects/Scenes/SceneCatalog_Main.asset"));

            var ck = sysGo.AddComponent<CheckpointManager>();
            SetField(ck, "mission", mission);

            var sp = sysGo.AddComponent<SpawnManager>();
            SetField(sp, "enemyPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/Enemies/Enemy.prefab"));
            SetField(sp, "globalAliveCap", 8);

            var boot = sysGo.AddComponent<GameplayBootstrapper>();
            SetField(boot, "difficultyLibrary", AssetDatabase.LoadAssetAtPath<DifficultyLibrarySO>(GenRoot + "/ScriptableObjects/Difficulty/DifficultyLibrary.asset"));
            SetField(boot, "grenadeData", AssetDatabase.LoadAssetAtPath<GrenadeDataSO>(GenRoot + "/ScriptableObjects/Weapons/GD_Grenade.asset"));
            SetField(boot, "startingWeapons", new[]
            {
                AssetDatabase.LoadAssetAtPath<WeaponDataSO>(GenRoot + "/ScriptableObjects/Weapons/WD_Pistol.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponDataSO>(GenRoot + "/ScriptableObjects/Weapons/WD_AssaultRifle.asset"),
            });
            SetField(boot, "impactLibrary", AssetDatabase.LoadAssetAtPath<ImpactLibrarySO>(GenRoot + "/ScriptableObjects/Combat_ImpactLibrary.asset"));
            var spawnMarker = new GameObject("PlayerSpawn");
            spawnMarker.transform.position = new Vector3(0f, 0.1f, streetStartZ + 2f);
            SetField(boot, "player", playerComp);
            SetField(boot, "playerSpawnPoint", spawnMarker.transform);

            // NavMesh
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0f, 0f, (streetStartZ + streetEndZ) / 2f);
            surface.size = new Vector3(30f, 10f, streetLength + 20f);
            surface.layerMask = 1 << GameplayLayers.Environment;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            SaveScene(scene, GenRoot + "/Scenes/Levels/" + bp.SceneName + ".unity");
            Debug.Log($"[FogWalker] مرحله {bp.LevelId} ساخته شد.");
        }

        // ---------- کمک‌کارهای ساخت مرحله ----------

        private static float AnchorZOf(string[] ids, float[] zs, string id)
        {
            for (int i = 0; i < ids.Length; i++) if (ids[i] == id) return zs[i];
            return 0f;
        }

        private static GameObject CreateEnvBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.layer = GameplayLayers.Environment;
            go.isStatic = true;
            go.GetComponent<Renderer>().material.color = color;
            go.AddComponent<SurfaceTag>();
            return go;
        }

        private static void MakeCoverWall(Transform parent, Vector3 pos, float yawDeg)
        {
            var wall = CreateEnvBox(parent, "CoverWall", pos + Vector3.up * 0.55f, new Vector3(2.2f, 1.1f, 0.5f), new Color(0.38f, 0.36f, 0.35f));
            wall.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            var cp = new GameObject("CoverPoint").AddComponent<CoverPoint>();
            cp.transform.SetParent(wall.transform, false);
            cp.transform.localPosition = new Vector3(0f, 0f, -0.7f);
            cp.forward = Vector3.back * -1f; // نرمال به خارج از دیوار (به سمت بازیکن معمولاً)
        }

        private static void MakeAnchor(Transform parent, string id, Vector3 pos)
        {
            var go = new GameObject("Anchor_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            // نوع واقعی HUD: FogWalker.UI.HUD.ObjectiveAnchor
            go.AddComponent<FogWalker.UI.HUD.ObjectiveAnchor>().anchorId = id;
        }

        private static void MakeReachVolume(Transform parent, string objectiveId, Vector3 pos, Vector3 size)
        {
            var go = new GameObject("Reach_" + objectiveId);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = size;
            var rv = go.AddComponent<ObjectiveReachVolume>();
            SetField(rv, "objectiveId", objectiveId);
        }

        private static void MakeCheckpoint(Transform parent, string id, Vector3 pos, Vector3 size)
        {
            var go = new GameObject("Checkpoint_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = size;
            var cp = go.AddComponent<CheckpointVolume>();
            SetField(cp, "id", id);
        }

        private static GameObject PlacePickup(Scene scene, string prefabName, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + "/Environment/Pickups/" + prefabName + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = pos;
            return instance;
        }

        private static void PlaceWeaponPickup(Scene scene, string weaponAssetName, Vector3 pos)
        {
            var wp = PlacePickup(scene, "Pickup_Weapon", pos);
            var data = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(GenRoot + "/ScriptableObjects/Weapons/" + weaponAssetName);
            SetField(wp.GetComponent<WeaponPickup>(), "weaponData", data);
        }

        private static void MakeSpawnZone(Transform parent, ZoneCfg cfg)
        {
            var go = new GameObject("SpawnZone_" + cfg.GroupId);
            go.transform.SetParent(parent, false);
            go.transform.position = cfg.Center + Vector3.up * 1.5f;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(14f, 4f, 12f);

            var zone = go.AddComponent<SpawnZone>();
            SetField(zone, "groupId", cfg.GroupId);
            SetField(zone, "trigger", cfg.Trigger);
            SetField(zone, "objectiveId", cfg.ObjectiveId);

            var entries = new List<WaveEntry>();
            string dir = GenRoot + "/ScriptableObjects/Enemies/";
            foreach (var (path, count) in cfg.Units)
            {
                entries.Add(new WaveEntry
                {
                    archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeDataSO>(dir + path + ".asset"),
                    count = count,
                    delayBetween = 0.7f,
                    initialDelay = 0.4f,
                });
            }
            SetField(zone, "entries", entries.ToArray());

            // نقاط Spawn سه‌تایی اطراف ناحیه
            var points = new List<Transform>();
            for (int i = 0; i < 3; i++)
            {
                var sp = new GameObject("Spawn" + i);
                sp.transform.SetParent(go.transform, false);
                float ang = i * 2.1f;
                sp.transform.position = cfg.Center + new Vector3(Mathf.Cos(ang) * 3.5f, 0f, Mathf.Sin(ang) * 3.5f);
                points.Add(sp.transform);
            }
            SetField(zone, "spawnPoints", points.ToArray());
            SetField(zone, "maxAliveForZone", 6);
        }

        private static MissionDataSO CreateMissionAsset(LevelBlueprint bp)
        {
            return GetOrCreate<MissionDataSO>(GenRoot + "/ScriptableObjects/Missions/MD_" + bp.LevelId + ".asset")
                .ConfigureNow(m =>
                {
                    m.levelId = bp.LevelId;
                    m.objectives = bp.Objectives;
                });
        }
    }

    internal static class SoConfigureExtensions
    {
        public static T ConfigureNow<T>(this T asset, System.Action<T> action) where T : Object
        {
            action(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
