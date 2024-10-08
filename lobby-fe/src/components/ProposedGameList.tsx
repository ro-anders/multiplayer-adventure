import React, { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import {Game} from '../domain/Game'
import GameService from '../services/GameService'
import SettingsService from '../services/SettingsService';

interface ProposedGameListProps {
  /** The name of the currently logged in user */
  current_user: string;
  games: Game[];
  game_change_callback: (games:Game[]) => void;
}


/**
 * Displays a list of proposed games and allows the user
 * to join, start or leave a game.
 */
function ProposedGameList({current_user, games, game_change_callback}: ProposedGameListProps) {

  function joinGame(game: Game) {
    if (!game.player1_name) {
      game.player1_name = current_user;
    } else if (!game.player2_name) {
      game.player2_name = current_user;
    } else {
      game.player3_name = current_user;
    }
    GameService.updateGame(game)
    game_change_callback(games)
  }

  async function startGame(game: Game) {
    const slot = (game.player1_name === current_user ? 0 : (game.player2_name === current_user ? 1 : 2));
    const game_server_ip = await SettingsService.getGameServerIP()
    window.open(`${process.env.REACT_APP_MPLAYER_GAME_URL}/index.html?gamecode=${game.session}&slot=${slot}&host=${game_server_ip}`)
  }

  /**
   * A short description of the game including its number and whose joined so far
   * @param game a proposed game in question
   * @returns a description of the game
   */
  function gameLabel(game: Game): string {
    var playerList = (!game.player1_name ? "?" : game.player1_name)
    playerList += (!game.player2_name ? ", ?" : `, ${game.player2_name}`)
    if (game.number_players > 2) {
      playerList += (!game.player3_name ? ", ?" : `, ${game.player3_name}`)
    }

    return `Game #${game.game_number+1}: ${playerList}`
  } 

  /**
   * Return whether the current user can join the game (meaning there is room in the game and
   * they aren't already joined.)
   * @param game a proposed game in question
   * @returns whether the current user can join the game
   */
  function isJoinable(game: Game): boolean {
    const inGame = game.player1_name === current_user || 
      game.player2_name === current_user || 
      game.player3_name === current_user;
    const currentPlayers = (!game.player1_name ? 0 : (!game.player2_name ? 1 : (!game.player3_name ? 2 : 3)))
    return !inGame && (currentPlayers < game.number_players);
  }

  /**
   * Return whether the current user can start this game (meaning they're in the game and it has
   * enough players)
   * @param game a proposed game in question
   * @returns whether the current user can start the game
   */
    function isStartable(game: Game): boolean {
      const inGame = game.player1_name === current_user || 
        game.player2_name === current_user || 
        game.player3_name === current_user;
      const currentPlayers = (!game.player1_name ? 0 : (!game.player2_name ? 1 : (!game.player3_name ? 2 : 3)))
      return inGame && (currentPlayers+1 > game.number_players);
    }
  
  return (

    <div className="Roster">
      <h2>Join a proposed game</h2>
      <header className="Roster-header">
        <ListGroup>
            {games.map((game) => (
            <ListGroup.Item key={game.player1_name}>
              {gameLabel(game)} 
              {isJoinable(game) && <Button onClick={() => joinGame(game)}>Join</Button>}
              {isStartable(game) && <Button onClick={() => startGame(game)}>Start</Button>}
            </ListGroup.Item>
            ))}
        </ListGroup>
      </header>
    </div>
  );
}

export default ProposedGameList;
