using UnityEngine;
using TMPro;
using Mirror;
using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField roomCodeInputField; // code à 6 caractères pour rejoindre

    [Header("Test local (à décocher avant de distribuer le jeu)")]
    [SerializeField] private bool localTest;
    [SerializeField] private kcp2k.KcpTransport kcpTransport;

    void Start()
    {
        if (localTest)
        {
            // KCP sur localhost : plusieurs instances sur le même PC, sans EOS
            NetworkManager.singleton.transport = kcpTransport;
            Transport.active = kcpTransport;
            return;
        }

        // Connexion anonyme (pas besoin de compte Epic pour les joueurs)
        EOSManager.Initialize(new TransportInitializeOptions
        {
            ProductName = "Taotl",
            ProductId = EosCredentials.ProductId,
            SandboxId = EosCredentials.SandboxId,
            DeploymentId = EosCredentials.DeploymentId,
            ClientId = EosCredentials.ClientId,
            ClientSecret = EosCredentials.ClientSecret,
            EncryptionKey = EosCredentials.EncryptionKey,
            AuthInterfaceCredentialType = LoginCredentialType.ExternalAuth,
            ConnectInterfaceCredentialType = ExternalCredentialType.DeviceidAccessToken,
            DisplayName = string.IsNullOrWhiteSpace(PlayerData.LocalPlayerName) ? "Player" : PlayerData.LocalPlayerName
        });

        EOSTransport.OnJoinedLobby += OnJoinedLobby;
    }

    // À lier au bouton "Créer"
    public void HostGame()
    {
        SaveName();
        if (localTest)
        {
            NetworkManager.singleton.StartHost();
            NetworkManager.singleton.ServerChangeScene("Lobby");
            return;
        }
        StartCoroutine(HostWhenReady());
    }

    IEnumerator HostWhenReady()
    {
        yield return new WaitUntil(() => EOSManager.Initialized);
        // Code à 6 caractères que l'hôte partage à ses amis pour qu'ils rejoignent
        string code = System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        EOSTransport.CreateLobby(code, 8); // démarre le host tout seul une fois la lobby créée
    }

    // À lier au bouton "Rejoindre"
    public void JoinGame()
    {
        SaveName();
        if (localTest)
        {
            NetworkManager.singleton.networkAddress = "localhost";
            NetworkManager.singleton.StartClient();
            return;
        }
        StartCoroutine(JoinWhenReady(roomCodeInputField.text.Trim().ToUpper()));
    }

    IEnumerator JoinWhenReady(string code)
    {
        yield return new WaitUntil(() => EOSManager.Initialized);
        EOSTransport.JoinLobbyByID(code);
        // Le client n'a pas besoin de charger la scène manuellement :
        // Mirror synchronise automatiquement la scène du serveur dès qu'il est connecté !
    }

    // Seul l'hôte doit faire avancer la scène commune ; le client la reçoit automatiquement de Mirror
    void OnJoinedLobby(string lobbyId)
    {
        if (EOSTransport.ConnectedLobbyInfo.IsLobbyOwner)
            NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    private void SaveName()
    {
        if (!string.IsNullOrWhiteSpace(nameInputField.text))
        {
            PlayerData.LocalPlayerName = nameInputField.text;
        }
    }
}
