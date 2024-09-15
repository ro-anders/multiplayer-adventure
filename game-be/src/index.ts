/**
 * This sets up a server that listens for websocket connections and maintains server-side state.
 * State takes the form of multiple sessions (a client identifies which session it belongs to)
 * and within each session the state is a color.
 * Clients can query the color or increment the color with websocket messages.
 * 
 */

//const express = require('express');
import express, { Express, Request, Response } from "express";
import WebSocket from 'ws';

import GameMgr from "./biz/GameMgr";
import ServiceMgr from "./biz/ServiceMgr";

const { createServer } = require('http');


console.log("Stating game back end")
console.log(`Environment = ${process.env.NODE_ENV}`)
console.log(`Lobby URL = ${process.env.LOBBY_URL}`)
const gamemgr: GameMgr = new GameMgr();
const servicemgr: ServiceMgr = new ServiceMgr(process.env.LOBBY_URL);

const app: Express = express();
const port = 4000;

const server = createServer(app);
const server_socket: WebSocket.Server = new WebSocket.Server({ server: server, path: '/ws' });

server_socket.on('connection', (ws: WebSocket) => {
  console.log("client connected.  Waiting for join message.");


  // send "hello world" interval
  //const textInterval = setInterval(() => ws.send("hello world!"), 100);

  ws.on('message', function(data: WebSocket.RawData, isBinary: boolean) {
    if (isBinary) {
      gamemgr.process_message(data as Uint8Array, ws)
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
 * Occassionally the lobby will want to warn the game backend that a game is about to start and not
 * to shutdown due to inactivity.
 */
app.put('/timer/reset', function (req, res) {
  servicemgr.got_game_message()
  res.send("OK")
})

