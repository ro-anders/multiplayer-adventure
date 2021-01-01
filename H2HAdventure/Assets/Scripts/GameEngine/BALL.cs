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
        public int linkedObjectX;          // X value representing the offset from the ball to the object being carried
        public int linkedObjectY;          // Y value representing the offset from the ball to the object being carried
        public bool hit;                  // the ball hit something
        public int hitObject;              // the object that the ball hit
        public readonly byte[] gfxData;        // graphics data for ball
        public Portcullis homeGate;       // The gate of the castle you start at
        public bool isAi;                   // Whether this ball is for an AI player

        /** During the gauntlet, once you reach the black castle you flash like the chalise until you reset or you reach the
            * gold castle where you win. */
        private bool glowing;

        public BALL(int inPlayerNum, Portcullis inHomeGate, bool inAltIcons, bool inIsAi)
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
            isAi = inIsAi;
        }

        public string toString()
        {
            return "player " + playerNum + " at (" + x + "," + y + ")@" + room +
                (linkedObject >= 0 ? " with " + linkedObject : "");
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

        public void setGlowing(bool nowIsGlowing)
        {
            glowing = nowIsGlowing;
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
            int xdist = this.midX - 2 * objectMidX;
            if (xdist < 0)
            {
                xdist = -xdist;
            }
            int ydist = this.midY - 2 * objectMidY;
            if (ydist < 0)
            {
                ydist = -ydist;
            }
            int dist = (xdist > ydist ? xdist : ydist);
            return dist;
        }

        public enum Adjust
        {
            CLOSEST, // Find the closest reachable point the desired point
            BELOW, // Find the closest reachable point no higher than the desired point
            ABOVE // Find the closest reachable point no lower than the desired point
        }
        /**
         * Because a ball moves in 6 pixel increments, some destinations
         * it can't exactly reach.  This looks at the balls current position
         * and determines the closest possible position to the desired destination.
         * @param destX the x-coordinate of the desired destination.  This method will
         * modify this value to be a reachable pixel value.
         * @param destY the y-coordinate of the desired destination.  This method will
         * modify this value to be a reachable pixel value.
         * @param guidance - if the desired destination is not reachable, this 
         * parameter gives guidance on which reachable destination should be 
         * chosen.  Default is CLOSEST.
         */
        public void adjustDestination(ref int destX, ref int destY, Adjust guidance = Adjust.CLOSEST)
        {
            const int HALF_MOVEMENT = MOVEMENT / 2;
            int xAdjustment = MOD.mod(destX - this.x, MOVEMENT);
            destX -= xAdjustment;
            if (xAdjustment > HALF_MOVEMENT)
            {
                destX += MOVEMENT;
            }

            int yAdjustment = MOD.mod(destY - this.y, MOVEMENT);
            destY -= yAdjustment;
            if (((yAdjustment > HALF_MOVEMENT) || (guidance == Adjust.ABOVE)) &&
                (guidance != Adjust.BELOW))
            {
                destX += MOVEMENT;
            }
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
