using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class StatGame : MonoBehaviourPunCallbacks
{
    public static StatGame instance;


    public int PlayerCount;
    public int RealPlayer;
    public int Bot;

    public Dictionary<int, string> PlayersPseudo = new Dictionary<int, string>();
    public Dictionary<int, int> Playersorder = new Dictionary<int, int>();

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        instance = this;
    }


    public void StartGame()
    {
        GameObject[] playerID = GameObject.FindGameObjectsWithTag("Player");
        var lobby = GameObject.FindObjectOfType<LobbyManager>();
        PlayersPseudo = lobby.PlayersPseudo;
        PlayerCount = lobby.transform.childCount - 1;
        PlayerLobby[] players = lobby.transform.GetComponentsInChildren<PlayerLobby>();
        RealPlayer = players.Length;
        Bot = PlayerCount - RealPlayer;
        photonView.RPC("UpdateStat", RpcTarget.Others,PlayerCount,RealPlayer,Bot);

        List<int> orders = new List<int>();
        for (int i = 0; i < PlayerCount; i++)
        { orders.Add(i); }
        Shuffle(orders);
        for (int i = 0; i < playerID.Length; i++)
        {
            photonView.RPC("dicoOrder", RpcTarget.AllBuffered, playerID[i].GetComponent<PhotonView>().ViewID, orders[i]);
        }

    }

    [PunRPC]
    public void UpdateStat(int PC,int RP,int B)
    {
        PlayerCount = PC;
        RealPlayer = RP;
        Bot = B;
        //pseudo
        var lobby = GameObject.FindObjectOfType<LobbyManager>();
        PlayersPseudo = lobby.PlayersPseudo;
    }

    [PunRPC]
    public void dicoOrder(int key, int order)
    {
        Playersorder.Add(key, order);
        print(key + "," + order);   
    }








    void Shuffle<T>(List<T> list)
    {
        System.Random random = new System.Random();
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
