using System;
namespace GameEngine
{
    public class BALL
    {
        public const int RADIUS = 4;
        public const int DIAMETER = 2 * RADIUS;
        public const int MOVEMENT = 6;

        public int playerNum;              // 0-2.  Which player this is.
        public int room;                   // room
        public int previousRoom;
        public int displayedRoom;          // occassionally ball will appear in room other than it is in (usually using bridge across rooms)
        public int x;                      // x position
        public int y;                      // y position
        public int previousX;              // previous x position
        public int previousY;              // previous y position
        public int velx;                   // Current horizontal speed (walls notwithstanding).  Positive = right.  Negative = left.
        public int vely;                   // Current vertical speed (walls notwithstanding).  Positive = right.  Negative = down.
        public int linkedObject;           // index of linked (carried) object
        public int linkedObjectX;          // X value representing the offset from the ball to the object being carried (object scale)
        public int linkedObjectY;          // Y value representing the offset from the ball to the object being carried (object scale)
        public bool hit;                  // the ball hit something
        public int hitObject;              // the object that the ball hit
        public readonly byte[] gfxData;        // graphics data for ball
        public Portcullis homeGate;       // The gate of the castle you start at
        public Ai.AiPlayer ai;                   // The ai behind this player, or null if not an computer player

        /** During the gauntlet, once you reach the black castle you flash like the chalise until you reset or you reach the
            * gold castle where you win. */
        private bool glowing;

        public BALL(int inPlayerNum, Portcullis inHomeGate, bool inAltIcons)
        {
            playerNum = inPlayerNum;
            room = 0;
            previousRoom = 0;
            displayedRoom = 0;
            x = 0;
            y = 0;
            previousX = 0;
            previousY = 0;
            velx = 0;
            vely = 0;
            linkedObject = Board.OBJECT_NONE;
            linkedObjectX = 0;
            linkedObjectY = 0;
            hit = false;
            hitObject = Board.OBJECT_NONE;
            gfxData = (inAltIcons ? 
                (playerNum == 0 ? ballGfxShield : (playerNum == 1 ? ballGfxSmall : ballGfxCross)) :
                (playerNum == 0 ? ballGfxSolid : (playerNum == 1 ? ballGfxOne : ballGfxTwo)));
            homeGate = inHomeGate;
            glowing = false;
            ai = null;
        }

        public override string ToString()
        {
            return "player " + playerNum + " at (" + x + "," + y + ")@" + room +
                (linkedObject >= 0 ? " with " + linkedObject : "");
        }

        public int BTop
        {
            get { return y; }
        }
        public int BRight
        {
            get { return x + DIAMETER - 1; }
        }
        public int BBottom
        {
            get { return y - DIAMETER + 1; }
        }
        public int BLeft
        {
            get { return x; }
        }
        /** x coordinate of the middle of the ball **/
        public int midX
        {
            get { return x + RADIUS; }
            set { x = value - RADIUS; }
        }
        /** y coordinate of the middle of the ball **/
        public int midY
        {
            get { return y - RADIUS; }
            set { y = value + RADIUS; }
        }
        /** The rectangle of the ball in object coordinate system */
        public RRect ORect
        {
            get { return new RRect(room, x / Adv.BALL_SCALE, y / Adv.BALL_SCALE, BALL.DIAMETER / Adv.BALL_SCALE, BALL.DIAMETER / Adv.BALL_SCALE); }
        }
        /** The rectangle of the ball in object coordinate system */
        public RRect BRect
        {
            get { return new RRect(room, x, y, BALL.DIAMETER, BALL.DIAMETER); }
        }
        public int linkedObjectBX
        {
            get { return linkedObjectX * Adv.BALL_SCALE; }
        }
        public int linkedObjectBY
        {
            get { return linkedObjectY * Adv.BALL_SCALE; }
        }

        public bool isGlowing()
        {
            return glowing;
        }

        public bool isAi
        {
            get { return (ai != null); }
        }

        public void setGlowing(bool nowIsGlowing)
        {
            glowing = nowIsGlowing;
        }

        /**
         * Used when computing the closest point a ball can get
         * to a given point.  Often a ball cannot get to the exact
         * coordinate because it moves in 6 pixel steps.  This indicates
         * whether to aim for the closest coordinate that is less than or equal 
         * to the desired coordinate, the closest coordinate that is greated than
         * or equal to the desired coordinate, or the closest coordinate.
         */
        public enum STEP_ALG
        {
            LTE,
            CLOSEST,
            GTE
        }

        /**
         * Return the X coordinate that the ball can get to that is closest to 
         * the desired X coordinate.
         * @param desiredBX the x coordinate we want to get to
         * @param step_computation whether we're looking for the closest coordinate without 
         * going over, without going under, or just the closest.
         * @return the closest x coordinate given the limitations
         */
        public int getSteppedBX(int desiredBX, STEP_ALG step_computation = STEP_ALG.CLOSEST)
        {
            switch (step_computation)
            {
                case STEP_ALG.LTE:
                    return desiredBX - MOD.mod(desiredBX - this.x, BALL.MOVEMENT);
                case STEP_ALG.GTE:
                    return desiredBX + MOD.mod(this.x - desiredBX, BALL.MOVEMENT);
                default:
                    return (MOD.mod(desiredBX - this.x, BALL.MOVEMENT) <= BALL.MOVEMENT / 2 ?
                        desiredBX - MOD.mod(desiredBX - this.x, BALL.MOVEMENT) :
                        desiredBX + MOD.mod(this.x - desiredBX, BALL.MOVEMENT));
            }

        }

        /**
         * Return the Y coordinate that the ball can get to that is closest to 
         * the desired Y coordinate.
         * @param desiredBY the y coordinate we want to get to
         * @param step_computation whether we're looking for the closest coordinate without 
         * going over, without going under, or just the closest.
         * @return the closest y coordinate given the limitations
         */
        public int getSteppedBY(int desiredBY, STEP_ALG step_computation = STEP_ALG.CLOSEST)
        {
            switch (step_computation)
            {
                case STEP_ALG.LTE:
                    return desiredBY - MOD.mod(desiredBY - this.y, BALL.MOVEMENT);
                case STEP_ALG.GTE:
                    return desiredBY + MOD.mod(this.y - desiredBY, BALL.MOVEMENT);
                default:
                    return (MOD.mod(desiredBY - this.y, BALL.MOVEMENT) <= BALL.MOVEMENT / 2 ?
                        desiredBY - MOD.mod(desiredBY - this.y, BALL.MOVEMENT) :
                        desiredBY + MOD.mod(this.y - desiredBY, BALL.MOVEMENT));
            }

        }

        /**
         * The distance to an object
         * @param objectX the x of the object (IN OBJECT COORDINATE SYSTEM)
         * @param otherY the y of the object (IN OBJECT COORDINATE SYSTEM)
         * @return the distance IN BALL COORDINATE SYSTEM to object
         */
        public int distanceToObject(int objectMidX, int objectMidY)
        {
            // Figure out the distance (which is really the max difference along one axis)
            int xdist = this.midX - Adv.BALL_SCALE * objectMidX;
            if (xdist < 0)
            {
                xdist = -xdist;
            }
            int ydist = this.midY - Adv.BALL_SCALE * objectMidY;
            if (ydist < 0)
            {
                ydist = -ydist;
            }
            int dist = (xdist > ydist ? xdist : ydist);
            return dist;
        }

        private readonly byte[] ballGfxSolid = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF                   // XXXXXXXX
        };

        private readonly byte[] ballGfxOne = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xFF                   // XXXXXXXX
        };

        private readonly byte[] ballGfxTwo = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0x18,                  //    XX
            0x18,                  //    XX
            0x18,                  //    XX
            0x18,                  //    XX
            0xFF,                  // XXXXXXXX
            0xFF                   // XXXXXXXX
        };

        private readonly byte[] ballGfxShield = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x3C,                  //   XXXX
            0x18                   //    XX
        };

        private readonly byte[] ballGfxSmall = new byte[]
        {
            0x00,                  //         
            0x00,                  //         
            0x7E,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x00,                  //         
            0x00                   //         
        };


        private readonly byte[] ballGfxCross = new byte[]
        {
            0x18,                  //    XX
            0x18,                  //    XX
            0x18,                  //    XX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0x18,                  //    XX
            0x18,                  //    XX
            0x18                   //    XX
        };


    }
}
