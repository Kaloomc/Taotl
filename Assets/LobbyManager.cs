using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public GameObject BotPrefabs;
    public GameObject PlayerPrefabs;
    public GameObject ButtonBot;

    public GameObject StartButton;

    [Header("Players")]
    public Dictionary<int, string> PlayersPseudo = new Dictionary<int, string>();

    private void Start()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        PhotonNetwork.Instantiate(PlayerPrefabs.name, Vector3.zero,Quaternion.identity);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Update()
    {
        ButtonBot.gameObject.SetActive(!(PhotonNetwork.IsMasterClient == false || SceneManager.GetActiveScene().name == "Game"));
        StartButton.gameObject.SetActive(!(PhotonNetwork.IsMasterClient == false || SceneManager.GetActiveScene().name == "Game"));
        ButtonBot.transform.SetAsLastSibling();

        if (SceneManager.GetActiveScene().name == "Game")
            return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            player.transform.SetParent(transform);
        }
        GameObject[] bots = GameObject.FindGameObjectsWithTag("Bot");
        foreach (var bot in bots)
        {
            bot.transform.SetParent(transform);
        }



        
    }


    public void AddBot()
    {
        PhotonNetwork.Instantiate(BotPrefabs.name,transform.position, Quaternion.identity);
    }

    public void startGame()
    {
       
        PhotonNetwork.LoadLevel("Game");
        gameObject.transform.parent.GetChild(1).gameObject.SetActive(false);
    }



    public void AddPseudo(string pseudo,int num)
    {
        photonView.RPC("AddPseudoRPC", RpcTarget.AllBuffered,pseudo,num);
    }

    [PunRPC]
    public void AddPseudoRPC(string pseudo, int num)
    {
        PlayersPseudo.Add(num, pseudo);

    }
}
