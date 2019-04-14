using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class Popup
    {
        protected PopupMgr popupMgr;
        protected string message;
        public string Message
        {
            get { return message; }
        }
        private readonly string imageName;
        public string ImageName
        {
            get { return imageName; }
        }
        private bool hasFired;
        public bool HasFired
        {
            get { return hasFired; }
        }

        public Popup(string inImageName, string inMessage, PopupMgr inPopupMgr)
        {
            message = inMessage;
            imageName = inImageName;
            hasFired = false;
            popupMgr = inPopupMgr;
        }

        /**
         * There may be some time between when a popup is triggered
         * and when it is shown.  This will return if it is still valid
         * to be shown.
         */
        public virtual bool ShouldStillShow()
        {
            return true;
        }

        public virtual void MarkHandled()
        {
            hasFired = true;
        }
    }

    //-------------------------------------------------------------------------
    public class ObjectInRoomPopup : Popup
    {

        public int objectNum;

        public ObjectInRoomPopup(int inObjectNum, string inImageName, string inMessage, PopupMgr inPopupMgr) :
            base(inImageName, inMessage, inPopupMgr)
        {
            objectNum = inObjectNum;
        }

        public override bool ShouldStillShow()
        {
            return popupMgr.ShouldStillShowObjectInRoomPopup(this);
        }

        public override void MarkHandled()
        {
            base.MarkHandled();
            popupMgr.MarkObjectInRoomPopupHandled(this);
        }

    }

    //-------------------------------------------------------------------------
    public class EnterRoomPopup : Popup
    {

        public List<int> roomNums = new List<int>();

        public EnterRoomPopup(int[] inRoomNums, string inMessage, PopupMgr inPopupMgr) :
            base("", inMessage, inPopupMgr)
        {
            roomNums.AddRange(inRoomNums);
        }

        public override bool ShouldStillShow()
        {
            return popupMgr.ShouldStillShowEnterRoomPopup(this);
        }

        public override void MarkHandled()
        {
            base.MarkHandled();
            popupMgr.MarkEnterRoomPopupHandled(this);
        }

    }

    //-------------------------------------------------------------------------
    public class TimedPopup : Popup
    {

        public int relativeSeconds;
        public int absoluteFrame = -1;

        public TimedPopup(int inRelativeSeconds, string inImageName, string inMessage, PopupMgr inPopupMgr) :
            base(inImageName, inMessage, inPopupMgr)
        {
            relativeSeconds = inRelativeSeconds;
        }

    }

    //-------------------------------------------------------------------------
    public class HowToMovePopup : Popup
    {

        public HowToMovePopup(PopupMgr inPopupMgr) :
            base(null, "Use arrow keys to move up, down, left and right", inPopupMgr)
        {}

        public override bool ShouldStillShow()
        {
            BALL ball = popupMgr.gameBoard.getCurrentPlayer();
            return (ball.room == ball.homeGate.room) &&
                ((ball.x == 0x50 * 2) ||
                 (ball.y == 0x20 * 2));
        }

    }

    //-------------------------------------------------------------------------
    public class DragonPopup: Popup
    {
        private Board gameBoard;

        public DragonPopup(PopupMgr inPopupMgr) :
            base("dragon", "That is a dragon.  Run!", inPopupMgr)
        { }

        public override bool ShouldStillShow()
        {
            // Determine if there is still a dragon in the room.
            bool stillInRoom = false;
            BALL currentPlayer = popupMgr.gameBoard.getCurrentPlayer();
            int playersRoom = currentPlayer.room;
            for(int ctr=Board.OBJECT_REDDRAGON; ctr<=Board.OBJECT_GREENDRAGON; ++ctr)
            {
                int dragonsRoom = popupMgr.gameBoard.getObject(ctr).room;
                stillInRoom = stillInRoom || (playersRoom == dragonsRoom);
            }

            if (stillInRoom)
            {
                // Figure out what the message should be.
                if (currentPlayer.linkedObject == Board.OBJECT_SWORD)
                {
                    message = "That is a dragon.  You can kill him with your sword.";
                }
            } else
            {
                // Reset that we need the dragon popup
                popupMgr.needPopup[PopupMgr.SEE_DRAGON] = true;
            }

            return stillInRoom;
        }

    }

    //-------------------------------------------------------------------------
    public class PopupMgr
    {
        public const int MIN_SECONDS_BETWEEN_POPUPS = 10;

        public const int SEE_DRAGON = 0;
        public const int NUM_NEED_POPUPS = 1;
        public bool[] needPopup;

        private List<Popup> popupsToShow = new List<Popup>();
        public Board gameBoard;
        private bool hasPopups;
        public bool HasPopups { 
            get { return hasPopups; }
        }


        public PopupMgr(Board inBoard)
        {
            gameBoard = inBoard;
            needPopup = new bool[NUM_NEED_POPUPS];
        }

        public void SetupPopups() { 
            initializeStartOfGamePopups();
            initializeObjectInRoomPopups();
            initializeEnterRoomPopups();
            EnteredRoomShowPopups(gameBoard.getCurrentPlayer().room);
            for(int ctr=0; ctr<NUM_NEED_POPUPS; ++ctr)
            {
                needPopup[ctr] = true;
            }
        }

        public Popup GetNextPopup()
        {
            while (popupsToShow.Count > 0)
            {
                Popup nextPopup = popupsToShow[0];
                popupsToShow.RemoveAt(0);
                hasPopups = popupsToShow.Count > 0;

                if (nextPopup.ShouldStillShow())
                {
                    nextPopup.MarkHandled();
                    return nextPopup;
                }
            }
            return null;
        }

        private void showPopup(Popup popup)
        {
            popupsToShow.Add(popup);
            hasPopups = true;
        }

        private void showPopupNow(Popup popup)
        {
            popupsToShow.Insert(0, popup);
            hasPopups = true;
        }

        private void initializeStartOfGamePopups()
        {
            showPopup(new Popup("chalice", "This is your home castle.  " +
            "Bring the chalice back here to win the game.", this));
            // Only put the line about the key if the key isn't sitting in 
            // the same rooom
            BALL player = gameBoard.getCurrentPlayer();
            int key_id = Board.OBJECT_YELLOWKEY + player.playerNum;
            OBJECT key = gameBoard[key_id];
            if (player.room != key.room)
            {
                string key_name = (player.playerNum == 0 ? "gold" :
                 (player.playerNum == 1 ? "copper" : "jade"));
                showPopup(new Popup(key_name + "key", "But first you " +
                    "need to unlock your castle.  Find the " + key_name + 
                    " key and bring it back here.", this));
            }
            showPopup(new HowToMovePopup(this));
        }

        //----------------------------------------------------------
        // Popups for First time we see an object


        private int[] objectsInRooms;
        private List<Popup> objectInRoomPopups;


        public bool ShouldStillShowObjectInRoomPopup(ObjectInRoomPopup popup)
        {
            // Make sure the current player is still in the room with the object
            // and that the popup hasn't been displayed before.
            int playersRoom = gameBoard.getCurrentPlayer().room;
            int objectRoom = gameBoard.getObject(popup.objectNum).room;
            return (playersRoom == objectRoom) && !popup.HasFired;
        }

        public void MarkObjectInRoomPopupHandled(ObjectInRoomPopup popup)
        {
            List<int> objects = new List<int>(objectsInRooms);
            int index = objects.FindIndex(x => x == popup.objectNum);
            if (index >= 0)
            {
                objects.RemoveAt(index);
                objectsInRooms = objects.ToArray();
                objectInRoomPopups.RemoveAt(index);
            }
        }

        private void initializeObjectInRoomPopups()
        {
            List<int> objects = new List<int>();
            List<Popup> popups = new List<Popup>();
            objects.Add(Board.OBJECT_BLACKKEY);
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_BLACKKEY,
                "blackkey", "That key can unlock the black castle", this));
            objects.Add(Board.OBJECT_WHITEKEY);
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_WHITEKEY,
                "whitekey", "That key can unlock the white castle", this));
            objects.Add(Board.OBJECT_SWORD);
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_SWORD,
                "sword", "That is the sword.  It can kill dragons.", this));


            string myKeyMessage = "That key will unlock your home castle.  You need it!";
            string otherKeyMessage = "That key is to your opponent's castle.  " +
                "Lock their castle to slow them down.";
            objects.Add(Board.OBJECT_YELLOWKEY);
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_YELLOWKEY, "goldkey",
                (gameBoard.getCurrentPlayer().playerNum == 0 ? myKeyMessage : otherKeyMessage), this));
            objects.Add(Board.OBJECT_COPPERKEY);
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_COPPERKEY, "copperkey",
                (gameBoard.getCurrentPlayer().playerNum == 1 ? myKeyMessage : otherKeyMessage), this));
            objects.Add(Board.OBJECT_JADEKEY);
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_JADEKEY, "jadekey",
                (gameBoard.getCurrentPlayer().playerNum == 2 ? myKeyMessage : otherKeyMessage), this));



            objectsInRooms = objects.ToArray();
            objectInRoomPopups = popups;
        }

        //----------------------------------------------------------
        // Popups for when we enter a room for the first time


        private int[] roomsToPopup;
        private List<Popup> enterRoomPopups;


        // This applies to both object in room popups and entered room popups
        public void EnteredRoomShowPopups(int room)
        {
            for (int ctr = 0; ctr < roomsToPopup.Length; ++ctr)
            {
                if (roomsToPopup[ctr] == room)
                {
                    showPopup(enterRoomPopups[ctr]);
                }
            }
            for (int ctr2 = 0; ctr2 < objectsInRooms.Length; ++ctr2)
            {
                OBJECT objct = gameBoard[objectsInRooms[ctr2]];
                if (objct.room == room)
                {
                    showPopup(objectInRoomPopups[ctr2]);
                }
            }
        }


        public bool ShouldStillShowEnterRoomPopup(EnterRoomPopup popup)
        {
            // Make sure the current player is still in one the rooms
            // and that the popup hasn't been displayed before.
            int playersRoom = gameBoard.getCurrentPlayer().room;
            return popup.roomNums.Contains(playersRoom) && !popup.HasFired;
        }

        public void MarkEnterRoomPopupHandled(EnterRoomPopup popup)
        {
            List<int> rooms = new List<int>(roomsToPopup);
            int index = enterRoomPopups.FindIndex(x => x == popup);
            while (index >= 0)
            {
                rooms.RemoveAt(index);
                enterRoomPopups.RemoveAt(index);
                index = enterRoomPopups.FindIndex(x => x == popup);
            }
            roomsToPopup = rooms.ToArray();
        }

        private void initializeEnterRoomPopups()
        {
            List<int> rooms = new List<int>();
            List<Popup> popups = new List<Popup>();

            int[] firstMazeRooms = {Map.BLUE_MAZE_1, Map.BLUE_MAZE_2,
                Map.WHITE_MAZE_1, Map.WHITE_MAZE_3};
            EnterRoomPopup firstMazePopup = new EnterRoomPopup(firstMazeRooms,
                "This is a labyrinth.  Follow the black line to " +
                "get to the other castles.", this);
            foreach(int room in firstMazeRooms)
            {
                rooms.Add(room);
                popups.Add(firstMazePopup);
            }
            if (gameBoard.map.layout == Map.MAP_LAYOUT_BIG)
            {
                int[] firstDarkMazeRooms = {Map.WHITE_MAZE_1, Map.WHITE_MAZE_2,
                    Map.WHITE_MAZE_3, Map.BLACK_MAZE_ENTRY};
                EnterRoomPopup firstDarkMazePopup = new EnterRoomPopup(firstMazeRooms,
                    "This maze is dark.  Imagine you have a torch and can only " +
                    "see the area the torch lights up.", this);
                foreach (int room in firstDarkMazeRooms)
                {
                    rooms.Add(room);
                    popups.Add(firstDarkMazePopup);
                }
            }
            int[] brownGuideRooms = {Map.WHITE_MAZE_2, Map.RED_MAZE_1,
                Map.BLACK_MAZE_ENTRY};
            EnterRoomPopup brownGuidePopup = new EnterRoomPopup(brownGuideRooms,
                "Follow the brown line to get to useful points in the maze.", this);
            foreach (int room in brownGuideRooms)
            {
                rooms.Add(room);
                popups.Add(brownGuidePopup);
            }
            rooms.Add(Map.RED_MAZE_3);
            popups.Add(new EnterRoomPopup(new int[] { Map.RED_MAZE_3 },
                "The room below this can only be reached with the bridge.",
                 this));



            roomsToPopup = rooms.ToArray();
            enterRoomPopups = popups;
        }

        //----------------------------------------------------------
        // Popups for appearing in a certain amount of time 

        private List<TimedPopup> scheduledPopups = new List<TimedPopup>();
        private List<TimedPopup> unscheduledPopups = new List<TimedPopup>();

        public void CheckTimedPopups(int currentFrameNumber)
        {
            // First we run through the unscheduled popups and schedule them
            while (unscheduledPopups.Count > 0)
            {
                TimedPopup next = unscheduledPopups[0];
                next.absoluteFrame = currentFrameNumber + 60 * next.relativeSeconds;
                unscheduledPopups.RemoveAt(0);
                // Find where in the scheduled popups the popup should go
                int slot = 0;
                while ((slot < scheduledPopups.Count) && (scheduledPopups[slot].absoluteFrame < next.absoluteFrame))
                {
                    ++slot;
                }
                scheduledPopups.Insert(slot, next);
            }

            // Now popup any that have hit their scheduled time
            while ((scheduledPopups.Count > 0) && (scheduledPopups[0].absoluteFrame <= currentFrameNumber))
            {
                showPopup(scheduledPopups[0]);
                scheduledPopups.RemoveAt(0);
            }
        }

        public void AddTimedPopup(TimedPopup popup)
        {
            unscheduledPopups.Add(popup);
        }

        //----------------------------------------------------------
        // Popups for dragons

        public void ShowDragonPopup()
        {
            needPopup[SEE_DRAGON] = false;
            showPopupNow(new DragonPopup(this));
            showPopup(new Popup("sword", "Find the sword to kill dragons.", this));
        }


    }


}
