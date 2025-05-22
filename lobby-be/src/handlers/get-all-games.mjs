import { DynamoDBDocumentClient, ScanCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Get a list of games, either proposed games or active games
 */
export const getAllGamesHandler = async (event) => {
    if (event.httpMethod !== 'GET') {
        throw new Error(`getGames only accepts GET method, you tried: ${event.httpMethod}`);
    }
    // All log statements are written to CloudWatch
    //console.info('received:', event);

    await CheckDDB();

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

    const response = {
        statusCode: 200,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "GET" // Allow only GET request 
        },
        body: JSON.stringify(items)
    };

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
}
