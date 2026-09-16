using UnityEngine;
using Mirror;
using DG.Tweening;

public class Card : MonoBehaviour
{

    public int cardID;
    public string suit;
    public Vector3 originalPos;
    public float ySelected;
    public bool Playable;

    public void goDown()
    {
        transform.DOMoveY(originalPos.y - ySelected / 2,0.2f).SetEase(Ease.OutBack);
    }

    public void Setup(CardData data)
    {
        GetComponent<SpriteRenderer>().sprite = data.cardVisual;
        name = data.cardName;
        suit = data.suit;
    }

    void OnMouseEnter()
    {
        if(NetworkClient.localPlayer != null && NetworkClient.localPlayer.GetComponent<Player>().id == TableManager.instance.currentPlayerTurnId && Playable)
            transform.DOMoveY(originalPos.y + ySelected,0.2f).SetEase(Ease.OutBack);
    }


    void OnMouseExit()
    {
        if(NetworkClient.localPlayer != null && NetworkClient.localPlayer.GetComponent<Player>().id == TableManager.instance.currentPlayerTurnId && Playable)
            posReset();
    }

    public void posReset()
    {
        transform.DOMoveY(originalPos.y,0.2f).SetEase(Ease.OutBack);
    }

    void OnMouseDown()
    {
        if (!Playable) return;

        Player localPlayer = NetworkClient.localPlayer.GetComponent<Player>();
        if (localPlayer != null)
        {
            // Plus aucune carte jouable jusqu'au prochain YourTurn : évite d'envoyer des coups hors tour
            foreach (Card card in localPlayer.GetComponentsInChildren<Card>())
                card.Playable = false;
            localPlayer.playCard(this.cardID);
        }

    }
}
