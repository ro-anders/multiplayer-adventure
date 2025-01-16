import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import {GameInLobby, GAMESTATE__PROPOSED} from '../domain/GameInLobby'
import GameStartingModal from './GameStartingModal'
import GameService from '../services/GameService'
import SettingsService from '../services/SettingsService';

interface ProposedGameListProps {
  /** The name of the currently logged in user */
  current_user: string;

  /** The experience level of the current user */
  experience_level: number;

  /** The lobby's list of games to display */
  games: GameInLobby[];

  /** A callback to call if a game is modified */
  game_change_callback: (games:GameInLobby) => void;

  /** Whether this list is for displaying proposed games or running games */
  state_to_display: number; 

  /** Whether user is allowed to join or leave games. */
  actions_disabled: boolean;
}

const MODAL_HIDDEN='no game'
const MODAL_WAITING_ON_SERVER='server starting'
const MODAL_WAITING_MINIMUM_TIME='waiting'
const MODAL_MINIMUM_TIME=10000; // Milliseconds

/**
 * Displays a list of games.
 * This is used both to display a list of proposed games that allows the user
 * to join, start or leave a game, and also display a non-interactive list of running games.
 */
function ProposedGameList({current_user, 
    experience_level, 
    games: games, 
    game_change_callback, 
    state_to_display,
    actions_disabled}: ProposedGameListProps) {

  /** Whether the modal is shown and, if it is, what it is waiting for to dismiss */
  const [startGameModal, setStartGameModal] = useState<string>(MODAL_HIDDEN);

  /** Whether to display buttons that allow you to manipulate the state of games */
  const interactive: boolean = state_to_display == GAMESTATE__PROPOSED;

  /** Filter the list to only the games we want to display */
  const games_to_display = games.filter((game: GameInLobby)=>{return game.state==state_to_display})

  const list_title = (state_to_display == GAMESTATE__PROPOSED ? "Join a Game" : "Running Games")

  /**
   * User has just pressed "Join" on a game.
   * @param game which game they have joined
   */
  function joinGame(game: GameInLobby) {
    // Figure out the next open slot in the game and add the user
    // to that slot.
    if (game.player_names.length < game.number_players) {
      game.player_names.push(current_user)
      game_change_callback(game)
    }
  }

  /**
   * User has just pressed "Leave" on a game.
   * @param game which game they are leaving
   */
  function quitGame(game: GameInLobby) {
    // Remove the player from the game
    game.player_names = game.player_names.filter((name: string) => name != current_user)
    game_change_callback(game)
  }

  /**
   * User has just pressed "Start" on a game.
   * @param game Which game the have started
   */
  async function startGame(game: GameInLobby) {
    // TODO: Randomize the order of players
    const slot = game.player_names.indexOf(current_user);
    const code =
      // highest bits hold the session
      16 * game.session +
      // Next two bits hold the slot number
      4 * game.player_names.indexOf(current_user) +
      // next bit holds the help popups flag
      2 * (experience_level == 1 ? 1 : 0) +
      // last bit holds the maze guide flag
      (experience_level <= 2 ? 1 : 0);
    setStartGameModal(MODAL_WAITING_ON_SERVER);
    const game_server_ip = await SettingsService.getGameServerIP();
    // Bring down the modal
    setStartGameModal(MODAL_HIDDEN)
    window.open(`${process.env.REACT_APP_MPLAYER_GAME_URL}/index.html?gamecode=${code}&host=${game_server_ip}`, '_self')
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
      playerList += (game.player_names.length < 3 ? (game.number_players < 3 ? ", ..." : ", ?")  : `, ${game.player_names[2]}`)
    }

    return playerList
  } 

  /**
   * Return whether the current user can join the game (meaning there is room in the game and
   * they aren't already joined.)
   * @param game a proposed game in question
   * @returns whether the current user can join the game
   */
  function isQuitable(game: GameInLobby): boolean {
    const inGame = game.player_names.includes(current_user);
    return inGame && (game.player_names.length < game.number_players);
  }

  /**
   * Return whether the current user can leave the game (meaning the player is currently
   * enrolled in the game and it isn't ready to start)
   * @param game a proposed game in question
   * @returns whether the current user can leave the game
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

    <div className="pgame-list">
      <h3>{list_title}</h3>
      <header>
        <ListGroup>
            {games_to_display.map((game) => (
            <ListGroup.Item className="lobby-game" key={game.session}>
              <div>
                Game #{game.game_number+1}:
                {game.fast_dragons && 
                  <img src="dragon_head.png" 
                    alt="fast dragons" 
                    className="lobby-game-attribute-icon" 
                  />}
                {game.fearful_dragons && 
                  <img src="sword.png" 
                    alt="scary sword" 
                    className="lobby-game-attribute-icon" 
                  />}
                {isJoinable(game) && 
                  <Button size="sm" className='lobby-game-action' 
                    disabled={actions_disabled} 
                    onClick={() => joinGame(game)}>Join</Button>}
                {isQuitable(game) && 
                  <Button size="sm" className='lobby-game-action' 
                    disabled={actions_disabled} 
                    onClick={() => quitGame(game)}>Leave</Button>}
                {isStartable(game) && 
                  <Button size="sm" className='lobby-game-action' 
                    disabled={actions_disabled} 
                    onClick={() => startGame(game)}>Start</Button>}
              </div>
              <div>{gameLabel(game)}</div>
            </ListGroup.Item>
            ))}
        </ListGroup>
        {(startGameModal !== MODAL_HIDDEN) && 
          <GameStartingModal waiting_on_server={startGameModal===MODAL_WAITING_ON_SERVER}/>
        }
      </header>
    </div>
  );
}

export default ProposedGameList;
