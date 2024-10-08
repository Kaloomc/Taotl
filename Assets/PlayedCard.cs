using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayedCard : MonoBehaviour
{
    public GameObject CartePrefab;
    int numberOfPlayers; // Le nombre de joueurs
    public float radius = 5.0f; // Le rayon du cercle
    public Vector3 center = new Vector3(0, 0, 0); // Le centre du cercle

    Manager manager;
    LocalPlayerUI localplayer;

    List<Vector3> playerPositions = new List<Vector3>();   

    

    private void Start()
    {
        manager = GameObject.FindObjectOfType<Manager>();
        localplayer = GameObject.FindObjectOfType<LocalPlayerUI>();
        numberOfPlayers = GameObject.FindObjectOfType<Manager>().PlayerCount;
        PlacePlayersOnCircle(1f, true);
        PlacePlayersOnCircle(8f, false);
    }

    void PlacePlayersOnCircle(float radiusMultiplicator,bool CardSprite)
    {
        // Calculer l'angle de décalage pour chaque joueur
        float angleIncrement = Mathf.PI * 2 / numberOfPlayers;
        float offset = -Mathf.PI / 2; // Décalage de -90 degrés en radians

        for (int i = 0; i < numberOfPlayers; i++)
        {
            // Calculer l'angle pour chaque joueur en partant de 0 pour le premier joueur et ajouter l'offset
            float angle = i*(CardSprite ? 1 : -1) * angleIncrement + offset;

            // Calculer la position de chaque joueur
            float x = Mathf.Cos(angle) * radius * radiusMultiplicator;
            float y = Mathf.Sin(angle) * radius * radiusMultiplicator;

            // Créer la position du joueur
            Vector3 playerPosition = new Vector3(x, y, center.z) + center;

            if (CardSprite)
            {
                // Pour l'exemple, créer une sphère à la position calculée
                GameObject player = Instantiate(CartePrefab, transform);
                player.transform.localPosition = playerPosition;

            }
            else
                playerPositions.Add(playerPosition);
        }
    }

    private void Update()
    {
        if (transform.childCount == 0)
            return;
        if(localplayer.LocalPlayer == null) return;
        for (int i = 0; i < transform.childCount; i++)
        {
            if(manager.playedCard.TryGetValue(i, out var card))
            {
                transform.GetChild((i - localplayer.LocalPlayer.Numéro + numberOfPlayers) % numberOfPlayers).GetComponent<SpriteRenderer>().sprite = localplayer.Textures[card];
            }
            else
            {
                transform.GetChild((i - localplayer.LocalPlayer.Numéro + numberOfPlayers) % numberOfPlayers).GetComponent<SpriteRenderer>().sprite = null;

            }
        }
    }


    public void Plis(int player)
    {
        for(int i = 0;i < transform.childCount; i++)
        {
            iTween.MoveTo(transform.GetChild(i).gameObject, playerPositions[(player - localplayer.LocalPlayer.Numéro + numberOfPlayers) % numberOfPlayers], 1.5f);
        }
        StartCoroutine(Reset());
    }


    public IEnumerator Reset()
    {
        yield return new WaitForSeconds(1.5f);
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        PlacePlayersOnCircle(1f, true);
    }
}
