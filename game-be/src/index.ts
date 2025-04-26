/**
 * This sets up a server that listens for websocket connections and 
 * maintains server-side state.
 * State takes the form of multiple sessions (a client identifies which session 
 * it belongs to).
 * 
 */

//const express = require('express');
import express, { Express, Request, Response } from "express";
import WebSocket from 'ws';

import output from "./Output"
import GameMgr from "./biz/GameMgr";
import ServiceMgr from "./biz/ServiceMgr";
import LobbyBackend from "./biz/LobbyBackend";

const { createServer } = require('http');


output.log("Starting game back end")
output.log(`Environment = ${process.env.NODE_ENV}`)
output.log(`Lobby URL = ${process.env.LOBBY_URL}`)
output.log(`Log level = ${process.env.LOG_LEVEL}`)

// If running in production we need a lobby url.  
// If running locally, we assume the standard localhost lobby port.
const lobby_url: string = (process.env.NODE_ENV === 'development' ? 'http://host.docker.internal:3000' : process.env.LOBBY_URL)
const lobby_backend = new LobbyBackend(lobby_url);
const gamemgr: GameMgr = new GameMgr(lobby_backend);
const servicemgr: ServiceMgr = new ServiceMgr(lobby_backend);

const app: Express = express();
const port = 80;

const server = createServer(app);
const server_socket: WebSocket.Server = new WebSocket.Server({ server: server, path: '/ws' });

/**
 * Produce a string to identify a web socket
 */
const wsToString = (ws: WebSocket) => {
  return (ws as any)._socket?.remoteAddress + ":" + (ws as any)._socket?.remotePort
}

server_socket.on('connection', (ws: WebSocket) => {
  output.debug(`client ${wsToString(ws)} connected.  Waiting for join message.`);

  ws.on('message', function(data: WebSocket.RawData, isBinary: boolean) {
    if (isBinary) {
      gamemgr.process_message(data as Uint8Array, ws)
      servicemgr.got_game_message()
    }
  });

  ws.on('close', function() {
    console.log("client left.");
    // TBD: How do we remove the websocket from the session map?
    //clearInterval(textInterval);
  });

});

server.listen(port, function() {
  console.log(`Listening on http://localhost:${port}`);
});

app.get('/health', (req, res) => {
  res.send("OK")
})

/**
 * Occassionally the lobby will want to warn the game backend that a game is about
 * to start and not to shutdown due to inactivity.
 */
app.put('/timer/reset', function (req, res) {
  servicemgr.got_game_message()
  res.send("OK")
})

