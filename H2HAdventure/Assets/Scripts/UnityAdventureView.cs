using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class UnityAdventureView : MonoBehaviour, AdventureView
{
    public AdventureAudio adv_audio;
    public AdventureDirectional adv_input;
    public RenderTextureDrawer screenRenderer;

    private UnityTransport xport;

    private const int DRAW_AREA_WIDTH = 320;
    private const int DRAW_AREA_HEIGHT = 256;

    private AdventureGame gameEngine;

    private PlayerSync localPlayer;

    private bool gameStarted = false;
    private int numPlayersReady = 0;

    void Start() {
        xport = this.gameObject.GetComponent<UnityTransport>();
        if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) {
            AdventureSetup(0);
            gameStarted = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameStarted)
        {
            AdventureUpdate();
        }
    }

    public UnityTransport RegisterNewPlayer(PlayerSync newPlayer)
    {
        if (newPlayer.isLocalPlayer)
        {
            localPlayer = newPlayer;
        }
        xport.registerSync(newPlayer);
        if (newPlayer.isServer)
        {
            ++numPlayersReady;
            if (numPlayersReady >= SessionInfo.GameToPlay.numPlayers)
            {
                StartCoroutine(SignalStartGame());
            }
        }
        return xport;
    }

    private IEnumerator SignalStartGame()
    {
        const float GAME_START_BANNER_TIME = 3f;
        yield return new WaitForSeconds(GAME_START_BANNER_TIME);
        localPlayer.RpcStartGame();
    }



    public void StartGame()
    {
        AdventureSetup(localPlayer.getSlot());
        gameStarted = true;
    }

    public void AdventureSetup(int inLocalPlayerSlot) {
        Debug.Log("Starting game.");
        gameEngine = new AdventureGame(this, 2, inLocalPlayerSlot, null/*xport*/, 1, false, false);
    }

    public void AdventureUpdate() {
        screenRenderer.StartUpdate();
        gameEngine.Adventure_Run();
        screenRenderer.EndUpdate();
    }

    public void Platform_PaintPixel(int r, int g, int b, int x, int y, int width, int height)
    {
        Color color = new Color(r/256.0f, g/256.0f, b/256.0f);
        for (int i = 0; i < width; ++i)
            for (int j = 0; j < height; ++j) {
                int xi = x + i;
                int yj = y + j;
                if ((xi >= 0) && (xi < DRAW_AREA_WIDTH) && (yj >= 0) && (yj < DRAW_AREA_HEIGHT)) {

                    screenRenderer.SetPixel(x + i, y + j, color);
                }
            }
    }

    public void Platform_ReadJoystick(ref bool joyLeft, ref bool joyUp, ref bool joyRight, ref bool joyDown, ref bool joyFire) {
        if (adv_input != null)
        {
            adv_input.getDirection(ref joyLeft, ref joyUp, ref joyRight, ref joyDown);
            joyFire = adv_input.getDropButton();
        }
    }

    public void Platform_ReadConsoleSwitches(ref bool reset) {
        if (adv_input != null)
        {
            reset = adv_input.getResetButton();
        }
    }

    public void Platform_MakeSound(SOUND sound, float volume) {
        if (adv_audio != null)
        {
            adv_audio.play(sound, volume);
        }
    }

    public void Platform_ReportToServer(string message) {
        Debug.Log("Message to server: " + message);
    }


    public void Platform_DisplayStatus(string message, int durationSecs) {
        Debug.Log("Message for player: " + message);
    }


}
