using UnityEngine;
using UnityEngine.Serialization;

public class GetSceneScript : MonoBehaviour
{
    [Header("シーン")]
    [SerializeField, SceneAssetPath, FormerlySerializedAs("gameSceneName")]
    private string gameScenePath;
    [SerializeField, SceneAssetPath, FormerlySerializedAs("mainSceneName")]
    private string mainScenePath;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void callGameScene()
    {
        SceneController.Instance.MoveScene(gameScenePath);
    }
    public void callMainScene()
    {
        SceneController.Instance.MoveScene(mainScenePath);
    }
}
