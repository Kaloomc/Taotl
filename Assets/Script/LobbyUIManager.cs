using UnityEngine;
using Mirror;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager instance;

    [Header("UI References")]
    public Transform playerListContainer; // Glisse ici ton 'Panel'
    public GameObject startGameButton;     // Ton bouton "Lancer la partie"

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