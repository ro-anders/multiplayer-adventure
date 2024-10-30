import React, { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import {GameInLobby} from '../domain/GameInLobby'
import GameStartingModal from './GameStartingModal'
import GameService from '../services/GameService'
import SettingsService from '../services/SettingsService';

interface ProposedGameListProps {
  /** The name of the currently logged in user */
  current_user: string;
  games: GameInLobby[];
  game_change_callback: (games:GameInLobby[]) => void;
}

const MODAL_HIDDEN='no game'
const MODAL_WAITING_ON_SERVER='server starting'
const MODAL_WAITING_MINIMUM_TIME='waiting'
const MODAL_MINIMUM_TIME=10000; // Milliseconds

/**
 * Displays a list of proposed games and allows the user
 * to join, start or leave a game.
 */
function ProposedGameList({current_user, games, game_change_callback}: ProposedGameListProps) {

  /** The list of players (in player order) in the game that is starting */
  const [startingGamePlayers, setStartingGamePlayers] = useState<string[]>([]);

  /** Whether the modal is shown and, if it is, what it is waiting for to dismiss */
  const [startGameModal, setStartGameModal] = useState<string>(MODAL_HIDDEN);

  /**
   * User has just pressed "Join" on a game.
   * @param game which game they have joined
   */
  function joinGame(game: GameInLobby) {
    // Figure out the next open slot in the game and add the user
    // to that slot.
    if (game.player_names.length < game.number_players) {
      game.player_names.push(current_user)
      GameService.updateGame(game)
      game_change_callback(games)
    }
  }

  /**
   * User has just pressed "Start" on a game.
   * @param game Which game the have started
   */
  async function startGame(game: GameInLobby) {
    // TODO: Randomize the order of players
    const slot = game.player_names.indexOf(current_user);
    setStartingGamePlayers(game.player_names);
    // Popup the modal but leave it up for at least 10 seconds
    setStartGameModal(MODAL_WAITING_ON_SERVER);
    const start_time = Date.now();
    const game_server_ip = await SettingsService.getGameServerIP();
    // TODO: Tell the game server not to shutdown
    const elapsed_time = Date.now()-start_time;
    if (elapsed_time < MODAL_MINIMUM_TIME) {
      setStartGameModal(MODAL_WAITING_MINIMUM_TIME)
      await new Promise((resolve) => setTimeout(resolve, MODAL_MINIMUM_TIME - elapsed_time));
    }
    // Bring down the modal
    setStartGameModal(MODAL_HIDDEN)
    window.open(`${process.env.REACT_APP_MPLAYER_GAME_URL}/index.html?gamecode=${game.session}&slot=${slot}&host=${game_server_ip}`)
  }

  /**
   * A short description of the game including its number and whose joined so far
   * @param game a proposed game in question
   * @returns a description of the game
   */
  function gameLabel(game: GameInLobby): string {
    var playerList = (game.player_names.length < 1 ? "?" : game.player_names[0])
    playerList += (game.player_names.length < 2 ? ", ?" : `, ${game.player_names[1]}`)
    if (game.number_players > 2) {
      playerList += (game.player_names.length < 3 ? ", ?" : `, ${game.player_names[2]}`)
    }

    return `Game #${game.game_number+1}: ${playerList}`
  } 

  /**
   * Return whether the current user can join the game (meaning there is room in the game and
   * they aren't already joined.)
   * @param game a proposed game in question
   * @returns whether the current user can join the game
   */
  function isJoinable(game: GameInLobby): boolean {
    const inGame = game.player_names.includes(current_user);
    return !inGame && (game.player_names.length < game.number_players);
  }

  /**
   * Return whether the current user can start this game (meaning they're in the game and it has
   * enough players)
   * @param game a proposed game in question
   * @returns whether the current user can start the game
   */
  function isStartable(game: GameInLobby): boolean {
    const inGame = game.player_names.includes(current_user);
    return inGame && (game.player_names.length+1 > game.number_players);
  }
  
  return (

    <div className="Roster">
      <h2>Join a proposed game</h2>
      <header className="Roster-header">
        <ListGroup>
            {games.map((game) => (
            <ListGroup.Item key={game.session}>
              {gameLabel(game)} 
              {isJoinable(game) && <Button onClick={() => joinGame(game)}>Join</Button>}
              {isStartable(game) && <Button onClick={() => startGame(game)}>Start</Button>}
            </ListGroup.Item>
            ))}
        </ListGroup>
        {(startGameModal != MODAL_HIDDEN) && 
          <GameStartingModal player_list={startingGamePlayers} waiting_on_server={startGameModal==MODAL_WAITING_ON_SERVER}/>
        }
      </header>
    </div>
  );
}

export default ProposedGameList;
