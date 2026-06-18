using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    private GameSystem gameSystem;

    private CardColor cardColor;
    private Card card;

    [SerializeField]
    private TextMeshProUGUI text;

    void Start()
    {
        gameSystem = GameObject.Find("GameSystem").GetComponent<GameSystem>();
        if (gameSystem == null)
        {
            throw new System.Exception("no game system connected to the color");
        }
    }

    void Update()
    {
        
    }

    public void ReceiveColor(CardColor color, Card newCard)
    {
        card = newCard;
        cardColor = color;

        Image button = GetComponent<Image>();

        switch (cardColor)
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
        }

        text.text = color.ToString();
    }

    //Button click
    public void ColorChosen()
    {
        Debug.Log("Card played: " + card.ToString() + "\n" +
            "Color chosen: " + cardColor.ToString());

        gameSystem.CardPlayed(card, cardColor);
    }
}
