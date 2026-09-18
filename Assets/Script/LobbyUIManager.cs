using UnityEngine;
using Mirror;
using TMPro;
using EpicTransport;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager instance;

    [Header("UI References")]
    public Transform playerListContainer; // Glisse ici ton 'Panel'
    public GameObject startGameButton;     // Ton bouton "Lancer la partie"
    public TMP_Text roomCodeText;          // Affiche le code à partager aux amis

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Seul l'hôte voit le bouton pour lancer le jeu
        if (startGameButton != null)
        {
            startGameButton.SetActive(NetworkServer.active);
        }

        if (roomCodeText != null && EOSTransport.ConnectedToLobby)
        {
            roomCodeText.text = "Code : " + EOSTransport.ConnectedLobbyInfo.LobbyId;
        }
    }

    public void OnStartGameClicked()
    {
        // Il faut au moins 2 joueurs (hôte compris) pour lancer
        if (NetworkServer.active && NetworkServer.connections.Count >= 2)
        {
            NetworkManager.singleton.ServerChangeScene("GameScene");
        }
    }
}