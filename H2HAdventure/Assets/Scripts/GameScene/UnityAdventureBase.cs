using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.UI;

public class Rectangle
{
    public Rectangle(int r, int g, int b, int x, int y, int width, int height)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;

    }
    public readonly int r;
    public readonly int g;
    public readonly int b;
    public readonly int x;
    public readonly int y;
    public readonly int width;
    public readonly int height;
}


abstract public class UnityAdventureBase : MonoBehaviour, AdventureView
{
    private const int DRAW_AREA_WIDTH = Adv.ADVENTURE_SCREEN_WIDTH;
    private const int DRAW_AREA_HEIGHT = Adv.ADVENTURE_SCREEN_HEIGHT;
    private const int POPUP_DURATION = 8;

    // We hold a big buffer of rectangles as compactly as possible, so we
    // convert a Rectange (red, green, blue, x, y, width, length) and store
    // them serially in a 1D array
    private const short RECTSIZE = 7;
    private const int RECTS_START_SIZE = 3000;
    private short[] rectsToDisplay = new short[RECTS_START_SIZE * RECTSIZE];
    private readonly float scale = 1;

    public RenderTextureDrawer screenRenderer;
    public RawImage screen;
    public AdventureDirectional adv_input;
    public AdventureAudio adv_audio;
    public Button respawnButton;
    public GameObject messagePanel;
    public Text messageText;
    public GameObject popupPanel;

    protected bool gameRenderable; // This is true from the moment the 
    // game engine starts showing the opening number room to when the user leaves
    // the screen, returning to game setup
    protected bool gameRunning; // This is true from the moment the players can
    // start moving to when someone wins
    protected AdventureGame gameEngine;
    private int rectsBufferSize = RECTS_START_SIZE;
    private int numRects;
    private bool painting;
    private bool displaying;
    private GameObject gamePanel;
    private PopupController popupController;


    // Start is called before the first frame update
    public virtual void Start()
    {
        gamePanel = gameObject.transform.parent.gameObject;
        popupController = popupPanel.GetComponent<PopupController>();
    }

    // FixedUpdate is called exactly 60 times per second
    public void FixedUpdate()
    {
        if (gameRenderable)
        {
            AdventureUpdate();
        }
    }

    // Update is called up to 60 times per second, but may be slower depending
    // on device and load
    public virtual void Update()
    {
        if (gameRenderable)
        {
            // Make the game screen as big as possible
            if (gamePanel != null)
            {
                RectTransform rt = (RectTransform)gamePanel.transform;
                float maxWidth = rt.rect.width;
                float maxHeight = rt.rect.height;
                float scale1 = maxWidth / DRAW_AREA_WIDTH;
                float scale2 = maxHeight / DRAW_AREA_HEIGHT;
                float newScale = (scale1 <= scale2 ? scale1 : scale2);
                if (Math.Abs(scale - newScale) > 0.01)
                {
                    RectTransform screenRect = screen.GetComponent<RectTransform>();
                    screenRect.sizeDelta = new Vector2(newScale * DRAW_AREA_WIDTH, newScale * DRAW_AREA_HEIGHT);
                }
            }
            DisplayRectangles();
        }
    }

    public void ShutdownGame()
    {
        // Clear the audio so it doesn't replay
        adv_audio.PlaySystemSound(null);
        gameRenderable = false;
    }

    public virtual void Platform_GameChange(GAME_CHANGES change)
    {
        if (change == GAME_CHANGES.GAME_STARTED)
        {
            gameRunning = true;
        } else if (change == GAME_CHANGES.GAME_ENDED)
        {
            gameRunning = false;
            // Keep track of how many games have been played
            string PREF_KEY = "GamesPlayed";
            int gamesPlayed = PlayerPrefs.GetInt(PREF_KEY, 0);
            PlayerPrefs.SetInt(PREF_KEY, gamesPlayed + 1);
        }
    }

    public void Platform_PaintPixel(int r, int g, int b, int x, int y, int width, int height)
    {
        if (numRects >= rectsBufferSize)
        {
            int newBufferSize = 2 * rectsBufferSize;
            short[] newArray = new short[newBufferSize];
            rectsToDisplay.CopyTo(newArray, 0);
            rectsToDisplay = newArray;
            rectsBufferSize = newBufferSize;
        }
        int at = numRects * RECTSIZE;
        // Range checking and overscan adjustment
        y = y - Adv.ADVENTURE_OVERSCAN;
        if ((x > DRAW_AREA_WIDTH) || (x + width < 0) ||
            (y > DRAW_AREA_HEIGHT) || (y + height < 0))
        {
            return;
        }
        if (x + width > DRAW_AREA_WIDTH)
        {
            width = DRAW_AREA_WIDTH - x;
        }
        if (x < 0)
        {
            width = width + x;
            x = 0;
        }
        if (y + height > DRAW_AREA_HEIGHT)
        {
            height = DRAW_AREA_HEIGHT - y;
        }
        if (y < 0)
        {
            height = height + y;
            y = 0;
        }

        rectsToDisplay[at] = (short)r;
        rectsToDisplay[at + 1] = (short)g;
        rectsToDisplay[at + 2] = (short)b;
        rectsToDisplay[at + 3] = (short)x;
        rectsToDisplay[at + 4] = (short)y;
        rectsToDisplay[at + 5] = (short)width;
        rectsToDisplay[at + 6] = (short)height;
        ++numRects;
    }

    public void Platform_ReadJoystick(ref bool joyLeft, ref bool joyUp, ref bool joyRight, ref bool joyDown, ref bool joyFire)
    {
        if (adv_input != null)
        {
            adv_input.getDirection(ref joyLeft, ref joyUp, ref joyRight, ref joyDown);
            joyFire = adv_input.getDropButton();
        }
    }

    public void Platform_ReadConsoleSwitches(ref bool reset)
    {
        if (adv_input != null)
        {
            reset = adv_input.getResetButton();
        }
    }

    public void Platform_MakeSound(SOUND sound, float volume)
    {
        if (adv_audio != null)
        {
            adv_audio.play(sound, volume);
        }
    }

    public virtual void Platform_ReportToServer(string message)
    {
        Debug.Log("Message to server: " + message);
    }


    public void Platform_PopupHelp(string message, string imageName)
    {
        StartCoroutine(DisplayPopupHelp(message, imageName, POPUP_DURATION));
    }

    public void Platform_DisplayStatus(string message, int durationSecs)
    {
        Debug.Log("Message for player: " + message);
        StartCoroutine(DisplayStatus(message, durationSecs));
    }

    private IEnumerator DisplayPopupHelp(string message, string imageName, int durationSecs)
    {
        // We mute the sound if the game is over so not to 
        // occlude cool end game segment
        if (gameRunning)
        {
            adv_audio.PlaySystemSound(adv_audio.blip);
        }
        popupController.Popup(message, imageName);
        if (durationSecs > 0)
        {
            yield return new WaitForSeconds(durationSecs);
            // Check to make sure another popup hasn't come along
            // superceding this one (it shouldn't)
            popupController.Hide(message);
        }
    }

    private IEnumerator DisplayStatus(string message, int durationSecs)
    {
        messagePanel.SetActive(true);
        messageText.text = message;
        if (durationSecs > 0)
        {
            yield return new WaitForSeconds(durationSecs);
            // Check to make sure another message hasn't come along
            // superceding this one.
            if (messageText.text == message)
            {
                messageText.text = "";
                messagePanel.SetActive(false);
            }
        }
    }


    private void DisplayRectangles()
    {
        // Pretty sure this is single threaded and paint and display loops can't
        // overlap, but put this in to check that.
        if (painting)
        {
            Debug.Log("DisplayRectangles called while in the middle of paint loop");
        }
        if (numRects >= 0)
        {
            displaying = true;
            screenRenderer.StartUpdate();
            for (int ctr = 0; ctr < numRects; ++ctr)
            {
                int at = ctr * RECTSIZE;
                Color color = new Color(rectsToDisplay[at] / 256.0f, rectsToDisplay[at + 1] / 256.0f, rectsToDisplay[at + 2] / 256.0f);
                for (int i = 0; i < rectsToDisplay[at + 5]; ++i)
                    for (int j = 0; j < rectsToDisplay[at + 6]; ++j)
                    {
                        screenRenderer.SetPixel(rectsToDisplay[at + 3] + i, rectsToDisplay[at + 4] + j, color);
                    }
            }
            screenRenderer.EndUpdate();
            numRects = 0;
            displaying = false;
        }
    }



    protected void AdventureUpdate()
    {
        // Pretty sure this is single threaded and paint and display loops can't
        // overlap, but put this in to check that.
        if (displaying)
        {
            Debug.Log("AdventureUpdate called while in the middle of display loop");
        }
        painting = true;
        numRects = 0;
        gameEngine.Adventure_Run();
        painting = false;
    }



}
