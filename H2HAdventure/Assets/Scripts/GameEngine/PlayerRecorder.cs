using System;
using System.IO;

namespace GameEngine
{
    /**
     * This class can record all the actions of a human player to a file,
     * or it can playback the file to mimic the human player in a later game.
     */
    public class PlayerRecorder
    {
        public enum Modes
        {
            RECORD,
            PLAYBACK,
            NEITHER
        }

        /** What mode the system is in.  Currently just hard coded and changed
         * in the code when running tests. */
        public const Modes GLOBAL_PLAYER_RECORDER_MODE = Modes.RECORD;
        public const int PLAYBACK_PLAYER_VIEW = -1;

        /** The recording file. */
        private const string RECORDING_FILE = "recorder.txt";

        /** Names of indexes */
        private const int RESET = 0;
        private const int LEFT = 1;
        private const int UP = 2;
        private const int RIGHT = 3;
        private const int DOWN = 4;
        private const int FIRE = 5;

        /** Whether recording or playing */
        private Modes mode;

        /** The reader for reading a recording file.  If not playing back is null. */
        private StreamReader reader = null;

        /** The writer for creating a recording file.  If not recording is null. */
        private StreamWriter writer = null;

        /** The current value of all the flags smuooshed into one int */
        private int flags = 0;

        /** The value of all the flags during the previous frame (flags all 
         * smooshed into one int).  Used only for recording. */
        private int previousRecordedFlags = 0;

        /** The values of what the flags will be the next time one changes
         * (smooshed into one int).  Used only for playback. */
        private int nextFlags = 0;

        /** The frame when the flags will change to their next value */
        private int nextFrame = int.MaxValue;

        /** Whether this PlayerRecorder is recording actions or 
         * playing them back. */
        public Modes Mode
        {
            get { return mode; }
        }

        /** 
         * Create a PlayerRecorder 
         * @param inMode whether the PlayerRecorder is recording actions
         * or playing them back.
         */
        public PlayerRecorder(Modes inMode)
        {
            mode = inMode;

            if (mode == Modes.RECORD)
            {
                // Delete any previous files.
                if (File.Exists(RECORDING_FILE))
                {
                    File.Delete(RECORDING_FILE);
                }
                writer = new StreamWriter(RECORDING_FILE);
            } else if (mode == Modes.PLAYBACK)
            {
                reader = new StreamReader(RECORDING_FILE);
                loadNextChange();
            }
        }

        public void close()
        {
            if (mode == Modes.RECORD)
            {
                writer.Close();
            } else if (mode == Modes.PLAYBACK)
            {
                reader.Close();
            }
            mode = Modes.NEITHER;
        }

        /**
         * Record the latest change to the reset switch.
         */
        public void recordSwitches(int currentFrame, bool reset)
        {
            if (mode == Modes.RECORD)
            {
                // We happen to know that recordSwitches is always called first
                // (whereas recordJoystick is not only called second but
                // sometimes never called)
                // So record changes during the last frame
                if (flags != previousRecordedFlags)
                {
                    saveChange(currentFrame-1);
                    previousRecordedFlags = flags;
                }

                // Setup for this frame
                setCurrentValue(RESET, reset);
            }
        }

        public void playSwitches(int currentFrame, ref bool reset)
        {
            if (mode == Modes.PLAYBACK)
            {
                // We happen to know that playSwitches is always called first
                // (whereas playJoystick is not only called second but
                // sometimes never called)
                // So load the next changes here
                if (currentFrame >= nextFrame)
                {
                    flags = nextFlags;
                    loadNextChange();
                }
                reset = getCurrentValue(RESET);
            }
        }

        /**
         * Record the latest change to the joystick's state 
         */
        public void recordJoystick(int currentFrame, bool joyLeft, bool joyUp, bool joyRight, bool joyDown, bool joyFire)
        {
            if (mode == Modes.RECORD)
            {
                setCurrentValue(LEFT, joyLeft);
                setCurrentValue(UP, joyUp);
                setCurrentValue(RIGHT, joyRight);
                setCurrentValue(DOWN, joyDown);
                setCurrentValue(FIRE, joyFire);
            }
        }

        public void playJoystick(int currentFrame, ref bool joyLeft, ref bool joyUp, ref bool joyRight, ref bool joyDown, ref bool joyFire)
        {
            if (mode == Modes.PLAYBACK)
            {
                joyLeft = getCurrentValue(LEFT);
                joyUp = getCurrentValue(UP);
                joyRight = getCurrentValue(RIGHT);
                joyDown = getCurrentValue(DOWN);
                joyFire = getCurrentValue(FIRE);
            }
        }

        /**
         * Set the flag in the current value register.
         * @param slot the bit slot of the flag (e.g. RESET or LEFT)
         * @param the value to set the flag to
         */
        private void setCurrentValue(int slot, bool value)
        {
            flags = (value ? flags | (1 << slot) : flags & ~(1 << slot));
        }

        /**
         * Get the flag in the current value register
         * @param slot the bit slot of the flag (e.g. RESET or LEFT)
         */
        private bool getCurrentValue(int slot)
        {
            return (flags & (1 << slot)) > 0;
        }

        /**
         * Save the current value of all the flags to the recording file.  
         * Only used when recording.
         */
        private void saveChange(int currentFrame)
        {
            writer.WriteLine(currentFrame + " " + flags);
            writer.FlushAsync();
        }

        /** 
         * Load the next flags from the playback file.
         * The flags are stored in nextFlags and the frame
         * by which they become effective is put in nextFrame
         */
        private void loadNextChange()
        {
            string line = reader.ReadLine();
            if (line == null)
            {
                nextFrame = int.MaxValue;
            }
            else
            {
                // Will be "<frame> <flags>"
                // Split on the space and decode the two ints.
                string[] ints = line.Split(' ');
                nextFrame = Int32.Parse(ints[0]);
                nextFlags = Int32.Parse(ints[1]);
            }
        }

    }
}