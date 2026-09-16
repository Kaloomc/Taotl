using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public struct PlayedCard
{
    public int cardID;
    public int playerSeat;
}


public class TableManager : NetworkBehaviour
{
    public static TableManager instance;
    
    // SyncList pour que tout le monde voie les cartes sur la table
    public readonly SyncList<PlayedCard> cardsOnTable = new SyncList<PlayedCard>();
    
    List<GameObject> cardsVisual = new List<GameObject>();

    [SyncVar] public int currentPlayerTurnId;
    [SyncVar] public string suit;

    [Header("Configuration Circulaire")]
    [SerializeField] float tableRadius = 4f; // Rayon du cercle où les cartes se posent
    [SerializeField] float startRadius = 20f; // Rayon de départ "hors écran" (grande valeur)
    [SerializeField] float tweenDuration = 0.5f;
    [SerializeField] public float yOffset = 3f;
    [SerializeField] float xMultiplier = 1.05f;
    [SerializeField] float roundEndDelay = 3f; // pause avant la manche suivante

    public bool IsItPlayersTurn(int playerId) => playerId == currentPlayerTurnId;

    void Awake() 
    {
        instance = this;
        cardsOnTable.Callback += OnTableChanged;
    }

   void OnTableChanged(SyncList<PlayedCard>.Operation op, int index, PlayedCard oldItem, PlayedCard newItem)
    {
        if (op == SyncList<PlayedCard>.Operation.OP_ADD)
        {
            ShowCardVisual(newItem);
        }
        else if (op == SyncList<PlayedCard>.Operation.OP_CLEAR)
        {
            ClearTableVisuals();
        }
    }

        
    void ShowCardVisual(PlayedCard playedCard)
    {
        int myLocalSeat = NetworkClient.localPlayer.GetComponent<Player>().id;
        // ATTENTION : Tu dois récupérer le nombre total de joueurs connectés !
        int numPlayers = FindAnyObjectByType<PartyManager>().synchronizedPlayerCount;

        // 1. CALCULER LA POSITION CIBLE
        Vector3 targetPos = GetRelativeCircularPosition(
            playedCard.playerSeat,
            myLocalSeat,
            numPlayers,
            tableRadius,
            0f,
            xMultiplier,
            1f);

        // 2. CALCULER LA POSITION DE DÉPART (Hors Écran)
        // C'est exactement la même direction que la cible, mais sur un cercle beaucoup plus grand.
        Vector3 direction = targetPos.normalized;
        Vector3 spawnPos = direction * startRadius;

        // 3. APPARITION DE LA CARTE
        CardData data = CardDatabase.instance.GetCardByID(playedCard.cardID);
        GameObject cardVisual = Instantiate(CardDatabase.instance.cardPrefab, spawnPos + new Vector3(0,yOffset,0), Quaternion.identity);
        cardVisual.GetComponent<Card>().Setup(data);
        cardVisual.GetComponent<BoxCollider2D>().enabled = false;

        // 4. L'ANIMATION DOTWEEN !
        // La carte vole de sa spawnPos vers sa targetPos
        cardVisual.transform.DOMove(targetPos + new Vector3(0,yOffset,-0.1f * cardsVisual.Count), tweenDuration).SetEase(Ease.OutCubic);
        
        // On l'ajoute à la liste des visuels pour le nettoyage
        cardsVisual.Add(cardVisual);
    }

    void ClearTableVisuals() {
        foreach(GameObject card in cardsVisual)
        {
            if (card != null) Destroy(card);
        }
        cardsVisual.Clear();
    }

    [Server]
    public void AddCardToTable(int cardID, int playerId)
    {
        PlayedCard newCard = new PlayedCard { cardID = cardID, playerSeat = playerId };

        // La couleur est fixée par la 1ère carte qui n'est pas une Chipelt (excuse)
        string cardSuit = CardDatabase.instance.GetCardByID(cardID).suit;
        if (suit == "" && cardSuit != "Chipelt")
        {
            suit = cardSuit;
        }
        cardsOnTable.Add(newCard);
        int totalPlayers = FindAnyObjectByType<PartyManager>().synchronizedPlayerCount;
        currentPlayerTurnId = (currentPlayerTurnId + 1) % totalPlayers;
        if (cardsOnTable.Count == totalPlayers) {
            currentPlayerTurnId = -1;
            StartCoroutine(ResolveTrickCoroutine());
        }
        else
        {
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if(conn.identity.GetComponent<Player>().id == currentPlayerTurnId)
                    conn.identity.GetComponent<Player>().YourTurn(suit);
            }
        }
    }
    
    int FindWinner()
    {
        // Sécurité au cas où
        if (cardsOnTable.Count == 0) return 0;

        PlayedCard biggestCard = cardsOnTable[0];
        
        foreach (PlayedCard card in cardsOnTable)
        {
            if (CardStrength(card) > CardStrength(biggestCard))
            {
                biggestCard = card;
            }
        }

        return biggestCard.playerSeat;
    }

    int CardStrength(PlayedCard card)
    {
        CardData data = CardDatabase.instance.GetCardByID(card.cardID);
        if (data.suit == "Chipelt") return -1; // l'excuse ne remporte jamais le pli (sauf si tout le pli est en Chipelt : le 1er joueur gagne)
        return data.value + (data.suit == suit ? 20 : 0);
    }

    IEnumerator ResolveTrickCoroutine()
    {
        
        int numPlayers = FindAnyObjectByType<PartyManager>().synchronizedPlayerCount;

        int winnerSeat = FindWinner();
        yield return new WaitForSeconds(2f);
        MoveToWinner(winnerSeat, numPlayers);
        yield return new WaitForSeconds(tweenDuration * 2);
        cardsOnTable.Clear();
        suit = "";

        bool roundOver = true;
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null && conn.identity.TryGetComponent<Player>(out Player p) && p.myHand.Count > 0)
            {
                roundOver = false;
                break;
            }
        }

        // Dernier pli : personne n'a la main, StartGame désignera le premier joueur de la manche suivante
        currentPlayerTurnId = roundOver ? -1 : winnerSeat;
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if(conn.identity.GetComponent<Player>().id == winnerSeat)
            {
                Player winnerScript = conn.identity.GetComponent<Player>();
                winnerScript.tricksWon++;
                conn.identity.GetComponent<BidManager>().NewTrick();
                if (!roundOver) winnerScript.YourTurn(suit);
            }
        }

        if (roundOver)
        {
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null || !conn.identity.TryGetComponent<Player>(out Player playerScript)) continue;

                int diff = playerScript.tricksWon - playerScript.bid;
                playerScript.score += diff == 0 ? 10 + 5 * playerScript.bid : -5 * Mathf.Abs(diff);
                playerScript.tricksWon = 0;
            }

            // Pause pour laisser voir les plis réussis/ratés et les scores
            yield return new WaitForSeconds(roundEndDelay);

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null && conn.identity.TryGetComponent<BidManager>(out BidManager bidScript))
                    bidScript.SetUi(0);
            }

            FindAnyObjectByType<PartyManager>().EndRound();
        }
    }

    [ClientRpc]
    public void MoveToWinner(int winnerSeat, int numPlayers)
    {
        int myLocalSeat = NetworkClient.localPlayer.GetComponent<Player>().id;
        Vector3 targetPos = GetRelativeCircularPosition(
            winnerSeat,
            myLocalSeat,
            numPlayers,
            startRadius,
            0f,
            xMultiplier,
            1f);
        foreach (GameObject card in cardsVisual)
        {
           card.transform.DOMove(targetPos + new Vector3(0,yOffset,0), tweenDuration * 2).SetEase(Ease.OutCubic);
        }
    }

    public static Vector3 GetRelativeCircularPosition(int playerSeat, int myLocalSeat, int numPlayers, float radius, float angleOffset = 0f, float xMultiplier = 1f, float yMultiplier = 1f)
    {
        float angleStep = 360f / numPlayers;
        int relativeIndex = (playerSeat - myLocalSeat + numPlayers) % numPlayers;

        float appliedOffset = relativeIndex == 0 ? angleOffset * 2f : angleOffset;
        float angleDeg = (270f + appliedOffset - (relativeIndex * angleStep) + 360f) % 360f;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float x = radius * Mathf.Cos(angleRad);
        float y = radius * Mathf.Sin(angleRad);

        return new Vector3(x * xMultiplier, y * yMultiplier, 0f);
    }

}