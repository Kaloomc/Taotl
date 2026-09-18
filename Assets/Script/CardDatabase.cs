using UnityEngine;
using System.Collections.Generic;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase instance;

    public GameObject cardPrefab;
    public List<CardData> allCards = new List<CardData>();

    void Awake()
    {
        instance = this;
    }

    public CardData GetCardByID(int id)
    {
        if (id >= 0 && id < allCards.Count)
            return allCards[id];
        
        return null;
    }

    public int GetIdByCard(CardData card) => card == null ? -1 : allCards.IndexOf(card);
}