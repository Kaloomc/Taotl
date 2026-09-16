using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using DG.Tweening;
using UnityEngine.UIElements;

public class Player : NetworkBehaviour
{
    // La main locale du joueur (ce qu'il voit à l'écran)
    public List<int> myHand = new List<int>();
    public GameObject CardPrefab;
    public float cardOffset = 0.8f;

    [SyncVar (hook = nameof(OnPseudoChanged))] 
    public string Pseudo = "Player";
    public TextMeshProUGUI PseudoText;

    [SyncVar] public int id;

    [SyncVar] public int bid;
    [SyncVar] public bool bidLocked;
    public GameObject bidUI;
    public TextMeshProUGUI bidText;

    [SyncVar] public int tricksWon;
    [SyncVar (hook = nameof(OnScoreChanged))]
    public int score;
    public TextMeshProUGUI ScoreText;


    public override void OnStartClient()
    {
        base.OnStartClient();
        // Met à jour l'UI avec la valeur actuelle de la SyncVar
        UpdatePseudoUI(Pseudo);
        UpdateScoreUI(score);
        StartCoroutine(WaitAndPosition());
    }

    public void OnPseudoChanged(string oldVal, string newVal)
    {
        UpdatePseudoUI(newVal);
    }

    private void UpdatePseudoUI(string nameToDisplay)
    {
        if (PseudoText != null)
        {
            PseudoText.text = string.IsNullOrWhiteSpace(nameToDisplay) ? "En attente..." : nameToDisplay;
        }
    }

    public void OnScoreChanged(int oldVal, int newVal)
    {
        UpdateScoreUI(newVal);
    }

    private void UpdateScoreUI(int scoreToDisplay)
    {
        if (ScoreText != null)
        {
            ScoreText.text = scoreToDisplay.ToString();
        }
    }

    System.Collections.IEnumerator WaitAndPosition()
    {
        // synchronizedPlayerCount vaut 0 tant que la SyncVar n'est pas arrivée (division par 0 sinon)
        yield return new WaitUntil(() => NetworkClient.localPlayer != null && FindAnyObjectByType<PartyManager>().synchronizedPlayerCount > 0);
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        int totalPlayers = FindAnyObjectByType<PartyManager>().synchronizedPlayerCount;
        int myLocalSeat = NetworkClient.localPlayer.GetComponent<Player>().id;

        Vector3 circularPos = TableManager.GetRelativeCircularPosition(id, myLocalSeat, totalPlayers, 5f, -18f, 1.5f, 0.7f);
    
        transform.position = circularPos + new Vector3(0, TableManager.instance.yOffset, 0);
    }

    [TargetRpc]
    public void TargetReceiveHand(int[] cardIDs)
    {
        myHand.Clear();
        myHand.AddRange(cardIDs);

        foreach (var myCarte in myHand)
        {
            GameObject currentCard = Instantiate(CardPrefab,transform);
            currentCard.transform.position = new Vector3(0,-10f,0);
            currentCard.GetComponent<Card>().cardID = myCarte;
            CardData cardData = FindFirstObjectByType<CardDatabase>().GetCardByID(myCarte);
            currentCard.GetComponent<Card>().Setup(cardData);
        }
        bidLocked = false;
        bid = 0;
        changeBid(0);
        bidUI.SetActive(true);

        SortHand();
    }

    void rearrangeCard()
    {
        List<Card> cards = new List<Card>(); 
        
        foreach (Transform child in transform) 
        {
            if (child != null)
            {
                Card cardScript = child.GetComponent<Card>();
                
                if (cardScript != null) 
                {
                    cards.Add(cardScript);
                }
            }
        }

        if (cards.Count == 0) return;

        float y = -5f;
        float z = 0f;

        float totalWidth = (cards.Count - 1) * cardOffset;
        float startX = -totalWidth / 2f;

        // 2. On positionne
        for (int i = 0; i < cards.Count; i++)
        {
            float posX = startX + (i * cardOffset);
            Vector3 targetPos = new Vector3(posX, y, z);
            cards[i].transform.DOKill();
    
            cards[i].transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCubic);
            
            cards[i].originalPos = targetPos; 
            
            z -= 0.1f;
        }
    }

    public void changeBid(int change)
    {
        bid += change;
        bid = math.clamp(bid,0,myHand.Count);
        bidText.text = bid.ToString();
    }

    public void lockBid()
    {
        bidLocked = !bidLocked;
        CmdLockBid(bid, bidLocked);
    }

    [Command]
    public void CmdLockBid(int bidAmount, bool bidstatus)
    {
        // On ne fait pas confiance au client : uniquement pendant les enchères, et mise bornée à la main
        if (FindAnyObjectByType<PartyManager>().GameState != PartyManager.Bidding) return;
        bid = math.clamp(bidAmount, 0, myHand.Count);
        bidLocked = bidstatus;
    }

    [TargetRpc]
    public void disableBidUI()
    {
        bidUI.SetActive(false);
    }

    [Command]
    public void playCard(int cardId)
    {
         // 1. Est-ce le tour de ce joueur ?
        if (!TableManager.instance.IsItPlayersTurn(id)) return;

        // 2. Le joueur possède-t-il vraiment cette carte ?
        if (!myHand.Contains(cardId)) return;

        // 3. La carte respecte-t-elle la règle de couleur ?
        string currentSuit = TableManager.instance.suit;
        string cardSuit = CardDatabase.instance.GetCardByID(cardId).suit;
        bool haveSuit = myHand.Exists(handId => CardDatabase.instance.GetCardByID(handId).suit == currentSuit);
        if (!CanPlayCard(cardSuit, currentSuit, haveSuit)) return;

        // Si tout est OK :
        myHand.Remove(cardId); // On retire de la main sur le serveur
        TableManager.instance.AddCardToTable(cardId, id); // On pose sur la table
        
        // On informe le client qu'il doit supprimer la carte visuellement
        TargetRemoveCardFromHand(cardId);
    }

    [TargetRpc]
    void TargetRemoveCardFromHand(int cardID) {
        Card []mycardstmp = GetComponentsInChildren<Card>();
        foreach (var card in mycardstmp)
        {
            if(card.cardID == cardID)
            {
                card.transform.SetParent(null);
                Destroy(card.gameObject);
            }
        }
        rearrangeCard();
    }

    [TargetRpc]
    public void YourTurn(string currentSuit)
    {
        List<Card> cards = new List<Card>(); 
        
            foreach (Transform child in transform) 
            {
                if (child != null)
                {
                    Card cardScript = child.GetComponent<Card>();
                    
                    if (cardScript != null) 
                    {
                        cards.Add(cardScript);
                    }
                }
            }
        
        bool haveSuit = false;
        foreach (Card card in cards)
        {
            if (card.suit == currentSuit)
                haveSuit = true;
        }

        foreach (Card card in cards)
        {
            if (CanPlayCard(card.suit, currentSuit, haveSuit))
            {
                card.Playable = true;
                card.posReset();
            }
            else
            {
                card.goDown();
                card.Playable = false;
            }
        }
    }

    public static bool CanPlayCard(string cardSuit, string currentSuit, bool haveCurrentSuit)
    {
        bool isAlwaysPlayable = cardSuit == "Atout" || cardSuit == "Chipelt";
        return currentSuit == "" || !haveCurrentSuit || cardSuit == currentSuit || isAlwaysPlayable;
    }


    public void SortHand()
    {
        List<Card> cardsInHand = new List<Card>(); 
        
        foreach (Transform child in transform) 
        {
            if (child != null)
            {
                Card cardScript = child.GetComponent<Card>();
                if (cardScript != null) cardsInHand.Add(cardScript);
            }
        }

        if (cardsInHand.Count == 0) return;

        cardsInHand.Sort((cardA, cardB) => 
        {
            CardData dataA = CardDatabase.instance.GetCardByID(cardA.cardID);
            CardData dataB = CardDatabase.instance.GetCardByID(cardB.cardID);

            int suitComparison = string.Compare(dataA.suit, dataB.suit);
            
            if (suitComparison != 0)
            {
                return suitComparison;
            }
            return dataA.value.CompareTo(dataB.value);
        });

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            cardsInHand[i].transform.SetSiblingIndex(i);
        }
        rearrangeCard();
    }
}
