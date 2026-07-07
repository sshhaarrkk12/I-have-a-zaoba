using UnityEngine;
using UnityEngine.SceneManagement;

public class StatsHUDBootstrapper : MonoBehaviour
{
    const string HudPrefabPath = "Scenes/StatsHUD";

    static StatsHUDBootstrapper instance;
    StatsHUD hud;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureBootstrapper();
    }

    static void EnsureBootstrapper()
    {
        if (instance != null) return;

        var go = new GameObject("StatsHUDBootstrapper");
        instance = go.AddComponent<StatsHUDBootstrapper>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        EnsureSingleHud();
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSingleHud();
    }

    void EnsureSingleHud()
    {
        StatsHUD[] existingHuds = FindObjectsOfType<StatsHUD>(true);

        if (hud == null)
        {
            hud = PickExistingHud(existingHuds);
            if (hud == null)
                hud = CreateHud();

            MakePersistent(hud);
        }

        foreach (var candidate in existingHuds)
        {
            if (candidate == null || candidate == hud) continue;
            Destroy(candidate.gameObject);
        }

        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            MakePersistent(hud);
        }
    }

    StatsHUD PickExistingHud(StatsHUD[] existingHuds)
    {
        if (existingHuds == null || existingHuds.Length == 0)
            return null;

        for (int i = 0; i < existingHuds.Length; i++)
        {
            if (existingHuds[i] != null && existingHuds[i].gameObject.scene.name == "DontDestroyOnLoad")
                return existingHuds[i];
        }

        return existingHuds[0];
    }

    StatsHUD CreateHud()
    {
        GameObject prefab = Resources.Load<GameObject>(HudPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[StatsHUD] Missing prefab at Resources/{HudPrefabPath}");
            return null;
        }

        GameObject go = Instantiate(prefab);
        go.name = "StatsHUD";
        return go.GetComponent<StatsHUD>();
    }

    void MakePersistent(StatsHUD target)
    {
        if (target == null) return;

        if (target.transform.parent != null && target.transform.parent.name == "StatsHUDScreenCanvas")
        {
            DontDestroyOnLoad(target.transform.parent.gameObject);
            return;
        }

        target.transform.SetParent(null, false);
        DontDestroyOnLoad(target.gameObject);
    }
}
