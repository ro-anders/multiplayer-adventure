import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

export const createGameServerHandler = async (event) => {
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    let status_code = 200
    if (process.env.ENVIRONMENT_TYPE !== 'developmentXXX') {        
        // Set the game server setting to "starting", but only if the setting doesn't
        // already exist.  If it does, return an error that we can catch.
        const setting_name='game_server_ip'
        const params = {
            TableName: "Settings",
            Item: {
                setting_name: setting_name,
                setting_value: 'starting',
                time_set: Date.now()
            },
            ConditionExpression: 'attribute_not_exists(setting_name)'
        }
        try {
            // Execute transaction
            await ddbDocClient.send(new PutCommand(params))
            console.log("Transaction completed successfully");
            // Then issue the AWS commands to spawn a server in a fargate task
        } catch (error) {
            if (error.name === "ConditionalCheckFailedException") {
                console.error("Condition check failed: Item already exists.");
                status_code = 302;
            } else {
                console.error("Transaction failed", error);
                status_code = 500;
            }
        }
          
    }

    const response = {
        statusCode: status_code,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "PUT" // Allow only GET request 
        }
    };

    return response;
};
