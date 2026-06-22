using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StarterAssets
{
    // 三人称 Starter Assets のプレイヤーとカメラを再配置するエディタメニューです。
    public partial class StarterAssetsDeployMenu : ScriptableObject
    {
        // prefab paths
        private const string PlayerArmaturePrefabName = "PlayerArmature";

        /// <summary>
        /// Check the Armature, main camera, cinemachine virtual camera, camera target and references
        /// </summary>
        [MenuItem(MenuRoot + "/Reset Third Person Controller Armature", false)]
        static void ResetThirdPersonControllerArmature()
        {
            // Animator 付きの三人称プレイヤーを探し、なければプレハブから生成します。
            var thirdPersonControllers = FindObjectsByType<ThirdPersonController>();
            var player = thirdPersonControllers.FirstOrDefault(controller =>
                controller.GetComponent<Animator>() && controller.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            // player
            if (player == null)
            {
                if (TryLocatePrefab(PlayerArmaturePrefabName, null, new[] { typeof(ThirdPersonController), typeof(StarterAssetsInputs) }, out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Couldn't find player armature prefab");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
            {
                // cameras
                CheckCameras(playerGameObject.transform, GetThirdPersonPrefabPath());
            }
        }

        [MenuItem(MenuRoot + "/Reset Third Person Controller Capsule", false)]
        static void ResetThirdPersonControllerCapsule()
        {
            // カプセル版の三人称プレイヤーを探し、なければプレハブから生成します。
            var thirdPersonControllers = FindObjectsByType<ThirdPersonController>();
            var player = thirdPersonControllers.FirstOrDefault(controller =>
                !controller.GetComponent<Animator>() && controller.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            // player
            if (player == null)
            {
                if (TryLocatePrefab(PlayerCapsulePrefabName, null, new[] { typeof(ThirdPersonController), typeof(StarterAssetsInputs) }, out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Couldn't find player capsule prefab");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
            {
                // cameras
                CheckCameras(playerGameObject.transform, GetThirdPersonPrefabPath());
            }
        }

        static string GetThirdPersonPrefabPath()
        {
            // Armature プレハブ位置から三人称用プレハブフォルダを推定します。
            if (TryLocatePrefab(PlayerArmaturePrefabName, null, new[] { typeof(ThirdPersonController), typeof(StarterAssetsInputs) }, out GameObject _, out string prefabPath))
            {
                var pathString = new StringBuilder();
                var currentDirectory = new FileInfo(prefabPath).Directory;
                while (currentDirectory.Name != "Packages")
                {
                    pathString.Insert(0, $"/{currentDirectory.Name}");
                    currentDirectory = currentDirectory.Parent;
                }

                pathString.Insert(0, currentDirectory.Name);
                return pathString.ToString();
            }

            return null;
        }
    }
}
