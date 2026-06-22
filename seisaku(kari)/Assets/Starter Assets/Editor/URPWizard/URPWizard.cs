using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
#if USE_URP
using UnityEngine.Rendering.Universal;
#endif

// URP が未設定のプロジェクトへ、必要な URP パッケージと設定を補助するエディタウィンドウです。
public class URPWizard : EditorWindow
{
    [InitializeOnLoadMethod]
    static void OnInitialize()
    {
        // エディタ読み込み時に URP 設定を確認します。
        URPCheck();
    }

    static void URPCheck()
    {
        // すでに Render Pipeline が設定されていれば何もしません。
        if (GraphicsSettings.currentRenderPipeline != null) 
            return;

        // Package Manager から URP パッケージの有無を確認します。
        var request = Client.List();
        while (!request.IsCompleted) { }

        if (request.Status != StatusCode.Success) 
            return;
        
        if (request.Result.All(info => info.name != "com.unity.render-pipelines.universal"))
        {
            // URP パッケージがなければ追加を試みます。
            var addRequest = Client.Add("com.unity.render-pipelines.universal");
            
            while (!addRequest.IsCompleted) { }
                    
            Client.Resolve();
        }
        else
        {
            FindAndAssignPipeline();
        }
    }

#if USE_URP
    static void FindAndAssignPipeline()
    {
        // プロジェクト内の URP Asset を探して GraphicsSettings に割り当てます。
        var existingPipelines = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");

        if (existingPipelines.Length == 0)
        {
            Debug.LogError($"Universal Render Pipeline Asset was not found.\n" +
                           $"Please create one and assign under the Project Settings > Graphics > Scriptable Render Pipeline Settings.");
            return;
        }
        
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(existingPipelines[0]));
        GraphicsSettings.defaultRenderPipeline = pipeline;
    }
    
    class PipelineAssetProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            // Import 後に URP Asset が見つかるようになった場合、自動で割り当てます。
            //if we have no pipeline set, we try to find one as one may have been imported
            if (GraphicsSettings.currentRenderPipeline != null) 
                return;
            
            FindAndAssignPipeline();
        }
    }
#else
    static void FindAndAssignPipeline(){}
#endif
}
