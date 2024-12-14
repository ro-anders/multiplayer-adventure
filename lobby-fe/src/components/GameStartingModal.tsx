import '../App.css';
import '../css/GameStartingModal.css'

interface GameStartingModalProps {
  /** Whether the game backend server is up and running */
  waiting_on_server: boolean;
}

/**
 * Displays a waiting message while the game server is starting up.
 * This probably didn't need to be it's own class but it used to do more stuff.
 */
function GameStartingModal({waiting_on_server}: GameStartingModalProps) {

    return (
        <div className="starting-modal-overlay">
          <div className="starting-modal-dialog">
            {waiting_on_server && 
              <p>Waiting for Game Server to start.</p>
            }
          </div>
        </div>
      );
    }

export default GameStartingModal;
