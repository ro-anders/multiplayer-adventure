import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * 
 * @returns Generate a unique session id
 */
const generateSessionId = async () => {
    // TODO: actually check with existing sessions to make sure it's unique.
    // Needs to fit in a byte, and, just to be safe, in a signed byte. 
    return Math.floor(100 * Math.random())
}

export const createGameHandler = async (event) => {
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    //console.info('received:', event);

    await CheckDDB();

    // Get body of the request
    const body = JSON.parse(event.body);
    body.session = await generateSessionId();

    // For right now we just trust that the client is giving us the right structure.

    // Creates a new game
    var params = {
        TableName : "Games"+process.env.ENVIRONMENT_TYPE,
        Item: body
    };

    try {
        const data = await ddbDocClient.send(new PutCommand(params));
        console.log("Success - game added", data);
      } catch (err) {
        console.log("Error", err.stack);
      }

    const response = {
        statusCode: 200,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "PUT" // Allow only GET request 
        }
    };

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.httpMethod} ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
};
