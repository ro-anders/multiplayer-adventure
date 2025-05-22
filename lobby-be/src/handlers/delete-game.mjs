
import { DeleteCommand, DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * HTTP delete method to delete a game in the DynamoDB table.
 */
export const deleteGameHandler = async (event) => {
    if (event.httpMethod !== 'DELETE') {
        throw new Error(`deleteMethod only accepts DELETE method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    // Get game session from the request path
    const session = parseInt(event.pathParameters.session);
    var params = {
        TableName : "Games"+process.env.ENVIRONMENT_TYPE,
        Key: { session: session },
    };

    try {
        const data = await ddbDocClient.send(new DeleteCommand(params));
      } catch (err) {
        console.error("Error", err.stack);
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
