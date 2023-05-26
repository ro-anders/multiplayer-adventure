using GameEngine;

namespace GameEngine.Ai
{
    public class WinGameWithChalice : AiObjective
    {
        public override string ToString()
        {
            return "retrieve chalice";
        }

        protected override void doComputeStrategy()
        {
            Portcullis homeGate = this.aiPlayer.homeGate;
            this.addChild(new UnlockCastle(homeGate.getPKey()));
            this.addChild(new ObtainObject(Board.OBJECT_CHALISE));
            this.addChild(new BringObjectToRoomObjective(homeGate.insideRoom, Board.OBJECT_CHALISE));
        }

        protected override bool computeIsCompleted()
        {
            // This is never completed, or by the time it is we don't
            // ever call this anymore
            return false;
        }

    }
}