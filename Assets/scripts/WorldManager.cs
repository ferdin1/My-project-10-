using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public class WorldData
{
    public string worldName;
    public int seed = 12345;
    public Vector3 playerSpawnPosition;
}

public class WorldManager : MonoBehaviour
{
    private static WorldManager instance;
    public static WorldManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<WorldManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("WorldManager");
                    instance = go.AddComponent<WorldManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    public WorldData ActiveWorld { get; private set; }

    public WorldData LoadWorld(string worldName)
    {
        string folder = Path.Combine(Application.persistentDataPath, "Worlds");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filePath = Path.Combine(folder, $"{worldName}.json");

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            ActiveWorld = JsonUtility.FromJson<WorldData>(json);
            Debug.Log($"[World] Wereld JSON geladen uit: {filePath}");
        }
        else
        {
            ActiveWorld = new WorldData
            {
                worldName = worldName,
                seed = UnityEngine.Random.Range(1000, 9999),
                playerSpawnPosition = Vector3.zero
            };

            string json = JsonUtility.ToJson(ActiveWorld, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[World] Nieuw wereld JSON bestand aangemaakt op: {filePath}");
        }

        return ActiveWorld;
    }
}
