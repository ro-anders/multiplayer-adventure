import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**Updates or creates a subscription
 */
export const putSubscriptionHandler = async (event) => {
    if (event.httpMethod !== 'PUT') {
        throw new Error(`putMethod only accepts PUT method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    console.info('received:', event);

    await CheckDDB();

    // Get address from the URL and the rest of the game from the body
    const address = event.pathParameters.address;
    const body = JSON.parse(event.body);
    if (body.address != address) {
        throw new Error(`Invalid request: address in request URL does not match address in body.`);
    }

    var params = {
        TableName: "Subscriptions",
        Item: body
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
        },
        body: JSON.stringify(body)
    };

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
};
