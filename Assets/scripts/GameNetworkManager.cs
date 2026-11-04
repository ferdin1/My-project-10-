using System;
using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    // Zet dit op iets unieks. Anders vind je alle willekeurige Spacewar-lobby's
    // van vreemden op app 480, en dat zijn er veel.
    public const string GameKey    = "mijn_unieke_game_tag_verander_dit";
    public const string GameKeyVal = "1";

    public Lobby? CurrentLobby { get; private set; }
    public bool IsInLobby => CurrentLobby.HasValue;

    private FacepunchTransport transport;

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
        transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated      += HandleLobbyCreated;
        SteamMatchmaking.OnLobbyEntered      += HandleLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += HandleLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave  += HandleLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite       += HandleLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested += HandleGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated      -= HandleLobbyCreated;
        SteamMatchmaking.OnLobbyEntered      -= HandleLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= HandleLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave  -= HandleLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite       -= HandleLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested -= HandleGameLobbyJoinRequested;

        if (Instance == this) Instance = null;
    }

    // ---------------- Lokaal spelen (geen lobby, niet joinable) ----------------

    public void StartLocalGame()
    {
        if (!NetworkManager.Singleton.StartHost())
            Debug.LogError("[Net] StartHost() gaf false terug.");
    }

    // ---------------- Openen naar multiplayer (maakt pas nu de lobby aan) ----------------

    public async void OpenToMultiplayer(int maxMembers = 4)
    {
        SteamManager.Instance.EnsureInitialized();

        // Als er nog geen host draait (bv. rechtstreeks openen zonder eerst
        // lokaal te spelen), start 'm dan alsnog.
        if (!NetworkManager.Singleton.IsHost)
        {
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[Net] StartHost() gaf false terug.");
                return;
            }
        }

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

        lobby.SetPublic();          // of SetFriendsOnly() / SetPrivate()
        lobby.SetJoinable(true);
        lobby.SetData(GameKey, GameKeyVal);
        lobby.SetData("HostSteamId", SteamClient.SteamId.ToString());
        lobby.SetData("LobbyName", $"{SteamClient.Name}'s game");

        Debug.Log($"[Steam] Lobby aangemaakt: {lobby.Id}");
        OnLobbyStateChanged?.Invoke();
    }

    // ---------------- Client ----------------

    public async void JoinLobby(SteamId lobbyId)
    {
        SteamManager.Instance.EnsureInitialized();

        Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

        if (!lobby.HasValue)
            Debug.LogError($"[Steam] Kon lobby {lobbyId} niet joinen.");
        // Bij succes vuurt HandleLobbyEntered vanzelf.
    }

    private void HandleLobbyEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        OnLobbyStateChanged?.Invoke();

        // De host is zelf ook "entered" in zijn eigen lobby.
        // Die draait al als server en moet géén client starten.
        if (NetworkManager.Singleton.IsHost) return;

        StartClient(lobby.Owner.Id);
    }

    public void StartClient(SteamId hostSteamId)
    {
        // Dit is de kern van de hele transport: geen IP, maar een SteamID.
        transport.targetSteamId = hostSteamId;

        if (NetworkManager.Singleton.StartClient())
            Debug.Log($"[Net] Verbinden met host {hostSteamId}...");
        else
            Debug.LogError("[Net] StartClient() gaf false terug.");
    }

    // ---------------- Lobby's zoeken ----------------

    public async Task<Lobby[]> FindLobbies(int max = 20)
    {
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
        if (!CurrentLobby.HasValue)
        {
            Debug.LogWarning("[Steam] Geen lobby — kan geen uitnodiging sturen.");
            return;
        }

        // Dit opent de Steam Overlay, géén Unity-UI. In de Editor werkt die overlay
        // bijna nooit: Steam injecteert hem in Unity.exe niet.
        if (!SteamUtils.IsOverlayEnabled)
        {
            Debug.LogWarning("[Steam] Overlay staat uit. Zet 'Enable the Steam Overlay' " +
                             "aan in Steam → Instellingen → In-game, of nodig vrienden " +
                             "uit via de lijst in de Editor-UI.");
            return;
        }

        SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
    }

    public bool TryInviteFriend(SteamId friendId)
    {
        if (!CurrentLobby.HasValue)
        {
            Debug.LogWarning("[Steam] Geen lobby — kan geen uitnodiging sturen.");
            return false;
        }

        bool sent = CurrentLobby.Value.InviteFriend(friendId);
        if (sent)
            Debug.Log($"[Steam] Uitnodiging gestuurd naar {friendId}.");
        else
            Debug.LogWarning($"[Steam] Uitnodiging naar {friendId} mislukt.");
        return sent;
    }

    private void HandleLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log($"[Steam] Uitnodiging van {friend.Name} voor lobby {lobby.Id}");
    }

    // Vuurt als je in Steam op "Join Game" klikt of een uitnodiging accepteert.
    private async void HandleGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
    {
        RoomEnter joinResult = await lobby.Join();

        if (joinResult != RoomEnter.Success)
            Debug.LogError($"[Steam] Joinen mislukt: {joinResult}");
        // Bij Success vuurt HandleLobbyEntered vanzelf.
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
        CurrentLobby?.Leave();
        CurrentLobby = null;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        // NetworkManager.Shutdown() roept transport.Shutdown() aan,
        // en die roept SteamClient.Shutdown() aan. Zet Steam dus weer aan.
        Invoke(nameof(ReinitSteam), 0.5f);

        OnLobbyStateChanged?.Invoke();
    }

    private void ReinitSteam() => SteamManager.Instance.EnsureInitialized();
}