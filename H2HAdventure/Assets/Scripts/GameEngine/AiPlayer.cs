
namespace GameEngine
{
    public class AiPlayer
    {
        private const int BALL_MOVEMENT = 6;

        private Board gameBoard;
        private AI ai;
        private int thisPlayer;
        private BALL thisBall;

        private int desiredRoom = Map.BLUE_MAZE_1;
        private int desiredX = Portcullis.EXIT_X;
        private int desiredY = 40;

        private AiPathNode desiredPath = null;
        private int nextStepX = int.MinValue;
        private int nextStepY = int.MinValue;

        public AiPlayer(AI inAi, Board inBoard, int inPlayerSlot)
        {
            gameBoard = inBoard;
            ai = inAi;
            thisPlayer = inPlayerSlot;
            thisBall = gameBoard.getPlayer(thisPlayer);
        }

        public void chooseDirection()
        {
            if (desiredRoom < 0)
            {
                // We have no goal.  Don't do anything.
                thisBall.velx = 0;
                thisBall.vely = 0;
                return;

            }

            if (desiredPath == null)
            {
                // We don't even know where we are going.  Figure it out.
                UnityEngine.Debug.Log("Get player " + thisPlayer + " from " +
                    thisBall.room + "-(" + thisBall.x + "," + thisBall.y + ") to " +
                    desiredRoom + "-(" + desiredX + "," + desiredY + ")");
                desiredPath = ai.ComputePath(thisBall.room, thisBall.x, thisBall.y, desiredRoom, desiredX, desiredY);
                if (desiredPath == null)
                {
                    // No way to get to where we want to go.  Give up
                    UnityEngine.Debug.Log("Couldn't compute path for AI player " + thisPlayer);
                    desiredRoom = -1;
                    thisBall.velx = 0;
                    thisBall.vely = 0;
                    return;
                }
                desiredPath.ThisPlot.GetOverlap(desiredPath.nextNode.ThisPlot,
                    desiredPath.nextDirection, ref nextStepX, ref nextStepY);
            }

            // See if we've gotten to the next step of the path
            while (!desiredPath.ThisPlot.Contains(thisBall.room, thisBall.x, thisBall.y))
            {
                desiredPath = desiredPath.nextNode;
                if (desiredPath == null)
                {
                    UnityEngine.Debug.LogError(thisBall.room + "(" + thisBall.x + "," + thisBall.y + ")" +
                        " has traversed past end of AI path!");
                    desiredRoom = -1;
                    thisBall.velx = 0;
                    thisBall.vely = 0;
                    return;
                }
                else if (!desiredPath.ThisPlot.Contains(thisBall.room, thisBall.x, thisBall.y))
                {
                    UnityEngine.Debug.LogError(thisBall.room + "(" + thisBall.x + "," + thisBall.y + ")" +
                        "has fallen off the path.");
                    // TODO: What do we do now?  Right now we just check along the path
                    // to see if we ended up any further along it, and if not, give up.
                }
                else
                {
                    UnityEngine.Debug.Log(thisBall.room + "(" + thisBall.x + "," + thisBall.y + ")" +
                        " has reached next node in path: " + desiredPath);
                    if (desiredPath.nextNode == null)
                    {
                        // We've reached the last plot in the path.  Now go to the desired coordinates
                        nextStepX = desiredX;
                        nextStepY = desiredY;
                    }
                    else
                    {
                        desiredPath.ThisPlot.GetOverlap(desiredPath.nextNode.ThisPlot,
                            desiredPath.nextDirection, ref nextStepX, ref nextStepY);
                        UnityEngine.Debug.Log(" At " + thisBall.room + "(" + thisBall.x + "," + thisBall.y + ")" +
                            " in plot " + desiredPath.ThisPlot +
                            " and shooting for (" + nextStepX + "," + nextStepY + ")" + " in plot " + desiredPath.nextNode.ThisPlot); 

                    }
                }
            }

            if (desiredPath != null)
            {
                // Go to the nextStep coordinates to get us to the next step on the path
                // or the desired coordinates.
                thisBall.velx = (nextStepX > thisBall.x ? BALL_MOVEMENT : -BALL_MOVEMENT);
                thisBall.vely = (nextStepY > thisBall.y ? BALL_MOVEMENT : -BALL_MOVEMENT);
            }
            UnityEngine.Debug.Log("Chose direction " + thisBall.velx + "," + thisBall.vely);

        }
    }

}
