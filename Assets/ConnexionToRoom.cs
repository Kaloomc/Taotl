using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ConnexionToRoom : MonoBehaviourPunCallbacks
{
    // Nom de la salle
    private string roomName = "TestRoom";

    void Start()
    {
        // Connecte au serveur Photon en utilisant le réseau principal
        PhotonNetwork.ConnectUsingSettings();
    }

    // Callback appelé après la connexion au serveur Photon
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connecté au serveur Photon!");
        // Joindre le lobby par défaut
        PhotonNetwork.JoinLobby();
    }

    // Callback appelé après avoir rejoint un lobby
    public override void OnJoinedLobby()
    {
        Debug.Log("Rejoint le lobby Photon!");
        // Tenter de rejoindre une salle spécifique
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 8}, TypedLobby.Default) ;
    }

    // Callback appelé après avoir rejoint une salle
    public override void OnJoinedRoom()
    {
        Debug.Log("Rejoint la salle: " + roomName);
        PhotonNetwork.LoadLevel("Lobby");
        // Optionnel: Vous pouvez instancier un joueur ou faire d'autres actions ici
    }

    // Callback appelé si une tentative de rejoindre une salle échoue
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Échec de rejoindre la salle: " + message);
        // Vous pouvez gérer l'échec ici, par exemple en réessayant ou en créant une nouvelle salle
    }

    // Callback appelé si une tentative de créer une salle échoue
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Échec de créer la salle: " + message);
        // Vous pouvez gérer l'échec ici
    }
}
