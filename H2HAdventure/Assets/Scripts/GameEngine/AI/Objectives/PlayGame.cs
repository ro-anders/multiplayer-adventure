namespace GameEngine.Ai
{
    public class PlayGame : AiObjective
    {
        public PlayGame(Board inBoard, int inAiPlayerNum, AiStrategy inAiStrategy, AiNav inAiNav)
        {
            base.board = inBoard;
            base.aiPlayerNum = inAiPlayerNum;
            base.aiPlayer = board.getPlayer(inAiPlayerNum);
            base.strategy = inAiStrategy;
            base.nav = inAiNav;
        }

        public override string ToString()
        {
            return "win game";
        }

        protected override void doComputeStrategy()
        {
            // Unlike all the other objectives, this objective has to make sure
            // things are possible.

            if (strategy.eatenByDragon() || strategy.isBallEmbeddedInWall(true))
            {
                markShouldReset();
                return;
            }

            if (strategy.behindLockedGate(aiPlayer.room) != null)
            {
                // We can't do anything if we are locked inside a castle, so reset.
                // More intelligent code may decide if the object we are carrying
                // needs to be shoved in a wall or if the player with the key is
                // waiting outside and may unlock it shortly
                markShouldReset();
                return;
            }
         
            int playerToBlock = strategy.shouldBlockPlayer();
            if (playerToBlock >= 0)
            {
                AiObjective objective = new LockCastleAndHideKeyObjective(playerToBlock);
                this.addChild(objective);
                // Make sure this objective is achievable
                try
                {
                    objective.getNextObjective();
                    // Recursively add this objective so we check again if we should block the other player.
                    this.addChild(new PlayGame(board, aiPlayerNum, strategy, nav));
                }
                catch (Abort)
                {
                    // If we can't block the player, then don't.
                    this.removeChild(objective);
                }
            }

            AiObjective retrieveChalice = new WinGameWithChalice();
            this.addChild(retrieveChalice);
            // Check to see if we can actually win (might be permanently blocked)
            try
            {
                retrieveChalice.getNextObjective();
            }
            catch (Abort a)
            {
                GameEngine.Logger.Error(a.ToString());
                this.removeChild(retrieveChalice);

                // Nothing to do but block other players
                for (int ctr = 0; ctr < board.getNumPlayers(); ++ctr)
                {
                    if (ctr != aiPlayer.playerNum)
                    {
                        AiObjective objective = new LockCastleAndHideKeyObjective(ctr);
                        this.addChild(objective);
                        try
                        {
                            objective.getNextObjective();
                        }
                        catch (Abort)
                        {
                            // If we can't block the player just forget about it
                            this.removeChild(objective);
                        }
                    }
                }

            }
        }

        protected override bool computeIsCompleted()
        {
            // This is never completed, or by the time it is we don't
            // ever call this anymore
            return false;
        }

    }
}