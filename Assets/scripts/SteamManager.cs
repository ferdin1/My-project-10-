using System;
using Steamworks;
using UnityEngine;
using System.Linq;
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    [Tooltip("Uit = geen Steam, gebruikt UnityTransport met IP (handig voor testen).")]
    [SerializeField] private bool useSteam = true;

    [Tooltip("480 = Spacewar, Valve's publieke test-app.")]
    [SerializeField] private uint appId = 480;

    /// <summary>Of Steam gebruikt wordt. -nosteam op de commandline forceert uit.</summary>
    public bool UseSteam { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UseSteam = useSteam && !Environment.GetCommandLineArgs().Contains("-nosteam");

        if (!UseSteam)
        {
            Debug.Log("[Steam] UIT — we draaien zonder Steam (UnityTransport).");
            return;
        }

        try
        {
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
        if (UseSteam && SteamClient.IsValid)
            SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (UseSteam) SteamClient.Shutdown();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (UseSteam) SteamClient.Shutdown();
    }

    public void EnsureInitialized()
    {
        if (!UseSteam || SteamClient.IsValid) return;

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