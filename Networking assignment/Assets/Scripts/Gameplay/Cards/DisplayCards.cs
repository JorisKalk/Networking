using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class DisplayCards : MonoBehaviour
{
    [SerializeField]
    private GameObject cardDisplayButton;
    [SerializeField]
    private GameObject colorChoiceButton;

    [Header("Placement Values")]
    [SerializeField]
    private int startingX;
    [SerializeField]
    private int startingY;
    [SerializeField]
    private int offsetX;
    [SerializeField]
    private int offsetY;
    [SerializeField]
    private int maxColumns;


    private int currentColumn;
    private int currentRow;

    private List<GameObject> currentDisplayedButtons = new List<GameObject>();

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void DisplayNewCards(PlayerData player)
    {
        ClearButtons();

        currentColumn = 0;
        currentRow = 0;

        List<Card> cardsToDisplay = player.heldCards;
        foreach (Card card in cardsToDisplay)
        {
            if (currentColumn >= maxColumns)
            {
                currentColumn = 0;
                currentRow++;
            }

            GameObject displayCard = Instantiate(cardDisplayButton);
            displayCard.transform.SetParent(transform, false);
            CardButton button = displayCard.GetComponent<CardButton>();
            button.ReceiveCard(card);
            currentDisplayedButtons.Add(displayCard);

            displayCard.transform.localPosition = new Vector3(startingX + (offsetX * currentColumn), startingY - (offsetY * currentRow));
            currentColumn++;
        }
    }

    public void DisplayColorChoices(Card card)
    {
        ClearButtons();

        for (int i = 0; i < 4; i++)
        {
            GameObject colorChoice = Instantiate(colorChoiceButton);
            colorChoice.transform.SetParent(transform, false);
            ColorButton button = colorChoice.GetComponent<ColorButton>();
            
            switch (i)
            {
                case 0:
                    button.ReceiveColor(CardColor.RED, card);
                    break;
                case 1:
                    button.ReceiveColor(CardColor.GREEN, card);
                    break;
                case 2:
                    button.ReceiveColor(CardColor.BLUE, card);
                    break;
                case 3:
                    button.ReceiveColor(CardColor.YELLOW, card);
                    break;
            }

            currentDisplayedButtons.Add(colorChoice);

            colorChoice.transform.localPosition = new Vector3(-600 + (400 * i), 0);
        }
    }

    private void ClearButtons()
    {
        if (currentDisplayedButtons.Count >= 1)
        {
            for (int i = currentDisplayedButtons.Count - 1; i >= 0; i--)
            {
                Destroy(currentDisplayedButtons[i].gameObject);
            }
            currentDisplayedButtons.Clear();
        }
    }
}
