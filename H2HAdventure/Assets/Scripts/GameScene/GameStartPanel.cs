using System.Collections;
using System.Collections.Generic;
using GameScene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameStartPanel : MonoBehaviour
{
    public TMP_Text p1placement;
    public TMP_Text p2placement;
    public TMP_Text p3placement;
    public UnityEngine.UI.Button startButton;
    public TMP_Text startInstructions;

    /// <summary>
    /// This immediately deactivates until it gets info from the
    /// server.
    /// </summary>
    void Start()
    {
        this.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Display the game startup window and populate the content
    /// with the game info
    /// </summary>
    public void showGameInfo(WebGameSetup setup) {
        string[] players = setup.PlayerNames;
        if (setup.Slot == 0) {
            p1placement.text = p1placement.text.Replace("Player1 is", "You are");
        } else {
            p1placement.text = p1placement.text.Replace("Player1", players[0]);
        }
        if (setup.Slot == 1) {
            p2placement.text = p2placement.text.Replace("Player2 is", "You are");
        } else {
            p2placement.text = p2placement.text.Replace("Player2", players[1]);
        }
        if (players.Length > 2) {
            if (setup.Slot == 2) {
                p3placement.text = p3placement.text.Replace("Player3 is", "You are");
            } else {
                p3placement.text = p3placement.text.Replace("Player3", players[2]);
            }
        }
        else {
            p3placement.gameObject.SetActive(false);
        }
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// The start button has been pressed.  Remove the button
    /// and display a waiting message.
    /// </summary>
    public void markStartPressed() {
        startButton.interactable = false;
        startInstructions.text = "Waiting for others...";
    }
}
