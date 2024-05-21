/**
 * This sets up a server that listens for websocket connections and maintains server-side state.
 * State takes the form of multiple sessions (a client identifies which session it belongs to)
 * and within each session the state is a color.
 * Clients can query the color or increment the color with websocket messages.
 * 
 */

const crypto = require('crypto');
const express = require('express');
const { createServer } = require('http');
const WebSocket = require('ws');

const app = express();
const port = 3000;

const server = createServer(app);
const server_socket = new WebSocket.Server({ server: server, path: '/ws' });
const sessions = {}; // Map of session numbers to an array of client sockets in that session.

// Join and existing session or create a new session if one doesn't exist.
function join_session(session, client_socket) {
  if (!(session in sessions)) {
    sessions[session] = []
  }
  client_sockets = sessions[session]
  client_sockets.push(client_socket)
  console.log("client joined.");
}

// Send the current color to every client in the specified session
function broadcast_message(session, data) {
  for (let socket of sessions[session]) {
    const buffer = data.buffer;
    socket.send(buffer); 
  }
}

server_socket.on('connection', function(ws) {
  console.log("client connected.  Waiting for join message.");


  // send "hello world" interval
  //const textInterval = setInterval(() => ws.send("hello world!"), 100);

  ws.on('message', function(data) {
    if (typeof(data) === "string") {
      console.log("Unexpected string received from client -> \"" + data + "\"");
    } else if (data.length < 2) {
      console.log("Unexpected message too short. " + Array.from(data).join(", ") + "")
    } else {
      console.log("Received message: [" + Array.from(data).join(", ") + "]")
      // The very first byte should indicate the session
      session = data[0]
      // This is either a connection request or a message to be broadcast.
      // The second byte is 0x01 for connection requests and 0x00 for messages
      if (data[1] == 0x01) {
        console.log("Request to join session " + session)
        join_session(session, ws)
      }
      else {
        console.log("Request to broadcast message to " + session)
        broadcast_message(session, data);
      }
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
