using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerSync : NetworkBehaviour
{
    public int slot = -1;

    public UnityTransport xport;

    // Use this for initialization
    void Start()
    {
        Initialize();
        CmdAssignSlot();
    }

    void Initialize() {
        GameObject quadGameObject = GameObject.Find("Quad");
        xport = quadGameObject.GetComponent<UnityTransport>();
    }

    public int getSlot() {
        return slot;
    }

    [Command]
    public void CmdAssignSlot() {
        if (slot < 0) {
            if (xport == null) {
                Initialize();
            }
            slot = xport.assignPlayerSlot();
            // This code is repeated from client RPC
            xport.registerSync(this);
            Debug.Log("Player #" + slot + " setup." +
                      (isLocalPlayer ? " This is the local player." : "") +
                      (isServer ? " This is on the server." : ""));
        }
        RpcAssignSlot(slot);
    }

    [ClientRpc]
    public void RpcAssignSlot(int inSlot) {
        if (slot < 0)
        {
            slot = inSlot;
            if (xport == null)
            {
                Initialize();
            }
            // Repeat this code in server CMD
            xport.registerSync(this);
            Debug.Log("Player #" + slot + " setup." +
                      (isLocalPlayer ? " This is the local player." : "") +
                      (isServer ? " This is on the server." : ""));
        }
    }

    [Command]
    public void CmdBroadcast(int[] dataPacket)
    {
        RpcReceiveBroadcast(dataPacket);
    }

    [ClientRpc]
    public void RpcReceiveBroadcast(int[] dataPacket)
    {
        if (xport == null)
        {
            Initialize();
        }
        xport.receiveBroadcast(slot, dataPacket);
    }

}
