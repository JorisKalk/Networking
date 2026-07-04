using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplayCards : MonoBehaviour
{
    [SerializeField]
    private GameObject cardDisplayButton;
    [SerializeField]
    private GameObject colorChoiceButton;

    [Header("Pile Display")]
    [SerializeField]
    private GameObject pileCardButton;
    [SerializeField]
    private TextMeshProUGUI pileCardButtonText;

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

    public void SubscribeEvents(UnoClient client)
    {
        client.OnTopCardChanged += OnTopCardUpdated;
        client.OnColorChoice += OnColorChoice;
    }

    private void OnTopCardUpdated(Card card, CardColor wildColorChoice = CardColor.NULL)
    {
        Debug.Log("update pile");
        Image button = pileCardButton.GetComponent<Image>();

        CardColor checkedColor;

        if (wildColorChoice == CardColor.NULL)
        {
            checkedColor = card.color;
        }
        else
        {
            checkedColor = wildColorChoice;
        }

        switch (checkedColor)
        {
            case CardColor.RED:
                button.color = Color.red;
                pileCardButtonText.color = Color.white;
                break;
            case CardColor.GREEN:
                button.color = Color.green;
                pileCardButtonText.color = Color.black;
                break;
            case CardColor.BLUE:
                button.color = Color.blue;
                pileCardButtonText.color = Color.white;
                break;
            case CardColor.YELLOW:
                button.color = Color.yellow;
                pileCardButtonText.color = Color.black;
                break;
            case CardColor.BLACK:
                button.color = Color.black;
                pileCardButtonText.color = Color.white;
                break;
        }

        pileCardButtonText.text = card.ToString();
    }

    private void OnColorChoice()
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
                    button.ReceiveColor(CardColor.RED);
                    break;
                case 1:
                    button.ReceiveColor(CardColor.GREEN);
                    break;
                case 2:
                    button.ReceiveColor(CardColor.BLUE);
                    break;
                case 3:
                    button.ReceiveColor(CardColor.YELLOW);
                    break;
            }

            currentDisplayedButtons.Add(colorChoice);

            colorChoice.transform.localPosition = new Vector3(-600 + (400 * i), 0);
        }
    }

    public void DisplayNewCards(PlayerData player)
    {
        Debug.Log("test");

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
