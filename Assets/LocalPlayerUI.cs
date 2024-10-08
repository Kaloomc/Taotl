using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


public class LocalPlayerUI : MonoBehaviour
{
    [SerializeField] GameObject CartePrefab;
    [SerializeField] float CarteOffsetMain;

    [Header("Game")]
    
    public Player LocalPlayer;
    public TextMeshProUGUI debugTxt;
    [Header("Texture")]
    [SerializeField] public List<Sprite> Textures;
    Manager manager;
    public bool myTurn;

    private void Start()
    {
        manager = GameObject.FindObjectOfType<Manager>();
    }
    private void FixedUpdate()
    {
        if (LocalPlayer == null)
        {
            LocalPlayer = GetLocalPlayer();
            return;
        }

        instantiateCard();
        MyTurn();

    }

    private void LateUpdate()
    {
        ApplyCardTexture();
    }

    public void MyTurn()
    {
        

        if(manager.currentPlayer % manager.PlayerCount == LocalPlayer.Numéro)
        {
            
            GameObject.FindObjectOfType<UImessage>().Message("à vous de jouer");
        }
        else
        {

            GameObject.FindObjectOfType<UImessage>().Message("");
        }

    }

    public void instantiateCard()
    {
        CarteOffsetMain = 1.5f - 0.035f * LocalPlayer.Carte.Count;


        if (transform.childCount != LocalPlayer.Carte.Count)
        {
            if (transform.childCount < LocalPlayer.Carte.Count)
            {
                GameObject temp = Instantiate(CartePrefab, transform);
                temp.transform.position = new Vector2(0, -10f);
            }
        }

        //ajouter ou suppr les cartes
        if (LocalPlayer.Carte.Count == 0)
            return;
        //Aligner les cartes
        float TailleMain = (LocalPlayer.Carte.Count - 1) * CarteOffsetMain;
        for (int i = 0; i < transform.childCount; i++)
        {
            iTween.MoveTo(transform.GetChild(i).gameObject, new Vector3((-TailleMain / 2) + CarteOffsetMain * i, transform.position.y, -0.01f * i), .3f);
        }
    }


    public Player GetLocalPlayer()
    {
        foreach (var player in manager.Players)
        {
            if (player.GetComponent<PhotonView>().IsMine && !player.GetComponent<Player>().Bot)
            {
                return player.GetComponent<Player>();
                

            }
        }
        return null;
    }


    public void ApplyCardTexture()
    {
        if (LocalPlayer == null)
            return;
        if (LocalPlayer.Carte.Count == 0)
            return;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponentInChildren<SpriteRenderer>().sprite = Textures[LocalPlayer.Carte[i]];

        }
    }



    public void Play(Sprite carte)
    {
        if (manager.currentPlayer % manager.PlayerCount  != LocalPlayer.Numéro)
            return;
        int index = LocalPlayer.Carte.IndexOf(Textures.IndexOf(carte));
        LocalPlayer.Play(Textures.IndexOf(carte));
        Destroy(transform.GetChild(index).gameObject);
        
    }
}
