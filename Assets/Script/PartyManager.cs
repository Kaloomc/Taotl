using UnityEngine;
using Mirror;
using TMPro;

public class PartyManager : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI PlayerNbrUI;
    [SerializeField] TextMeshProUGUI GameStateUI;

    [SyncVar(hook = nameof(OnStateChanged))]
    public string GameState = "Waiting For Players";

    public const string Waiting = "Waiting For Players";
    public const string Dealing = "Dealing";
    public const string Bidding = "Bidding";
    public const string Playing = "Playing";
    public const string GameOver = "Game Over";

    [SyncVar] public int maxPlayer = 4;
    [SyncVar(hook = nameof(OnPlayersChanged))]
    public int synchronizedPlayerCount = 0;

    public int cardToDeal;
    int firstPlayerId = -1; // joueur qui a commencé la manche en cours
    int roundsAtMaxCards; // manches jouées au nombre max de cartes

    // Vrai quand on ne peut plus donner 2 cartes de plus à chacun (72 cartes dans le paquet)
    bool AtMaxCards => (cardToDeal + 2) * synchronizedPlayerCount > 72;

    public readonly SyncList<int> deckIndices = new SyncList<int>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Force l'affichage initial sur les clients
        if (GameStateUI != null) GameStateUI.text = GameState;
        if (PlayerNbrUI != null) PlayerNbrUI.text = "Current Player : " + synchronizedPlayerCount;
    }

    void Update()
    {
        if (!isServer) return; // Seul le serveur contrôle la machine à états

        if (synchronizedPlayerCount < maxPlayer)
        {
            if (GameState != Waiting)
            {
                GameState = Waiting;
                TableManager.instance.currentPlayerTurnId = -1;
            }
        }
        else if (GameState == Waiting)
        {
            GameState = Dealing;
            DistributeCards();
        }
        else if (GameState == Bidding)
        {
            int playerReady = 0;
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null && conn.identity.TryGetComponent<Player>(out Player playerScript))
                {
                    if (playerScript.bidLocked)
                        playerReady++;
                }
            }

            if (playerReady == synchronizedPlayerCount)
            {
                // Le donneur est le joueur juste avant celui qui démarre (firstPlayerId), pas celui qui démarre
                int dealerId = (firstPlayerId - 1 + synchronizedPlayerCount) % synchronizedPlayerCount;

                int bidSum = 0;
                Player dealer = null;
                foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                {
                    if (conn.identity != null && conn.identity.TryGetComponent<Player>(out Player p))
                    {
                        bidSum += p.bid;
                        if (p.id == dealerId) dealer = p;
                    }
                }

                // La somme des mises ne peut pas être égale au nombre de cartes : le donneur doit changer sa mise
                if (dealer != null && bidSum == cardToDeal)
                {
                    // On cache le bidUI des autres (ils ont fini d'enchérir) sans le réactiver : seul le donneur agit encore
                    foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                    {
                        if (conn.identity != null && conn.identity.TryGetComponent<Player>(out Player p) && p.id != dealerId)
                            p.disableBidUI();
                    }
                    dealer.ForceRebid();
                    return;
                }

                foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                {
                    if (conn.identity != null && conn.identity.TryGetComponent<Player>(out Player playerScript))
                    {
                        BidManager bidScript = conn.identity.GetComponent<BidManager>();
                        playerScript.disableBidUI();
                        if (bidScript != null) bidScript.SetUi(playerScript.bid);
                    }
                }
                GameState = Playing;
                StartGame();
            }
        }
    }

    [Server]
    public void StartGame()
    {
        // firstPlayerId est déjà choisi par DistributeCards() au début de la manche
        TableManager.instance.currentPlayerTurnId = firstPlayerId;
        TableManager.instance.suit = "";
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if(conn.identity.GetComponent<Player>().id == TableManager.instance.currentPlayerTurnId)
                    conn.identity.GetComponent<Player>().YourTurn("");
            }

        Debug.Log($"La partie commence ! Le joueur {TableManager.instance.currentPlayerTurnId} a la main.");
    }

    [Server]
    public void EndRound()
    {
        // Au nombre max de cartes, on joue une manche par joueur puis la partie s'arrête
        if (AtMaxCards)
            roundsAtMaxCards++;

        GameState = roundsAtMaxCards >= synchronizedPlayerCount ? GameOver : Waiting;
    }

    public void UpdatePlayerCount(int count)
    {
        synchronizedPlayerCount = count;
        
    }

    void OnPlayersChanged(int oldNbr, int newNbr)
    {
        if (PlayerNbrUI != null)
            PlayerNbrUI.text = "Current Player : " + newNbr;
    }

    void OnStateChanged(string oldNbr, string newNbr)
    {
        if (GameStateUI != null)
            GameStateUI.text = newNbr;
    }

    [Server]
    public void DistributeCards()
    {
        // 1ère manche : joueur au hasard, ensuite le suivant à chaque manche (le "donneur")
        firstPlayerId = firstPlayerId < 0 ? Random.Range(0, synchronizedPlayerCount) : (firstPlayerId + 1) % synchronizedPlayerCount;

        if (!AtMaxCards)
            cardToDeal += 2;
        CreateAndShuffleDeck();

        int cardIndex = 0;

        // 2. On parcourt tous les joueurs connectés
        // NetworkServer.connections contient tous les clients liés au serveur
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            // On prépare un paquet de cartes pour ce joueur
            int[] handForThisPlayer = new int[cardToDeal];
            for (int i = 0; i < cardToDeal; i++)
            {
                handForThisPlayer[i] = deckIndices[cardIndex];
                cardIndex++;
            }

            // 3. On récupère le script PlayerBelote de cette connexion
            Player playerScript = conn.identity.GetComponent<Player>();

            // 4. ON ENVOIE !
            if (playerScript != null)
            {
                playerScript.myHand.Clear();
                playerScript.myHand.AddRange(handForThisPlayer);
                playerScript.bidLocked = false; // reset côté serveur, sinon il garde la valeur de la manche précédente

                playerScript.TargetReceiveHand(handForThisPlayer);
            }
        }
        
        GameState = Bidding; // On passe aux enchère
    }


    [Server] // Seul le serveur peut appeler cette fonction
    public void CreateAndShuffleDeck()
    {
        deckIndices.Clear();
        for (int i = 0; i < 72; i++) deckIndices.Add(i);
        
        // Mélange (Fisher-Yates shuffle)
        for (int i = 0; i < deckIndices.Count; i++) {
            int temp = deckIndices[i];
            int randomIndex = Random.Range(i, deckIndices.Count);
            deckIndices[i] = deckIndices[randomIndex];
            deckIndices[randomIndex] = temp;
        }
    }
    
}