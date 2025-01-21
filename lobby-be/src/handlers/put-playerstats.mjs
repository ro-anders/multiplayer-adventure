import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB, ACTIVE_PLAYERS_TTL} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Create or Update a row with info about a player
 */
export const putPlayerStatsHandler = async (event) => {
    if (event.httpMethod !== 'PUT') {
        throw new Error(`putMethod only accepts PUT method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    console.info('received:', event);

    await CheckDDB();

    // Get playername from the URL and the stats  from the body
    const playername = event.pathParameters.playername;
    const body = JSON.parse(event.body);
    if (body.playername != playername) {
        throw new Error(`Invalid request: player name in request URL does not match player name in body.`);
    }

    var params = {
        TableName : "PlayerStats",
        Item: body
    };

    try {
        const data = await ddbDocClient.send(new PutCommand(params));
    } catch (err) {
        console.log("Error", err.stack);
    }

    const response = {
        statusCode: 200,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "PUT" // Allow only PUT request 
        }
    };

    return response;
};
