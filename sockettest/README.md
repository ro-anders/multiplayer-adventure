Proof of concept of javascript running in browser opening a socket to a node application running on a server.

Attempt 1:
Vanilla node applications, client and server, connect and execute handshake.
Successfully run standalone but can't use vanilla sockets in web app.  Must use websockets.

Attempt 2:
React client running in browser talking via websockets to Express server.
Successfully ran.  

Attempt 3:
Unity client running in browser talking to Unity server.  
Succesfully ran, but used UnityTransport which supports web sockets, but requires a propriatary and possibly undocument protocol to actually pass messages.  Choices are to either run a headless Unity Transport on the server as the server or use another option.

Attempt 4:
Unity client running in browser explicitly using web sockets to talk to node server.
Succesfully ran.

Attempt 5:
Node server running in Fargate with multiple Unity clients connecting and communicating.
