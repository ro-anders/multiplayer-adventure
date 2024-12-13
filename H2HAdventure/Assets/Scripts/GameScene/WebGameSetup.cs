using System;
using System.Collections.Generic;
using System.Web;
using GameScene;
using JetBrains.Annotations;
using UnityEngine;

namespace GameScene
{

    /************************************************************************************
    * The WebGameSetup handles the setup of a WebGL-based game which has only one scene and
    * assumes all game setup and lobbying has been done in a separate web application
    * with all needed game information passed through URL parameters and web sockets calls.
    * It will connect with the WebSocket server and, once all players are connected, start
    * the game using the WebSocket server as a transport for messages
    */
    public class WebGameSetup
    {
        // States of Web Game Setup
        public const int WAITING_FOR_GAMEINFO = 0;
        public const int WAITING_FOR_PLAYER = 1;
        public const int WAITING_FOR_OTHERS = 2;
        public const int GO = 3;

        private WebSocketTransport transport;

        private int slot = -1;

        private bool help_popups = false;

        private bool map_guides = false;

        private byte session = 0x01;

        private int web_game_setup_state = WAITING_FOR_GAMEINFO;

        /** The IP address of the Game Backend server which is either running
         * in Fargate or locally */
        private string backend_host = "localhost";

        public int Slot {
            get {return slot;}
        }

        public bool HelpPopups {
            get {return help_popups;}
        }

        public bool MapGuides {
            get {return map_guides;}
        }

        public int WebGameSetupState {
            get {
                // Check to see if state needs to be updated before returning.
                if (web_game_setup_state == WAITING_FOR_GAMEINFO) {
                    if ((transport != null) && (transport.GameInfo != null)) {
                        web_game_setup_state = WAITING_FOR_PLAYER;
                    }
                }
                if (web_game_setup_state == WAITING_FOR_OTHERS) {
                    if ((transport != null) && (transport.ReceivedReady)) {
                        web_game_setup_state = GO;
                    }
                }
                return web_game_setup_state;
            }
        }

        public string[] PlayerNames {
            get {return transport.GameInfo == null ? new string[0] : transport.GameInfo.player_names;}
        }

        public int NumPlayers {
            get {return (transport.GameInfo == null ? 10 : transport.GameInfo.number_players);}
        }

        public int GameNumber {
            get {return (transport.GameInfo == null ? -1 : transport.GameInfo.game_number);}
        }

        public bool FastDragons {
            get {return (transport.GameInfo == null ? false : transport.GameInfo.fast_dragons); }
        }

        public bool FearfulDragons {
            get {return (transport.GameInfo == null ? false : transport.GameInfo.fearful_dragons); }
        }

        // In a WebGame, setup parameters are passed through the URL of the game.
        // Once read in, connection with a broker server is made and, once all 
        // players are connected, the game starts.
        public WebGameSetup(WebSocketTransport transport_in)
        {
            transport = transport_in;
            const string GAMECODE_PARAM = "gamecode";
            const string HOST_PARAM = "host";

            slot = 0;
            help_popups = false;
            map_guides = false;

            if (Application.isEditor) {
                UnityEngine.Debug.Log("Web game setup disabled when running in editor");
                session = 0x01;
                slot=0;
                help_popups=false;
                map_guides= false;
            }
            else {
                // Expecting a URL like http://localhost:55281/?gamecode=1234&slot=1
                Debug.Log("Reading URL");
                string urlstr = Application.absoluteURL;
                Debug.Log("URL = " + urlstr);
                Uri url = new Uri(urlstr);
                string gamecode_str = HttpUtility.ParseQueryString(url.Query).Get(GAMECODE_PARAM);
                if (gamecode_str != null) {
                    // Not dealing with hexadecimal, so parse into an int and then to a byte
                    int gamecode_int = Int32.Parse(gamecode_str);
                    // First bit of gamecode is map guides boolean
                    map_guides = gamecode_int % 2 == 1;
                    // Second bit is help popups boolean
                    help_popups = (gamecode_int/2) % 2 == 1;
                    // Next two bits are slot 0-2
                    slot = (gamecode_int/4) % 4;
                    // Rest is session
                    session = (byte)(gamecode_int/16);
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

        /// <summary>
        /// Mark that this player is ready to start the game.
        /// </summary>
        public void SetReady() {
            if (web_game_setup_state == WAITING_FOR_PLAYER) {
                web_game_setup_state = WAITING_FOR_OTHERS;
                if (!Application.isEditor) {
                    transport.sendReady();
                }
            }
        }
    }

}