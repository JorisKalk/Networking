using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour
{
    private GameSystem gameSystem;
    private Card card;

    [SerializeField]
    private TextMeshProUGUI text;

    void Start()
    {
        gameSystem = GameObject.Find("GameSystem").GetComponent<GameSystem>();
        if (gameSystem == null)
        {
            throw new System.Exception("no game system connected to the card");
        }
    }

    void Update()
    {
        
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
                text.color = Color.black;
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
        
        if (gameSystem.CheckCardPlayable(card))
        {
            if (card.cardType != Card.CardType.WILD)
            {
                gameSystem.CardPlayed(card);
            }
            else
            {
                GameObject.Find("CardDisplay").GetComponent<DisplayCards>().DisplayColorChoices(card);
            }
        }
    }
}
