using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour
{
    private Player controller;
    private Card card;

    [SerializeField]
    private TextMeshProUGUI text;

    void Start()
    {
        controller = FindAnyObjectByType<Player>();
        if (controller == null)
        {
            throw new System.Exception("no controller connected to the card");
        }
    }

    public void ReceiveCard(Card newCard)
    {
        card = newCard;
        text.text = card.ToString();
        Image button = GetComponent<Image>();
        
        switch (card.color)
        {
            case CardColor.RED:
                button.color = Color.red;
                text.color = Color.white;
                break;
            case CardColor.GREEN:
                button.color = Color.green;
                text.color = Color.black;
                break;
            case CardColor.BLUE:
                button.color = Color.blue;
                text.color = Color.white;
                break;
            case CardColor.YELLOW:
                button.color = Color.yellow;
                text.color = Color.black;
                break;
            case CardColor.BLACK:
                button.color = Color.black;
                text.color = Color.white;
                break;
        }

    }

    //Button click
    public void CardPlayed()
    {
        Debug.Log("Card played: " + card.ToString());
        
        controller.MakeMove(card);
    }
}
