using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PseudoUIGame : MonoBehaviour
{
    float radius = 150f;
    public GameObject Pseudo;
    public float offset;

    Manager manager;
    void Start()
    {
        PlacePlayersOnCircle(StatGame.instance.PlayerCount);
        manager = GameObject.FindObjectOfType<Manager>();
    }

    void PlacePlayersOnCircle(int numberOfPlayers)
    {
        // Calculer l'angle de décalage pour chaque joueur
        float angleIncrement = Mathf.PI * 2 / numberOfPlayers;
        

        for (int i = 0; i < numberOfPlayers; i++)
        {
            // Calculer l'angle pour chaque joueur en partant de 0 pour le premier joueur et ajouter l'offset
            float angle = i * angleIncrement + offset;

            // Calculer la position de chaque joueur
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            // Créer la position du joueur
            Vector3 playerPosition = new Vector3(x * 2, y + 50f, 0);

            
            
            // Pour l'exemple, créer une sphère à la position calculée
            GameObject player = Instantiate(Pseudo, transform);
            player.transform.localPosition = playerPosition;

            
            
        }

        
       


    }
    private void Update()
    {
        if(GameObject.FindObjectOfType<LocalPlayerUI>().LocalPlayer == null) { return; }
        foreach (var player in manager.PlayerListInOrder)
        {
            string pseudo = "Player";
            if(StatGame.instance.PlayersPseudo.TryGetValue(player.photonView.ViewID, out pseudo)) { 
                
            }
            if (pseudo == null)
            {
                pseudo = "Player" + StatGame.instance.Playersorder[player.photonView.ViewID];
            }
            transform.GetChild((StatGame.instance.Playersorder[player.photonView.ViewID] + GameObject.FindObjectOfType<LocalPlayerUI>().LocalPlayer.Numéro  + transform.childCount - 1) % transform.childCount).transform.GetComponent<TextMeshProUGUI>().text = pseudo;

        }
    }
}
