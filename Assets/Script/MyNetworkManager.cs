using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class MyNetworkManager : NetworkManager
{
    [Header("Configuration des Scènes")]
    public GameObject playerLobbyPrefab; 
    public string lobbySceneName = "Lobby";
    public string gameSceneName = "GameScene";

    // Dictionnaire pour conserver les pseudos par connexion
    public static readonly Dictionary<int, string> playerNames = new Dictionary<int, string>();

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        // Impossible de rejoindre une partie en cours (même logique que NetworkRoomManager de Mirror)
        if (networkSceneName == gameSceneName)
        {
            conn.Disconnect();
            return;
        }
        base.OnServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        playerNames.Remove(conn.connectionId);
        base.OnServerDisconnect(conn);

        PartyManager party = FindFirstObjectByType<PartyManager>();
        if (party != null)
        {
            party.UpdatePlayerCount(NetworkServer.connections.Count);
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName == gameSceneName)
        {
            StartCoroutine(SetupGameSceneRoutine());
        }
    }

    private IEnumerator SetupGameSceneRoutine()
    {
        yield return new WaitForEndOfFrame();

        int totalPlayers = NetworkServer.connections.Count;
        int currentSeat = 0;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            // 1. Récupération du pseudo sauvegardé
            string savedName = "Player";
            if (playerNames.TryGetValue(conn.connectionId, out string registeredName) && !string.IsNullOrWhiteSpace(registeredName))
            {
                savedName = registeredName;
            }
            else if (conn.identity != null && conn.identity.TryGetComponent<PlayerLobby>(out PlayerLobby lobbyPlayer))
            {
                if (!string.IsNullOrWhiteSpace(lobbyPlayer.playerName))
                    savedName = lobbyPlayer.playerName;
            }

            // 2. Instanciation du Player de jeu
            GameObject gamePlayerInstance = Instantiate(playerPrefab);
            Player playerScript = gamePlayerInstance.GetComponent<Player>();

            if (playerScript != null)
            {
                playerScript.id = currentSeat;
                playerScript.Pseudo = savedName; // Affecté sur le serveur !
            }

            NetworkServer.ReplacePlayerForConnection(conn, gamePlayerInstance);
            currentSeat++;
        }

        yield return null;

        PartyManager party = FindFirstObjectByType<PartyManager>();
        if (party != null)
        {
            party.maxPlayer = totalPlayers;
            party.UpdatePlayerCount(totalPlayers);
        }
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        if (SceneManager.GetActiveScene().name == lobbySceneName && conn.identity == null)
        {
            if (playerLobbyPrefab != null)
            {
                GameObject lobbyObj = Instantiate(playerLobbyPrefab);
                NetworkServer.AddPlayerForConnection(conn, lobbyObj);
            }
        }
    }
}