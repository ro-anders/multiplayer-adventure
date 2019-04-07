using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class Popup
    {
        protected PopupMgr popupMgr;
        private readonly string message;
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
            return popupMgr.ShouldStillShowEnterRoomPopup(this);
        }

        public override void MarkHandled()
        {
            base.MarkHandled();
            popupMgr.MarkEnterRoomPopupHandled(this);
        }

    }

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

    public class PopupMgr
    {
        public const int MIN_SECONDS_BETWEEN_POPUPS = 10;

        private List<Popup> popupsToShow = new List<Popup>();
        public Board gameBoard;
        private bool hasPopups;
        public bool HasPopups { 
            get { return hasPopups; }
        }


        public PopupMgr(Board inBoard)
        {
            gameBoard = inBoard;
            initializeStartOfGamePopups();
            initializeEnterRoomPopups();
            EnteredRoomShowPopups(gameBoard.getCurrentPlayer().room);
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
        private List<Popup> enterRoomPopups;


        public void EnteredRoomShowPopups(int room)
        {
            for (int ctr = 0; ctr < objectsInRooms.Length; ++ctr)
            {
                OBJECT objct = gameBoard[objectsInRooms[ctr]];
                if (objct.room == room)
                {
                    showPopup(enterRoomPopups[ctr]);
                }
            }
        }

        public bool ShouldStillShowEnterRoomPopup(ObjectInRoomPopup popup)
        {
            // Make sure the current player is still in the room with the object
            // and that the popup hasn't been displayed before.
            int playersRoom = gameBoard.getCurrentPlayer().room;
            int objectRoom = gameBoard.getObject(popup.objectNum).room;
            return (playersRoom == objectRoom) && !popup.HasFired;
        }

        public void MarkEnterRoomPopupHandled(ObjectInRoomPopup popup)
        {
            List<int> objects = new List<int>(objectsInRooms);
            int index = objects.FindIndex(x => x == popup.objectNum);
            if (index >= 0)
            {
                objects.RemoveAt(index);
                objectsInRooms = objects.ToArray();
                enterRoomPopups.RemoveAt(index);
            }
        }

        private void initializeEnterRoomPopups()
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
            UnityEngine.Debug.Log("Current player = " + gameBoard.getCurrentPlayer());
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

    }


}
