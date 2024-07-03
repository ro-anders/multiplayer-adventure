using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * This is the canvas for the Single Player Screen.
 * It handles events, which is just the button presses.
 */
public class MultiPlayerScreen : MonoBehaviour
{
    public GameObject gamePanel;
    public SinglePlayerAdvView advView;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartClicked()
    {
        gamePanel.SetActive(true);
        advView.PlayGame();
    }

}
