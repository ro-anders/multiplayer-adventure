﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;
using UnityEngine.UI;

public class RenderTextureDrawer : MonoBehaviour
{
    public RenderTexture renderTexture; // renderTextuer that you will be rendering stuff on
    public RawImage placeToDraw; // renderer in which you will apply changed texture
    Texture2D texture;
    const int DRAW_AREA_WIDTH = Adv.ADVENTURE_SCREEN_BWIDTH;
    const int DRAW_AREA_HEIGHT = Adv.ADVENTURE_SCREEN_BHEIGHT;
    float[,] red = new float[DRAW_AREA_WIDTH, DRAW_AREA_HEIGHT];
    float[,] green = new float[DRAW_AREA_WIDTH, DRAW_AREA_HEIGHT];
    float[,] blue = new float[DRAW_AREA_WIDTH, DRAW_AREA_HEIGHT];



    void Start()
    {
        texture = new Texture2D(renderTexture.width, renderTexture.height);
        placeToDraw.texture = texture;
    }

    // Update is called once per frame
    void Update()
    {
        RenderTexture.active = renderTexture;
        //don't forget that you need to specify rendertexture before you call readpixels
        //otherwise it will read screen pixels.
        //texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        for (int xctr = 0; xctr < DRAW_AREA_WIDTH; ++xctr)
        {
            for (int yctr = 0; yctr < DRAW_AREA_HEIGHT; ++yctr)
            {
                texture.SetPixel(xctr, yctr, new Color(red[xctr, yctr], green[xctr, yctr], blue[xctr, yctr]));
            }
        }
        texture.Apply();
        RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it. 
    }

    public void StartUpdate() {
        //RenderTexture.active = renderTexture;
    }

    public void EndUpdate() {
        //texture.Apply();
        //RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it. 
    }

    public void SetPixel(int x, int y, UnityEngine.Color color) {
        //texture.SetPixel(x, y, color);
        red[x, y] = color.r;
        green[x, y] = color.g;
        blue[x, y] = color.b;
    }

    private int at = 0;
    public void DemoUpdate()
    {
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
