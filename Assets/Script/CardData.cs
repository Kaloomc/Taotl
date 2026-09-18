using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Taotl/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite cardVisual;
    public int value;
    public string suit;

    // Dieux uniquement : la carte à poser quand le joueur choisit de la jouer en chipelt
    public CardData chipeltVersion;
}