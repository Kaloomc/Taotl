using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using DG.Tweening;

public class Player : NetworkBehaviour
{
    // La main locale du joueur (ce qu'il voit à l'écran)
    public List<int> myHand = new List<int>();
    public GameObject CardPrefab;
    public float cardOffset = 0.8f;
    public float maxHandWidth = 9f; // largeur max de la main (unités monde), le pseudo local est juste à côté

    [SyncVar (hook = nameof(OnPseudoChanged))] 
    public string Pseudo = "Player";
    public TextMeshProUGUI PseudoText;

    [SyncVar] public int id;

    [SyncVar] public int bid;
    [SyncVar] public bool bidLocked;

    public Sprite bidLockSprite;
    public Sprite bidUnLockSprite;
    public Image lockUI;

    public GameObject bidUI;
    public TextMeshProUGUI bidText;

    // UI affichée uniquement au donneur quand la somme des mises tombe pile sur le nombre de cartes
    public GameObject ajustBidUI;
    public TextMeshProUGUI ajustBidText;
    bool forcedRebid; // serveur uniquement : vrai tant que le donneur n'a pas changé sa mise
    int forbiddenBid; // serveur uniquement : la mise qu'il n'a pas le droit de reprendre

    // UI de choix quand on clique sur un dieu : le jouer en dieu ou en chipelt
    public GameObject chooseGodUI;
    int pendingGodCardId = -1;

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

        transform.position = TableManager.instance.SeatPosition(id, myLocalSeat, totalPlayers);
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
        ajustBidUI.SetActive(false);
        chooseGodUI.SetActive(false);

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

        // Les cartes se chevauchent davantage plutôt que de sortir de l'écran (jusqu'à 36 cartes à 2 joueurs)
        float offset = cards.Count > 1 ? Mathf.Min(cardOffset, maxHandWidth / (cards.Count - 1)) : 0f;
        float totalWidth = (cards.Count - 1) * offset;
        float startX = -totalWidth / 2f;

        // 2. On positionne
        for (int i = 0; i < cards.Count; i++)
        {
            float posX = startX + (i * offset);
            Vector3 targetPos = new Vector3(posX, y, z);
            cards[i].transform.DOKill();
    
            cards[i].transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCubic);
            
            cards[i].originalPos = targetPos; 
            
            z -= 0.1f;
        }
    }

    public void changeBid(int change)
    {
        if(bidLocked) return;
        bid += change;
        bid = math.clamp(bid,0,myHand.Count);
        bidText.text = bid.ToString();
        if (ajustBidText != null) ajustBidText.text = bid.ToString();
    }

    public void lockBid()
    {
        bidLocked = !bidLocked;
        lockUI.sprite = bidLocked ? bidLockSprite : bidUnLockSprite;
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

    // Appelé par PartyManager quand la somme des mises == nombre de cartes : ce joueur (le donneur) doit changer sa mise
    [Server]
    public void ForceRebid()
    {
        forcedRebid = true;
        forbiddenBid = bid;
        bidLocked = false;
        TargetShowAjustBidUI();
    }

    [TargetRpc]
    void TargetShowAjustBidUI()
    {
        bidUI.SetActive(false);
        ajustBidUI.SetActive(true);
    }

    // Boutons + / - de l'AjustBidUi : un seul cran, validé dès le clic (pas de bouton Lock)
    public void ajustBidBy(int delta)
    {
        CmdAjustBid(delta);
    }

    [Command]
    public void CmdAjustBid(int delta)
    {
        if (FindAnyObjectByType<PartyManager>().GameState != PartyManager.Bidding) return;
        if (!forcedRebid) return;

        int newBid = math.clamp(bid + delta, 0, myHand.Count);
        if (newBid == forbiddenBid) return; // seul coup qui redonnerait la mise interdite (borne 0 ou main pleine) : on ignore le clic

        bid = newBid;
        bidLocked = true;
        forcedRebid = false;
        TargetHideAjustBidUI();
    }

    [TargetRpc]
    void TargetHideAjustBidUI()
    {
        ajustBidUI.SetActive(false);
    }

    [Command]
    public void playCard(int cardId, bool asChipelt)
    {
         // 1. Est-ce le tour de ce joueur ?
        if (!TableManager.instance.IsItPlayersTurn(id)) return;

        // 2. Le joueur possède-t-il vraiment cette carte ?
        if (!myHand.Contains(cardId)) return;

        // 3. Seuls les dieux peuvent être joués en chipelt
        CardData data = CardDatabase.instance.GetCardByID(cardId);
        if (asChipelt && !IsGod(data.suit)) return;

        // 4. La carte respecte-t-elle la règle de couleur ?
        string currentSuit = TableManager.instance.suit;
        string cardSuit = asChipelt ? "Chipelt" : data.suit;
        bool haveSuit = myHand.Exists(handId => MatchesSuit(CardDatabase.instance.GetCardByID(handId).suit, currentSuit));
        if (!CanPlayCard(cardSuit, currentSuit, haveSuit)) return;

        // Si tout est OK :
        myHand.Remove(cardId); // On retire de la main sur le serveur

        // Joué en chipelt : on pose la version chipelt pour que tout le monde voie le choix
        int tableCardId = cardId;
        if (asChipelt)
        {
            tableCardId = CardDatabase.instance.GetIdByCard(data.chipeltVersion);
            // Sans elle le dieu serait posé tel quel et remporterait le pli : on refuse plutôt que de fausser la règle
            if (tableCardId < 0)
            {
                Debug.LogError($"{data.cardName} : chipeltVersion non assignée, ou absente de CardDatabase.allCards");
                return;
            }
        }
        TableManager.instance.AddCardToTable(tableCardId, id); // On pose sur la table

        // On informe le client qu'il doit supprimer la carte visuellement
        TargetRemoveCardFromHand(cardId);
    }

    // Appelé quand le joueur clique sur un dieu : il doit d'abord choisir comment le jouer
    public void AskGodOrChipelt(int cardId)
    {
        pendingGodCardId = cardId;
        chooseGodUI.SetActive(true);
    }

    public void PlayAsGod() => PlayPendingGod(false);      // bouton "Dieu"
    public void PlayAsChipelt() => PlayPendingGod(true);   // bouton "Chipetl"

    void PlayPendingGod(bool asChipelt)
    {
        if (pendingGodCardId < 0) return;

        chooseGodUI.SetActive(false);
        playCard(pendingGodCardId, asChipelt);
        pendingGodCardId = -1;
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
            if (MatchesSuit(card.suit, currentSuit))
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

    public static bool IsGod(string cardSuit) => cardSuit == "Noir" || cardSuit == "Rouge" || cardSuit == "Taotl";

    // "Rouge"/"Noir" sont demandés par les dieux : n'importe quelle carte de cette couleur convient
    public static bool MatchesSuit(string cardSuit, string currentSuit)
    {
        if (currentSuit == "Rouge") return cardSuit == "Coeur" || cardSuit == "Carreau";
        if (currentSuit == "Noir") return cardSuit == "Pique" || cardSuit == "Trefle";
        return cardSuit == currentSuit;
    }

    public static bool CanPlayCard(string cardSuit, string currentSuit, bool haveCurrentSuit)
    {
        // Seuls le chipelt et les dieux échappent à l'obligation de fournir la couleur (l'atout non)
        bool isAlwaysPlayable = cardSuit == "Chipelt" || IsGod(cardSuit);
        return currentSuit == "" || !haveCurrentSuit || isAlwaysPlayable || MatchesSuit(cardSuit, currentSuit);
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
