import '../App.css';
import '../css/GameStartingModal.css'

interface GameStartingModalProps {
  /** The names of those playing the game in slot order */
  player_list: string[];

  /** Whether the game backend server is up and running */
  waiting_on_server: boolean;
}

/**
 * Displays a list of proposed games and allows the user
 * to join, start or leave a game.
 */
function GameStartingModal({player_list, waiting_on_server}: GameStartingModalProps) {

    return (
        <div className="modal-overlay">
          <div className="modal-dialog">
            <ul>
            <li key="player1">
                    <p>
                        <img src="logo192.png" alt="gold castle"/>
                        &nbsp;
                        <b>{player_list[0]}</b> is in the gold castle.
                    </p>
                </li>
                <li key="player2">
                    <p>
                        <img src="logo192.png" alt="copper castle"/>
                        &nbsp;
                        <b>{player_list[1]}</b> is in the copper castle.
                    </p>
                </li>
                {(player_list.length > 2) &&
                  <li key="player2">
                    <p>
                        <img src="logo192.png" alt="jade castle"/>
                        &nbsp;
                        <b>{player_list[2]}</b> is in the jade castle.
                    </p>
                  </li>
                }
            </ul>
            {waiting_on_server && 
              <p>Waiting for Game Server to start.</p>
            }
          </div>
        </div>
      );
    }

export default GameStartingModal;
