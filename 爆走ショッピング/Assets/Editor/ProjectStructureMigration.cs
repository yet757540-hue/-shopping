using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-time, GUID-preserving migration for this project. It intentionally uses
/// AssetDatabase rather than file-system moves so every serialized reference is
/// retained by Unity.
/// </summary>
[InitializeOnLoad]
public static class ProjectStructureMigration
{
    private const string SessionKey = "Shopping.ProjectStructureMigration.20260726";
    private const string Root = "Assets/_Project";
    private const string MainMenuScene = Root + "/Scenes/Menu/MainMenu.unity";
    private const string GameplayScene = Root + "/Scenes/Gameplay/ShoppingGameplay.unity";

    static ProjectStructureMigration()
    {
        EditorApplication.delayCall += RunOnceAfterCompilation;
    }

    [MenuItem("Tools/Shopping/Apply Project Structure Migration")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ProjectStructureMigration] Exit Play Mode before migrating assets.");
            return;
        }

        try
        {
            RemoveEmptyGeneratedDuplicateFolders();
            RemoveEmptyLegacyFolders();
            CreateFolders();
            MoveOwnedAssets();
        }
        finally
        {
            AssetDatabase.Refresh();
        }

        RemoveEmptyGeneratedDuplicateFolders();
        RemoveEmptyLegacyFolders();
        UpdateScenesAndBuildSettings();
        SessionState.SetBool(SessionKey, true);
        Debug.Log("[ProjectStructureMigration] Project structure migration completed.");
    }

    private static void RunOnceAfterCompilation()
    {
        if (!AssetDatabase.IsValidFolder(Root))
        {
            return;
        }

        if (SessionState.GetBool(SessionKey, false))
        {
            RemoveEmptyGeneratedDuplicateFolders();
            RemoveEmptyLegacyFolders();
            CreateUiPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return;
        }

        Apply();
    }

    private static void CreateFolders()
    {
        string[] folders =
        {
            Root,
            Root + "/Runtime/Bootstrap", Root + "/Runtime/Flow",
            Root + "/Runtime/Gameplay/Player", Root + "/Runtime/Gameplay/Scoring",
            Root + "/Runtime/Gameplay/Timer", Root + "/Runtime/Gameplay/Rewards",
            Root + "/Runtime/Presentation/UI/Hud", Root + "/Runtime/Presentation/UI/Options",
            Root + "/Runtime/Presentation/UI/Results", Root + "/Runtime/Presentation/UI/Shared",
            Root + "/Runtime/Presentation/Menu", Root + "/Runtime/Presentation/Camera",
            Root + "/Runtime/Presentation/Feedback", Root + "/Scenes/Menu",
            Root + "/Scenes/Gameplay", Root + "/Scenes/Tests", Root + "/Scenes/Archive",
            Root + "/Prefabs/Gameplay/Player", Root + "/Prefabs/Gameplay/Environment",
            Root + "/Prefabs/System/Camera", Root + "/Prefabs/UI/Hud",
            Root + "/Prefabs/UI/Options", Root + "/Prefabs/UI/Rewards", Root + "/Prefabs/UI/Results",
            Root + "/Data/CandyEffects", Root + "/Input", Root + "/Art/Models/Player",
            Root + "/Art/Models/Environment", Root + "/Art/Materials/World",
            Root + "/Art/Materials/Gameplay", Root + "/Physics/Materials",
            Root + "/Audio/SFX/Collision", Root + "/Rendering/Shaders", Root + "/Settings/Rendering"
        };

        foreach (string folder in folders)
        {
            EnsureFolder(folder);
        }
    }

    private static void RemoveEmptyGeneratedDuplicateFolders()
    {
        List<string> candidates = new List<string>();
        candidates.AddRange(System.IO.Directory.GetDirectories("Assets")
            .Where(folder => System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(folder), @"^_Project \d+$")));
        candidates.AddRange(System.IO.Directory.GetDirectories(Root)
            .Where(folder => System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(folder), @" \d+$")));

        foreach (string folder in candidates.OrderByDescending(path => path.Length))
        {
            bool containsNonMetaFile = System.IO.Directory.EnumerateFiles(folder, "*", System.IO.SearchOption.AllDirectories)
                .Any(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
            if (!containsNonMetaFile)
            {
                AssetDatabase.DeleteAsset(folder.Replace('\\', '/'));
            }
        }
    }

    private static void RemoveEmptyLegacyFolders()
    {
        string[] legacyFolders =
        {
            "Assets/Materials", "Assets/Prefab", "Assets/Resources", "Assets/Scenes",
            "Assets/Script", "Assets/Settings", "Assets/Shaders", "Assets/sound", "Assets/_Recovery"
        };

        foreach (string folder in legacyFolders)
        {
            if (!System.IO.Directory.Exists(folder))
            {
                continue;
            }

            bool containsNonMetaFile = System.IO.Directory.EnumerateFiles(folder, "*", System.IO.SearchOption.AllDirectories)
                .Any(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
            if (!containsNonMetaFile)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }

    private static void MoveOwnedAssets()
    {
        Dictionary<string, string> moves = new Dictionary<string, string>
        {
            { "Assets/Scenes/StartMenu.unity", MainMenuScene },
            { "Assets/Scenes/idou.unity", GameplayScene },
            { "Assets/Scenes/TestScene.unity", Root + "/Scenes/Tests/GameplaySmokeTest.unity" },
            { "Assets/_Recovery/0.unity", Root + "/Scenes/Archive/Recovery_001.unity" },
            { "Assets/Prefab/PlayerMovementPrefab.prefab", Root + "/Prefabs/Gameplay/Player/Player.prefab" },
            { "Assets/Prefab/Map1.prefab", Root + "/Prefabs/Gameplay/Environment/ShoppingMap_01.prefab" },
            { "Assets/Prefab/CameraRig.prefab", Root + "/Prefabs/System/Camera/CameraRig.prefab" },
            { "Assets/Prefab/Main Camera.prefab", Root + "/Prefabs/System/Camera/MainCamera.prefab" },
            { "Assets/Resources/CandyEffectLibrary.asset", Root + "/Data/CandyEffects/CandyEffectLibrary.asset" },
            { "Assets/Resources/CandyEffects/高効率回収.asset", Root + "/Data/CandyEffects/EfficientCollection.asset" },
            { "Assets/Resources/CandyEffects/走行速度アップ.asset", Root + "/Data/CandyEffects/RunningSpeedUp.asset" },
            { "Assets/Resources/CandyEffects/目標数ダウン.asset", Root + "/Data/CandyEffects/NextRequiredCountOffset.asset" },
            { "Assets/Resources/CandyEffects/時間追加.asset", Root + "/Data/CandyEffects/AddTime.asset" },
            { "Assets/Resources/CandyEffects/慣性軽減.asset", Root + "/Data/CandyEffects/InertiaReduction.asset" },
            { "Assets/Resources/ScoreTargetVisibleOverlay.mat", Root + "/Art/Materials/Gameplay/ScoreTargetVisibleOverlay.mat" },
            { "Assets/Resources/Materials/1.mat", Root + "/Art/Materials/Gameplay/OverlayFallback.mat" },
            { "Assets/InputSystem_Actions.inputactions", Root + "/Input/Shopping.inputactions" },
            { "Assets/player_test.fbx", Root + "/Art/Models/Player/PlayerCharacter.fbx" },
            { "Assets/reji_test.fbx", Root + "/Art/Models/Environment/CashRegister.fbx" },
            { "Assets/sound/crash.mp3", Root + "/Audio/SFX/Collision/Crash.mp3" },
            { "Assets/Shaders/ScoreTargetVisibleOverlay.shader", Root + "/Rendering/Shaders/ScoreTargetVisibleOverlay.shader" },
            { "Assets/Materials/Slippery.physicMaterial", Root + "/Physics/Materials/Slippery.physicMaterial" },
            { "Assets/Materials/Plane.physicMaterial", Root + "/Physics/Materials/Plane.physicMaterial" },
            { "Assets/Materials/redMaterial.mat", Root + "/Art/Materials/World/World_Red.mat" },
            { "Assets/Materials/greenMaterial.mat", Root + "/Art/Materials/World/World_Green.mat" },
            { "Assets/Materials/blueMaterial.mat", Root + "/Art/Materials/World/World_Blue.mat" },
            { "Assets/Materials/New Material.mat", Root + "/Art/Materials/World/World_Black.mat" },
            { "Assets/Materials/New Material 1.mat", Root + "/Art/Materials/Gameplay/UI_Transparent.mat" },
            { "Assets/Materials/New Material 2.mat", Root + "/Art/Materials/World/World_White_A.mat" },
            { "Assets/Materials/New Material 3.mat", Root + "/Art/Materials/World/World_White_B.mat" },
            { "Assets/Materials/New Material 4.mat", Root + "/Art/Materials/Gameplay/Gameplay_HighlightYellow.mat" },
            { "Assets/Materials/New Material 5.mat", Root + "/Art/Materials/World/World_White_C.mat" },
            { "Assets/Materials/New Material 6.mat", Root + "/Art/Materials/World/World_White_D.mat" },
            { "Assets/Materials/New Material 7.mat", Root + "/Art/Materials/World/World_White_E.mat" },
            { "Assets/Materials/New Material 8.mat", Root + "/Art/Materials/World/World_White_F.mat" }
        };

        foreach (KeyValuePair<string, string> entry in moves)
        {
            Move(entry.Key, entry.Value);
        }

        MoveScripts();

        foreach (string asset in AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Settings" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(asset);
            if (!AssetDatabase.IsValidFolder(path))
            {
                Move(path, Root + "/Settings/Rendering/" + System.IO.Path.GetFileName(path));
            }
        }
    }

    private static void MoveScripts()
    {
        MoveScript("GameFlow/GameManager.cs", "Runtime/Bootstrap/GameManager.cs");
        MoveScript("GameFlow/GameSessionRoot.cs", "Runtime/Bootstrap/GameSessionRoot.cs");
        MoveScript("GameFlow/GameSessionServices.cs", "Runtime/Bootstrap/GameSessionServices.cs");
        MoveScript("GameFlow/GameRestartManager.cs", "Runtime/Flow/GameRestartManager.cs");
        MoveScript("GameFlow/GameTimePauseManager.cs", "Runtime/Flow/GameTimePauseManager.cs");
        MoveScript("GameFlow/InGameWindowManager.cs", "Runtime/Flow/InGameWindowManager.cs");
        MoveScriptDirectory("Player", "Runtime/Gameplay/Player");
        MoveScriptDirectory("Scoring", "Runtime/Gameplay/Scoring");
        MoveScriptDirectory("Timer", "Runtime/Gameplay/Timer");
        MoveScriptDirectory("CandyEffects", "Runtime/Gameplay/Rewards");
        MoveScript("Camera/CameraFollowController.cs", "Runtime/Presentation/Camera/CameraFollowController.cs");
        MoveScript("Camera/SpeedFOVController.cs", "Runtime/Presentation/Camera/SpeedFOVController.cs");
        MoveScriptDirectory("Feedback", "Runtime/Presentation/Feedback");
        MoveScript("UI/StartMenuManager.cs", "Runtime/Presentation/Menu/StartMenuManager.cs");
        MoveScript("UI/StartMenuView.cs", "Runtime/Presentation/Menu/StartMenuView.cs");
        MoveScript("UI/InventoryStatusUI.cs", "Runtime/Presentation/UI/Hud/InventoryStatusUI.cs");
        MoveScript("UI/ControlsGuideUI.cs", "Runtime/Presentation/UI/Hud/ControlsGuideUI.cs");
        MoveScript("UI/ScoreboardView.cs", "Runtime/Presentation/UI/Hud/ScoreboardView.cs");
        MoveScript("UI/InGameOptionMenu.cs", "Runtime/Presentation/UI/Options/InGameOptionMenu.cs");
        MoveScript("UI/RuntimeOptionMenu.cs", "Runtime/Presentation/UI/Shared/RuntimeOptionMenu.cs");
        MoveScript("UI/JapaneseUIFont.cs", "Runtime/Presentation/UI/Shared/JapaneseUIFont.cs");
        MoveScript("UI/GameResultScreenManager.cs", "Runtime/Presentation/UI/Results/GameResultScreenManager.cs");
    }

    private static void MoveScriptDirectory(string sourceDirectory, string targetDirectory)
    {
        string source = "Assets/Script/" + sourceDirectory;
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { source }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Move(path, Root + "/" + targetDirectory + "/" + System.IO.Path.GetFileName(path));
        }
    }

    private static void MoveScript(string source, string target)
    {
        Move("Assets/Script/" + source, Root + "/" + target);
    }

    private static void UpdateScenesAndBuildSettings()
    {
        ConfigureMainMenu();
        ConfigureGameplayScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScene, true),
            new EditorBuildSettingsScene(GameplayScene, true)
        };
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        StartMenuManager menu = UnityEngine.Object.FindFirstObjectByType<StartMenuManager>();
        if (menu != null)
        {
            SerializedObject serialized = new SerializedObject(menu);
            serialized.FindProperty("gameSceneName").stringValue = "ShoppingGameplay";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureGameplayScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        PlayerManager player = UnityEngine.Object.FindFirstObjectByType<PlayerManager>();
        TimerManager timer = UnityEngine.Object.FindFirstObjectByType<TimerManager>();
        ScoreboardManager scoreboard = UnityEngine.Object.FindFirstObjectByType<ScoreboardManager>();
        SettlementArea settlement = UnityEngine.Object.FindFirstObjectByType<SettlementArea>();
        CollisionFeedbackManager feedback = UnityEngine.Object.FindFirstObjectByType<CollisionFeedbackManager>();

        if (gameManager == null || player == null || timer == null || scoreboard == null || settlement == null || feedback == null)
        {
            Debug.LogError("[ProjectStructureMigration] Gameplay scene is missing one or more required existing components.");
            return;
        }

        GameObject rootObject = gameManager.gameObject;
        GameSessionRoot root = GetOrAdd<GameSessionRoot>(rootObject);
        PlayerInventory inventory = GetOrAdd<PlayerInventory>(player.gameObject);
        PlayerCollisionReporter reporter = GetOrAdd<PlayerCollisionReporter>(player.gameObject);
        ImpactSettings impact = GetOrAdd<ImpactSettings>(player.gameObject);
        InventoryInfluenceSettings influence = GetOrAdd<InventoryInfluenceSettings>(player.gameObject);
        GameTimePauseManager pause = GetOrAdd<GameTimePauseManager>(rootObject);
        GameRestartManager restart = GetOrAdd<GameRestartManager>(rootObject);
        InGameWindowManager windows = GetOrAdd<InGameWindowManager>(rootObject);
        InGameOptionMenu options = GetOrAdd<InGameOptionMenu>(rootObject);
        CandyRewardWindowManager rewards = GetOrAdd<CandyRewardWindowManager>(rootObject);
        GameResultScreenManager results = GetOrAdd<GameResultScreenManager>(rootObject);
        InventoryStatusUI inventoryHud = GetOrAdd<InventoryStatusUI>(rootObject);
        TimerDisplayUI timerHud = GetOrAdd<TimerDisplayUI>(timer.gameObject);
        CandyEffectLibrary library = AssetDatabase.LoadAssetAtPath<CandyEffectLibrary>(Root + "/Data/CandyEffects/CandyEffectLibrary.asset");
        Material overlayMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/Gameplay/ScoreTargetVisibleOverlay.mat");
        ScoreboardView scoreboardView = EnsureScoreboardView();

        SerializedObject restartSerialized = new SerializedObject(restart);
        restartSerialized.FindProperty("gameSceneName").stringValue = "ShoppingGameplay";
        restartSerialized.FindProperty("startMenuSceneName").stringValue = "MainMenu";
        restartSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject rootSerialized = new SerializedObject(root);
        SetReference(rootSerialized, "player", player);
        SetReference(rootSerialized, "inventory", inventory);
        SetReference(rootSerialized, "collisionReporter", reporter);
        SetReference(rootSerialized, "impactSettings", impact);
        SetReference(rootSerialized, "settlementArea", settlement);
        SetReference(rootSerialized, "timerManager", timer);
        SetReference(rootSerialized, "scoreboardManager", scoreboard);
        SetReference(rootSerialized, "pauseManager", pause);
        SetReference(rootSerialized, "restartManager", restart);
        SetReference(rootSerialized, "candyEffectLibrary", library);
        SetReference(rootSerialized, "collisionFeedback", feedback);
        SetReference(rootSerialized, "inventoryInfluence", influence);
        SetReference(rootSerialized, "inventoryHud", inventoryHud);
        SetReference(rootSerialized, "timerHud", timerHud);
        SetReference(rootSerialized, "scoreboardView", scoreboardView);
        SetReference(rootSerialized, "windowManager", windows);
        SetReference(rootSerialized, "optionMenu", options);
        SetReference(rootSerialized, "rewardWindow", rewards);
        SetReference(rootSerialized, "resultScreen", results);
        SerializedProperty targets = rootSerialized.FindProperty("scoreTargets");
        ScoreTarget[] scoreTargets = UnityEngine.Object.FindObjectsByType<ScoreTarget>(FindObjectsSortMode.None);
        targets.arraySize = scoreTargets.Length;
        for (int i = 0; i < scoreTargets.Length; i++)
        {
            targets.GetArrayElementAtIndex(i).objectReferenceValue = scoreTargets[i];
            SerializedObject targetSerialized = new SerializedObject(scoreTargets[i]);
            SetReference(targetSerialized, "visibleOverlayMaterialTemplate", overlayMaterial);
            targetSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
        rootSerialized.ApplyModifiedPropertiesWithoutUndo();

        CreateUiPrefabs();
        EditorSceneManager.SaveScene(scene);
    }

    private static ScoreboardView EnsureScoreboardView()
    {
        ScoreboardView existing = UnityEngine.Object.FindFirstObjectByType<ScoreboardView>();
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Gameplay UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject panel = new GameObject("Scoreboard HUD", typeof(RectTransform), typeof(Image), typeof(ScoreboardView));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-32f, 32f);
        panelRect.sizeDelta = new Vector2(360f, 150f);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        GameObject textObject = new GameObject("Objectives", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.LowerRight;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        ScoreboardView view = panel.GetComponent<ScoreboardView>();
        SerializedObject serialized = new SerializedObject(view);
        SetReference(serialized, "text", text);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static void CreateUiPrefabs()
    {
        EnsureControllerPrefab<InventoryStatusUI>("Inventory HUD", Root + "/Prefabs/UI/Hud/InventoryHud.prefab");
        EnsureControllerPrefab<InGameOptionMenu>("In-Game Options", Root + "/Prefabs/UI/Options/InGameOptions.prefab");
        EnsureControllerPrefab<CandyRewardWindowManager>("Candy Rewards", Root + "/Prefabs/UI/Rewards/CandyRewards.prefab");
        EnsureControllerPrefab<GameResultScreenManager>("Game Results", Root + "/Prefabs/UI/Results/GameResults.prefab");
    }

    private static void EnsureControllerPrefab<T>(string displayName, string assetPath) where T : Component
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing != null && existing.GetComponent<T>() != null && existing.GetComponent<GameSessionRoot>() == null)
        {
            return;
        }

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        GameObject root = new GameObject(displayName, typeof(RectTransform));
        root.AddComponent<T>();
        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static T GetOrAdd<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(owner);
    }

    private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void Move(string source, string destination)
    {
        if ((!System.IO.File.Exists(source) && !System.IO.Directory.Exists(source)) ||
            System.IO.File.Exists(destination) || System.IO.Directory.Exists(destination))
        {
            return;
        }

        EnsureFolder(System.IO.Path.GetDirectoryName(destination)?.Replace('\\', '/'));
        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("[ProjectStructureMigration] Could not move " + source + ": " + error);
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || System.IO.Directory.Exists(folder))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
}
