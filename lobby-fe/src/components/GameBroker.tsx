import Button from 'react-bootstrap/Button';
import '../App.css';
import ProposedGameList from './ProposedGameList'
import {Game} from '../domain/Game'
import GameService from '../services/GameService'

interface GameBrokerProps {
  /** The name of the currently logged in user */
  username: string;
}


/**
 * 
 */
function GameBroker({username}: GameBrokerProps) {
  /**
   * Create a proposed game
   */
  async function proposeClicked() {
    const new_game: Game = {
      game_number: 2,
      number_players: 2,
      player1_name: username
    }
    await GameService.proposeNewGame(new_game)
  }

  return (

    <div className="Roster">
      <ProposedGameList current_user={username}/>
      <Button onClick={proposeClicked}>Propose Game</Button>
    </div>
  );
}

export default GameBroker;
