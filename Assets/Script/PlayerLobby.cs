using UnityEngine;
using Mirror;
using TMPro;

public class PlayerLobby : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 1. Trouver le conteneur UI dans la scène Lobby et s'y accrocher
        if (LobbyUIManager.instance != null && LobbyUIManager.instance.playerListContainer != null)
        {
            transform.SetParent(LobbyUIManager.instance.playerListContainer, false);
        }

        // Rafraîchir le texte à l'apparition
        UpdateText(playerName);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // 2. Le joueur local envoie au serveur le pseudo stocké depuis le menu
        CmdSetPlayerName(PlayerData.LocalPlayerName);
    }

    private void OnNameChanged(string oldName, string newName)
    {
        UpdateText(newName);
    }

    private void UpdateText(string displayName)
    {
        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(displayName) ? "En attente..." : displayName;
        }
    }

    [Command]
    public void CmdSetPlayerName(string newName)
    {
        playerName = newName;
        
        // Sauvegarde immédiate sur le serveur pour cette connexion
        if (connectionToClient != null)
        {
            MyNetworkManager.playerNames[connectionToClient.connectionId] = newName;
        }
    }
}