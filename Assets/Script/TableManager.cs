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

    // Valeurs par défaut validées par simulation de 2 à 8 joueurs (caméra orthographique taille 5, 16:9)
    [Header("Places des joueurs")]
    // (les adversaires restent à l'intérieur du cadre foncé de BACKGROUND.png)
    [SerializeField] Vector2 seatCenter = new Vector2(0f, 0.4f);   // centre de l'arche des adversaires
    [SerializeField] Vector2 seatRadius = new Vector2(6.8f, 3.2f); // demi-largeur / demi-hauteur de l'arche
    [SerializeField, Range(90f, 270f)] float seatArc = 220f;       // ouverture de l'arche, en degrés
    [SerializeField] Vector2 localSeat = new Vector2(-3.3f, -1.7f); // pseudo et pastilles du joueur local, juste au-dessus de la main

    [Header("Pli (cartes posées au centre)")]
    [SerializeField] Vector2 trickCenter = new Vector2(0f, 0f);
    [SerializeField] Vector2 trickRadius = new Vector2(1.5f, 0.8f);
    [SerializeField] float tableCardScale = 0.7f;  // cartes du pli plus petites que celles de la main
    [SerializeField] float startRadius = 20f;      // distance "hors écran" d'où arrivent / où partent les cartes
    [SerializeField] float tweenDuration = 0.5f;
    [SerializeField] float roundEndDelay = 3f;     // pause avant la manche suivante

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

        // 1. La carte se pose au centre, du côté du joueur qui l'a jouée
        float angle = SeatAngle(playedCard.playerSeat, myLocalSeat, numPlayers);
        Vector3 targetPos = (Vector3)(trickCenter + PointOnEllipse(angle, trickRadius));

        // 2. Elle arrive de hors écran, dans la direction de ce joueur
        Vector3 spawnPos = (Vector3)(trickCenter + PointOnEllipse(angle, Vector2.one * startRadius));

        // 3. APPARITION DE LA CARTE
        CardData data = CardDatabase.instance.GetCardByID(playedCard.cardID);
        GameObject cardVisual = Instantiate(CardDatabase.instance.cardPrefab, spawnPos, Quaternion.identity);
        cardVisual.transform.localScale *= tableCardScale;
        cardVisual.GetComponent<Card>().Setup(data);
        cardVisual.GetComponent<BoxCollider2D>().enabled = false;

        // 4. L'ANIMATION DOTWEEN !
        // La carte vole de sa spawnPos vers sa targetPos
        cardVisual.transform.DOMove(targetPos + new Vector3(0, 0, -0.1f * cardsVisual.Count), tweenDuration).SetEase(Ease.OutCubic);
        
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

        // La couleur est fixée par la 1ère carte qui n'est pas une Chipelt (excuse).
        // Un dieu impose sa couleur : rouge, noir, ou atout pour le Taotl.
        string cardSuit = CardDatabase.instance.GetCardByID(cardID).suit;
        if (suit == "" && cardSuit != "Chipelt")
        {
            suit = cardSuit == "Taotl" ? "Atout" : cardSuit;
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

    bool IsRedTrick => suit == "Coeur" || suit == "Carreau" || suit == "Rouge";
    bool IsBlackTrick => suit == "Pique" || suit == "Trefle" || suit == "Noir";

    int CardStrength(PlayedCard card)
    {
        CardData data = CardDatabase.instance.GetCardByID(card.cardID);

        // L'excuse, et un dieu joué en chipelt, ne remportent jamais le pli
        // (si tout le pli est en Chipelt, le 1er joueur gagne)
        if (data.suit == "Chipelt") return -1;

        // Un dieu joué sur la couleur opposée ne vaut rien ; sinon sa valeur le place
        // au-dessus des couleurs et des atouts jusqu'au 10, sous les atouts supérieurs
        if (data.suit == "Rouge") return IsBlackTrick ? -1 : data.value;
        if (data.suit == "Noir") return IsRedTrick ? -1 : data.value;
        if (data.suit == "Taotl") return data.value; // le Taotl prend tout

        if (data.suit == "Atout") return data.value; // l'atout coupe les autres couleurs
        return Player.MatchesSuit(data.suit, suit) ? data.value : 0; // hors couleur demandée : ne peut pas gagner
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
        float angle = SeatAngle(winnerSeat, myLocalSeat, numPlayers);
        Vector3 targetPos = (Vector3)(trickCenter + PointOnEllipse(angle, Vector2.one * startRadius));
        foreach (GameObject card in cardsVisual)
        {
           card.transform.DOMove(targetPos, tweenDuration * 2).SetEase(Ease.OutCubic);
        }
    }

    // Position du bloc pseudo + pastilles d'un joueur, vue depuis le joueur local
    public Vector3 SeatPosition(int playerSeat, int myLocalSeat, int numPlayers)
    {
        if (playerSeat == myLocalSeat) return localSeat;
        return seatCenter + PointOnEllipse(SeatAngle(playerSeat, myLocalSeat, numPlayers), seatRadius);
    }

    // Angle (degrés) de la place d'un joueur : le joueur local en bas (270°), les adversaires
    // sur l'arche du haut, le suivant à sa gauche. Espacés à distance égale le long de l'arche :
    // à angles égaux, ils se tassaient sur les côtés et se chevauchaient à 8 joueurs.
    float SeatAngle(int playerSeat, int myLocalSeat, int numPlayers)
    {
        int relativeIndex = (playerSeat - myLocalSeat + numPlayers) % numPlayers;
        if (relativeIndex == 0) return 270f;
        if (numPlayers == 2) return 90f;

        const int steps = 200;
        float startAngle = 90f + seatArc / 2f;
        float[] lengths = new float[steps + 1];
        Vector2 previous = PointOnEllipse(startAngle, seatRadius);
        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = PointOnEllipse(startAngle - seatArc * i / steps, seatRadius);
            lengths[i] = lengths[i - 1] + Vector2.Distance(previous, point);
            previous = point;
        }

        float target = lengths[steps] * (relativeIndex - 1) / (numPlayers - 2);
        int k = 1;
        while (k < steps && lengths[k] < target) k++;
        float t = Mathf.InverseLerp(lengths[k - 1], lengths[k], target);
        return startAngle - seatArc * (k - 1 + t) / steps;
    }

    static Vector2 PointOnEllipse(float angleDeg, Vector2 radius)
    {
        float a = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y);
    }

}