import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import '../App.css';
import ProposedGameList from './ProposedGameList'
import {GameInLobby, GAMESTATE__PROPOSED} from '../domain/GameInLobby'
import GameService from '../services/GameService'
import ProposeModal from './ProposeModal';

interface GameBrokerProps {
  /** The name of the currently logged in user */
  username: string;

  /** The experience level of the currently logged in user 1, 2 or 3. */
  experience_level: number;

  /** List of currently proposed games */
  proposed_games: GameInLobby[];

  /** Callback to call if we change something about the currently proposed games (e.g. we
   * proposed a new one, or withdrew the current player from an existing one) */
  game_change_callback: (games:GameInLobby) => void;
}

/**
 * Displays the list of proposed games, allowing the user to join one.
 * Also displays an option to propose a new game.
 */
function GameBroker({username, experience_level, proposed_games, game_change_callback}: GameBrokerProps) {

  const [proposeModalVisible, setProposeModalVisible] = useState(false);

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
      <ProposedGameList 
        current_user={username} 
        experience_level={experience_level}
        games={proposed_games} 
        game_change_callback={game_change_callback}
        state_to_display={GAMESTATE__PROPOSED}
      />
      <Button disabled={playerCommitted()} onClick={()=>setProposeModalVisible(true)}>Propose Game</Button>
      <ProposeModal
        username={username}
        show={proposeModalVisible}
        onHide={()=>setProposeModalVisible(false)}
        propose_game_callback={game_change_callback}
      />
    </div>
  );
}

export default GameBroker;
