﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GameScene
{

    /** 
    * This is the canvas for the test multiplayer screen.
    * It handles events like button presses, and it is
    * the one who coordinates that start of the game before
    * handing it off to the Adventure View.
    */
    public class SimpleMplayerScreen : MonoBehaviour
    {
        public GameObject gamePanel;
        public SimpleMplayerAdvView advView;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        [DllImport("__Internal")]
        private static extern void BrowserGoBack();


        public void OnStartClicked()
        {
            gamePanel.SetActive(true);
            advView.SetReady();
        }

        public void OnRespawnClicked()
        {
            
        }

        public void OnQuickClicked() {
            BrowserGoBack();
        }

    }
}