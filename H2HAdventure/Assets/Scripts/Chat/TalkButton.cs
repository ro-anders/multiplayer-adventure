using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private static Color pressedColor = new Color(0.5f, 0.2f, 0.2f);
    private static Color unpressedColor = new Color(1, 1, 1);

    public ChatPanelController controller;
    public Button lockButton;

    private bool pressed = false;
    private bool locked = false;
    private Button thisButton;

    void Start()
    {
        thisButton = GetComponent<Button>();
    }

    public void SetEnabled(bool inEnabled)
    {
        if (!inEnabled)
        {
            MakeLookUnpressed();
            locked = false;
            pressed = false;
        }
        thisButton.interactable = inEnabled;
        lockButton.interactable = inEnabled;
    }

    public void MakeLookPressed()
    {
        gameObject.GetComponentInChildren<Text>().color = unpressedColor;
        // Sometimes we call this even when the button isn't pressed, when
        // we want to make it look pressed.  So change the background color, too.
        ColorBlock colors = thisButton.colors;
        colors.normalColor = pressedColor;
        thisButton.colors = colors;
    }

    public void MakeLookUnpressed()
    {
        gameObject.GetComponentInChildren<Text>().color = pressedColor;
        ColorBlock colors = thisButton.colors;
        colors.normalColor = unpressedColor;
        thisButton.colors = colors;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (thisButton.interactable)
        {
            locked = false;
            pressed = true;
            MakeLookPressed();
            controller.OnTalkPressed();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        if (!locked)
        {
            MakeLookUnpressed();
            controller.OnTalkReleased();
        }
    }

    public void OnLockPressed()
    {
        if (!locked)
        {
            locked = true;
            MakeLookPressed();
            if (!pressed)
            {
                controller.OnTalkPressed();
            }
        }
        else
        {
            locked = false;
            if (!pressed)
            {
                MakeLookUnpressed();
                controller.OnTalkReleased();
            }
        }
    }

}
