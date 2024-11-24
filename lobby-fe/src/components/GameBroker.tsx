import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import '../App.css';
import ProposedGameList from './ProposedGameList'
import {GameInLobby} from '../domain/GameInLobby'
import GameService from '../services/GameService'
import ProposeModal from './ProposeModal';

interface GameBrokerProps {
  /** The name of the currently logged in user */
  username: string;
  proposed_games: GameInLobby[];
  game_change_callback: (games:GameInLobby[]) => void;
}

/**
 * Displays the list of proposed games, allowing the user to join one.
 * Also displays an option to propose a new game.
 */
function GameBroker({username, proposed_games, game_change_callback}: GameBrokerProps) {

  const [proposeModalVisible, setProposeModalVisible] = useState(false);

  /**
   * Create a proposed game
   */
  function gameProposed(new_game: GameInLobby) {
    proposed_games.push(new_game);
    GameService.proposeNewGame(new_game);
    game_change_callback(proposed_games);
  }

  /**
   * Return true if the current player is already selected to join an existing proposed game
   * or is already in a running game.
   */
  function playerCommitted(): boolean {
    let committed = false;
    for (const proposed_game of proposed_games) {
      committed = committed || (proposed_game.player_names.indexOf(username) >= 0);
    }
    return committed;
  }

  return (

    <div className="GameBroker">
      <ProposedGameList current_user={username} games={proposed_games} game_change_callback={game_change_callback}/>
      <Button disabled={playerCommitted()} onClick={()=>setProposeModalVisible(true)}>Propose Game</Button>
      <ProposeModal
        username={username}
        show={proposeModalVisible}
        onHide={()=>setProposeModalVisible(false)}
        propose_game_callback={gameProposed}
      />
    </div>
  );
}

export default GameBroker;
