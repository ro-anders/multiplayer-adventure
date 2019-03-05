using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdventureDirectional : MonoBehaviour
{
    bool hasResetBeenPressed = false;

#if UNITY_STANDALONE || UNITY_WEBPLAYER
#else
    private bool isDragging = false;
    private int dragId;
    private float dragStartX;
    private float dragStartY;
    private bool joyLeft = false;
    private bool joyRight = false;
    private bool joyUp = false;
    private bool joyDown = false;
#endif

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_STANDALONE || UNITY_WEBPLAYER
#else
        float centerx = Screen.width/2.0f;
        float tan22 = 0.414f;
        joyLeft = joyRight = joyUp = joyDown = false;
        bool gotDrop = false;
        bool gotDrag = false;
        for(int ctr=0; ctr<Input.touches.Length; ++ctr)
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
                        float dirx = nextTouch.position.x - dragStartX;
                        float diry = nextTouch.position.y - dragStartY;
                        joyLeft = (dirx < 0) && (Mathf.Abs(dirx) > Mathf.Abs(diry) * tan22);
                        joyRight = (dirx > 0) && (Mathf.Abs(dirx) > Mathf.Abs(diry) * tan22);
                        joyUp = (diry > 0) && (Mathf.Abs(diry) > Mathf.Abs(dirx) * tan22);
                        joyDown = (diry < 0) && (Mathf.Abs(diry) > Mathf.Abs(dirx) * tan22);
                    }
                } else if ((nextTouch.phase == TouchPhase.Began) && (nextTouch.position.x < centerx))
                {
                    gotDrag = true;
                    isDragging = true;
                    dragStartX = nextTouch.position.x;
                    dragStartY = nextTouch.position.y;
                }
            }
        }
#endif
    }

    public void getDirection(ref bool outLeft, ref bool outUp, ref bool outRight, ref bool outDown)
    {
#if UNITY_STANDALONE || UNITY_WEBPLAYER
        outLeft = Input.GetKey(KeyCode.LeftArrow);
        outUp = Input.GetKey(KeyCode.UpArrow);
        outRight = Input.GetKey(KeyCode.RightArrow);
        outDown = Input.GetKey(KeyCode.DownArrow);
#else
        outLeft = joyLeft;
        outRight = joyRight;
        outUp = joyUp;
        outDown = joyDown;
#endif
    }

    public bool getDropButton() {
        return Input.GetKey(KeyCode.Space);
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
