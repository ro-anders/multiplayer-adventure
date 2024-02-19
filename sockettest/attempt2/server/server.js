var app = require('express')();
var http = require('http').createServer(app);
var cors = require('cors');
app.use(cors());

const PORT = 7777;
var io = require('socket.io')(http, {
    cors: {
        origin: "http://localhost:3000"
    }
});
const STATIC_CHANNELS = ['global_notifications', 'global_chat'];

http.listen(PORT, () => {
    console.log(`listening on *:${PORT}`);
});


io.on('connection', (socket) => { 
    /* socket object may be used to send specific messages to the new connected client */
    console.log('new client connected');
    socket.emit('connection', null);
});


