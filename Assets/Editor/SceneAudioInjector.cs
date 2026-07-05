using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneAudioInjector
{
    [MenuItem("Tools/Scene Audio/Inject SceneAudioConfig Into All Scenes")]
    public static void InjectIntoAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot run while in Play Mode.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] {"Assets/Scenes"});
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            bool added = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                var cfg = root.GetComponentInChildren<SceneAudioConfig>(true);
                if (cfg != null) { added = true; break; }
            }

            if (!added)
            {
                var go = new GameObject("SceneAudio");
                var cfg = go.AddComponent<SceneAudioConfig>();
                cfg.loop = true;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Injected SceneAudio into {path}");
            }
            else
            {
                Debug.Log($"Scene {path} already has SceneAudioConfig");
            }
        }

        AssetDatabase.Refresh();
    }
}
