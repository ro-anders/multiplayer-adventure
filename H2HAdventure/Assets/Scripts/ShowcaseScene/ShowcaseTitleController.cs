using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowcaseTitleController : MonoBehaviour
{
    public ShowcaseController parent;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            parent.TitleHasBeenDismissed();
        }
    }
}
