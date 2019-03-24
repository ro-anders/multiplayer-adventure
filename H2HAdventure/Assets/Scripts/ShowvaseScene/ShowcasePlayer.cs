﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShowcasePlayer : NetworkBehaviour
{
    private ShowcaseTransport xport;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Showcase player " + GetId() + " started.");
        GameObject transportGameObject = GameObject.FindGameObjectWithTag("PlayerPrefabRegistry");
        xport = transportGameObject.GetComponent<ShowcaseTransport>();
        xport.AddClient(this);
    }

    public int GetId()
    {
        return (int)GetComponent<NetworkIdentity>().netId.Value;
    }

    [Command]
    public void CmdProposeGame(string gameJson)
    {
        xport.FflProposeGame(this, gameJson);
    }

    [Command]
    public void CmdAcceptGame(int acceptingPlayerId)
    {
        xport.FflAcceptGame(acceptingPlayerId);
    }

    [Command]
    public void CmdAbortGame(int abortingPlayerId)
    {
        xport.FflAbortGame(abortingPlayerId);
    }

    [ClientRpc]
    public void RpcNewProposedGame(string serializedGame)
    {
        xport.HdlNewProposedGame(serializedGame);
    }

    [ClientRpc]
    public void RpcClearGame()
    {
        xport.HdlNoGame();
    }

}

