import { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

export const createGameServerHandler = async (event) => {
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    if (process.env.ENVIRONMENT_TYPE !== 'development') {
        // TODO
        // Inside a Dynamo transaction query the settings table and make sure the game server setting
        // is empty, then set it to "starting".
        // Then issue the AWS commands to spawn a server in a fargate task
    }

    const response = {
        statusCode: 200,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "PUT" // Allow only GET request 
        }
    };

    return response;
};
