import { DynamoDBDocumentClient, GetCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Get the info on a single game, either proposed or running
 */
export const getGameBySessionHandler = async (event) => {
    if (event.httpMethod !== 'GET') {
        throw new Error(`getGame only accepts GET method, you tried: ${event.httpMethod}`);
    }
    await CheckDDB();

    /** Game is identified by session which is passed on path */
    const session = parseInt(event.pathParameters.session);
    console.log(`session = "${session}" of type ${typeof session}`)
    // get all items from the table (only first 1MB data, but we shouldn't have that much game data)
    var params = {
        TableName : "Games"+process.env.ENVIRONMENT_TYPE,
        Key: { session: session },
      };
    
    try {
        const data = await ddbDocClient.send(new GetCommand(params));
        var item = ( data.Item ? data.Item : null)
    } catch (err) {
        console.log("Error", err);
    }

    const response = (item == null ? 
        {
            statusCode: 400,
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

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
}
