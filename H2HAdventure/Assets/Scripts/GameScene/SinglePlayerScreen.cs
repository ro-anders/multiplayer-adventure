using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using System.Runtime.InteropServices;

/** 
 * This is the canvas for the Single Player Screen.
 * It handles events, which is just the button presses.
 */
public class MultiPlayerScreen : MonoBehaviour
{
    public GameObject gamePanel;
    public GameObject instructionPanel;
    public SinglePlayerAdvView advView;
    public Text startButtonText;

    [DllImport("__Internal")]
    private static extern void BrowserGoBack();

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
        instructionPanel.SetActive(false);
        startButtonText.text = "New Game";
        advView.PlayGame();
    }

    public void onQuitClicked() {
        BrowserGoBack();
    }

}
