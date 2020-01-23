using System;
using System.Collections.Generic;
namespace GameEngine
{
    public class Sync
    {
        private List<RemoteAction> batActions = new List<RemoteAction>();
        private List<RemoteAction> dragonActions = new List<RemoteAction>();
        private List<PlayerPickupAction> playerPickups = new List<PlayerPickupAction>();
        private List<PlayerResetAction> playerResets = new List<PlayerResetAction>();
        private List<PortcullisStateAction> gateStateChanges = new List<PortcullisStateAction>();
        private List<ObjectMoveAction> mazeSetupActions = new List<ObjectMoveAction>();
        private PlayerMoveAction[] playersLastMove;
        private PlayerWinAction gameWon;

        private const int CHECK_PERIOD = 15000; // Fifteen seconds

        private Transport transport;

        private int thisPlayer;

        private int numPlayers;

        private int[] msgsRcvdFromPlayer;

        public Sync(int inNumPlayers, int inThisPlayer, Transport inTransport)
        {
            numPlayers = inNumPlayers;
            thisPlayer = inThisPlayer;
            gameWon = null;
            transport = inTransport;
            playersLastMove = new PlayerMoveAction[numPlayers];
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                playersLastMove[ctr] = null;
            }
            msgsRcvdFromPlayer = new int[numPlayers];
            resetMessagesReceived();
        }

        /**
         * This pulls messages off the socket until there are none waiting.
         * This does not process them, but demuxes them and puts them where they can be grabbed
         * when it is time to process that type of message.
         */
        public void PullLatestMessages()
        {
            RemoteAction nextAction = (transport != null ? transport.get() : null);

            while (nextAction != null)
            {
                bool hitError = false;
                switch (nextAction.typeCode)
                {
                    case ActionType.PLAYER_MOVE:
                        int messageSender = nextAction.sender;
                        playersLastMove[messageSender] = (PlayerMoveAction)nextAction;
                        break;
                    case ActionType.PLAYER_PICKUP:
                        playerPickups.Add((PlayerPickupAction)nextAction);
                        break;
                    case ActionType.PLAYER_RESET:
                        playerResets.Add((PlayerResetAction)nextAction);
                        break;
                    case ActionType.PLAYER_WIN:
                        // Don't know how we'd get this, but we ignore any win message after we receive the first one.
                        if (gameWon == null)
                        {
                            gameWon = (PlayerWinAction)nextAction;
                        }
                        break;
                    case ActionType.DRAGON_MOVE:
                    case ActionType.DRAGON_STATE:
                        dragonActions.Add(nextAction);
                        break;
                    case ActionType.BAT_MOVE:
                    case ActionType.BAT_PICKUP:
                        batActions.Add(nextAction);
                        break;
                    case ActionType.PORTCULLIS_STATE:
                        gateStateChanges.Add((PortcullisStateAction)nextAction);
                        break;
                    case ActionType.OBJECT_MOVE:
                        mazeSetupActions.Add((ObjectMoveAction)nextAction);
                        break;
                    case ActionType.PING:
                        break;
                    default:
                        // Hit unknown type
                        hitError = true;
                        break;
                }
                if (!hitError)
                {
                    handled(nextAction);
                }

                nextAction = transport.get();
            }
        }


        /**
         *  Get the latest changes to another player.  Returns the last known state of a
         * player including their position and velocity.  Caller must delete this object.
         * If no changes have been received since the last call, this will return
         * null.
         */
        public PlayerMoveAction GetLatestBallSync(int player)
        {
            PlayerMoveAction rtn = playersLastMove[player];
            playersLastMove[player] = null;
            return rtn;
        }

        /**
         * Get the next player pickup or player drop action.
         * Caller must delete this action.
         * If no actions have been received, this will return null.
         */
        public PlayerPickupAction GetNextPickupAction()
        {
            PlayerPickupAction next = null;
            if (playerPickups.Count > 0)
            {
                next = playerPickups[0];
                playerPickups.RemoveAt(0);
            }
            return next;

        }

        /**
         * Get the next player reset action.
         * Caller must delete this action.
         * If no actions have been received, this will return null.
         */
        public PlayerResetAction GetNextResetAction()
        {
            PlayerResetAction next = null;
            if (playerResets.Count > 0)
            {
                next = playerResets[0];
                playerResets.RemoveAt(0);
            }
            return next;
        }

        /**
         * If another player has won, this will return that action.
         * Otherwise will return null.
         * Caller must delete this action.
         */
        public PlayerWinAction GetGameWon()
        {
            PlayerWinAction next = gameWon;
            gameWon = null;
            return next;
        }

        /**
         * Get the next dragon action.  Caller must delete this object.
         * Caller must delete this action.
         * If no actions have been received, this will return null.
         */
        public RemoteAction GetNextDragonAction()
        {
            RemoteAction next = null;
            if (dragonActions.Count > 0)
            {
                next = dragonActions[0];
                dragonActions.RemoveAt(0);
            }
            return next;
        }

        /**
         * Get the next portcullis action.  Caller must delete this object.
         * Caller must delete this action.
         * If no actions have been received, this will return null.
         */
        public PortcullisStateAction GetNextPortcullisAction()
        {
            PortcullisStateAction next = null;
            if (gateStateChanges.Count > 0)
            {
                next = gateStateChanges[0];
                gateStateChanges.RemoveAt(0);
            }
            return next;
        }

        /**
         * Get the next bat action.  Caller must delete this object.
         * Caller must delete this action.
         * If no actions have been received, this will return null.
         */
        public RemoteAction GetNextBatAction()
        {
            RemoteAction next = null;
            if (batActions.Count > 0)
            {
                next = batActions[0];
                batActions.RemoveAt(0);
            }
            return next;
        }

        /**
         * Get the next maze setup action.
         * Caller must delete this action.
         * If no actions have been received, this will return null.
         */
        public ObjectMoveAction GetNextSetupAction()
        {
            ObjectMoveAction next = null;
            if (mazeSetupActions.Count > 0)
            {
                next = mazeSetupActions[0];
                mazeSetupActions.RemoveAt(0);
            }
            return next;
        }

        /**
         * Broadcast an event to the other players
         * @param action an action to broadcast.  The Sync now owns this action and is responsible
         * for deleting it.
         */
        public void BroadcastAction(RemoteAction action)
        {
            if (action != null)
            {
                action.setSender(thisPlayer);
                if (transport != null)
                {
                    transport.send(action);
                }
            }

        }


        public int getMessagesReceived(int player)
        {
            return msgsRcvdFromPlayer[player];
        }

        public void resetMessagesReceived()
        {
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                msgsRcvdFromPlayer[ctr] = 0;
            }
        }

        private void handled(RemoteAction action)
        {
            // Record we got a message from the sender
            if (action != null)
            {
                int sender = action.sender;
                if ((sender >= 0) && (sender < numPlayers))
                {
                    ++msgsRcvdFromPlayer[sender];
                }
            }
        }

    }
}
