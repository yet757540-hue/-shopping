using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

// Play Mode でタイトル画面を動かし、次の 3 点を確認します。
// フィルムが流れること、タイトルと小 UI が右から飛び込むこと、UI がクリックに反応すること。
// Play Mode 開始時はドメインが再読み込みされるため、InitializeOnLoad から処理を再接続します。
[InitializeOnLoad]
public static class StartMenuPlayModeSmokeTest
{
    private const string MenuScenePath = "Assets/_Project/Scenes/Menu/MainMenu.unity";
    private const string ReportPath = "Logs/start-menu-smoke-test.txt";
    private const string PlayModeScreenshotPath = "Logs/start-menu-playmode.png";
    private const string EntranceScreenshotPath = "Logs/start-menu-entrance.png";
    private const string PendingKey = "Shopping.StartMenuSmokeTest.Pending";

    private static readonly List<string> Failures = new List<string>();
    private static readonly List<string> Notes = new List<string>();
    private static readonly List<string> Errors = new List<string>();

    private static Stopwatch stopwatch;
    private static EditorApplication.CallbackFunction ticker;
    private static float firstUv;
    private static float secondUv;
    private static bool finished;
    private static bool startClicked;
    private static float startClickTime;
    private static int menuPhaseErrorCount;

    static StartMenuPlayModeSmokeTest()
    {
        if (SessionState.GetBool(PendingKey, false))
        {
            EditorApplication.delayCall += ResumeAfterPlayModeReload;
        }
    }

    [MenuItem("Tools/Start Screen/Run Play Mode Smoke Test")]
    public static void Run()
    {
        SessionState.SetBool(PendingKey, true);
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void ResumeAfterPlayModeReload()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.delayCall += ResumeAfterPlayModeReload;
            return;
        }

        Failures.Clear();
        Notes.Clear();
        Errors.Clear();
        finished = false;
        firstUv = 0f;
        secondUv = 0f;
        startClicked = false;
        menuPhaseErrorCount = 0;
        stopwatch = Stopwatch.StartNew();

        Application.logMessageReceived += OnLog;

        ticker = Tick;
        EditorApplication.update += ticker;
        Notes.Add("play mode started");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            Errors.Add(type + ": " + condition + "\n" + stackTrace);
        }
    }

    private static void Tick()
    {
        if (finished)
        {
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        float elapsed = (float)stopwatch.Elapsed.TotalSeconds;

        if (elapsed < 0.4f)
        {
            return;
        }

        if (!firstSampleTaken && elapsed < 0.8f)
        {
            firstUv = SampleFilmStripUv();
            firstSampleTaken = true;
            Notes.Add("film strip uv sample A: " + firstUv.ToString("0.0000"));
            StartMenuSetup.CaptureCanvas(EntranceScreenshotPath, out string captureMessage);
            Notes.Add("entrance capture: " + captureMessage);
            return;
        }

        if (!secondSampleTaken && elapsed < 1.2f)
        {
            secondUv = SampleFilmStripUv();
            secondSampleTaken = true;
            Notes.Add("film strip uv sample B: " + secondUv.ToString("0.0000"));
            return;
        }

        if (elapsed < 2.6f)
        {
            return;
        }

        if (!startClicked)
        {
            Evaluate();

            // ここまではタイトル画面での記録です。以降のログは「開始」で読み込む
            // ゲームシーンのものなので、タイトル画面の失敗としては扱いません。
            menuPhaseErrorCount = Errors.Count;
            EvaluateStartClick();
            startClicked = true;
            startClickTime = elapsed;
            return;
        }

        if (elapsed < startClickTime + 4f)
        {
            return;
        }

        VerifyStartLoadedGameScene();
        Finish();
    }

    private static bool firstSampleTaken;
    private static bool secondSampleTaken;

    private static void Evaluate()
    {
        StartMenuManager manager = UnityEngine.Object.FindAnyObjectByType<StartMenuManager>();

        if (manager == null)
        {
            Failures.Add("StartMenuManager was not found in the MainMenu scene.");
            return;
        }

        if (Mathf.Approximately(firstUv, secondUv))
        {
            Failures.Add(string.Format("Film strip is not rolling (uv stayed at {0:0.0000}).", firstUv));
        }
        else
        {
            Notes.Add("film strip rolls (uv moved " + (secondUv - firstUv).ToString("0.0000") + ")");
        }

        MenuRushInAnimator[] animators = UnityEngine.Object.FindObjectsByType<MenuRushInAnimator>(FindObjectsInactive.Exclude);
        int stillPlaying = 0;

        foreach (MenuRushInAnimator animator in animators)
        {
            if (animator.IsPlaying)
            {
                stillPlaying++;
            }
        }

        Notes.Add(animators.Length + " rush-in elements, " + stillPlaying + " still animating");

        if (animators.Length < 5)
        {
            Failures.Add("Expected the title, character and three carts to have rush-in animators, found " + animators.Length + ".");
        }

        if (stillPlaying > 0)
        {
            Failures.Add(stillPlaying + " entrance animations never settled.");
        }

        if (manager.ActiveMenuView != null)
        {
            Failures.Add("The themed start screen should not use the legacy prefab view.");
        }

        if (!Application.CanStreamedLevelBeLoaded("ShoppingGameplay"))
        {
            Failures.Add("The 開始 cart scene 'ShoppingGameplay' is not available in Build Settings.");
        }
        else
        {
            Notes.Add("the 開始 cart target scene ShoppingGameplay is in Build Settings");
        }

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            Failures.Add("No canvas was created for the start screen.");
            return;
        }

        if (canvas.transform.Find("Film Strip Top Left") == null || canvas.transform.Find("Film Strip Bottom Right") == null)
        {
            Failures.Add("The two film strips were not created.");
        }

        if (canvas.transform.Find("Title Logo") == null || canvas.transform.Find("Hero Character") == null)
        {
            Failures.Add("The title logo or hero character layer is missing.");
        }

        MenuCartButton[] carts = UnityEngine.Object.FindObjectsByType<MenuCartButton>(FindObjectsInactive.Exclude);

        if (carts.Length != 3)
        {
            Failures.Add("Expected three cart buttons, found " + carts.Length + ".");
            return;
        }

        MenuCartButton optionCart = FindCart(carts, "設定");

        if (optionCart == null)
        {
            Failures.Add("The 設定 cart button was not found.");
            return;
        }

        // ポインターを重ねると、そのカートへ選択が移ります。
        Vector2 optionCartPoint = GetScreenPoint(optionCart.Root);
        manager.ProcessPointerInput(optionCartPoint, true, false, false);

        if (manager.SelectedMenuIndex != 1)
        {
            Failures.Add("Pointer hover did not select the second cart (index " + manager.SelectedMenuIndex + ").");
        }
        else
        {
            Notes.Add("pointer hover selected the 設定 cart");
        }

        if (!optionCart.IsSelected)
        {
            Failures.Add("The hovered cart did not switch to its selected visual.");
        }

        // 同じカートで押して離すと、オプションが開きます。
        manager.ProcessPointerInput(optionCartPoint, false, true, false);
        manager.ProcessPointerInput(optionCartPoint, false, false, true);

        if (!manager.IsOptionOpen)
        {
            Failures.Add("Clicking the 設定 cart did not open the option window.");
        }
        else
        {
            Notes.Add("clicking the 設定 cart opened the option window");
        }

        // 音量の行をクリックすると、その位置の値へ移動します。
        RectTransform volumeRow = FindRect(canvas.transform, "VOLUME Row");

        if (volumeRow == null)
        {
            Failures.Add("The VOLUME row was not found in the option window.");
        }
        else
        {
            float before = AudioListener.volume;
            Vector2 rowPoint = GetScreenPoint(volumeRow);
            manager.ProcessPointerInput(rowPoint, true, true, false);
            manager.ProcessPointerInput(rowPoint, false, false, true);
            float after = AudioListener.volume;

            if (Mathf.Abs(before - after) < 0.05f)
            {
                Failures.Add(string.Format("Clicking the volume row did not change the value ({0:0.00} -> {1:0.00}).", before, after));
            }
            else
            {
                Notes.Add(string.Format("volume row click changed the slider ({0:0.00} -> {1:0.00})", before, after));
            }
        }

        // 枠の外をクリックすると、オプションが閉じます。
        manager.ProcessPointerInput(new Vector2(60f, 60f), true, true, false);
        manager.ProcessPointerInput(new Vector2(60f, 60f), false, false, true);

        if (manager.IsOptionOpen)
        {
            Failures.Add("Clicking outside the option window did not close it.");
        }
        else
        {
            Notes.Add("clicking outside closed the option window");
        }

        CapturePlayModeScreenshot();
    }

    // 「開始」のカートをポインターでクリックし、ゲームシーンが読み込まれるか確認します。
    private static void EvaluateStartClick()
    {
        StartMenuManager manager = UnityEngine.Object.FindAnyObjectByType<StartMenuManager>();
        MenuCartButton[] carts = UnityEngine.Object.FindObjectsByType<MenuCartButton>(FindObjectsInactive.Exclude);
        MenuCartButton startCart = FindCart(carts, "開始");

        if (manager == null || startCart == null)
        {
            Failures.Add("The 開始 cart could not be clicked because it was not found.");
            return;
        }

        Vector2 point = GetScreenPoint(startCart.Root);
        manager.ProcessPointerInput(point, true, true, false);
        manager.ProcessPointerInput(point, false, false, true);
        Notes.Add("clicked the 開始 cart at " + point.ToString("0"));
    }

    private static void VerifyStartLoadedGameScene()
    {
        UnityEngine.SceneManagement.Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        if (active.name != "ShoppingGameplay")
        {
            Failures.Add("Clicking the 開始 cart should load ShoppingGameplay but the active scene is '" + active.name + "'.");
        }
        else
        {
            Notes.Add("clicking the 開始 cart loaded the ShoppingGameplay scene");
        }
    }

    private static void Finish()
    {
        finished = true;
        EditorApplication.update -= ticker;
        Application.logMessageReceived -= OnLog;
        SessionState.SetBool(PendingKey, false);

        StringBuilder builder = new StringBuilder();
        int menuErrorCount = Mathf.Min(menuPhaseErrorCount, Errors.Count);
        bool passed = Failures.Count == 0 && menuErrorCount == 0;

        builder.AppendLine("Start screen play mode smoke test");
        builder.AppendLine("result: " + (passed ? "PASS" : "FAIL"));
        builder.AppendLine();
        builder.AppendLine("checks:");

        foreach (string note in Notes)
        {
            builder.AppendLine("  ok   " + note);
        }

        foreach (string failure in Failures)
        {
            builder.AppendLine("  FAIL " + failure);
        }

        for (int i = 0; i < menuErrorCount; i++)
        {
            builder.AppendLine("  FAIL log " + Errors[i].Replace("\n", "\n           "));
        }

        for (int i = menuErrorCount; i < Errors.Count; i++)
        {
            builder.AppendLine("  note log " + Errors[i].Replace("\n", "\n           "));
        }

        string report = builder.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)));
        File.WriteAllText(ReportPath, report);
        Debug.Log(report);

        EditorApplication.isPlaying = false;
        EditorApplication.Exit(passed ? 0 : 1);
    }

    private static float SampleFilmStripUv()
    {
        FilmStripScroller scroller = UnityEngine.Object.FindAnyObjectByType<FilmStripScroller>();

        if (scroller == null)
        {
            return 0f;
        }

        RawImage image = scroller.GetComponent<RawImage>();
        return image == null ? 0f : image.uvRect.x;
    }

    private static Vector2 GetScreenPoint(RectTransform rect)
    {
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }

    private static RectTransform FindRect(Transform root, string name)
    {
        RectTransform direct = root.Find(name) as RectTransform;

        if (direct != null)
        {
            return direct;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform nested = FindRect(root.GetChild(i), name);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static MenuCartButton FindCart(MenuCartButton[] carts, string label)
    {
        foreach (MenuCartButton cart in carts)
        {
            if (cart != null && cart.gameObject.name.StartsWith(label))
            {
                return cart;
            }
        }

        return null;
    }

    // 実際の Play Mode の見た目を確認できるように画面を保存します。
    private static void CapturePlayModeScreenshot()
    {
        if (StartMenuSetup.CaptureCanvas(PlayModeScreenshotPath, out string message))
        {
            Notes.Add("play mode capture: " + PlayModeScreenshotPath);
        }
        else
        {
            Notes.Add("play mode capture failed: " + message);
        }
    }
}
