using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using TMPro;

public class Manager : MonoBehaviourPunCallbacks
{

    List<int> CarteMélangé = new List<int>();
    public int PlayerCount;
    [SerializeField] public GameObject[] Players;

    public List<Player> PlayerListInOrder = new List<Player>();
    

   

    [Header("Game")]
    public int Tour;
    public int Plis;
    public int Couleur;
    public int firstPlayer;
    public int currentPlayer;

    public Dictionary<int,int> playedCard = new Dictionary<int, int>();

    private void Start()
    {
        
        initatialize();
    }
    public void initatialize()
    {
        PlayerCount = StatGame.instance.PlayerCount;
        Players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in Players)
        {
            player.GetComponent<Player>().SetOrder();
            PlayerListInOrder.Add(player.GetComponent<Player>());
        }
        PlayerListInOrder = PlayerListInOrder.OrderByDescending(player => player.Numéro).ToList();

        Couleur = -1;

        if (!PhotonNetwork.IsMasterClient)
        {
            
            return;
        }
        firstPlayer = Random.Range(0, PlayerCount);
        NewTurn();
       
        
        
    }

    public void NewTurn()
    {
        
        if (!PhotonNetwork.IsMasterClient)
        {

            return;
        }
        Shuffle();
        Tour++;
        for (int p = 0; p < PlayerCount; p++)
        {
            for (int i = 0; i < Tour * 2; i++)
            {
                photonView.RPC("GiveCardRPC", RpcTarget.All, p, CarteMélangé[i + PlayerListInOrder[p].Numéro * Tour * 2]);
            }
        }
        firstPlayer++;
        currentPlayer = firstPlayer % PlayerCount;
        Plis = Tour * 2;
        photonView.RPC("SetCurrentPlayer", RpcTarget.OthersBuffered, currentPlayer,Plis,Tour);
    }

    private void Update()
    {

        transform.GetComponentInChildren<TextMeshProUGUI>().text = "Tour " + Tour;
    }
    public void Shuffle()
    {
        CarteMélangé.Clear();
        for (int i = 0; i < 72; i++)
        { CarteMélangé.Add(i); }
        Shuffle(CarteMélangé);
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


    [PunRPC]
    public void GiveCardRPC(int player,int card)
    {
        PlayerListInOrder[player].Carte.Add(card);
    }
    [PunRPC]
    public void PlayedCard(int player,int card)
    {
        playedCard.Add(player, card);
        currentPlayer++;
        GameObject.FindObjectOfType<LocalPlayerUI>().myTurn = false;
        if(playedCard.Count == 1)
        {
            Couleur = Mathf.FloorToInt(playedCard[player] / 13);
        }




        if(playedCard.Count == PlayerCount)
        {
            
            StartCoroutine(Plis_());
        }
            
                

    }

    public IEnumerator Plis_()
    {
        currentPlayer = 100;
        int strongest = 0;
        for (int p = 0; p < PlayerCount; p++)
        {
            if (cardValue(playedCard[p]) > cardValue(playedCard[strongest]))
            {
                strongest = p;
            }
                
        }
        currentPlayer = strongest;
        Couleur = -1;
        yield return new WaitForSeconds(.5f);
        GameObject.FindObjectOfType<PlayedCard>().Plis(strongest);
        yield return new WaitForSeconds(1.4f);
        playedCard.Clear();
        Plis--;
        if (Plis == 0)
        {
            
            NewTurn();
        }
    }


    public float cardValue(float card)
    {
        int couleur = Mathf.FloorToInt(card / 13) == Couleur ? 10 : 1;
        couleur = Mathf.FloorToInt(card / 13) == 4 ? 100 : couleur;
        return ((card % 13f) + 1) * couleur;
    }


    [PunRPC]
    public void SetCurrentPlayer(int c,int p,int t)
    {
        currentPlayer = c;
        Plis = p;
        Tour = t;
    }


    

}
