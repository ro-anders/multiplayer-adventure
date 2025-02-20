import { DynamoDBDocumentClient, QueryCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Get a list of scheduled events that occur in the future
 */
export const getAllScheduledEventsHandler = async (event) => {
    if (event.httpMethod !== 'GET') {
        throw new Error(`getAllEvents only accepts GET method, you tried: ${event.httpMethod}`);
    }
    // All log statements are written to CloudWatch
    //console.info('received:', event);

    await CheckDDB();

     var params = {
         TableName : "ScheduledEvents",
         KeyConditionExpression: 'partitionkey = :pkey AND starttime >= :skey',
         ExpressionAttributeValues: {
             ":pkey": "EVENT",
             ":skey": Date.now(),
           },
         
     };
    
    var items
    try {
        const data = await ddbDocClient.send(new QueryCommand(params));
        items = data.Items;    
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
