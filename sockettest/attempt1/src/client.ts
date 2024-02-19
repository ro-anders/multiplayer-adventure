import net from 'net'

var config = {
    host: '127.0.0.1',
    port: 28274
};

var move = {
    forward: 'READER_FWD',
    backward: 'READER_BWD'
};

var client = new net.Socket();

client.connect({
        host: config.host,
        port: config.port
    }, function () {
        console.log('connected to ' + config.host + ':' + config.port);
        client.write(move.forward, function () {
            console.log('move forward command sent');
        });
});

client.on('data', function (data) {
    var str = data.toString();
    if (str === 'ACK') {
        console.log('ACK received');
        client.write(move.backward, function () {
            console.log('move backward sent');
            client.end();
        });
    }
});

client.on('error', function (err) {
    console.log('Error : ', err);
});

client.on('close', function () {
    console.log('socket closed');
});

// const port = 28274

// const client = net.createConnection(port, '127.0.0.1', () => {
// 	console.log('Succesfully connected with server');
// })

// client.on('data', function(data) {
//     if (data.toString() == 'sined') {
//         console.log('Received initiation sequence.  Acknowledging.')
//         client.write('ACK sined')
//     } 
//     else if (data.toString() == 'sealed') {
//         console.log('Recevied second message.  Acknowledging')
//         client.write('ACK sealed')
//     }
//     else if (data.toString() == 'delivered') {
//         console.log('Sequence complete!')
//     }
//     else {
//         console.log(`Received unexpected data "${data}".  Aborting.`)
//         client.end()
//     }
// });

// client.on('close', function() {
// 	console.log('Connection closed by server');
// });

// client.on('error', (error) => {
//     console.log(`Encountered error "${error}".  Aborting.`)
//     if (!client.closed) {
//         client.end()
//     }
// })
