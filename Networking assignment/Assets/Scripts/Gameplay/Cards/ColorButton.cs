using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    private Player controller;

    private CardColor cardColor;

    [SerializeField]
    private TextMeshProUGUI text;

    void Start()
    {
        controller = FindAnyObjectByType<Player>();
    }

    public void ReceiveColor(CardColor color)
    {
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
                text.color = Color.white    ;
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
        controller.ChooseColor(cardColor);
    }
}
