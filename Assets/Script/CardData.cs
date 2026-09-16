using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Taotl/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite cardVisual;
    public int value;
    public string suit;
}