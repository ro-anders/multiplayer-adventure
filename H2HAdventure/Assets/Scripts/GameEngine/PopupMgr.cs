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

    public class ObjectInRoomPopup: Popup {

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

    public class PopupMgr
    {
        public const int MIN_SECONDS_BETWEEN_POPUPS = 10;

        private List<Popup> popupsToShow = new List<Popup>();
        private Board gameBoard;
        private bool hasPopups;
        public bool HasPopups { 
            get { return hasPopups; }
        }


        public PopupMgr(Board inBoard)
        {
            gameBoard = inBoard;
            initializeEnterRoomPopups();
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
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_COPPERKEY,
                "whitekey", "That key can unlock the white castle", this));
            popups.Add(new ObjectInRoomPopup(Board.OBJECT_SWORD,
                "sword", "That is the sword.  It can kill dragons.", this));


            //string myKeyMessage = "That key will unlock your home castle.  You need it!";
            //string otherKeyMessage = "That key is to your opponent's castle.  " +
            //    "Lock their castle to slow them down.";
            //UnityEngine.Debug.Log("Current player = " + gameBoard.getCurrentPlayer());
            //popups.Add(new ObjectInRoomPopup(Board.OBJECT_YELLOWKEY, "yellowkey",
            //    (gameBoard.getCurrentPlayer().playerNum == 0 ? myKeyMessage : otherKeyMessage), this));
            //popups.Add(new ObjectInRoomPopup(Board.OBJECT_COPPERKEY, "copperkey",
            //    (gameBoard.getCurrentPlayer().playerNum == 1 ? myKeyMessage : otherKeyMessage), this));
            //popups.Add(new ObjectInRoomPopup(Board.OBJECT_JADEKEY, "jadekey",
                //(gameBoard.getCurrentPlayer().playerNum == 2 ? myKeyMessage : otherKeyMessage), this));



            objectsInRooms = objects.ToArray();
            enterRoomPopups = popups;
        }
        
      }
}
