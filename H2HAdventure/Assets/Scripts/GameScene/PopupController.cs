using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    private const float POPUP_TIME = 0.2f; // tenth of a second
    private const float WAIT_TIME = 0.3f;
    private static readonly float[] MOVE_TIME = new float[]{ 1f, 0.6f, 0.3f};
    public GameObject target;

    private bool startPopup;
    private int popupsDisplayed;
    private Text popupText;
    private Image popupImage;
    private float popupProgress;
    private float waitProgress;
    private float moveDownTime;
    private float moveDownProgress;
    private float targetPopupWidth;
    private float targetPopupHeight;
    private Vector3 targetPosition;
    private Vector3 finalPopupPosition;
    private string message;
    private Sprite sprite;
    private Sprite emptySprite;

    // Start is called before the first frame update
    void Start()
    {
        popupText = this.transform.Find("PopupText").gameObject.GetComponent<Text>();
        popupImage = this.transform.Find("PopupImage").gameObject.GetComponent<Image>();
        emptySprite = Resources.Load<Sprite>("Sprites/nothing");
    }

    // Update is called once per frame
    void Update()
    {
        if (startPopup)
        {
            ReadyPopup();
            startPopup = false;
        }
        else if (popupProgress > 0)
        {
            popupProgress -= Time.deltaTime;
            if (popupProgress <= 0)
            {
                popupProgress = 0;
                popupText.text = message;
                popupImage.sprite = sprite;
            }
            float ratio = (POPUP_TIME - popupProgress) / POPUP_TIME;
            RectTransform rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(ratio*targetPopupWidth, ratio*targetPopupHeight);
        }
        else if (waitProgress > 0)
        {
            waitProgress -= Time.deltaTime;
            waitProgress = (waitProgress < 0 ? 0 : waitProgress);
        }
        else if (moveDownProgress > 0)
        {
            moveDownProgress -= Time.deltaTime;
            moveDownProgress = (moveDownProgress < 0 ? 0 : moveDownProgress);
            float ratio = (moveDownTime - moveDownProgress) / moveDownTime;
            float newX = targetPosition.x + (finalPopupPosition.x - targetPosition.x) * ratio;
            float newY = targetPosition.y + (finalPopupPosition.y - targetPosition.y) * ratio;
            transform.position = new Vector3(newX, newY);
        }
    }

    private void ReadyPopup()
    {
        // Put the popup in the right place
        RectTransform targetTransform = (RectTransform)target.transform;
        targetPopupWidth = targetTransform.rect.width;
        targetPopupHeight = targetTransform.rect.height;
        targetPosition = targetTransform.position;
        RectTransform popupTransform = (RectTransform)transform;
        finalPopupPosition = popupTransform.position;
        popupTransform.position = targetPosition;
        popupImage.sprite = Resources.Load<Sprite>("Sprites/nothing");
        popupText.text = "";


        // Set the timers for the popup.
        popupProgress = POPUP_TIME;
        waitProgress = WAIT_TIME;
        moveDownTime = (popupsDisplayed - 1 >= MOVE_TIME.Length ?
            MOVE_TIME[MOVE_TIME.Length - 1] : MOVE_TIME[popupsDisplayed - 1]);
        moveDownProgress = moveDownTime;
        popupTransform.sizeDelta = new Vector2(0, 0);
    }

    public void Popup(string inMessage, string imageName)
    {
        ++popupsDisplayed;

        // Load the popup message and image.
        message = inMessage;
        imageName = ((imageName == null) || (imageName.Trim() == "") ? "nothing" : imageName);
        Sprite loaded = Resources.Load<Sprite>("Sprites/" + imageName);
        if (loaded == null)
        {
            loaded = emptySprite;
        }
        sprite = loaded;
        startPopup = true;

        // Display it
        this.gameObject.SetActive(true);
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
