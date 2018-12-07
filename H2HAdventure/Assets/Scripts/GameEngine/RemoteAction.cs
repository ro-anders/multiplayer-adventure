using System;
namespace GameEngine
{
    public enum ActionType
    {
        PING = 1,
        PLAYER_MOVE,
        PLAYER_PICKUP,
        PLAYER_RESET,
        PLAYER_WIN,
        DRAGON_MOVE,
        DRAGON_STATE,
        PORTCULLIS_STATE,
        BAT_MOVE,
        BAT_PICKUP,
        OBJECT_MOVE
    }


    public abstract class RemoteAction
    {
        public int sender;             // The number of the player sending this action (1-3)
        public ActionType typeCode;
        public RemoteAction(ActionType inCode)
        {
            typeCode = inCode;
        }

        public void setSender(int inSender)
        {
            sender = inSender;
        }

        public abstract int[] serialize();

        public abstract void deserialize(int[] serialized);
    }


    public abstract class MoveAction : RemoteAction
    {
        public int room;               // The room the player was in
        public int posx;               // The x-coordinate of the player in the room
        public int posy;               // The y-coordinate of the player in the room
        public int velx;               // -1 for moving left, 1 for right, and 0 for still or just up/down
        public int vely;               // -1 for down, 1 for up, and 0 for still or just left/right

        public MoveAction(ActionType inCode) :
        base(inCode)
        { }

        public MoveAction(ActionType inCode, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
        base(inCode)
        {
            room = inRoom;
            posx = inPosx;
            posy = inPosy;
            velx = inVelx;
            vely = inVely;
        }
    }

    public class PlayerMoveAction : MoveAction
    {
        public const ActionType CODE = ActionType.PLAYER_MOVE;

        public PlayerMoveAction() :
            base(CODE)
        { }

        public PlayerMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
        base(CODE, inRoom, inPosx, inPosy, inVelx, inVely) {}

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, room, posx, posy, velx, vely };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            room = serialized[2];
            posx = serialized[3];
            posy = serialized[4];
            velx = serialized[5];
            vely = serialized[6];
        }
    }

    public class PlayerPickupAction : RemoteAction {
        public int pickupObject;
        public int pickupX;
        public int pickupY;
        public int dropObject;
        public int dropRoom;
        public int dropX;
        public int dropY;

        public const ActionType CODE = ActionType.PLAYER_PICKUP;

        public PlayerPickupAction():
        base(CODE) {}

        public PlayerPickupAction(int inPickupObject, int inPickupX, int inPickupY, int inDropObject, int inDropRoom, int inDropX, int inDropY):
        base(CODE) {
            pickupObject = inPickupObject;
            pickupX = inPickupX;
            pickupY = inPickupY;
            dropObject = inDropObject;
            dropRoom = inDropRoom;
            dropX = inDropX;
            dropY = inDropY;
        }

        public void setPickup(int inPickupObject, int inPickupX, int inPickupY) {
            pickupObject = inPickupObject;
            pickupX = inPickupX;
            pickupY = inPickupY;
        }

        public void setDrop(int inDropObject, int inDropRoom, int inDropX, int inDropY) {
            dropObject = inDropObject;
            dropRoom = inDropRoom;
            dropX = inDropX;    
            dropY = inDropY;
        }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, pickupObject, pickupX, pickupY, dropObject, dropRoom, dropX, dropY };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            pickupObject = serialized[2];
            pickupX = serialized[3];
            pickupY = serialized[4];
            dropObject = serialized[5];
            dropRoom = serialized[6];
            dropX = serialized[7];
            dropY = serialized[8];
        }

    };

    public class PlayerResetAction : RemoteAction {

        public static ActionType CODE = ActionType.PLAYER_RESET;

        public PlayerResetAction() :
        base(CODE)
        { }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
        }


    };

    public class PlayerWinAction : RemoteAction {
        public int winInRoom;

        public const ActionType CODE = ActionType.PLAYER_WIN;

        public PlayerWinAction(): 
        base(CODE) {}

        public PlayerWinAction(int inWinInRoom):
        base(CODE) {
            winInRoom = inWinInRoom;
        }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, winInRoom };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            winInRoom = serialized[2];
        }
    };


    public class DragonMoveAction : MoveAction {
        public int dragonNum;          // 0=Rhindle, 1=Yorgle, 2=Grindle
        public int distance;           // Distance from player reporting position

        public const ActionType CODE = ActionType.DRAGON_MOVE;

        public DragonMoveAction() :
        base(CODE){}

        public DragonMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely,
                                int inDragonNum, int inDistance):
            base(CODE, inRoom, inPosx, inPosy, inVelx, inVely) {
                dragonNum = inDragonNum;
                distance = inDistance;
            }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, dragonNum, room, posx, posy, velx, vely, distance };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            dragonNum = serialized[2];
            room = serialized[3];
            posx = serialized[4];
            posy = serialized[5];
            velx = serialized[6];
            vely = serialized[7];
            distance = serialized[8];
        }
    };

    public class DragonStateAction : RemoteAction {
        public int dragonNum;          // 0=Rhindle, 1=Yorgle, 2=Grindle
        public int newState;
        public int room;
        public int posx;
        public int posy;
        public int velx;
        public int vely;

        public const ActionType CODE = ActionType.DRAGON_STATE;

        public DragonStateAction() :
        base(CODE) {}

        public DragonStateAction(int inDragonNum, int inState, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
        base(CODE) {
            dragonNum = inDragonNum;
            newState = inState;
            room = inRoom;
            posx = inPosx;
            posy = inPosy;
            velx = inVelx;
            vely = inVely;
        }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, dragonNum, newState, room, posx, posy, velx, vely };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            dragonNum = serialized[2];
            newState = serialized[3];
            room = serialized[4];
            posx = serialized[5];
            posy = serialized[6];
            velx = serialized[7];
            vely = serialized[8];
        }
    };

    public class PortcullisStateAction : RemoteAction
    {
        public const ActionType CODE = ActionType.PORTCULLIS_STATE;
        public int portPkey; // The Object::pkey of the portcullis, nothing to do with the key that unlocks it
        public int newState;
        public bool allowsEntry;


        public PortcullisStateAction() :
        base(CODE)
        { }

        public PortcullisStateAction(int inPortPkey, int inNewState, bool inAllowsEntry) :
        base(CODE)
        {
            portPkey = inPortPkey;
            newState = inNewState;
            allowsEntry = inAllowsEntry;
        }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, portPkey, newState, (allowsEntry ? 1 : 0) };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            portPkey = serialized[2];
            newState = serialized[3];
            allowsEntry = (serialized[4] > 0);
        }
    }

    public class BatMoveAction : MoveAction {
        public int distance;           // Distance from player reporting position

        public const ActionType CODE = ActionType.BAT_MOVE;

        public BatMoveAction():
        base(CODE) {}

        public BatMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely, int inDistance) :
                    base(CODE, inRoom, inPosx, inPosy, inVelx, inVely)
        {
            distance = inDistance;
        }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, room, posx, posy, velx, vely, distance };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            room = serialized[2];
            posx = serialized[3];
            posy = serialized[4];
            velx = serialized[5];
            vely = serialized[6];
            distance = serialized[7];
        }
    };

    public class BatPickupAction : RemoteAction {
        public int pickupObject;
        public int pickupX;
        public int pickupY;
        public int dropObject;
        public int dropRoom;
        public int dropX;
        public int dropY;

        public const ActionType CODE = ActionType.BAT_PICKUP;

        public BatPickupAction():
        base(CODE) {}

        public BatPickupAction(int inPickupObject, int inPickupX, int inPickupY, int inDropObject, int inDropRoom, int inDropX, int inDropY):
        base(CODE) {
            pickupObject = inPickupObject;
            pickupX = inPickupX;
            pickupY = inPickupY;
            dropObject = inDropObject;
            dropRoom = inDropRoom;
            dropX = inDropX;
            dropY = inDropY;
        }

        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, pickupObject, pickupX, pickupY, dropObject, dropRoom, dropX, dropY };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            pickupObject = serialized[2];
            pickupX = serialized[3];
            pickupY = serialized[4];
            dropObject = serialized[5];
            dropRoom = serialized[6];
            dropX = serialized[7];
            dropY = serialized[8];
        }
    };

    /**
     * This places an object.  It is used when an object touches a gate and in level 3 when one client randomly
     * assigns the objects and then communicates the positions to the other clients.  It can be used to move
     * the dragons and bat, too.
     */
    public class ObjectMoveAction : RemoteAction
    {
        public const ActionType CODE = ActionType.OBJECT_MOVE;

        public int objct;
        public int room;
        public int x;
        public int y;


        public ObjectMoveAction() :
        base(CODE)
        { }

        public ObjectMoveAction(int inObject, int inRoom, int inX, int inY) :
        base(CODE)
        {
            objct = inObject;
            room = inRoom;
            x = inX;
            y = inY;
        }
        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender, room, x, y };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
            room = serialized[2];
            x = serialized[3];
            y = serialized[4];
        }
    }

    public class PingAction : RemoteAction
    {
        public const ActionType CODE = ActionType.PING;

        public PingAction() : base(CODE) { }
        public override int[] serialize()
        {
            int[] serialized = { (int)CODE, sender };
            return serialized;
        }

        public override void deserialize(int[] serialized)
        {
            sender = serialized[1];
        }
    }
}
