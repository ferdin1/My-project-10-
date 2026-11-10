using System;
using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    public const string GameKey    = "mijn_unieke_game_tag_verander_dit";
    public const string GameKeyVal = "1";

    [SerializeField] private ushort port = 7777;

    public Lobby? CurrentLobby { get; private set; }

    // Zonder Steam is er geen lobby; dit vlaggetje vervangt hem.
    private bool openedWithoutSteam;
    public bool IsInLobby => CurrentLobby.HasValue || openedWithoutSteam;

    private bool UseSteam => SteamManager.Instance != null && SteamManager.Instance.UseSteam;

    private FacepunchTransport steamTransport;
    private UnityTransport udpTransport;

    public event Action OnLobbyStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        steamTransport = nm.GetComponent<FacepunchTransport>();
        udpTransport   = nm.GetComponent<UnityTransport>();

        if (!UseSteam) return; // geen Steam-events aanzetten

        SteamMatchmaking.OnLobbyCreated       += HandleLobbyCreated;
        SteamMatchmaking.OnLobbyEntered       += HandleLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined  += HandleLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave   += HandleLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite        += HandleLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested += HandleGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        if (UseSteam)
        {
            SteamMatchmaking.OnLobbyCreated       -= HandleLobbyCreated;
            SteamMatchmaking.OnLobbyEntered       -= HandleLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined  -= HandleLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave   -= HandleLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite        -= HandleLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= HandleGameLobbyJoinRequested;
        }

        if (Instance == this) Instance = null;
    }

    // Kiest de juiste transport. Moet gebeuren vóór StartHost/StartClient.
    private void ApplyTransport()
    {
        NetworkManager.Singleton.NetworkConfig.NetworkTransport =
            UseSteam ? (NetworkTransport)steamTransport : udpTransport;
    }

    // ---------------- Lokaal spelen ----------------

    public void StartLocalGame()
    {
        ApplyTransport();

        // Zonder Steam luisteren we op 0.0.0.0 zodat een tweede instance
        // meteen kan joinen via IP. Voor testen is dat precies wat je wilt.
        if (!UseSteam)
            udpTransport.SetConnectionData("127.0.0.1", port, "0.0.0.0");

        if (!NetworkManager.Singleton.StartHost())
            Debug.LogError("[Net] StartHost() gaf false terug.");
    }

    // ---------------- Openen naar multiplayer ----------------

    public async void OpenToMultiplayer(int maxMembers = 4)
    {
        if (!NetworkManager.Singleton.IsHost)
            StartLocalGame();

        if (!UseSteam)
        {
            openedWithoutSteam = true;
            Debug.Log($"[Net] Open zonder Steam. Clients kunnen joinen via IP, poort {port}.");
            OnLobbyStateChanged?.Invoke();
            return;
        }

        SteamManager.Instance.EnsureInitialized();

        if (CurrentLobby.HasValue)
        {
            Debug.LogWarning("[Steam] Er is al een lobby open.");
            return;
        }

        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);

        if (!CurrentLobby.HasValue)
            Debug.LogError("[Steam] Lobby aanmaken mislukt.");
    }

    private void HandleLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"[Steam] Lobby aanmaken mislukt: {result}");
            return;
        }

        lobby.SetPublic();
        lobby.SetJoinable(true);
        lobby.SetData(GameKey, GameKeyVal);
        lobby.SetData("HostSteamId", SteamClient.SteamId.ToString());
        lobby.SetData("LobbyName", $"{SteamClient.Name}'s game");

        Debug.Log($"[Steam] Lobby aangemaakt: {lobby.Id}");
        OnLobbyStateChanged?.Invoke();
    }

    // ---------------- Client ----------------

    /// <summary>Joinen zonder Steam, direct via IP (bv. "127.0.0.1").</summary>
    public void JoinByIp(string ip)
    {
        if (UseSteam)
        {
            Debug.LogWarning("[Net] JoinByIp werkt alleen met Steam uit.");
            return;
        }

        ApplyTransport();
        udpTransport.SetConnectionData(ip, port);

        if (NetworkManager.Singleton.StartClient())
            Debug.Log($"[Net] Verbinden met {ip}:{port}...");
        else
            Debug.LogError("[Net] StartClient() gaf false terug.");
    }

    public async void JoinLobby(SteamId lobbyId)
    {
        if (!UseSteam) { Debug.LogWarning("[Steam] Uit — gebruik JoinByIp."); return; }

        SteamManager.Instance.EnsureInitialized();

        Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

        if (!lobby.HasValue)
            Debug.LogError($"[Steam] Kon lobby {lobbyId} niet joinen.");
    }

    private void HandleLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        OnLobbyStateChanged?.Invoke();

        if (NetworkManager.Singleton.IsHost) return;

        StartClient(lobby.Owner.Id);
    }

    public void StartClient(SteamId hostSteamId)
    {
        ApplyTransport();
        steamTransport.targetSteamId = hostSteamId;

        if (NetworkManager.Singleton.StartClient())
            Debug.Log($"[Net] Verbinden met host {hostSteamId}...");
        else
            Debug.LogError("[Net] StartClient() gaf false terug.");
    }

    // ---------------- Lobby's zoeken ----------------

    public async Task<Lobby[]> FindLobbies(int max = 20)
    {
        if (!UseSteam) return Array.Empty<Lobby>();

        SteamManager.Instance.EnsureInitialized();

        Lobby[] result = await SteamMatchmaking.LobbyList
            .WithMaxResults(max)
            .WithKeyValue(GameKey, GameKeyVal)
            .FilterDistanceWorldwide()
            .RequestAsync();

        return result ?? Array.Empty<Lobby>();
    }

    // ---------------- Uitnodigingen ----------------

    public void OpenInviteOverlay()
    {
        if (!UseSteam || !CurrentLobby.HasValue)
        {
            Debug.LogWarning("[Steam] Geen Steam/lobby — kan geen uitnodiging sturen.");
            return;
        }

        if (!SteamUtils.IsOverlayEnabled)
        {
            Debug.LogWarning("[Steam] Overlay staat uit.");
            return;
        }

        SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
    }

    public bool TryInviteFriend(SteamId friendId)
    {
        if (!UseSteam || !CurrentLobby.HasValue)
        {
            Debug.LogWarning("[Steam] Geen Steam/lobby — kan geen uitnodiging sturen.");
            return false;
        }

        bool sent = CurrentLobby.Value.InviteFriend(friendId);
        Debug.Log(sent
            ? $"[Steam] Uitnodiging gestuurd naar {friendId}."
            : $"[Steam] Uitnodiging naar {friendId} mislukt.");
        return sent;
    }

    private void HandleLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log($"[Steam] Uitnodiging van {friend.Name} voor lobby {lobby.Id}");
    }

    private async void HandleGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
    {
        RoomEnter joinResult = await lobby.Join();

        if (joinResult != RoomEnter.Success)
            Debug.LogError($"[Steam] Joinen mislukt: {joinResult}");
    }

    private void HandleLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log($"[Steam] {friend.Name} is de lobby binnengekomen.");
        OnLobbyStateChanged?.Invoke();
    }

    private void HandleLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Debug.Log($"[Steam] {friend.Name} heeft de lobby verlaten.");
        OnLobbyStateChanged?.Invoke();
    }

    // ---------------- Stoppen ----------------

    public void Disconnect()
    {
        if (UseSteam) CurrentLobby?.Leave();
        CurrentLobby = null;
        openedWithoutSteam = false;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        if (UseSteam)
            Invoke(nameof(ReinitSteam), 0.5f);

        OnLobbyStateChanged?.Invoke();
    }

    private void ReinitSteam() => SteamManager.Instance.EnsureInitialized();
}