using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerLobby : MonoBehaviourPunCallbacks
{
    public GameObject MasterLogo;
    string Pseudo;
    public TextMeshProUGUI PseudoText;
    void Update()
    {


        var lobbymanager = GameObject.FindObjectOfType<LobbyManager>();
        

        if(lobbymanager.PlayersPseudo.TryGetValue(photonView.ViewID, out string name))
        {
            Pseudo = name;
            PseudoText.text = Pseudo;

        }
        
       
        MasterLogo.SetActive(photonView.Owner.IsMasterClient);
    }

    private void OnDisable()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }

}
