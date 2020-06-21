using System;
namespace GameEngine
{
    public class Dragon: OBJECT
    {

        public enum Difficulty
        {
            TRIVIAL = 0xD0,
            EASY = 0xE8,
            MODERATE = 0xF0,
            HARD = 0xF6
        }

        public const int STALKING = 0;
        // State #1 is unused
        public const int DEAD = 2;
        public const int EATEN = 3;
        public const int ROAR = 4;

        private const int WARY_DISTANCE = 50;
        private const int RESURRECTION_WAIT = 1200; // One minute


        private static bool runFromSword = false;
        private static bool waryOfSword = false; // My own crazy ideas
        private static bool respawningDragons = false;

        public BALL eaten;

        private int dragonNumber;
            
        private static int dragonResetTime = (int)Difficulty.TRIVIAL;

        /** How many seconds left waiting to bite. */
        private int timer;

        /** How fast the dragon moves in Pixels/frame. */
        private int speed;

        /** The matrix of things the dragon runs from, attacks, and guards. */
        private int[] matrix = new int[0];

        private PopupMgr popupMgr;

        /**
     * Create a dragon
     * label - used purely for debugging and logging
     * number - the dragon's number in the game (used to identify it in remote messages)
     * color - the color of the dragon
     * speed - pixels/turn that the dragon can move
     * chaseMatrix - the list of items that the dragon either runs from, attacks, or guards
     *               NOTE: Assumes chaseMatrix will not be deleted.
     */
        public Dragon(String label, int inNumber, int inColor, int inSpeed, int[] chaseMatrix, PopupMgr inPopupMgr)
            : base(label, objectGfxDragon, dragonStates, 0, inColor)
        {
            dragonNumber = inNumber;
            speed = inSpeed;
            matrix = chaseMatrix;
            timer = 0;
            eaten = null;
            popupMgr = inPopupMgr;
        }

        public static void setRunFromSword(bool willRunFromSword)
        {
            runFromSword = willRunFromSword;
        }

        public static void setSuperDragons()
        {
            respawningDragons = true;
            waryOfSword = true;
        }

        public void respawn()
        {
            state = STALKING;
            timer = 0;
        }

        private void decrementTimer()
        {
            --timer;
        }

        private bool timerExpired()
        {
            return (timer <= 0);
        }

public void roar(int atRoom, int atX, int atY)
{
    state = ROAR;

    timer = 0xFC - dragonResetTime;

    // Set the dragon's position to the same as the ball
    room = atRoom;
    x = atX;
    y = atY;
}

public static void setDifficulty(Difficulty newDifficulty)
{
    dragonResetTime = (int)newDifficulty;
}

public bool hasEatenCurrentPlayer()
{
    return (state == EATEN) && (eaten == board.getCurrentPlayer());
}

    /**
     * Incorporate a state change from another machine into this dragon's state.
     * action - the state change message
     * volume - given how far this dragon is from this player, how loud would any
     *          sound be
     */
public void syncAction(DragonStateAction action, float volume)
{
    if (action.newState == EATEN)
    {

        BALL playerEaten = board.getPlayer(action.sender);
        // Ignore duplicates
        if (eaten != playerEaten)
        {
            // Set the State to 02 (eaten)
            eaten = playerEaten;
            state = EATEN;
            room = action.room;
            x = action.posx;
            y = action.posy;
            movementX = action.velx;
            movementY = action.vely;
            // Play the sound
            board.makeSound(SOUND.EATEN, volume);
        }
    }
    else if (action.newState == DEAD)
    {
        // We ignore die actions if the dragon has already eaten somebody or if it's a duplicate.
        if ((state != EATEN) && (state != DEAD))
        {
            state = DEAD;
            timer = (respawningDragons ? RESURRECTION_WAIT : 0);
            room = action.room;
            x = action.posx;
            y = action.posy;
            movementX = action.velx;
            movementY = action.vely;
            // Play the sound
            board.makeSound(SOUND.DRAGONDIE, volume);
        }
    }
    else if (action.newState == ROAR)
    {
        // We ignore roar actions if we are already in an eaten state or dead state
        if ((state != EATEN) && (state != DEAD))
        {
            roar(action.room, action.posx, action.posy);
            movementX = action.velx;
            movementY = action.vely;
            // Play the sound
            board.makeSound(SOUND.ROAR, volume);
        }
    }
}

    /**
     * Incorporate a move action from another machine into this dragon's state.
     * action - the move message
     */
public void syncAction(DragonMoveAction action)
{
    room = action.room;
    x = action.posx;
    y = action.posy;
    movementX = action.velx;
    movementY = action.vely;
}

        // Move the dragon this turn.
        // matrix - The dragon list of things he runs from, goes after, or guards
        // speed - the dragon's speed
        public RemoteAction move()
        {
            RemoteAction actionTaken = null;
            Dragon dragon = this;
            BALL objectBall = board.getCurrentPlayer();
            if (dragon.state == STALKING)
            {
                // Has the Ball hit the Dragon?
                if ((objectBall.room == dragon.room) &&
                    board.CollisionCheckObject(dragon, (objectBall.x - 4), (objectBall.y - 4), 8, 8))
                {
                    dragon.roar(objectBall.room, objectBall.x / 2, objectBall.y / 2);

                    // Notify others
                    actionTaken = new DragonStateAction(dragon.dragonNumber, ROAR, dragon.room, dragon.x, dragon.y,
                                                        dragon.movementX, dragon.movementY);

                    // Play the sound
                    board.makeSound(SOUND.ROAR, MAX.VOLUME);
                }

                // Has the Sword hit the Dragon?
                // Note, you have to be in the same room for a dragon to die on the sword
                if ((objectBall.room == dragon.room) &&
                    (board.CollisionCheckObjectObject(dragon, board.getObject(Board.OBJECT_SWORD))))
                {
                    // Set the State to 01 (Dead)
                    dragon.state = DEAD;
                    dragon.timer = (respawningDragons ? RESURRECTION_WAIT : 0);

                    // Notify others
                    actionTaken = new DragonStateAction(dragon.dragonNumber, DEAD, dragon.room, dragon.x, dragon.y,
                                                        dragon.movementX, dragon.movementY);

                    // Play the sound
                    board.makeSound(SOUND.DRAGONDIE, MAX.VOLUME);
                }

                if (dragon.state == STALKING)
                {
                    // Go through the dragon's object matrix
                    // Difficulty switch determines flee or don't flee from sword
                    BALL closest = closestBall();
                    int matrixStart = (runFromSword ? 0 : 2);
                    for (int matrixCtr = matrixStart; matrixCtr < matrix.Length; matrixCtr += 2) {
                        int seekDir = 0; // 1 is seeking, -1 is fleeing
                        int seekX = 0, seekY = 0;

                        int fleeObject = matrix[matrixCtr];
                        int seekObject = matrix[matrixCtr+1];

                        // Dragon fleeing an object
                        OBJECT fleeObjectPtr = board.getObject(fleeObject);
                        if ((fleeObject > Board.OBJECT_NONE) && (fleeObjectPtr != dragon))
                        {
                            // get the object it is fleeing
                            if ((fleeObjectPtr.room == dragon.room) && (fleeObjectPtr.exists()))
                            {
                                bool shouldRun = true;
                                // When dragons are wary of sword, they only run from it
                                // if they are really close to it or there is no one to go for
                                if ((fleeObject == Board.OBJECT_SWORD) && (waryOfSword) && (closest != null))
                                {
                                    int distanceToSword = distanceTo(fleeObjectPtr);
                                    shouldRun = (distanceToSword * speed < WARY_DISTANCE);

                                }
                                if (shouldRun)
                                {
                                    seekDir = -1;
                                    seekX = fleeObjectPtr.x;
                                    seekY = fleeObjectPtr.y;
                                }
                            }
                        }
                        else
                        {
                            // Dragon seeking the ball
                            if (seekDir == 0)
                            {
                                if (seekObject == Board.OBJECT_BALL)
                                {
                                    if (closest != null)
                                    {
                                        seekDir = 1;
                                        seekX = closest.x / 2;
                                        seekY = closest.y / 2;
                                        if ((popupMgr != null) &&
                                            popupMgr.needPopup[PopupMgr.SEE_DRAGON] &&
                                            (closest == board.getCurrentPlayer())) {
                                            popupMgr.ShowDragonPopup();
                                        }
                                    }
                                }
                            }

                            // Dragon seeking an object
                            if ((seekDir == 0) && (seekObject > Board.OBJECT_NONE))
                            {
                                // Get the object it is seeking
                                OBJECT objct = board.getObject(seekObject);
                                if (objct.room == dragon.room)
                                {
                                    seekDir = 1;
                                    seekX = objct.x;
                                    seekY = objct.y;
                                }
                            }
                        }

                        // Move the dragon
                        if ((seekDir > 0) || (seekDir < 0))
                        {
                            int newMovementX = 0;
                            int newMovementY = 0;

                            // horizontal axis
                            if (dragon.x < seekX)
                            {
                                newMovementX = seekDir * speed;
                            }
                            else if (dragon.x > seekX)
                            {
                                newMovementX = -(seekDir * speed);
                            }

                            // vertical axis
                            if (dragon.y < seekY)
                            {
                                newMovementY = seekDir * speed;
                            }
                            else if (dragon.y > seekY)
                            {
                                newMovementY = -(seekDir * speed);
                            }

                            // Notify others if we've changed our direction
                            if ((dragon.room == objectBall.room) && ((newMovementX != dragon.movementX) || (newMovementY != dragon.movementY)))
                            {
                                int distanceToMe = board.getCurrentPlayer().distanceTo(dragon.x, dragon.y);
                                actionTaken = new DragonMoveAction(dragon.room, dragon.x, dragon.y, newMovementX, newMovementY, dragon.dragonNumber, distanceToMe);
                            }
                            dragon.movementX = newMovementX;
                            dragon.movementY = newMovementY;

                            // Found something - we're done
                            return actionTaken;
                        }
                    }

                }
            }
            else if (dragon.state == EATEN)
            {
                // Eaten
                dragon.eaten.room = dragon.room;
                dragon.eaten.previousRoom = dragon.room;
                dragon.eaten.x = (dragon.x + 3) * 2;
                dragon.eaten.previousX = dragon.eaten.x;
                dragon.eaten.y = (dragon.y - 10) * 2;
                dragon.eaten.previousY = dragon.eaten.y;
            }
            else if (dragon.state == ROAR)
            {
                dragon.decrementTimer();
                if (dragon.timerExpired())
                {
                    // Has the Ball hit the Dragon?
                    if ((objectBall.room == dragon.room) && board.CollisionCheckObject(dragon, (objectBall.x - 4), (objectBall.y - 1), 8, 8))
                    {
                        // Set the State to 01 (eaten)
                        dragon.eaten = objectBall;
                        dragon.state = EATEN;
                        // Move the dragon up so that eating never causes the ball to shift screens
                        if (objectBall.y < 48)
                        {
                            dragon.y = 24;
                        }

                        // Notify others
                        actionTaken = new DragonStateAction(dragon.dragonNumber, EATEN, dragon.room,
                                                            dragon.x, dragon.y, dragon.movementX, dragon.movementY);
                                
                        // Play the sound
                        board.makeSound(SOUND.EATEN, MAX.VOLUME);

                        if ((popupMgr != null) && (popupMgr.needPopup[PopupMgr.EATEN_BY_DRAGON]))
                        {
                            popupMgr.ShowPopup(new Popup("dragon",
                                "You've been eaten by a dragon.\n" +
                                "Click 'Respawn' to continue.", 
                                popupMgr, PopupMgr.EATEN_BY_DRAGON));
                        }
                    }
                    else
                    {
                        // Go back to stalking
                        dragon.state = STALKING;
                    }
                }
            } 
            else if (dragon.state == DEAD)
            {
                if (respawningDragons)
                {
                    dragon.decrementTimer();
                    if (dragon.timerExpired())
                    {
                        dragon.respawn();
                    }
                }
            }

            return actionTaken;
        }

/**
* Returns the ball closest to the point in the adventure.
*/
BALL closestBall()
{
     // This finds the closest ball unless dragons are wary of the sword, then
     // it finds the closest ball not carrying the sword
    int shortestDistance = 10000; // Some big number greater than the diagnol of the board
    BALL found = null;
    int numPlayers = board.getNumPlayers();
    for (int ctr = 0; ctr < numPlayers; ++ctr)
    {
        BALL nextBall = board.getPlayer(ctr);
        if ((nextBall.room == room) && ((!waryOfSword) || nextBall.linkedObject != Board.OBJECT_SWORD))
        {
            int dist = nextBall.distanceTo(x, y);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                found = nextBall;
            }
        }
    }
    return found;
}

        public int distanceTo(OBJECT other)
        {
            // Figure out the distance (which is really the max difference along one axis)
            int xdist = 0;
            if (this.x < other.x)
            {
                // Measure from the dragon's right side to the object's left side
                xdist = other.x - (this.x + 8);
            }
            else
            {
                // Measure from the object's right side to the dragon's left side
                int width = 8 * (other.size / 2 + 1);
                xdist = this.x - (other.x + width);
            }
            int ydist;
            if (this.y < other.y)
            {
                // Measure from the dragon's top to the object's bottom
                ydist = (other.y-other.gfxData[other.state].Length) - this.y;
            }
            else
            {
                // Measure from the object's top to the dragon's bottom
                ydist = this.y-this.gfxData[this.state].Length - other.y;
            }
            int dist = (xdist > ydist ? xdist : ydist);
            return dist;
        }


        private static byte[][] objectGfxDragon = new byte[][]
        { new byte[] {
            // Object #6 : State #00 : Graphic
                0x06,                  //      XX
                0x0F,                  //     XXXX
                0xF3,                  // XXXX  XX
                0xFE,                  // XXXXXXX
                0x0E,                  //     XXX
                0x04,                  //      X
                0x04,                  //      X
                0x1E,                  //    XXXX
                0x3F,                  //   XXXXXX
                0x7F,                  //  XXXXXXX
                0xE3,                  // XXX   XX
                0xC3,                  // XX    XX
                0xC3,                  // XX    XX
                0xC7,                  // XX   XXX
                0xFF,                  // XXXXXXXX
                0x3C,                  //   XXXX
                0x08,                  //     X
                0x8F,                  // X   XXXX
                0xE1,                  // XXX    X
                0x3F},                 //   XXXXXX
            new byte[] {},
            new byte[] {
                // Object 6 : State 01 : Graphic
                0x80,                  // X
                0x40,                  //  X
                0x26,                  //   X  XX
                0x1F,                  //    XXXXX
                0x0B,                  //     X XX
                0x0E,                  //     XXX
                0x1E,                  //    XXXX
                0x24,                  //   X  X
                0x44,                  //  X   X
                0x8E,                  // X   XXX
                0x1E,                  //    XXXX
                0x3F,                  //   XXXXXX
                0x7F,                  //  XXXXXXX
                0x7F,                  //  XXXXXXX
                0x7F,                  //  XXXXXXX
                0x7F,                  //  XXXXXXX
                0x3E,                  //   XXXXX
                0x1C,                  //    XXX
                0x08,                  //     X
                0xF8,                  // XXXXX
                0x80,                  // X
                0xE0},                 // XXX
            new byte[] {
                // Object 6 : State 02 : Graphic
                0x0C,                  //     XX
                0x0C,                  //     XX
                0x0C,                  //     XX
                0x0E,                  //     XXX
                0x1B,                  //    XX X
                0x7F,                  //  XXXXXXX
                0xCE,                  // XX  XXX
                0x80,                  // X
                0xFC,                  // XXXXXX
                0xFE,                  // XXXXXXX
                0xFE,                  // XXXXXXX
                0x7E,                  //  XXXXXX
                0x78,                  //  XXXX
                0x20,                  //   X
                0x6E,                  //  XX XXX
                0x42,                  //  X    X
                0x7E}                  //  XXXXXX
        };

        private static byte[] dragonStates = 
        {
                    0,0,3,0,2
        };


    }
}
