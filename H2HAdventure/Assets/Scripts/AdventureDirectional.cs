using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdventureDirectional : MonoBehaviour
{

#if UNITY_STANDALONE || UNITY_WEBPLAYER
#else
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
        float centery = Screen.height/2.0f;
        float tan22 = 0.414f;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            float dirx = touch.position.x - centerx;
            float diry = touch.position.y - centery;
            joyLeft = (dirx < 0) && (Mathf.Abs(dirx) > Mathf.Abs(diry) * tan22);
            joyRight = (dirx > 0) && (Mathf.Abs(dirx) > Mathf.Abs(diry) * tan22);
            joyUp = (diry > 0) && (Mathf.Abs(diry) > Mathf.Abs(dirx) * tan22);
            joyDown = (diry < 0) && (Mathf.Abs(diry) > Mathf.Abs(dirx) * tan22);
        } else {
            joyLeft = joyRight = joyUp = joyDown = false;
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
        return Input.GetKey(KeyCode.Return);
    }


}
