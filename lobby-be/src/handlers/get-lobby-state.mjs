import { DynamoDBDocumentClient, ScanCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Get a list of all active people, all active games, and all recent chats
 */
export const getLobbyStateHandler = async (event) => {
    if (event.httpMethod !== 'GET') {
        throw new Error(`getLobbyState only accepts GET method, you tried: ${event.httpMethod}`);
    }

    await CheckDDB();

    // First, get all active players.  Players update their lastactive every minute,
    // so throw out anyone more than two minutes old
    var params = {
        TableName : "Players"
    };
    try {
        const data = await ddbDocClient.send(new ScanCommand(params));
        var items = data.Items;
    } catch (err) {
        console.log("Error", err);
    }
    const too_old = Date.now() - (2 * 60 * 1000)
    const active_players = items.filter((player) => player.lastactive >= too_old)
    const active_player_names = active_players.map((player) => player.playername)
    
    // get all items from the table (only first 1MB data, but we shouldn't have that much game data)
    var params = {
        TableName : "Games"
    };
    try {
        const data = await ddbDocClient.send(new ScanCommand(params));
        var items = data.Items;
    } catch (err) {
        console.log("Error", err);
    }

    const lobby_state = {
        online_player_names: active_player_names,
        games: items
    }

    const response = {
        statusCode: 200,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "GET" // Allow only GET request 
        },
        body: JSON.stringify(lobby_state)
    };

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
}
