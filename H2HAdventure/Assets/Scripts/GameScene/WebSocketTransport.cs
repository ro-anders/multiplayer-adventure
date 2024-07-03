using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using GameEngine;


namespace GameScene
{
    /**
     * This implements the H2HAdventure Transport for WebGL H2HAdventure games
     * by making websocket requests to a server running in Fargate that relays them
     * to other WebGL games.
     */
    public class WebSocketTransport: MonoBehaviour, Transport
    {

        private string HOST_ADDRESS = "ws://localhost:4080";
        private bool connected = false;

        private const int SERVER_BYTES = 2;
        private const byte NO_SESSION = 0x00;
        private const byte MESSAGE_CODE = 0x00;
        private const byte CONNECT_CODE = 0x01;
        /** A string representing the game this player is playing. */
        private byte session = NO_SESSION;

        /** Whether this is Player 1, 2 or 3.  Though value is acually 0, 1 or 2. */
        private int thisPlayerSlot = -1;

        private WebSocket websocket;

        /** Number of clients (including this one) connected to the session. */
        private int numClients = 0;

        /** Keep a queue of actions read from the server. */
        private Queue<RemoteAction> receviedActions = new Queue<RemoteAction>();

        public int ThisPlayerSlot
        {
            get { return thisPlayerSlot; }
        }

        public int NumberClientsConnected
        {
            get { return numClients; }
        }

        void Start()
        {
            // Start doesn't do anything.  It's all done
            // in Connect()
        }   

        // Update is called once per frame.
        // Do we need this?
        void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            if (websocket != null) {
                websocket.DispatchMessageQueue();
            }
            #endif
        }

        // Connect to the back end server
        public async Task<bool> Connect(byte session_in, int slot_in) {
            UnityEngine.Debug.Log("Setting up backend server connection");
            if ((session != NO_SESSION) && (session != session_in)) {
                // Something's wrong.  Why are we connecting twice?
                throw new Exception("Connect() called on already connected web socket transport");
            }
            session = session_in;
            thisPlayerSlot = slot_in;

            if (websocket != null) {
                return websocket.State == WebSocketState.Open;
            }

            var thisTask = new TaskCompletionSource<bool>();

            websocket = new WebSocket(HOST_ADDRESS + "/ws");

            websocket.OnOpen += async () =>
            {
                // Send a request to open a connection to the server.
                // An open request is 2 bytes, the first being the session and the second being 0x01.
                Debug.Log("Connection open!");
                connected = true;
                byte[] bytes = new byte[] {session, CONNECT_CODE};
                Debug.Log("Requesting connect with session " + session + ": [ " + String.Join(", ", bytes) + "]");
                await websocket.Send(bytes);
                thisTask.SetResult(true);
            };

            websocket.OnError += (e) =>
            {
                Debug.Log("Error! " + e);
                if (!connected) {
                    thisTask.SetResult(false);
                }
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                if (!connected) {
                    thisTask.SetResult(false);
                }
            };

            websocket.OnMessage += (bytes) =>
            {
                const int MIN_GAME_MESSAGE_SIZE = 10; // 1 for session, 1 for server code, 4 for slot, 4 for message code
                if (bytes.Length < 3) {
                    Debug.LogWarning("Unexpected " + (bytes.Length == 0 ? "empty" : "short") + "message.");
                    return;
                }
                byte msg_session = bytes[0];
                Debug.Log("Received [" + string.Join(" ", bytes) + "] for session " + msg_session);

                if (session != msg_session) {
                    Debug.LogWarning("Unexpected message.  Session mismatch.  '" + msg_session + "' != '" + session + "'");
                    return;
                }

                byte msg_code = bytes[1];
                if (msg_code == CONNECT_CODE) {
                    // Process a system message
                    numClients = bytes[2];
                }
                else {
                    // Process a game message
                    if (bytes.Length < MIN_GAME_MESSAGE_SIZE) {
                        Debug.LogWarning("Unexpected " + (bytes.Length == 0 ? "empty" : "short") + "message.");
                        return;
                    }

                    // Convert byte array to int array and deserialize.
                    // Skip the bytes reserved by the server.  They are used by the server. 
                    int[] ints = new int[(bytes.Length - SERVER_BYTES) / sizeof(int)];
                    Buffer.BlockCopy(bytes, SERVER_BYTES, ints, 0, bytes.Length - SERVER_BYTES);
                    
                    RemoteAction nextAction = deserializeAction(ints);
                    UnityEngine.Debug.Log("Decoded action " + nextAction);
                    if (nextAction != null) {
                        receviedActions.Enqueue(nextAction);
                    }
                }
            };

            UnityEngine.Debug.Log("Initiating connection");
            _ = websocket.Connect();
            UnityEngine.Debug.Log("waiting for callbacks");

            bool return_val = await thisTask.Task;
            UnityEngine.Debug.Log("Connection setup complete");
            return return_val;
        }

        public void send(RemoteAction action) {
            if (websocket.State == WebSocketState.Open)
            {
                // Serialize into a byte array.  The first byte of the byte array is the 
                // session.
                action.setSender(thisPlayerSlot);
                int[] ints = action.serialize();
                byte[] bytes = new byte[ints.Length * sizeof(int) + SERVER_BYTES];
                bytes[0] = session;
                bytes[1] = MESSAGE_CODE;
                Buffer.BlockCopy(ints, 0, bytes, SERVER_BYTES, bytes.Length-SERVER_BYTES);
                Debug.Log("Sending action " + action.ToString() + " on session " + session);
                Debug.Log("Sending [" + string.Join(" ", bytes) + "]");
                websocket.Send(bytes);
            }
        }
        
        private async void OnApplicationQuit()
        {
            if (websocket != null) {
                await websocket.Close();
            }
        }

        RemoteAction Transport.get()
        {
            if (receviedActions.Count == 0)
            {
                return null;
            }
            else
            {
                RemoteAction nextAction = receviedActions.Dequeue();
                return nextAction;
            }
        }

        private RemoteAction deserializeAction(int[] dataPacket) {
            ActionType type = (ActionType)dataPacket[0];
            int sender = dataPacket[1];
            if (sender != thisPlayerSlot)
            {
                RemoteAction action=null;
                switch (type)
                {
                    case ActionType.PLAYER_MOVE:
                        action = new PlayerMoveAction();
                        break;
                    case ActionType.PLAYER_PICKUP:
                        action = new PlayerPickupAction();
                        break;
                    case ActionType.PLAYER_RESET:
                        action = new PlayerResetAction();
                        break;
                    case ActionType.PLAYER_WIN:
                        action = new PlayerWinAction();
                        break;
                    case ActionType.DRAGON_MOVE:
                        action = new DragonMoveAction();
                        break;
                    case ActionType.DRAGON_STATE:
                        action = new DragonStateAction();
                        break;
                    case ActionType.PORTCULLIS_STATE:
                        action = new PortcullisStateAction();
                        break;
                    case ActionType.BAT_MOVE:
                        action = new BatMoveAction();
                        break;
                    case ActionType.BAT_PICKUP:
                        action = new BatPickupAction();
                        break;
                    case ActionType.OBJECT_MOVE:
                        action = new ObjectMoveAction();
                        break;
                    case ActionType.PING:
                        action = new PingAction();
                        break;
                }
                action.deserialize(dataPacket);
                return action;
            }
            else {
                return null;
            }
        }


    }
}



