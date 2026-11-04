
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class SteamLobbyUI : MonoBehaviour
{
    private Lobby[] lobbies = new Lobby[0];
    private bool searching;
    private bool showInvitePanel;

    private Vector2 scroll;
    private Vector2 inviteScroll;

    // ---------------- Wereldkeuze ----------------

    private string[] worlds =
    {
        "World 1",
        "World 2",
        "World 3"
    };

    private int selectedWorld = 0;

    private string SelectedWorldName
    {
        get
        {
            if (selectedWorld < 0 || selectedWorld >= worlds.Length)
                return worlds[0];

            return worlds[selectedWorld];
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(
            new Rect(10, 10, 360, 640),
            GUI.skin.box
        );

        if (!SteamClient.IsValid)
        {
            GUILayout.Label(
                "Steam is niet geïnitialiseerd.\n" +
                "Staat Steam open en ben je ingelogd?"
            );

            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"Ingelogd als: {SteamClient.Name}");
        GUILayout.Label($"SteamID: {SteamClient.SteamId}");

        GUILayout.Space(8);

        var nm = NetworkManager.Singleton;

        if (nm != null && (nm.IsHost || nm.IsClient))
        {
            GUILayout.Label(
                nm.IsHost ? "Rol: HOST" : "Rol: CLIENT"
            );

            GUILayout.Label(
                $"Verbonden clients: " +
                $"{(nm.IsServer ? nm.ConnectedClientsIds.Count : 1)}"
            );

            GUILayout.Label(
                $"Wereld: {GetActiveWorldName()}"
            );

            GUILayout.Label(
                "R = save (client stuurt positie naar host)"
            );

            // Host speelt lokaal, maar heeft nog geen lobby geopend.
            if (nm.IsHost &&
                !GameNetworkManager.Instance.IsInLobby)
            {
                if (GUILayout.Button("Open naar multiplayer"))
                {
                    GameNetworkManager.Instance.OpenToMultiplayer(4);
                }
            }

            // Host heeft wel al een lobby open.
            if (GameNetworkManager.Instance.IsInLobby &&
                nm.IsHost)
            {
                if (GUILayout.Button(
                    showInvitePanel
                        ? "Uitnodigen sluiten"
                        : "Vriend uitnodigen"))
                {
                    showInvitePanel = !showInvitePanel;

                    if (showInvitePanel)
                        GameNetworkManager.Instance.OpenInviteOverlay();
                }

                if (showInvitePanel)
                    DrawInvitePanel();
            }

            if (GUILayout.Button("Verbinding verbreken"))
            {
                GameNetworkManager.Instance.Disconnect();
            }
        }
        else
        {
            // ---------------- WERELD KIEZEN ----------------

            GUILayout.Label("Kies een wereld:");

            GUILayout.Space(4);

            // Toon de huidige geselecteerde wereld.
            GUILayout.Label(
                $"Geselecteerd: {SelectedWorldName}"
            );

            // Knop per wereld.
            for (int i = 0; i < worlds.Length; i++)
            {
                bool isSelected = selectedWorld == i;

                GUI.enabled = !isSelected;

                if (GUILayout.Button(worlds[i]))
                {
                    selectedWorld = i;
                }

                GUI.enabled = true;
            }

            GUILayout.Space(8);

            // ---------------- SPELEN ----------------

            if (GUILayout.Button($"Spelen - {SelectedWorldName}"))
            {
                StartSelectedWorld();
            }

            GUILayout.Space(12);

            // ---------------- LOBBY'S ----------------

            GUI.enabled = !searching;

            if (GUILayout.Button(
                searching
                    ? "Zoeken..."
                    : "Lobby's zoeken"))
            {
                RefreshLobbies();
            }

            GUI.enabled = true;

            GUILayout.Label(
                $"Gevonden: {lobbies.Length}"
            );

            scroll = GUILayout.BeginScrollView(
                scroll,
                GUILayout.Height(260)
            );

            foreach (Lobby lobby in lobbies)
            {
                string name =
                    lobby.GetData("LobbyName");

                if (string.IsNullOrEmpty(name))
                    name = lobby.Id.ToString();

                string world =
                    lobby.GetData("WorldName");

                if (string.IsNullOrEmpty(world))
                    world = "?";

                if (GUILayout.Button(
                    $"{name} [{world}] " +
                    $"({lobby.MemberCount}/{lobby.MaxMembers})"))
                {
                    GameNetworkManager.Instance.JoinLobby(
                        lobby.Id
                    );
                }
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    // ---------------- Wereld starten ----------------

    private void StartSelectedWorld()
    {
        Debug.Log(
            $"[World] Gekozen wereld: {SelectedWorldName}"
        );

        // Laad/maak het JSON-bestand voor de wereld
        WorldManager.Instance.LoadWorld(SelectedWorldName);

        GameNetworkManager.Instance.StartLocalGame();
    }

    // ---------------- Invite panel ----------------

    private void DrawInvitePanel()
    {
        GUILayout.Space(6);

        GUILayout.Label(
            "Steam Overlay opent in de Editor meestal niet."
        );

        GUILayout.Label(
            "Nodig hieronder een vriend uit " +
            "(Steam-popup bij hen):"
        );

        inviteScroll = GUILayout.BeginScrollView(
            inviteScroll,
            GUILayout.Height(180)
        );

        foreach (Friend friend in SteamFriends.GetFriends())
        {
            if (!friend.IsOnline)
                continue;

            if (GUILayout.Button(
                $"{friend.Name} ({friend.State})"))
            {
                GameNetworkManager.Instance
                    .TryInviteFriend(friend.Id);
            }
        }

        GUILayout.EndScrollView();
    }

    // ---------------- Actieve wereld ----------------

    private static string GetActiveWorldName()
    {
        var lobby =
            GameNetworkManager.Instance.CurrentLobby;

        if (lobby.HasValue)
        {
            string fromLobby =
                lobby.Value.GetData("WorldName");

            if (!string.IsNullOrEmpty(fromLobby))
                return fromLobby;
        }

        return "(host wereld)";
    }

    // ---------------- Lobby zoeken ----------------

    private async void RefreshLobbies()
    {
        searching = true;

        lobbies =
            await GameNetworkManager.Instance.FindLobbies();

        searching = false;
    }
}
