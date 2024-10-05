// Create clients and set shared const values outside of the handler.

// Create a DocumentClient that represents the query to add an item
import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB, ACTIVE_PLAYERS_TTL} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * A simple example includes a HTTP post method to add one item to a DynamoDB table.
 */
export const putPlayerHandler = async (event) => {
    if (event.httpMethod !== 'PUT') {
        throw new Error(`putMethod only accepts PUT method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    //console.info('received:', event);

    await CheckDDB();

    // Get name from the request path
    const name = event.pathParameters.name;

    // Creates a new item, or replaces an old item with a new item
    // https://docs.aws.amazon.com/AWSJavaScriptSDK/latest/AWS/DynamoDB/DocumentClient.html#put-property
    var params = {
        TableName : "Players",
        Item: { playername : name, lastactive: Date.now(), ttl: Date.now() + ACTIVE_PLAYERS_TTL }
    };

    try {
        const data = await ddbDocClient.send(new PutCommand(params));
        console.log("Success - item added or updated", data);
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

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.httpMethod} ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
};
