import Button from 'react-bootstrap/Button';
import '../App.css';
import ProposedGameList from './ProposedGameList'
import {Game} from '../domain/Game'
import GameService from '../services/GameService'

interface GameBrokerProps {
  /** The name of the currently logged in user */
  username: string;
  proposed_games: Game[];
  game_change_callback: (games:Game[]) => void;
}

/**
 * Displays the list of proposed games, allowing the user to join one.
 * Also displays an option to propose a new game.
 */
function GameBroker({username, proposed_games, game_change_callback}: GameBrokerProps) {
  /**
   * Create a proposed game
   */
  function proposeClicked() {
    const new_game: Game = {
      session: Math.floor(Math.random()*100000),
      game_number: 2,
      number_players: 2,
      player1_name: username
    };
    proposed_games.push(new_game);
    GameService.proposeNewGame(new_game);
    game_change_callback(proposed_games);
  }

  return (

    <div className="GameBroker">
      <ProposedGameList current_user={username} games={proposed_games} game_change_callback={game_change_callback}/>
      <Button onClick={proposeClicked}>Propose Game</Button>
    </div>
  );
}

export default GameBroker;
