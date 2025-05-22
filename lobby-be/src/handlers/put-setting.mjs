// Create clients and set shared const values outside of the handler.

// Create a DocumentClient that represents the query to add an item
import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * HTTP pput method to update one setting in the DynamoDB table.
 */
export const putSettingHandler = async (event) => {
    if (event.httpMethod !== 'PUT') {
        throw new Error(`putMethod only accepts PUT method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    //console.info('received:', event);

    await CheckDDB();

    // Get setting_name from the request path
    const setting_name = event.pathParameters.setting_name;
    const body = JSON.parse(event.body);
    if (body.setting_name != setting_name) {
        throw new Error(`Invalid request: setting name in request URL does not match name in body.`);
    }
    body.time_set = Date.now()

    var params = {
        TableName : "Settings"+process.env.ENVIRONMENT_TYPE,
        Item: body
    };

    try {
        const data = await ddbDocClient.send(new PutCommand(params));
        console.log("Success - setting added or updated", data);
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
