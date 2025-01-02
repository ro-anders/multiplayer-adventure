import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

export const createChatHandler = async (event) => {
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    console.info('received:', event);

    await CheckDDB();

    // Get body of the request
    const body = JSON.parse(event.body);
    console.info('received JSON body:', body);

    // body will be of the form 
    // { 
    //   "player_name": "jdoe",
    //   "message": "It's on like Red Dawn!"
    // }
    // 
    // We need to add a timestamp, sortkey and code
    if (!!!body.player_name) {
        throw new Error(`Cannot create chat with null player name: ${event.body}`)
    }
    body.timestamp = Date.now()
    body.sortkey = body.timestamp.toString() + body.player_name
    body.partitionkey = "CHAT"

    var params = {
        TableName : "Chat",
        Item: body
    };

    try {
        const data = await ddbDocClient.send(new PutCommand(params));
        console.log("Success - chat added", data);
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
