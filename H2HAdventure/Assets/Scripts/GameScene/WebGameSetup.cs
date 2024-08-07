using System;
using System.Web;
using GameScene;
using JetBrains.Annotations;
using UnityEngine;

namespace GameScene
{

    /************************************************************************************
    * The WebGameSetup handles the setup of a WebGL-based game which has only one scene and
    * assumes all game setup and lobbying has been done in a separate web application
    * with all needed game information passed through the URL parameters.
    * It will connect with the WebSocket server and, once all players are connected, start
    * the game using the WebSocket server as a transport for messages
    */
    public class WebGameSetup
    {
        private WebSocketTransport transport;

        private int slot = -1;

        private byte session = 0x01;

        private int numPlayersNeeded = 10; // Some big, unreachable number

        private int gameNum = -1;

        /** The IP address of the Game Backend server which is either running
         * in Fargate or locally */
        private string backend_host = "localhost";

        public int Slot {
            get {return slot;}
        }

        public bool IsReady { 
            get { return (transport != null) && (transport.NumberClientsConnected >= numPlayersNeeded) ;}
        }

        public int NumPlayers {
            get {return numPlayersNeeded;}
        }

        public int GameNumber {
            get {return gameNum;}
        }

        // In a WebGame, setup parameters are passed through the URL of the game.
        // Once read in, connection with a broker server is made and, once all 
        // players are connected, the game starts.
        public WebGameSetup(WebSocketTransport transport_in)
        {
            transport = transport_in;
            const string GAMECODE_PARAM = "gamecode";
            const string SLOT_PARAM = "slot";
            const string HOST_PARAM = "host";

            slot = 0;
            // Temporarily hardcode
            numPlayersNeeded = 2;
            gameNum = 0;

            if (Application.isEditor) {
                UnityEngine.Debug.Log("Web game setup disabled when running in editor");
                session = 0x01;
                slot=0;
            }
            else {
                // Expecting a URL like http://localhost:55281/?gamecode=1234&slot=1
                Debug.Log("Reading URL");
                string urlstr = Application.absoluteURL;
                Debug.Log("URL = " + urlstr);
                Uri url = new Uri(urlstr);
                string session_str = HttpUtility.ParseQueryString(url.Query).Get(GAMECODE_PARAM);
                if (session_str != null) {
                    // Not dealing with hexadecimal, so parse into an int and then to a byte
                    int session_int = Int32.Parse(session_str);
                    session = (byte)session_int;
                }
                string slot_str = HttpUtility.ParseQueryString(url.Query).Get(SLOT_PARAM);
                if (slot_str != null) {
                    slot = Int32.Parse(slot_str);
                }
                string host_str = HttpUtility.ParseQueryString(url.Query).Get(HOST_PARAM);
                if (host_str != null) {
                    backend_host = host_str;
                }
                Debug.Log("Game setup: session=" + session + ", slot=" + slot);
            }
        }

        public void Connect() {
            // Initiate transport connecting with the backend server and with all the other
            // clients
            _ = transport.Connect(backend_host, session, slot);
        }
    }

}