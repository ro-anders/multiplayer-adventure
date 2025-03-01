
import { DeleteCommand, DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * HTTP delete method to delete a subscription in the DynamoDB table.
 */
export const deleteSubscriptionHandler = async (event) => {
    if (event.httpMethod !== 'DELETE') {
        throw new Error(`deleteMethod only accepts DELETE method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    // Get address from the request path
    const address = decodeURIComponent(event.pathParameters.address);
    var params = {
        TableName : "Subscriptions",
        Key: { address: address },
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
