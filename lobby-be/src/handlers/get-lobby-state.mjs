import { DynamoDBDocumentClient, QueryCommand, ScanCommand } from '@aws-sdk/lib-dynamodb';
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

    const active_player_names = await getPlayerNames();
    const active_games = await getActiveGames();
    const recent_chats = await getRecentChats(event.queryStringParameters?.lastactivity);

    const lobby_state = {
        online_player_names: active_player_names,
        games: active_games,
        recent_chats: recent_chats
    }
    console.log(JSON.stringify(lobby_state))

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

/**
 * Get a list of all active people
 */
const getPlayerNames = async () => {
    // Get all active players.  Players update their lastactive every minute,
    // so throw out anyone more than two minutes old
    var params = {
        TableName : "Players"+process.env.ENVIRONMENT_TYPE
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

    return active_player_names;
}

/**
 * Get a list of all active games
 */
const getActiveGames = async () => {
    
    // get all items from the table (only first 1MB data, but we shouldn't have that much game data)
    var params = {
        TableName : "Games"+process.env.ENVIRONMENT_TYPE
    };
    try {
        const data = await ddbDocClient.send(new ScanCommand(params));
        var items = data.Items;
    } catch (err) {
        console.log("Error", err);
    }

    return items;
}

/**
 * Get a list of chats since a given time.
 */
const getRecentChats = async (since) => {
    console.log("Getting chats")
    if (!!!since) {
        // If no start time is specified, only return the last half hour of chats
        since = (Date.now() - 1800000).toString();
    } else {
        // If a start time is specified, we need to increment it by one millisecond
        // because the search excludes the passed in start time
        since = (parseInt(since)+1).toString()
    }
    var params = {
        TableName : "Chat"+process.env.ENVIRONMENT_TYPE,
        KeyConditionExpression: 'partitionkey = :pkey AND sortkey >= :skey',
        ExpressionAttributeValues: {
            ":pkey": "CHAT",
            ":skey": since,
          },
        
    };
    const data = await ddbDocClient.send(new QueryCommand(params));
    return data.Items;    
}
