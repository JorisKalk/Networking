using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class PlayerData
{
    //change to the right value
    public int playerID = 0;
    public List<Card> heldCards;

    //for testing purposes
    public List<string> heldNames;

    public PlayerData(int pPlayerIndex)
    {
        playerID = pPlayerIndex;
        heldCards = new List<Card>();

        //testing list
        heldNames = new List<string>();
    }

    public PlayerData()
    {
        heldCards = new List<Card>();
        heldNames = new List<string>();
    }

    public void AddSingleCardToHand(Card card)
    {
        heldCards.Add(card);

        UpdateHeldListInspector();
    }

    public void AddCardsToHand(List<Card> cards)
    {
        foreach (Card card in cards)
        {
            heldCards.Add(card);
        }

        UpdateHeldListInspector();
    }

    public void CardPlayed(Card card)
    {
        heldCards.Remove(card);

        UpdateHeldListInspector();
    }

    //for testing purposes
    private void UpdateHeldListInspector()
    {
        heldNames.Clear();
        foreach (Card card in heldCards)
        {
            heldNames.Add(card.ToString());
        }
    }
}
