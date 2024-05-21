using System;
using System.Web;
using GameScene;
using UnityEngine;

/************************************************************************************
 * The WebGameSetup is a component in the GameScene.  If enabled, it will
 * operate as if this is a WebGL-based game which has only one scene, GameScene, and
 * assumes all game setup and lobbying has been done in a separate web application
 * with all needed game information passed through the URL parameters.
 * It will connect with the WebSocket server and, once all players are connected, start
 * the game using the WebSocket server as a transport for messages
 */
public class WebGameSetup : MonoBehaviour
{

    public WebSocketTransport transport;

    public SinglePlayerAdvView view;

    // In a WebGame, setup parameters are passed through the URL of the game.
    // Once read in, connection with a broker server is made and, once all 
    // players are connected, the game starts.
    void Start()
    {
        const string GAMECODE_PARAM = "gamecode";
        const string SLOT_PARAM = "slot";
        byte session = 0x00;
        int slot = -1;

        if (Application.isEditor) {
            session = 0x01;
            slot=0;
        }
        else {
            Debug.Log("Reading URL");
            string urlstr = Application.absoluteURL;
            Debug.Log("URL = " + urlstr);
            Uri url = new Uri(urlstr);
            string session_str = HttpUtility.ParseQueryString(url.Query).Get(GAMECODE_PARAM);
            if (session_str != null) {
                // Not dealing with hexadecimal, so parse into an int and then to a byte
                int session_int = Int32.Parse(session_str);
                session = (byte)session_int;
                Debug.Log("Found the session = " + session);
            }
            string slot_str = HttpUtility.ParseQueryString(url.Query).Get(SLOT_PARAM);
            if (slot_str != null) {
                slot = Int32.Parse(slot_str);
            }
        }
        view.slot = slot;
        transport.ThisPlayerSlot = slot;

        // Now connect the transport
        _ = transport.Connect(session);
    }

    void rjaPrintSession(byte session) {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
