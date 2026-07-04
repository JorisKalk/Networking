using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MouseTracker
{
    private float mouseX;
    private float mouseY;

    private float oldMouseX = 0;
    private float oldMouseY = 0;

    public delegate void MouseMovedEvent(float x, float y);
    public event MouseMovedEvent OnMouseMoved;

    Canvas myCanvas;
    CanvasScaler scaler;

    public void AddCanvas(Canvas canvas, CanvasScaler scaler)
    {
        myCanvas = canvas;
        this.scaler = scaler;
    }

    public void Update()
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myCanvas.transform as RectTransform, Input.mousePosition, myCanvas.worldCamera, out pos);
        Vector2 indicatedPos = myCanvas.transform.TransformPoint(pos);
        mouseX = indicatedPos.x / scaler.referenceResolution.x;
        mouseY = indicatedPos.y / scaler.referenceResolution.y;

        if (mouseX != oldMouseX || mouseY != oldMouseY)
        {
            oldMouseX = mouseX;
            oldMouseY = mouseY;
            OnMouseMoved?.Invoke(mouseX, mouseY);
        }
    }
}
