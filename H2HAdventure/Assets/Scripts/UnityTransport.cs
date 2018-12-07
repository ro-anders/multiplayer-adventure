using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class UnityTransport : MonoBehaviour, Transport
{

    private PlayerSync thisPlayer;

    private List<PlayerSync> allPlayers = new List<PlayerSync>();

    private Queue<RemoteAction> receviedActions = new Queue<RemoteAction>();

    // This is only used by the view on the server
    private int lastUsedSlot = -1;

    public void registerSync(PlayerSync inPlayerSync)
    {
        Debug.Log("Registering " + (inPlayerSync.isLocalPlayer ? "local " : "remote ") + "player # " + inPlayerSync.getSlot());
        allPlayers.Add(inPlayerSync);
        if (inPlayerSync.isLocalPlayer)
        {
            thisPlayer = inPlayerSync;
            GameObject quadGameObject = GameObject.Find("Quad");
            UnityAdventureView view = quadGameObject.GetComponent<UnityAdventureView>();
            view.AdventureSetup(thisPlayer.getSlot());
            Debug.Log("Adventure game has been setup for player " + thisPlayer.getSlot());
        }
    }

    // This should only ever be called on the server
    public int assignPlayerSlot()
    {
        int newPlayerSlot = ++lastUsedSlot;
        return newPlayerSlot;
    }

    void Transport.send(RemoteAction action)
    {
        action.setSender(thisPlayer.getSlot());
        Debug.Log("Sending " + action.typeCode.ToString("g") + " message for player " + action.sender);
        thisPlayer.CmdBroadcast(action.serialize());
    }

    public void receiveBroadcast(int slot, int[] dataPacket)
    {
        ActionType type = (ActionType)dataPacket[0];
        int sender = dataPacket[1];
        if (sender != thisPlayer.slot)
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
            receviedActions.Enqueue(action);
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
            Debug.Log("Received " + nextAction.typeCode + " action from player #" + nextAction.sender + ".");
            return nextAction;
        }
    }
}
