import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import '../App.css';
import ProposedGameList from './ProposedGameList'
import {GameInLobby} from '../domain/GameInLobby'
import GameService from '../services/GameService'
import ProposeGameModal from './ProposeGameModal';

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

  const [proposeGameModalVisible, setProposeGameModalVisible] = useState(false);

  /**
   * Create a proposed game
   */
  function gameProposed(new_game: GameInLobby) {
    proposed_games.push(new_game);
    GameService.proposeNewGame(new_game);
    game_change_callback(proposed_games);
  }

  return (

    <div className="GameBroker">
      {proposeGameModalVisible && 
        <ProposeGameModal 
          username={username} 
          propose_game_callback={gameProposed} 
          close_modal_callback={()=>setProposeGameModalVisible(false)}
        />
      }

      <ProposedGameList current_user={username} games={proposed_games} game_change_callback={game_change_callback}/>
      <Button onClick={()=>setProposeGameModalVisible(true)}>Propose Game</Button>
    </div>
  );
}

export default GameBroker;
