using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>ビルド前に、ゲームシーンの必須参照と Canvas 数を検証します。</summary>
public sealed class GameSessionBuildValidator : IPreprocessBuildWithReport
{
    // 他の一般的なビルド処理より先に検証するための実行順を返します。
    public int callbackOrder => -1000;

    // ツールメニューからビルド対象シーンを検証し、成功時に結果を表示します。
    [MenuItem("Tools/Shopping/Validate Gameplay Session")]
    public static void ValidateFromMenu()
    {
        ValidateBuildScenes();
        Debug.Log("[GameSessionBuildValidator] Gameplay session validation passed.");
    }

    // ビルド開始前に同じシーン検証を実行し、不備があればビルドを止めます。
    public void OnPreprocessBuild(BuildReport _)
    {
        ValidateBuildScenes();
    }

    // 有効なビルド対象を開いて参照と Canvas 数を検証し、正常終了時は元のシーンを開き直します。
    private static void ValidateBuildScenes()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            GameSessionRoot root = Object.FindAnyObjectByType<GameSessionRoot>();

            // タイトル専用シーンにはゲーム進行サービスを要求しません。
            if (root == null)
            {
                continue;
            }

            if (!root.ValidateReferences(false))
            {
                throw new BuildFailedException("[GameSessionBuildValidator] Invalid GameSessionRoot in " + scene.path);
            }

            if (Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Length != 1)
            {
                throw new BuildFailedException("[GameSessionBuildValidator] Gameplay scene must contain exactly one authored Canvas: " + scene.path);
            }
        }

        if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
        }
    }
}
