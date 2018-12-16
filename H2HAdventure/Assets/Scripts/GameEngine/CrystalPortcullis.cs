using System;
namespace GameEngine
{
    public class CrystalPortcullis: Portcullis
    {

        /** The total number of keys needed to open the gate. */
        public const int TOTAL_KEYS_NEEDED = 3;


        private static int[] STOPPING_POINTS = { 0, 3, 6, 12 };

        /** The keys needed to open this portcullis */
        private OBJECT[] keys;

        /** How many keys still need to be touched to the gate to open it. */
        private int keysNeeded;


        /**
        * Constructs the portcullis for the crystal castle.
        * foyer - the crystal foyer
        * keys - the set of keys required to open the portcullis.  The portcullis will only
        * fully open once all keys have touched it.
        */
        public CrystalPortcullis(ROOM foyer, OBJECT[] inKeys) :
            base("crystal gate", Map.CRYSTAL_CASTLE, foyer, null) {
            keys = inKeys;
            keysNeeded = TOTAL_KEYS_NEEDED;
            color = COLOR.CRYSTAL;
        }

        /**
        * Override to put in multi-key behavior 
        */
        public override PortcullisStateAction checkKeyInteraction()
        {
            PortcullisStateAction gateAction = null;

            if (keysNeeded > 0)
            {
                // Run through the keys to see if any are touching.
                for (int ctr = 0; ctr < TOTAL_KEYS_NEEDED; ++ctr)
                {
                    if ((keys[ctr] != null) && (checkKeyTouch(keys[ctr])))
                    {
                        keys[ctr] = null;
                        --keysNeeded;
                        if (EasterEgg.crystalColor < COLOR.DARK_CRYSTAL3)
                        {
                            EasterEgg.darkenCastle(COLOR.DARK_CRYSTAL3);
                        }
                    }
                }
            }
            return gateAction;
        }

        /**
        * Update its internal state for this turn.  This involves lifting the gate if it is currently opening, etc...
        */
        public override void moveOneTurn()
        {
            if (state > STOPPING_POINTS[keysNeeded])
            {
                --state;
            }
            if (state == OPEN_STATE)
            {
                if (!allowsEntry)
                {
                    EasterEgg.openedCastle();
                }
                allowsEntry = true;
            }
        }


    }
}
