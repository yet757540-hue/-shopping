using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// タイトル画面用のエディターツールです。
// 素材のインポート設定、MainMenu シーンへの接続、構図のプレビュー出力を行います。
public static class StartMenuSetup
{
    private const string ArtFolder = "Assets/_Project/Art/UI/Menu";
    private const string MenuScenePath = "Assets/_Project/Scenes/Menu/MainMenu.unity";
    private const string PreviewPath = "Logs/start-menu-preview.png";
    private const string PreviewOptionsPath = "Logs/start-menu-preview-options.png";
    private const string ReportPath = "Logs/start-menu-report.txt";
    private const int ReferenceWidth = 1920;
    private const int ReferenceHeight = 1080;

    [MenuItem("Tools/Start Screen/Configure Art Importers")]
    public static void ConfigureArtImporters()
    {
        ConfigureTexture(ArtFolder + "/TitleLogo.png");
        ConfigureTexture(ArtFolder + "/Character.png");
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Start Screen/Wire Art Into MainMenu Scene")]
    public static void AssignArtToScene()
    {
        ConfigureArtImporters();

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        StartMenuManager manager = Object.FindAnyObjectByType<StartMenuManager>();

        if (manager == null)
        {
            Debug.LogError("[StartMenuSetup] StartMenuManager was not found in " + MenuScenePath);
            return;
        }

        ApplyArtTo(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StartMenuSetup] Start screen art wired into " + MenuScenePath);
    }

    [MenuItem("Tools/Start Screen/Render Preview")]
    public static void RenderPreviewFromCurrentScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        StartMenuManager manager = Object.FindAnyObjectByType<StartMenuManager>();

        if (manager == null)
        {
            Debug.LogError("[StartMenuSetup] StartMenuManager was not found in " + MenuScenePath);
            return;
        }

        RenderMenu(manager);
        manager.ClearStartScreenUI();
    }

    // バッチ実行用の入口です。インポート設定・シーン接続・プレビュー出力をまとめて行います。
    public static void RunBatchSetup()
    {
        AssignArtToScene();
        RenderPreviewIsolated();
    }

    private static void ApplyArtTo(StartMenuManager manager)
    {
        Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/TitleLogo.png");
        Texture2D character = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/Character.png");

        if (logo == null || character == null)
        {
            Debug.LogError("[StartMenuSetup] Start screen art is missing from " + ArtFolder);
            return;
        }

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("useThemedStartScreen").boolValue = true;
        serialized.FindProperty("titleLogoTexture").objectReferenceValue = logo;
        serialized.FindProperty("characterTexture").objectReferenceValue = character;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        manager.ApplyThemeCodeDefaults();
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogWarning("[StartMenuSetup] No texture importer for " + path);
            return;
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Default;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.filterMode = FilterMode.Bilinear;
        settings.npotScale = TextureImporterNPOTScale.None;
        settings.spriteMode = (int)SpriteImportMode.None;
        settings.readable = false;
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // 使い捨てのシーンでタイトル画面を組み立てます。
    // プレビュー用のオブジェクトがプロジェクトのシーンへ書き戻らないようにするためです。
    private static void RenderPreviewIsolated()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject host = new GameObject("StartMenuPreviewHost");
        StartMenuManager manager = host.AddComponent<StartMenuManager>();
        ApplyArtTo(manager);

        RenderMenu(manager);
    }

    private static void RenderMenu(StartMenuManager manager)
    {
        manager.RebuildStartScreen();
        manager.CompleteEntranceAnimations();
        CaptureCanvas(PreviewPath, out string firstMessage);
        Debug.Log("[StartMenuSetup] " + firstMessage);

        manager.OpenOptions();
        Canvas.ForceUpdateCanvases();
        CaptureCanvas(PreviewOptionsPath, out string secondMessage);
        Debug.Log("[StartMenuSetup] " + secondMessage);
    }

    // タイトル画面の Canvas を RenderTexture へ描き、PNG として保存します。
    // エディットモードと Play Mode のどちらでも使えます。
    public static bool CaptureCanvas(string outputPath, out string message)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            message = "The start screen did not create a canvas.";
            Debug.LogError("[StartMenuSetup] " + message);
            return false;
        }

        RenderTexture renderTexture = new RenderTexture(ReferenceWidth, ReferenceHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };
        renderTexture.Create();

        GameObject cameraObject = new GameObject("Preview Camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = ReferenceHeight * 0.5f / 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 100f;
        camera.targetTexture = renderTexture;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        RenderMode previousMode = canvas.renderMode;
        Camera previousCamera = canvas.worldCamera;
        float previousPlaneDistance = canvas.planeDistance;
        int previousSortingOrder = canvas.sortingOrder;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 0;

        Canvas.ForceUpdateCanvases();
        WriteReport(canvas, camera, outputPath);

        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D preview = new Texture2D(ReferenceWidth, ReferenceHeight, TextureFormat.RGBA32, false);
        preview.ReadPixels(new Rect(0f, 0f, ReferenceWidth, ReferenceHeight), 0, 0);
        preview.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
        File.WriteAllBytes(outputPath, preview.EncodeToPNG());

        RenderTexture.active = previous;
        camera.targetTexture = null;
        renderTexture.Release();
        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(cameraObject);

        canvas.sortingOrder = previousSortingOrder;
        canvas.planeDistance = previousPlaneDistance;
        canvas.worldCamera = previousCamera;
        canvas.renderMode = previousMode;

        message = "Start screen preview written to " + Path.GetFullPath(outputPath);
        return true;
    }

    private static void WriteReport(Canvas canvas, Camera camera, string outputPath)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Start screen layout report (" + ReferenceWidth + "x" + ReferenceHeight + " reference space)");
        builder.AppendLine("scaleFactor: " + canvas.scaleFactor);
        builder.AppendLine("capture: " + outputPath);
        builder.AppendLine();

        List<RectTransform> rects = new List<RectTransform>();
        Collect(canvas.transform as RectTransform, rects, 0);

        Vector3[] corners = new Vector3[4];

        foreach (RectTransform rect in rects)
        {
            rect.GetWorldCorners(corners);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 screen = camera.WorldToScreenPoint(corners[i]);
                minX = Mathf.Min(minX, screen.x);
                maxX = Mathf.Max(maxX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxY = Mathf.Max(maxY, screen.y);
            }

            builder.AppendLine(string.Format(
                "{0,-34} x {1,7:0.0} .. {2,7:0.0}   y {3,7:0.0} .. {4,7:0.0}   rot {5,6:0.0}   scale {6:0.00}",
                rect.name,
                minX,
                maxX,
                minY,
                maxY,
                rect.eulerAngles.z,
                rect.lossyScale.x
            ));
        }

        string reportPath = outputPath == PreviewPath ? ReportPath : ReportPath.Replace(".txt", "-options.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
        File.WriteAllText(reportPath, builder.ToString());
        Debug.Log(builder.ToString());
    }

    private static void Collect(RectTransform rect, List<RectTransform> results, int depth)
    {
        if (rect == null || depth > 4)
        {
            return;
        }

        results.Add(rect);

        for (int i = 0; i < rect.childCount; i++)
        {
            Collect(rect.GetChild(i) as RectTransform, results, depth + 1);
        }
    }
}
