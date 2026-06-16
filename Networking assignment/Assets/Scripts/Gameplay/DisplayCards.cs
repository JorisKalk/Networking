using UnityEngine;
using System.Collections.Generic;

public class DisplayCards : MonoBehaviour
{
    [SerializeField]
    private GameObject cardDisplayButton;


    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void DisplayNewCards(PlayerData player)
    {
        List<Card> cardsToDisplay = player.heldCards;
    }
}
