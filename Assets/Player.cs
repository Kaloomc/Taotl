using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Linq;

public class Player : MonoBehaviourPunCallbacks
{
    public int Numéro;
    public bool Bot;
    public List<int> Carte;


    private void Update()
    {
        if(!Bot)
            GetComponent<PlayerLobby>().enabled = !(SceneManager.GetActiveScene().name == "Game");

        if(Carte.Count > 0)
            Carte = Carte.OrderByDescending(x => x).Reverse().ToList();

    }




    public void SetOrder()
    {

        Numéro = StatGame.instance.Playersorder[photonView.ViewID];
    }

    public void Play(int carte)
    {
        Carte.Remove(carte);
        GameObject.FindObjectOfType<Manager>().photonView.RPC("PlayedCard",RpcTarget.All,Numéro, carte);
    }

}


