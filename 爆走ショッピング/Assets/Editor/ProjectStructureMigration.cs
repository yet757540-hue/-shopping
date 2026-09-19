using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 旧フォルダー構成から移行するための、手動実行専用ツールです。
/// GUID を保つ AssetDatabase 操作を使うため、既存のシリアライズ参照を維持します。
/// </summary>
public static class ProjectStructureMigration
{
    private const string Root = "Assets/_Project";
    private const string LegacyScriptFolder = "Assets/Script";
    private const string LegacySettingsFolder = "Assets/Settings";
    private const string MainMenuScene = Root + "/Scenes/Menu/MainMenu.unity";
    private const string GameplayScene = Root + "/Scenes/Gameplay/ShoppingGameplay.unity";

    // Play Mode 中の実行を防ぎ、フォルダー整理・移動・シーン設定を順に行います。
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
        // 移動処理が途中で失敗しても、アセット一覧の更新は実行します。
        finally
        {
            AssetDatabase.Refresh();
        }

        RemoveEmptyGeneratedDuplicateFolders();
        RemoveEmptyLegacyFolders();
        UpdateScenesAndBuildSettings();
        Debug.Log("[ProjectStructureMigration] Project structure migration completed.");
    }

    // 移行先として必要なプロジェクト所有フォルダーを用意します。
    private static void CreateFolders()
    {
        string[] folders =
        {
            Root,
            Root + "/Runtime/Bootstrap", Root + "/Runtime/Flow",
            Root + "/Runtime/Gameplay/Player", Root + "/Runtime/Gameplay/Scoring",
            Root + "/Runtime/Gameplay/Timer",
            Root + "/Runtime/Presentation/UI/Hud", Root + "/Runtime/Presentation/UI/Options",
            Root + "/Runtime/Presentation/UI/Results", Root + "/Runtime/Presentation/UI/Shared",
            Root + "/Runtime/Presentation/Menu", Root + "/Runtime/Presentation/Camera",
            Root + "/Runtime/Presentation/Feedback", Root + "/Scenes/Menu",
            Root + "/Scenes/Gameplay", Root + "/Scenes/Tests", Root + "/Scenes/Archive",
            Root + "/Prefabs/Gameplay/Player", Root + "/Prefabs/Gameplay/Environment",
            Root + "/Prefabs/System/Camera", Root + "/Prefabs/UI/Hud",
            Root + "/Prefabs/UI/Options", Root + "/Prefabs/UI/Results",
            Root + "/Input", Root + "/Art/Models/Player",
            Root + "/Art/Models/Environment", Root + "/Art/Materials/World",
            Root + "/Art/Materials/Gameplay", Root + "/Physics/Materials",
            Root + "/Audio/SFX/Collision", Root + "/Rendering/Shaders", Root + "/Settings/Rendering"
        };

        foreach (string folder in folders)
        {
            EnsureFolder(folder);
        }
    }

    // 番号付きの重複フォルダー候補を調べ、メタデータ以外のファイルがないものだけ削除します。
    private static void RemoveEmptyGeneratedDuplicateFolders()
    {
        foreach (string folder in FindGeneratedDuplicateFolders())
        {
            if (!ContainsOnlyMetaFiles(folder))
            {
                continue;
            }

            AssetDatabase.DeleteAsset(folder.Replace('\\', '/'));
        }
    }

    // 番号付きの重複フォルダーは階層のどの深さにもできるため、Assets 全体を走査します。
    private static List<string> FindGeneratedDuplicateFolders()
    {
        List<string> results = new List<string>();

        if (!System.IO.Directory.Exists("Assets"))
        {
            return results;
        }

        CollectGeneratedDuplicateFolders("Assets", results);
        return results.OrderByDescending(path => path.Length).ToList();
    }

    private static void CollectGeneratedDuplicateFolders(string folder, List<string> results)
    {
        string[] children;

        try
        {
            children = System.IO.Directory.GetDirectories(folder);
        }
        catch (System.IO.IOException)
        {
            return;
        }

        foreach (string child in children)
        {
            string name = System.IO.Path.GetFileName(child);

            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^_Project \d+$") ||
                System.Text.RegularExpressions.Regex.IsMatch(name, @" \d+$"))
            {
                results.Add(child);
                continue;
            }

            CollectGeneratedDuplicateFolders(child, results);
        }
    }

    // フォルダー内にメタデータ以外のファイルが無いかを返します。
    private static bool ContainsOnlyMetaFiles(string folder)
    {
        return !System.IO.Directory.EnumerateFiles(folder, "*", System.IO.SearchOption.AllDirectories)
            .Any(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
    }

    // 旧構成の既知フォルダーを調べ、実データが残っていないものだけ削除します。
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

            if (ContainsOnlyMetaFiles(folder))
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }

    // 旧パスから新パスへの対応表に従ってアセットを移動し、スクリプトと設定も整理します。
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

        // 移行済みなら旧フォルダーは残っていないため、存在するときだけ検索します。
        // 存在しないフォルダーを渡すと FindAssets が警告を出すためです。
        if (!AssetDatabase.IsValidFolder(LegacySettingsFolder))
        {
            return;
        }

        foreach (string asset in AssetDatabase.FindAssets(string.Empty, new[] { LegacySettingsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(asset);
            if (!AssetDatabase.IsValidFolder(path))
            {
                Move(path, Root + "/Settings/Rendering/" + System.IO.Path.GetFileName(path));
            }
        }
    }

    // スクリプトの役割ごとに移行先を指定して移動します。
    private static void MoveScripts()
    {
        MoveScript("GameFlow/GameSessionRoot.cs", "Runtime/Bootstrap/GameSessionRoot.cs");
        MoveScript("GameFlow/GameRestartManager.cs", "Runtime/Flow/GameRestartManager.cs");
        MoveScript("GameFlow/GameTimePauseManager.cs", "Runtime/Flow/GameTimePauseManager.cs");
        MoveScriptDirectory("Player", "Runtime/Gameplay/Player");
        MoveScriptDirectory("Scoring", "Runtime/Gameplay/Scoring");
        MoveScriptDirectory("Timer", "Runtime/Gameplay/Timer");
        MoveScript("Camera/CameraFollowController.cs", "Runtime/Presentation/Camera/CameraFollowController.cs");
        MoveScript("Camera/SpeedFOVController.cs", "Runtime/Presentation/Camera/SpeedFOVController.cs");
        MoveScriptDirectory("Feedback", "Runtime/Presentation/Feedback");
        MoveScript("UI/StartMenuManager.cs", "Runtime/Presentation/Menu/StartMenuManager.cs");
        MoveScript("UI/StartMenuView.cs", "Runtime/Presentation/Menu/StartMenuView.cs");
        MoveScript("UI/InventoryStatusUI.cs", "Runtime/Presentation/UI/Hud/InventoryStatusUI.cs");
        MoveScript("UI/ScoreboardView.cs", "Runtime/Presentation/UI/Hud/ScoreboardView.cs");
        MoveScript("UI/InGameOptionMenu.cs", "Runtime/Presentation/UI/Options/InGameOptionMenu.cs");
        MoveScript("UI/RuntimeOptionMenu.cs", "Runtime/Presentation/UI/Shared/RuntimeOptionMenu.cs");
        MoveScript("UI/JapaneseUIFont.cs", "Runtime/Presentation/UI/Shared/JapaneseUIFont.cs");
        MoveScript("UI/GameResultScreenManager.cs", "Runtime/Presentation/UI/Results/GameResultScreenManager.cs");
    }

    // 指定した旧フォルダー内のスクリプトを探し、名前を保って移行先へ移動します。
    private static void MoveScriptDirectory(string sourceDirectory, string targetDirectory)
    {
        string source = LegacyScriptFolder + "/" + sourceDirectory;

        // 旧スクリプトフォルダーは移行後に消えるため、無い場合は検索しません。
        if (!AssetDatabase.IsValidFolder(source))
        {
            return;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { source }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Move(path, Root + "/" + targetDirectory + "/" + System.IO.Path.GetFileName(path));
        }
    }

    // 旧スクリプトルートと新プロジェクトルートを補って移動処理へ渡します。
    private static void MoveScript(string source, string target)
    {
        Move(LegacyScriptFolder + "/" + source, Root + "/" + target);
    }

    // タイトルとゲームシーンを設定し、ビルド対象の順序を更新してアセットを保存します。
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

    // タイトルシーンを開き、開始先のゲームシーン名を更新して保存します。
    private static void ConfigureMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        StartMenuManager menu = UnityEngine.Object.FindAnyObjectByType<StartMenuManager>();
        if (menu != null)
        {
            SerializedObject serialized = new SerializedObject(menu);
            serialized.FindProperty("gameSceneName").stringValue = "ShoppingGameplay";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(scene);
    }

    // 必須の既存コンポーネントを確認し、不足する補助機能と参照を設定して保存します。
    private static void ConfigureGameplayScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        GameSessionRoot root = UnityEngine.Object.FindAnyObjectByType<GameSessionRoot>();
        PlayerManager player = UnityEngine.Object.FindAnyObjectByType<PlayerManager>();
        TimerManager timer = UnityEngine.Object.FindAnyObjectByType<TimerManager>();
        ScoreboardManager scoreboard = UnityEngine.Object.FindAnyObjectByType<ScoreboardManager>();
        SettlementArea settlement = UnityEngine.Object.FindAnyObjectByType<SettlementArea>();
        CollisionFeedbackManager feedback = UnityEngine.Object.FindAnyObjectByType<CollisionFeedbackManager>();

        if (root == null || player == null || timer == null || scoreboard == null || settlement == null || feedback == null)
        {
            Debug.LogError("[ProjectStructureMigration] Gameplay scene is missing one or more required existing components.");
            return;
        }

        GameObject rootObject = root.gameObject;
        PlayerInventory inventory = GetOrAdd<PlayerInventory>(player.gameObject);
        PlayerCollisionReporter reporter = GetOrAdd<PlayerCollisionReporter>(player.gameObject);
        ImpactSettings impact = GetOrAdd<ImpactSettings>(player.gameObject);
        InventoryInfluenceSettings influence = GetOrAdd<InventoryInfluenceSettings>(player.gameObject);
        GameTimePauseManager pause = GetOrAdd<GameTimePauseManager>(rootObject);
        GameRestartManager restart = GetOrAdd<GameRestartManager>(rootObject);
        InGameOptionMenu options = GetOrAdd<InGameOptionMenu>(rootObject);
        GameResultScreenManager results = GetOrAdd<GameResultScreenManager>(rootObject);
        InventoryStatusUI inventoryHud = GetOrAdd<InventoryStatusUI>(rootObject);
        TimerDisplayUI timerHud = GetOrAdd<TimerDisplayUI>(timer.gameObject);
        Material overlayMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/Gameplay/ScoreTargetVisibleOverlay.mat");
        ScoreboardView scoreboardView = EnsureScoreboardView();

        SerializedObject restartSerialized = new SerializedObject(restart);
        restartSerialized.FindProperty("gameSceneName").stringValue = "ShoppingGameplay";
        restartSerialized.FindProperty("startMenuSceneName").stringValue = "MainMenu";
        restartSerialized.ApplyModifiedPropertiesWithoutUndo();

        // シーンの構成ルートへ接続を集め、実行時の初期化で利用できる状態にします。
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
        SetReference(rootSerialized, "collisionFeedback", feedback);
        SetReference(rootSerialized, "inventoryInfluence", influence);
        SetReference(rootSerialized, "inventoryHud", inventoryHud);
        SetReference(rootSerialized, "timerHud", timerHud);
        SetReference(rootSerialized, "scoreboardView", scoreboardView);
        SetReference(rootSerialized, "optionMenu", options);
        SetReference(rootSerialized, "resultScreen", results);
        // シーン内の目標一覧と、それぞれの透視表示用材質を設定します。
        SerializedProperty targets = rootSerialized.FindProperty("scoreTargets");
        ScoreTarget[] scoreTargets = UnityEngine.Object.FindObjectsByType<ScoreTarget>(FindObjectsInactive.Exclude);
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

    // 既存の目標 HUD を探し、なければ Canvas と文字パネルを作って参照を接続します。
    private static ScoreboardView EnsureScoreboardView()
    {
        ScoreboardView existing = UnityEngine.Object.FindAnyObjectByType<ScoreboardView>();
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
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

    // ゲーム内で使う UI コントローラーの Prefab を用意します。
    private static void CreateUiPrefabs()
    {
        EnsureControllerPrefab<InventoryStatusUI>("Inventory HUD", Root + "/Prefabs/UI/Hud/InventoryHud.prefab");
        EnsureControllerPrefab<InGameOptionMenu>("In-Game Options", Root + "/Prefabs/UI/Options/InGameOptions.prefab");
        EnsureControllerPrefab<GameResultScreenManager>("Game Results", Root + "/Prefabs/UI/Results/GameResults.prefab");
    }

    // 既存 Prefab の構成を確認し、条件を満たさない場合は削除して必要なコントローラーだけで作り直します。
    private static void EnsureControllerPrefab<T>(string displayName, string assetPath) where T : Component
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing != null && existing.GetComponent<T>() != null && existing.GetComponent<GameSessionRoot>() == null)
        {
            return;
        }

        // 構成が条件に合わない既存 Prefab は、手動移行の実行時に置き換えます。
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        GameObject root = new GameObject(displayName, typeof(RectTransform));
        root.AddComponent<T>();
        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    // 指定した型のコンポーネントを取得し、存在しなければ追加します。
    private static T GetOrAdd<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(owner);
    }

    // SerializedObject の指定フィールドへ、オブジェクト参照を設定します。
    private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    // 移動元と移動先を確認し、AssetDatabase で GUID を保ちながら移動します。
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

    // 必要な親フォルダーから順に、存在しないフォルダーを作成します。
    private static void EnsureFolder(string folder)
    {
        folder = folder?.Replace('\\', '/');

        // AssetDatabase 基準で存在確認します。作業ディレクトリに左右されず、
        // 既存フォルダーに対して CreateFolder が「名前 1」を作ることも防げます。
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');

        if (string.IsNullOrEmpty(parent) || parent == folder)
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
}
