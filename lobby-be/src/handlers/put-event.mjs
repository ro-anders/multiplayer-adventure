import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

export const upsertScheduledEventHandler = async (event) => {
    if (event.httpMethod !== 'PUT') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    // Get body of the request
    const body = JSON.parse(event.body);

    // For right now we just trust that the client is giving us the right structure.
    // Though we do set the partition key
    body.partitionkey="EVENT"    
    var params = {
        TableName: "ScheduledEvents"+process.env.ENVIRONMENT_TYPE,
        Item: body
    };

    try {
        const data = await ddbDocClient.send(new PutCommand(params));
        console.log("Success - event added", data);
      } catch (err) {
        console.log(`Error sending Dynamo PUT ${JSON.stringify(params)}`, err.stack);
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
