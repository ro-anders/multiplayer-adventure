using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.Networking;

public class ShowcaseTransport : MonoBehaviour, GameEngine.Transport
{
    public ShowcaseNetworkController networkController;
    public ShowcaseLobbyController lobbyController;
    public ShowcasePrestartController prestartController;
    public ShowcaseGameController gameController;

    private ShowcasePlayer thisClient;
    private int thisClientGameSlot;
    private List<ShowcasePlayer> allClients = new List<ShowcasePlayer>();
    private LobbyServer lobbyServer;
    private Queue<RemoteAction> receviedActions = new Queue<RemoteAction>();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddClient(ShowcasePlayer newClient)
    {
        allClients.Add(newClient);
        if (newClient.isLocalPlayer)
        {
            thisClient = newClient;
            if (thisClient.isServer)
            {
                lobbyServer = new LobbyServer(this);
            }
        }
        networkController.PlayerStarted();
    }

    public void send(RemoteAction action)
    {
        action.setSender(thisClientGameSlot);
        thisClient.CmdBroadcastGameAction(action.serialize());
    }

    public RemoteAction get()
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

    public void receiveBroadcast(int[] dataPacket)
    {
        ActionType type = (ActionType)dataPacket[0];
        int sender = dataPacket[1];
        if (sender != thisClientGameSlot)
        {
            RemoteAction action = null;
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


    // ---- Reqs and Ffls ---------------------------------------------------------------

    public void ReqProposeGame(ProposedGame newGame)
    {
        newGame.players = new int[] { thisClient.GetId() };
        string newGameJson = JsonUtility.ToJson(newGame);
        thisClient.CmdProposeGame(newGameJson);
    }

    public void FflProposeGame(ShowcasePlayer player, 
        string gameJson)
    {
        ProposedGame newGame = JsonUtility.FromJson<ProposedGame>(gameJson);
        lobbyServer.HandleProposeGame(newGame);
    }

    public void ReqAcceptGame()
    {
        thisClient.CmdAcceptGame(thisClient.GetId());
    }

    public void FflAcceptGame(int player)
    {
        lobbyServer.HandleAcceptGame(player);
    }

    public void ReqAbortGame()
    {
        thisClient.CmdAbortGame(thisClient.GetId());
    }

    public void FflAbortGame(int abortingPlayerId)
    {
        lobbyServer.HandleAbortGame(abortingPlayerId);
    }

    public void ReqReadyToStart()
    {
        thisClient.CmdReadyToStart(thisClient.GetId());
    }

    public void FflReadyToStart(int abortingPlayerId)
    {
        lobbyServer.HandleReadyToStart(abortingPlayerId);
    }


    // ---- Bcasts and Hdls ---------------------------------------------------------------

    public void BcstNewProposedGame(ProposedGame proposedGame)
    {
        string proposedGameJson = JsonUtility.ToJson(proposedGame);
        thisClient.RpcNewProposedGame(proposedGameJson);

    }

    public void HdlNewProposedGame(string serializedProposedGame)
    {
        ProposedGame proposal = JsonUtility.FromJson<ProposedGame>(serializedProposedGame);
        lobbyController.OnProposalReceived(proposal, proposal.ContainsPlayer(thisClient.GetId()));
    }

    public void BcstNoGame()
    {
        thisClient.RpcClearGame();

    }

    public void HdlNoGame()
    {
        lobbyController.OnClearProposalReceived();
    }

    public void BcstStartGame(ProposedGame proposedGame)
    {
        string proposedGameJson = JsonUtility.ToJson(proposedGame);
        thisClient.RpcStartGame(proposedGameJson);

    }

    public void HdlStartGame(string serializedProposedGame)
    {
        ProposedGame gameToStart = JsonUtility.FromJson<ProposedGame>(serializedProposedGame);
        thisClientGameSlot = new ArrayList(gameToStart.players).IndexOf(thisClient.GetId());
        lobbyController.OnStartGame();
        prestartController.OnStartGame(gameToStart, thisClientGameSlot);
    }

}
