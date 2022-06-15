using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdventureDirectional : MonoBehaviour
{
    private const float MAX_TAP_TIME = 0.5f;

    public Image joystickImage;

    private bool hasResetBeenPressed = false;
    private int minDragDistance = 0;


#if UNITY_ANDROID || UNITY_IOS
    private bool isDragging = false;
    private int dragId;
    private float dragStartX;
    private float dragStartY;
    private bool isDropping = false;
    private bool hasDropBeenPressed = false;
    private int dropId;
    private float dropStart;
    private bool joyLeft = false;
    private bool joyRight = false;
    private bool joyUp = false;
    private bool joyDown = false;
#endif

    // Use this for initialization
    void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        Vector3 size = GetComponent<Renderer>().bounds.size;
        minDragDistance = (int)(size.x / 4);
#else
        if (joystickImage != null)
        {
            joystickImage.gameObject.SetActive(false);
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        float centerx = Screen.width/2.0f;
        float tan22 = 0.414f;
        joyLeft = joyRight = joyUp = joyDown = false;
        bool gotDrop = false;
        bool gotDrag = false;
        for (int ctr = 0; ctr < Input.touches.Length; ++ctr)
        {
            Touch nextTouch = Input.touches[ctr];
            // Look for drag
            if (!gotDrag)
            {
                if (isDragging && (nextTouch.fingerId == dragId))
                {
                    gotDrag = true;
                    if (nextTouch.phase == TouchPhase.Began)
                    {
                        // Why are we getting a BEGAN twice?
                        Debug.LogError("Received multiple BEGAN events for finger drag " + dragId);
                    }
                    else if ((nextTouch.phase == TouchPhase.Ended) || (nextTouch.phase == TouchPhase.Canceled))
                    {
                        // Stopped dragging.  Clear the drag state.
                        gotDrag = false;
                        isDragging = false;
                    }
                    else
                    {
                        // Put test for minumum distance here
                        float dirx = nextTouch.position.x - dragStartX;
                        float diry = nextTouch.position.y - dragStartY;
                        joyLeft = (dirx < 0) && (Mathf.Abs(dirx) > Mathf.Abs(diry) * tan22);
                        joyRight = (dirx > 0) && (Mathf.Abs(dirx) > Mathf.Abs(diry) * tan22);
                        joyUp = (diry > 0) && (Mathf.Abs(diry) > Mathf.Abs(dirx) * tan22);
                        joyDown = (diry < 0) && (Mathf.Abs(diry) > Mathf.Abs(dirx) * tan22);
                    }
                }
                else if ((nextTouch.phase == TouchPhase.Began) && (nextTouch.position.x < centerx))
                {
                    gotDrag = true;
                    isDragging = true;
                    dragId = nextTouch.fingerId;
                    dragStartX = nextTouch.position.x;
                    dragStartY = nextTouch.position.y;
                    joystickImage.transform.position = nextTouch.position;
                }
            }
            // Look for tap
            if (!gotDrop)
            {
                if (isDropping && (nextTouch.fingerId == dropId))
                {
                    gotDrop = true;
                    if (nextTouch.phase == TouchPhase.Began)
                    {
                        // Why are we getting a BEGAN twice?
                        Debug.LogError("Received multiple BEGAN events for finger drag " + dragId);
                    }
                    else if ((nextTouch.phase == TouchPhase.Moved) || (nextTouch.phase == TouchPhase.Canceled))
                    {
                        // A move invalidates the tap.
                        isDropping = false;
                    }
                    else if (nextTouch.phase == TouchPhase.Ended)
                    {
                        // Released.  See if it counts as a tap.
                        float holdTime = Time.realtimeSinceStartup - dropStart;
                        if (holdTime < MAX_TAP_TIME)
                        {
                            hasDropBeenPressed = true;
                        }
                        isDropping = false;
                    }
                }
                else if ((nextTouch.phase == TouchPhase.Began) && (nextTouch.position.x > centerx))
                {
                    gotDrop = true;
                    isDropping = true;
                    dropId = nextTouch.fingerId;
                    dropStart = Time.realtimeSinceStartup;
                }
            }
        }
#endif
    }

    public void getDirection(ref bool outLeft, ref bool outUp, ref bool outRight, ref bool outDown)
    {
#if UNITY_ANDROID || UNITY_IOS
        outLeft = joyLeft;
        outRight = joyRight;
        outUp = joyUp;
        outDown = joyDown;
#else
        outLeft = Input.GetKey(KeyCode.LeftArrow);
        outUp = Input.GetKey(KeyCode.UpArrow);
        outRight = Input.GetKey(KeyCode.RightArrow);
        outDown = Input.GetKey(KeyCode.DownArrow);
#endif
    }

    public bool getDropButton() {
#if UNITY_ANDROID || UNITY_IOS
        bool returnVal = hasDropBeenPressed;
        hasDropBeenPressed = false;
        return returnVal;
#else
        return Input.GetKey(KeyCode.Space);
#endif
    }

    public bool getResetButton() {
        bool returnVal = hasResetBeenPressed;
        hasResetBeenPressed = false;
        return returnVal;
    }

    public void OnResetPressed()
    {
        hasResetBeenPressed = true;
    }

}
