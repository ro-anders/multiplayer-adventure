import { Button } from 'react-bootstrap';
import '../App.css';
import '../css/GameStartingModal.css'
import { GameInLobby } from '../domain/GameInLobby';
import { useEffect, useState } from 'react';
import SettingsService from '../services/SettingsService';

interface GameStartingModalProps {

  /** The game the user has selected to start */
  game: GameInLobby;

  /** The slot of the current player */
  slot: number;

  /** The experience level of the current player */
  experience_level: number;
}

/**
 * Displays a waiting message while the game server is starting up.
 * This probably didn't need to be it's own class but it used to do more stuff.
 */
function GameStartingModal({game, slot, experience_level}: GameStartingModalProps) {

  /** Whether the modal is shown and, if it is, what it is waiting for to dismiss */
  const [gameServer, setGameServer] = useState<string>('');

  // Query the backend for the game server.  This
  // will block until a game server can be returned.
  useEffect(() => {
      async function getGameServer() {
        const game_server_ip = await SettingsService.getGameServerIP();
        setGameServer(game_server_ip)
      }

    if (gameServer === '') {
      getGameServer();
    }
  }, [gameServer]);

  /**
   * User has just pressed "Ok".  Start the game.
   */
  async function startGame() {
    const code =
      // highest bits hold the session
      16 * game.session +
      // Next two bits hold the slot number
      4 * slot +
      // next bit holds the help popups flag
      2 * (experience_level === 1 ? 1 : 0) +
      // last bit holds the maze guide flag
      (experience_level <= 2 ? 1 : 0);
    const url = process.env.REACT_APP_MPLAYER_GAME_URL
    window.open(`${url}/index.html?gamecode=${code}&host=${gameServer}`, '_self')
  }

    return (
        <div className="starting-modal-overlay">
          <div className="starting-modal-dialog">
            <p>For performance reasons, the game is served on an unsecured web page.
              Your browser may warn you and prompt before continuing.
            </p>
            {(gameServer === '') && 
              <p>Waiting for Game Server to start.  This takes about a minute</p>
            }
            {(gameServer !== '') && 
              <Button size="sm" className='lobby-game-action' 
                onClick={() => startGame()}>Ok</Button>
            }
          </div>
        </div>
      );
    }

export default GameStartingModal;
