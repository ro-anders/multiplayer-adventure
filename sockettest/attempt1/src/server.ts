
import net from 'net'

var port = 28274;
var move = {
    forward: 'READER_FWD',
    backward: 'READER_BWD'
};

var server = net.createServer(function (client) {
    console.log('client connected');

    client.on('end', function () {
        console.log('client disconnected');
    });

    client.on('data', function (data) {
        var str = data.toString();
        if (str === move.forward) {
            console.log('move forward command received');
            client.write('ACK', function () {
                console.log('ACK sent');
            });
        } else if (str === move.backward) {
            console.log('move backward command received: do nothing...');
        } else {
            console.log('unknown received message: ', str);
        }
    });
});

server.listen(port, function () { //'listening' listener
    console.log('server bound on port: ' + port);
});


// const port = 28274
// var server = net.createServer();

// server.on('connection', (socket) => {
//     console.log(`Received connect from ${socket.remoteAddress}:${socket.remotePort}`)

//     socket.on('data', (data) => {
//         if (data.toString() == 'ACK sined') {
//             console.log('Initialization success.  Attempting second message.')
//             socket.write('sealed')
//         }
//         else if (data.toString() == 'ACK sealed') {
//             console.log('Second success.  Terminating sequence.')
//             socket.write('delivered')
//         } else {
//             console.log(`Received unexpected data "${data}".  Aborting sequence.`)
//             socket.end()
//         }
//     })

//     socket.on('end', () => {
//         console.log('Connection closed by client.  Resetting sequence.')
//     })
    
//     socket.on('error', (error) => {
//         console.log(`Encountered error "${error.message}".  Aborting sequence.`)
//         socket.end()
//     })
    
//     console.log('Initiating sequence')
//     socket.write('sined')
// });

// server.listen(port, '127.0.0.1');
// console.log(`Listening on port ${port}`)
