using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using GameEngine;
using System.Linq;
using System.Xml;


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

    [Serializable]
    public class ChatMessage {
        public int slot;
        public string message;
    }

    /**
     * This implements the H2HAdventure Transport for WebGL H2HAdventure games
     * by making websocket requests to a server running in Fargate that relays them
     * to other WebGL games.
     */
    public class WebSocketTransport: MonoBehaviour, Transport
    {
        /** When wanting to run in dummy mode (running stand alone and mimicing
            an internet game) pass this in as the host URL. */
        public const string DUMMY_HOST="dummy";
        private const int DUMMY_FAKE_PAUSE=200; /* 200 clicks, ~3 seconds */

        private bool connected = false;

        private const int SERVER_BYTES = 2;
        private const byte NO_SESSION = 0x00;
        private const byte MESSAGE_CODE = 0x00;
        private const byte CONNECT_CODE = 0x01;
        private const byte READY_CODE = 0x02;
        private const byte CHAT_CODE = 0x03;
        private const byte REPORT_TO_SERVER_CODE = 0x04;
        private const byte LATENCY_CHECK_CODE = 0x05;

        /** For testing and debugging purposes DUMMY mode will not try
         * to connect to a back end server and will fake that the game is running
         * normally when it is running just by itself. */
        private bool dummy_mode = false;

        /** When in dummy mode we occasionally fake waiting for other clients
         * when really we are waiting a set time. */
        private int dummy_mode_timer = -1;

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

        private Queue<ChatMessage> receivedChats = new Queue<ChatMessage>();

        /** Keep some stats for latency */
        private float ave_latency = 0;
        private int num_latency_readings = 0;

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
        void Update()
        {
            // Do we need this?
            #if !UNITY_WEBGL || UNITY_EDITOR
            if (websocket != null) {
                websocket.DispatchMessageQueue();
            }
            #endif

            if (dummy_mode_timer > 0) {
                --dummy_mode_timer;
                if (dummy_mode_timer == 0) {
                    if (gameInfo == null) {
                        gameInfo = new RunningGame();
                        gameInfo.session = session;
                        gameInfo.game_number = 1;
                        gameInfo.number_players = 3;
                        gameInfo.fast_dragons = false;
                        gameInfo.fearful_dragons = false;
                        gameInfo.player_names = new string[]{"Tom", "Dick", "Harry"};
                        gameInfo.joined_players = 3;
                    } else {
                        receivedReady = true;
                    }
                }
            }

        }

        /// <summary>
        /// Connect to the back end server
        /// </summary>
        /// <param name="backend_host_in">the IP of the back end server</param>
        /// <param name="session_in">the unique id of the game to play</param>         
        /// <param name="slot_in">the player number of this player (0-2)</param>         
        public async Task<bool> Connect(String backend_host_in, byte session_in, int slot_in) {
            GameEngine.Logger.Info("Setting up backend server connection");

            // If running in dummy mode, set some fake setup values and return
            if (backend_host_in == DUMMY_HOST) {
                dummy_mode = true;
                dummy_mode_timer = DUMMY_FAKE_PAUSE;
                session = session_in;
                thisPlayerSlot = slot_in;
                return true;
            }

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

            // When running locally we use unsecured web socket on
            // port 4000.  When running in production we use a 
            // secured web socket on port 80.
            string wsurl = host_address == "127.0.0.1" ? 
                "ws://127.0.0.1:4000/ws" : 
                "wss://" + host_address + "/ws";
                GameEngine.Logger.Info($"Connecting to {wsurl}");
            websocket = new WebSocket(wsurl);

            websocket.OnOpen += async () =>
            {
                // Send a request to open a connection to the server.
                // An open request is 2 bytes, the first being the session and the second being 0x01.
                GameEngine.Logger.Debug("Connection open!");
                connected = true;
                byte[] bytes = new byte[] {session, CONNECT_CODE};
                GameEngine.Logger.Info("Requesting connect with session " + session + ": [ " + String.Join(", ", bytes) + "]");
                await websocket.Send(bytes);
                thisTask.SetResult(true);
            };

            websocket.OnError += (e) =>
            {
                GameEngine.Logger.Error("Error connecting to " + wsurl + "! " + e);
                if (!connected) {
                    thisTask.SetResult(false);
                }
            };

            websocket.OnClose += (e) =>
            {
                GameEngine.Logger.Info("Connection closed!");
                if (!connected) {
                    thisTask.SetResult(false);
                }
            };

            websocket.OnMessage += (bytes) =>
            {
                if (bytes.Length < 2) {
                    GameEngine.Logger.Warn("Unexpected " + (bytes.Length == 0 ? "empty" : "short") + "message.");
                    return;
                }
                byte msg_session = bytes[0];
                GameEngine.Logger.Debug("Received [" + string.Join(" ", bytes) + "] for session " + msg_session);

                if (session != msg_session) {
                    GameEngine.Logger.Warn("Unexpected message.  Session mismatch.  '" + msg_session + "' != '" + session + "'");
                    return;
                }

                byte msg_code = bytes[1];
                if (msg_code == READY_CODE) {
                    GameEngine.Logger.Info("Received message game is ready to start");
                    receivedReady = true;
                }
                else if (msg_code == CONNECT_CODE) {
                    // Process a system message.  A system message, after the first two bytes, is 
                    // JSON.
                    byte[] json_bytes = bytes.Skip(SERVER_BYTES).ToArray();
                    char[] json_chars = System.Text.Encoding.UTF8.GetChars(json_bytes);
                    string json_str = new string(json_chars);
                    GameEngine.Logger.Debug("Deserializing game info from \"" + json_str + "\"");
                    gameInfo = JsonUtility.FromJson<RunningGame>(json_str);
                    GameEngine.Logger.Info("Read game info for Game #" + (gameInfo.game_number+1) +
                        " (" + gameInfo.session + ") with " + gameInfo.joined_players + " of " + 
                        gameInfo.number_players + " players joined.");
                }
                else if (msg_code == CHAT_CODE) {
                    // Process a chat message. A system message, after the first two bytes, is 
                    // JSON.
                    byte[] json_bytes = bytes.Skip(SERVER_BYTES).ToArray();
                    char[] json_chars = System.Text.Encoding.UTF8.GetChars(json_bytes);
                    string json_str = new string(json_chars);
                    ChatMessage chat = JsonUtility.FromJson<ChatMessage>(json_str);
                    GameEngine.Logger.Debug("Read chat message from player #" + chat.slot + ": \"" +
                        chat.message);
                    receivedChats.Enqueue(chat);
                }
                else if (msg_code == LATENCY_CHECK_CODE) {
                    // Pull out the timestamp and compare it to the current time stamp
                    // The message contains the last 16 bits of a UNIX timestamp
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    int receivedMs = (int)(timestamp & 0xFFFF);
                    int sentMs = bytes[2]*256 + bytes[3];
                    int latency = receivedMs-sentMs;
                    ave_latency = ((ave_latency * num_latency_readings) + latency) / (num_latency_readings +1);
                    num_latency_readings += 1;
                    GameEngine.Logger.Info("Average latency = " + (int)(ave_latency) + "ms");
                    // Downgrade latency readings older than 3 minutes
                    if (num_latency_readings == 8) {
                        num_latency_readings = 4;
                    }
                }
                else {
                    // Process a game message
                    const int MIN_GAME_MESSAGE_SIZE = 10; // 1 for session, 1 for server code, 4 for slot, 4 for message code
                    if (bytes.Length < MIN_GAME_MESSAGE_SIZE) {
                        GameEngine.Logger.Warn("Unexpected short message: " + bytes);
                        return;
                    }

                    // Convert byte array to int array and deserialize.
                    // Skip the bytes reserved by the server.  They are used by the server. 
                    int[] ints = new int[(bytes.Length - SERVER_BYTES) / sizeof(int)];
                    Buffer.BlockCopy(bytes, SERVER_BYTES, ints, 0, bytes.Length - SERVER_BYTES);
                    
                    RemoteAction nextAction = deserializeAction(ints);
                    GameEngine.Logger.Debug("Decoded action " + nextAction);
                    if (nextAction != null) {
                        receviedActions.Enqueue(nextAction);
                    }
                }
            };

            GameEngine.Logger.Info("Initiating connection");
            _ = websocket.Connect();
            GameEngine.Logger.Info("waiting for callbacks");

            bool return_val = await thisTask.Task;
            GameEngine.Logger.Info("Connection setup complete");
            return return_val;
        }

        public void send(RemoteAction action) {
            if (!dummy_mode && websocket.State == WebSocketState.Open)
            {
                // Serialize into a byte array.  The first byte of the byte array is the 
                // session.
                action.setSender(thisPlayerSlot);
                int[] ints = action.serialize();
                byte[] bytes = new byte[ints.Length * sizeof(int) + SERVER_BYTES];
                bytes[0] = session;
                bytes[1] = MESSAGE_CODE;
                Buffer.BlockCopy(ints, 0, bytes, SERVER_BYTES, bytes.Length-SERVER_BYTES);
                GameEngine.Logger.Debug("Sending action " + action.ToString() + " on session " + session);
                GameEngine.Logger.Debug("Sending [" + string.Join(" ", bytes) + "]");
                websocket.Send(bytes);

                // Every time we send a ping, we also check the latency
                if (action.typeCode == ActionType.PING) {
                    sendLatencyCheck();
                }
            }
        }

        public void sendChat(string message) {
            if (!dummy_mode && websocket.State == WebSocketState.Open)
            {
                // First, express the player number and message as a JSON string
                // and encode it as a byte array.
                ChatMessage chat = new ChatMessage {slot = thisPlayerSlot, message = message};
                GameEngine.Logger.Debug("Sending chat \"" + chat.message + "\" on session " + session);
                string json = JsonUtility.ToJson(chat);
                char[] json_chars = json.ToCharArray();
                byte[] json_bytes = System.Text.Encoding.UTF8.GetBytes(json_chars);

                // The byte array to send starts with the session and code as the first
                // two bytes and the encoded JSON string as the rest.
                byte[] msg_bytes = new byte[json_bytes.Length + SERVER_BYTES];
                msg_bytes[0] = session;
                msg_bytes[1] = CHAT_CODE;
                Buffer.BlockCopy(json_bytes, 0, msg_bytes, SERVER_BYTES, json_bytes.Length);
                GameEngine.Logger.Debug("Sending [" + string.Join(" ", msg_bytes) + "]");
                websocket.Send(msg_bytes);
            }
        }

        // <summary>
        /// Send a message that this client is ready to start the game.
        /// </summary>
        public void sendReady() {
            if (dummy_mode) {
                dummy_mode_timer = DUMMY_FAKE_PAUSE;
            } else {
                if (websocket.State != WebSocketState.Open)
                {
                    throw new Exception("Cannot start game before web socket is open");
                }
                byte[] bytes = new byte[] {session, READY_CODE};
                GameEngine.Logger.Info("Requesting start game " + session);
                websocket.Send(bytes);
            }
        }

        // <summary>
        /// Send a message that the server sends right back so we can
        /// check how much latgency there is in the game
        /// </summary>
        private void sendLatencyCheck() {
            if (!dummy_mode && websocket.State == WebSocketState.Open) {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // Don't need the whole time stamp, just the seconds and milliseconds
                byte byte0 = (byte)(timestamp & 0xFF);
                byte byte1 = (byte)((timestamp >> 8) & 0xFF);               
                byte[] bytes = new byte[] {session, LATENCY_CHECK_CODE, byte1, byte0};
                websocket.Send(bytes);
            }
        }

        /// <summary>
        /// When the player stats change.  Usually this is someone won the game, but
        /// can also be used for discoveries
        /// </summary>
        public void reportToServer(int messageCode) {
            if (websocket.State != WebSocketState.Open)
            {
                throw new Exception("Cannot start game before web socket is open");
            }
            byte[] bytes = new byte[] {session, REPORT_TO_SERVER_CODE, (byte)thisPlayerSlot, (byte)messageCode };
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

        /// <summary>
        /// Get any chat messages that have been received
        /// </summary>
        /// <returns>the next chat message or null if no chat messages
        /// have been received</returns>
        public ChatMessage getChat() {
            return (receivedChats.Count == 0 ? null : receivedChats.Dequeue());
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



