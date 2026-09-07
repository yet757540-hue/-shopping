using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Build gate for explicit gameplay-scene composition.</summary>
public sealed class GameSessionBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    [MenuItem("Tools/Shopping/Validate Gameplay Session")]
    public static void ValidateFromMenu()
    {
        ValidateBuildScenes();
        Debug.Log("[GameSessionBuildValidator] Gameplay session validation passed.");
    }

    public void OnPreprocessBuild(BuildReport _)
    {
        ValidateBuildScenes();
    }

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
            GameSessionRoot root = Object.FindFirstObjectByType<GameSessionRoot>();

            // Menu-only scenes do not own gameplay services.
            if (root == null)
            {
                continue;
            }

            if (!root.ValidateReferences(false))
            {
                throw new BuildFailedException("[GameSessionBuildValidator] Invalid GameSessionRoot in " + scene.path);
            }

            if (Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length != 1)
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
