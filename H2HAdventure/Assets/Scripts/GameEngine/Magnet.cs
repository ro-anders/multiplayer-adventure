using System;

namespace GameEngine { 
    public class Magnet : OBJECT {

        public Magnet()
            : base("magnet", objectGfxMagnet, new byte[0], 0, COLOR.BLACK)
        { }

        /**
         * @return what object the magnet is currently attracted to
         * or null if none
         */
        public OBJECT getAtractedObject()
        {
            int numPlayers = board.getNumPlayers();
            for (int i = 0; i < magnetMatrix.Length; ++i)
            {
                // Look for items in the magnet matrix that are in the same room 
                // as the magnet
                OBJECT objct = board[magnetMatrix[i]];
                if ((objct.room == room) && (objct.exists()))
                {
                    // If the object is held by a player, then the magnet does not
                    // attract it, nor does it attract anything else.
                    if (board.getPlayerHoldingObject(objct) >= 0) {
                        return null;
                    } else {
                        return objct;
                    }
                }
            }
            return null;
        }

        // Object #11 : State FF : Graphic                                                                                   
        private static byte[][] objectGfxMagnet =
        { new byte[] {
            0x3C,                  //   XXXX                                                                    
            0x7E,                  //  XXXXXX                                                                   
            0xE7,                  // XXX  XXX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3                   // XX    XX                                                                  
        } };

        // Magnet Object Matrix
        private int[] magnetMatrix =
        {
               Board.OBJECT_YELLOWKEY,
               Board.OBJECT_JADEKEY,
               Board.OBJECT_COPPERKEY,
               Board.OBJECT_WHITEKEY,
               Board.OBJECT_BLACKKEY,
               Board.OBJECT_SWORD,
               Board.OBJECT_BRIDGE,
               Board.OBJECT_CHALISE
        };


    }
}