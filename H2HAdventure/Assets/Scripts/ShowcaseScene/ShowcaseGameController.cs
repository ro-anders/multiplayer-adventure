﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowcaseGameController : MonoBehaviour
{
    public ShowcaseController parent;
    public ShowcaseTransport xport;
    public ShowcaseAdventureView advView;

    private ProposedGame gameToPlay;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Play(ProposedGame game, int thisClientSlot)
    {
        gameToPlay = game;
        advView.PlayGame(game, thisClientSlot);
    }
}