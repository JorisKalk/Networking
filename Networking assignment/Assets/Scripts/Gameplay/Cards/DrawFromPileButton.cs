using UnityEngine;

public class DrawFromPileButton : MonoBehaviour
{
    private Player controller;

    void Start()
    {
        controller = FindAnyObjectByType<Player>();
    }

    public void ButtonPressed()
    {
        controller.DrawCard();
    }
}
