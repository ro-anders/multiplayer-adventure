
namespace GameEngine.Ai
{

    /**
     * Unlock a castle.
     */
    public class UnlockCastle : AiObjective
    {
        int portId;
        Portcullis port;

        public UnlockCastle(int inPortId)
        {
            portId = inPortId;
        }

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            port = (Portcullis)board.getObject(portId);
        }

        protected override void doComputeStrategy()
        {
            int key = port.key.getPKey();
            this.addChild(new ObtainObject(key));
            this.addChild(new GoTo(port.room, Portcullis.EXIT_X, 0x30, key));
            this.addChild(new RepositionKey(key));
        }

        public override RRect getBDestination()
        {
            return new RRect(port.room, Portcullis.EXIT_X - aiPlayer.linkedObjectX, 0x3D, 1, 1);
        }

        protected override bool computeIsCompleted()
        {
            // Need to make sure not only that the castle is locked but that the
            // key isn't overlapping the gate or else the gate could shut again.
            return port.allowsEntry && !port.BRect.overlaps(port.key.BRect);
        }

        public override string ToString()
        {
            return "unlock " + board.getObject(portId).label;
        }
    }

}