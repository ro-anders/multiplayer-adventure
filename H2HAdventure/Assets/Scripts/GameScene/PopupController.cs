﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    private const float POPUP_TIME = 0.1f; // tenth of a second
    private const float FLASH_TIME = 0.1f; // tenth of a second

    public string screenName;
    public float screenTop;
    public float screenBottom;
    public float screenLeft;
    public float screenRight;
    public float popupTop;
    public float popupBottom;
    public float popupLeft;
    public float popupRight;

    private Text popupText;
    private Image popupImage;
    private bool startPopup;
    private float popupProgress;
    private float targetPopupWidth;
    private float targetPopupHeight;
    private string message;
    private Sprite sprite;

    // Start is called before the first frame update
    void Start()
    {
        popupText = this.transform.Find("PopupText").gameObject.GetComponent<Text>();
        popupImage = this.transform.Find("PopupImage").gameObject.GetComponent<Image>();
        RectTransform rt = (RectTransform)transform;
        targetPopupWidth = rt.rect.width;
        targetPopupHeight = rt.rect.height;

        screenName = transform.parent.gameObject.name;
        screenBottom = transform.parent.position.y;
        screenTop = screenBottom + ((RectTransform)transform.parent).rect.height;
        screenLeft = transform.parent.position.x;
        screenRight = screenLeft + ((RectTransform)transform.parent).rect.width;

        popupBottom = transform.position.y;
        popupTop = popupBottom + ((RectTransform)transform).rect.height;
        popupLeft = transform.position.x;
        popupRight = popupLeft + ((RectTransform)transform).rect.width;

    }

    // Update is called once per frame
    void Update()
    {
        if (startPopup)
        {
            popupProgress = POPUP_TIME;
            startPopup = false;
        }
        if (popupProgress > 0)
        {
            popupProgress -= Time.deltaTime;
            if (popupProgress <= 0)
            {
                popupProgress = 0;
                popupText.text = message;
                popupImage.sprite = sprite;
            }
            popupProgress = (popupProgress < 0 ? 0 : popupProgress);
            float ratio = (POPUP_TIME - popupProgress) / POPUP_TIME;
            RectTransform rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(ratio*targetPopupWidth, ratio*targetPopupHeight);
        }
    }

    public void Popup(string inMessage, string imageName)
    {
        message = inMessage;
        imageName = ((imageName == null) || (imageName.Trim() == "") ? "nothing" : imageName);
        Sprite loaded = Resources.Load<Sprite>("Sprites/" + imageName);
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>("Sprites/nothing");
        }
        sprite = loaded;
        this.gameObject.SetActive(true);
        startPopup = true;
    }

    public void Hide(string message)
    {
        // Don't bother hiding the popup if it's not displaying this message
        if (popupText.text == message)
        {
            popupText.text = "";
            popupImage.sprite = Resources.Load<Sprite>("Sprites/nothing");
            this.gameObject.SetActive(false);
        }
    }
}
