import { DynamoDBDocumentClient, GetCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Get the info on a single player.  
 * Returns a 404 if player is not found
 */
export const getPlayerStatsByNameHandler = async (event) => {
    if (event.httpMethod !== 'GET') {
        throw new Error(`getPlayerStatsByName only accepts GET method, you tried: ${event.httpMethod}`);
    }
    await CheckDDB();

    /** PlayerStats is identified by playername which is passed on path */
    const playername = event.pathParameters.playername;
    var params = {
        TableName : "PlayerStats",
        Key: { playername: playername },
      };
    
    try {
        const data = await ddbDocClient.send(new GetCommand(params));
        var item = ( data.Item ? data.Item : null)
    } catch (err) {
        console.log("Error", err);
    }

    const response = (item == null ? 
        {
            statusCode: 404,
            headers: {
                "Access-Control-Allow-Headers" : "Content-Type",
                "Access-Control-Allow-Origin": "*", // Allow from anywhere 
                "Access-Control-Allow-Methods": "GET" // Allow only GET request 
            }
        } :
        {
            statusCode: 200,
            headers: {
                "Access-Control-Allow-Headers" : "Content-Type",
                "Access-Control-Allow-Origin": "*", // Allow from anywhere 
                "Access-Control-Allow-Methods": "GET" // Allow only GET request 
            },
            body: JSON.stringify(item)
        })

    return response;
}
