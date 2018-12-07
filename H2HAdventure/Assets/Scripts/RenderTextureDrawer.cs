using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class RenderTextureDrawer : MonoBehaviour
{
    public RenderTexture renderTexture; // renderTextuer that you will be rendering stuff on
    public Renderer quadRenderer; // renderer in which you will apply changed texture
    Texture2D texture;

    void Start()
    {
        texture = new Texture2D(renderTexture.width, renderTexture.height);
        quadRenderer.material.mainTexture = texture;

        // Start with the whole display black.
        RenderTexture.active = renderTexture;
        for (int i = 0; i < renderTexture.width; i++)
            for (int j = 0; j < renderTexture.height; j++)
            {
                texture.SetPixel(i, j, new Color(0, 0, 0));
            }
        texture.Apply();
        RenderTexture.active = null;
    }

    public void StartUpdate() {
        RenderTexture.active = renderTexture;
    }

    public void EndUpdate() {
        texture.Apply();
        RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it. 
    }

    public void SetPixel(int x, int y, UnityEngine.Color color) {
        texture.SetPixel(x, y, color);

    }

    private int at = 0;
    public void DemoUpdate()
    {
        int DRAW_AREA_WIDTH = 320;
        int DRAW_AREA_HEIGHT = 256;
         int viewWidth = renderTexture.width;
        int viewHeight = renderTexture.height;
        // Don't draw anything if the drawing space is not big enough.
        if ((viewWidth >= DRAW_AREA_WIDTH) && (viewHeight >= DRAW_AREA_HEIGHT))
        {
            RenderTexture.active = renderTexture;

            float stripeWidth = DRAW_AREA_WIDTH * .2f;
            at = (at < DRAW_AREA_WIDTH ? at + 1 : 0);

            int drawXStart = (viewWidth - DRAW_AREA_WIDTH) / 2;
            int drawYStart = (viewHeight - DRAW_AREA_HEIGHT) / 2;

            for (int i = 0; i < DRAW_AREA_WIDTH; i++)
                for (int j = 0; j < DRAW_AREA_HEIGHT; j++)
                {
                    Color color = ((i >= at) && (i <= at + stripeWidth) ? new Color(0xFF / 256.0f, 0xD8 / 256.0f, 0x4C / 256.0f) : new Color(0, 0, 0));
                    texture.SetPixel(drawXStart + i, drawYStart + j, color);
                }
            texture.Apply();
            RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it. 
        }
    }


}
