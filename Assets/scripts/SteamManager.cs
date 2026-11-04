using System;
using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    [Tooltip("480 = Spacewar, Valve's publieke test-app.")]
    [SerializeField] private uint appId = 480;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            // false = async callbacks uit. Wij pompen ze zelf in Update().
            // Dat moet, want Facepunch-callbacks mogen alleen op de Unity-hoofdthread.
            if (!SteamClient.IsValid)
                SteamClient.Init(appId, false);

            Debug.Log($"[Steam] Ingelogd als {SteamClient.Name} — SteamID {SteamClient.SteamId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steam] Init mislukt: {e.Message}\n" +
                           "Draait Steam? Ben je ingelogd? Staat steam_appid.txt in de projectroot?");
        }
    }

    private void Update()
    {
        if (SteamClient.IsValid)
            SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        SteamClient.Shutdown();
    }

    /// <summary>
    /// De transport sloopt bij NetworkManager.Shutdown() de hele SteamClient.
    /// Hiermee zet je hem weer aan zodat lobby-zoeken daarna blijft werken.
    /// </summary>
    public void EnsureInitialized()
    {
        if (SteamClient.IsValid) return;

        try
        {
            SteamClient.Init(appId, false);
            Debug.Log("[Steam] Opnieuw geïnitialiseerd na netwerk-shutdown.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steam] Her-init mislukt: {e.Message}");
        }
    }
}