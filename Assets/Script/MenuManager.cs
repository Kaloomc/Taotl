using UnityEngine;
using TMPro;
using Mirror;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;

    // À lier au bouton "Créer"
    public void HostGame()
    {
        SaveName();
        NetworkManager.singleton.StartHost();
        
        // Change la scène vers le Lobby pour l'hôte et tous les clients
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    // À lier au bouton "Rejoindre"
    public void JoinGame()
    {
        SaveName();
        NetworkManager.singleton.networkAddress = "localhost";
        NetworkManager.singleton.StartClient(); 
        // Le client n'a pas besoin de charger la scène manuellement :
        // Mirror synchronise automatiquement la scène du serveur dès qu'il est connecté !
    }

    private void SaveName()
    {
        if (!string.IsNullOrWhiteSpace(nameInputField.text))
        {
            PlayerData.LocalPlayerName = nameInputField.text;
        }
    }
}