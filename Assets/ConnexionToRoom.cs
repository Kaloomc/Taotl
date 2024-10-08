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
        // Connecte au serveur Photon en utilisant le r�seau principal
        PhotonNetwork.ConnectUsingSettings();
    }

    // Callback appel� apr�s la connexion au serveur Photon
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connect� au serveur Photon!");
        // Joindre le lobby par d�faut
        PhotonNetwork.JoinLobby();
    }

    // Callback appel� apr�s avoir rejoint un lobby
    public override void OnJoinedLobby()
    {
        Debug.Log("Rejoint le lobby Photon!");
        // Tenter de rejoindre une salle sp�cifique
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 8}, TypedLobby.Default) ;
    }

    // Callback appel� apr�s avoir rejoint une salle
    public override void OnJoinedRoom()
    {
        Debug.Log("Rejoint la salle: " + roomName);
        PhotonNetwork.LoadLevel("Lobby");
        // Optionnel: Vous pouvez instancier un joueur ou faire d'autres actions ici
    }

    // Callback appel� si une tentative de rejoindre une salle �choue
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("�chec de rejoindre la salle: " + message);
        // Vous pouvez g�rer l'�chec ici, par exemple en r�essayant ou en cr�ant une nouvelle salle
    }

    // Callback appel� si une tentative de cr�er une salle �choue
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("�chec de cr�er la salle: " + message);
        // Vous pouvez g�rer l'�chec ici
    }
}
