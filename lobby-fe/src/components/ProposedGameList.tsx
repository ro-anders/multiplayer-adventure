import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import {cloneGameInLobby, GameInLobby, GAMESTATE__PROPOSED, reorder} from '../domain/GameInLobby'
import GameStartingModal from './GameStartingModal'
import SettingsService from '../services/SettingsService';

interface ProposedGameListProps {
  /** The name of the currently logged in user */
  current_user: string;

  /** The experience level of the current user */
  experience_level: number;

  /** The lobby's list of games to display */
  games: GameInLobby[];

  /** A callback to call if a game is modified */
  game_change_callback: (updated_game: GameInLobby, original_version: GameInLobby | null) => Promise<boolean>;

  /** Whether this list is for displaying proposed games or running games */
  state_to_display: number; 

  /** Whether user is allowed to join or leave games. */
  actions_disabled: boolean;
}

/**
 * Displays a list of games.
 * This is used both to display a list of proposed games that allows the user
 * to join, start or leave a game, and also display a non-interactive list of running games.
 */
function ProposedGameList({current_user, 
    experience_level, 
    games, 
    game_change_callback, 
    state_to_display,
    actions_disabled}: ProposedGameListProps) {

  /** Whether the modal is shown and, if it is, what it is waiting for to dismiss */
  const [showStartModal, setShowStartModal] = useState<boolean>(false);

  /** Filter the list to only the games we want to display */
  const games_to_display = games.filter((game: GameInLobby)=>{return game.state===state_to_display})

  const list_title = (state_to_display === GAMESTATE__PROPOSED ? "Join a Game" : "Running Games")

  /**
   * User has just pressed "Join" on a game.
   * @param game which game they have joined
   */
  function joinGame(game: GameInLobby) {
    // Figure out the next open slot in the game and add the user
    // to that slot.
    if (game.display_names.length < game.number_players) {
      const original_version = cloneGameInLobby(game)
      game.display_names.push(current_user)
      game.player_names = reorder(game.display_names, game.order)
      game_change_callback(game, original_version)
    }
  }

  /**
   * User has just pressed "Leave" on a game.
   * @param game which game they are leaving
   */
  function quitGame(game: GameInLobby) {
    // Remove the player from the game
    const original_version = cloneGameInLobby(game)
    game.display_names = game.display_names.filter((name: string) => name !== current_user)
    game.player_names = reorder(game.display_names, game.order)
    game_change_callback(game, original_version)
  }

  /**
   * User has just pressed "Start" on a game.
   * @param game Which game the have started
   */
  async function startGamePressed(game: GameInLobby) {
    // If this is a 2/3 person game and only 2 have joined, lock it at 2 at this point.
    if (game.number_players == 2.5) {
      const original_version = cloneGameInLobby(game)
      game.number_players = game.display_names.length
      const success = await game_change_callback(game, original_version)
      if (!success) {
        return
      }
    }
    setShowStartModal(true)
  }

  /**
   * A short description of the game including its number and whose joined so far
   * @param game a proposed game in question
   * @returns a description of the game
   */
  function gameLabel(game: GameInLobby): string {
    var playerList = (game.display_names.length < 1 ? "?" : game.display_names[0])
    playerList += (game.display_names.length < 2 ? ", ?" : `, ${game.display_names[1]}`)
    if (game.number_players > 2) {
      playerList += (game.display_names.length < 3 ? (game.number_players < 3 ? ", ..." : ", ?")  : `, ${game.display_names[2]}`)
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
    const inGame = game.display_names.includes(current_user);
    return inGame && (game.display_names.length < game.number_players);
  }

  /**
   * Return whether the current user can leave the game (meaning the player is currently
   * enrolled in the game and it isn't ready to start)
   * @param game a proposed game in question
   * @returns whether the current user can leave the game
   */
  function isJoinable(game: GameInLobby): boolean {
    const inGame = game.display_names.includes(current_user);
    return !inGame && (game.display_names.length < game.number_players);
  }

  /**
   * Return whether the current user can start this game (meaning they're in the game and it has
   * enough players)
   * @param game a proposed game in question
   * @returns whether the current user can start the game
   */
  function isStartable(game: GameInLobby): boolean {
    const inGame = game.display_names.includes(current_user);
    return inGame && (game.display_names.length+1 > game.number_players);
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
                    onClick={() => startGamePressed(game)}>Start</Button>}
                {isStartable(game) && showStartModal &&
                  <GameStartingModal 
                    game={game} 
                    slot={ game.player_names.indexOf(current_user)} 
                    experience_level={experience_level}/>}
              </div>
              <div>{gameLabel(game)}</div>
            </ListGroup.Item>
            ))}
        </ListGroup>
      </header>
    </div>
  );
}

export default ProposedGameList;
