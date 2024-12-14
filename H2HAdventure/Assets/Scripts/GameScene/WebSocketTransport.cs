using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using GameEngine;


namespace GameScene
{
    /**
     * This represents the characteristics of the game (which board, the
     * difficulty settings).  It does not change once agreed upon.
     */
    public class GameDetails {
        public int session;
        public int game_number;
        public int number_players; /* Should be 2 or 3.  By this point 2.5 is not allowed. */
        public bool fast_dragons;
        public bool fearful_dragons;
        public string[] player_names;
    }

    /**
     * This represents a running game.  It has the game details but also
     * state of the running game.
     */
    public class RunningGame: GameDetails {
        public int joined_players;
    }

    /**
     * This implements the H2HAdventure Transport for WebGL H2HAdventure games
     * by making websocket requests to a server running in Fargate that relays them
     * to other WebGL games.
     */
    public class WebSocketTransport: MonoBehaviour, Transport
    {

        private bool connected = false;

        private const int SERVER_BYTES = 2;
        private const byte NO_SESSION = 0x00;
        private const byte MESSAGE_CODE = 0x00;
        private const byte CONNECT_CODE = 0x01;
        private const byte READY_CODE = 0x02;
        /** A string representing the game this player is playing. */
        private byte session = NO_SESSION;

        /** Whether this is Player 1, 2 or 3.  Though value is acually 0, 1 or 2. */
        private int thisPlayerSlot = -1;

        private string host_address = "localhost";

        private WebSocket websocket;

        /** The details of the game being played */
        private RunningGame gameInfo = null;

        /** Whether the server has announced it is ready to start the game */
        private bool receivedReady = false;

        /** Keep a queue of actions read from the server. */
        private Queue<RemoteAction> receviedActions = new Queue<RemoteAction>();

        public int ThisPlayerSlot
        {
            get { return thisPlayerSlot; }
        }

        public RunningGame GameInfo
        {
            get { return gameInfo; }
        }

        public int NumberClientsConnected
        {
            get { return (gameInfo == null ? 0 : gameInfo.joined_players); }
        }

        public bool ReceivedReady {
            get { return receivedReady; }
        }         

        public void Start()
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

        /// <summary>
        /// Connect to the back end server
        /// </summary>
        /// <param name="backend_host_in">the IP of the back end server</param>
        /// <param name="session_in">the unique id of the game to play</param>         
        /// <param name="slot_in">the player number of this player (0-2)</param>         
        public async Task<bool> Connect(String backend_host_in, byte session_in, int slot_in) {
            UnityEngine.Debug.Log("Setting up backend server connection");
            if ((session != NO_SESSION) && (session != session_in)) {
                // Something's wrong.  Why are we connecting twice?
                throw new Exception("Connect() called on already connected web socket transport");
            }
            session = session_in;
            thisPlayerSlot = slot_in;
            host_address = backend_host_in;

            if (websocket != null) {
                return websocket.State == WebSocketState.Open;
            }

            var thisTask = new TaskCompletionSource<bool>();

            websocket = new WebSocket("ws://" + host_address + ":4000/ws");

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
                if (bytes.Length < 2) {
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
                if (msg_code == READY_CODE) {
                    Debug.Log("Received message game is ready to start");
                    receivedReady = true;
                }
                if (msg_code == CONNECT_CODE) {
                    // Process a system message.  A system message, after the first two bytes, is 
                    // JSON.
                    char[] json_bytes = new char[bytes.Length - 1];
                    for(int i = 2; i < bytes.Length; i++) {
                        json_bytes[i - 2] = (char)bytes[i];
                    }
                    json_bytes[json_bytes.Length - 1] = '\0';
                    string json_str = new string(json_bytes);
                    Debug.Log("Deserializing game info from \"" + json_str + "\"");
                    gameInfo = JsonUtility.FromJson<RunningGame>(json_str);
                    Debug.Log("Read game info for Game #" + (gameInfo.game_number+1) +
                        " (" + gameInfo.session + ") with " + gameInfo.joined_players + " of " + 
                        gameInfo.number_players + " players joined.");
                }
                else {
                    // Process a game message
                    const int MIN_GAME_MESSAGE_SIZE = 10; // 1 for session, 1 for server code, 4 for slot, 4 for message code
                    if (bytes.Length < MIN_GAME_MESSAGE_SIZE) {
                        Debug.LogWarning("Unexpected short message: " + bytes);
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

        // <summary>
        /// Send a message that this client is ready to start the game.
        /// </summary>
        public void sendReady() {
            if (websocket.State != WebSocketState.Open)
            {
                throw new Exception("Cannot start game before web socket is open");
            }
            byte[] bytes = new byte[] {session, READY_CODE};
            Debug.Log("Requesting start game " + session);
            websocket.Send(bytes);
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

        /// <summary>
        /// For running in the editor, create fake game info
        /// </summary>
        public void fakeGameInfo() {
            gameInfo = new RunningGame();
            gameInfo.session = 42;
            gameInfo.game_number = 1;
            gameInfo.number_players = 3;
            gameInfo.fast_dragons = false;
            gameInfo.fearful_dragons = false;
            gameInfo.player_names = new string[]{"Tom", "Dick", "Harry"};
            gameInfo.joined_players = 3;
        }

        /// <summary>
        /// For running in the editor.  Pretend all other clients
        /// have reported they are ready to start the game.
        /// </summary>
        public void fakeReceivedReady() {
            receivedReady = true;
        }


    }
}



