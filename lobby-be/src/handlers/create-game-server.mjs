import { ECSClient, RunTaskCommand } from "@aws-sdk/client-ecs";
import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

export const createGameServerHandler = async (event) => {
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    let status_code = 200
    if (process.env.ENVIRONMENT_TYPE !== 'development') {        
        // Set the game server setting to "starting", but only if the setting doesn't
        // already exist.  If it does, return an error that we can catch.
        const setting_name='game_server_ip'
        const dynamo_params = {
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
            await ddbDocClient.send(new PutCommand(dynamo_params))
            console.log("Transaction completed successfully");

            // Then issue the AWS commands to spawn a server in a fargate task
            // aws ecs run-task \
            // --cluster h2hadv-serverCluster \ 
            // --task-definition arn:aws:ecs:us-east-2:637423607158:task-definition/h2hadv-serverTaskDefinition \
            // --launch-type FARGATE \
            // --network-configuration "awsvpcConfiguration={subnets=[subnet-0d46ce42b6ae7a1ee,subnet-011083badbc3f216e],securityGroups=[sg-07539077994dfb96c],assignPublicIp=ENABLED}" \
            // --overrides '{ "containerOverrides": [ { "name": "h2hadv-server", "environment": [ { "name": "LOBBY_URL", "value": "https://z2rtswo351.execute-api.us-east-2.amazonaws.com/Prod" } ] } ] }'
            // Couple of things are hard-coded that we eventually want to make dynamic
            const subnets = ["subnet-0d46ce42b6ae7a1ee","subnet-011083badbc3f216e"]
            const security_group = "sg-07539077994dfb96c"
            const lobby_url = "https://z2rtswo351.execute-api.us-east-2.amazonaws.com/Prod"
            const ecsClient = new ECSClient();
            const ecs_params = {
                cluster: "h2hadv-serverCluster",              
                taskDefinition: "arn:aws:ecs:us-east-2:637423607158:task-definition/h2hadv-serverTaskDefinition",    
                launchType: "FARGATE",
                networkConfiguration: {
                  awsvpcConfiguration: {
                    subnets: subnets,
                    securityGroups: [security_group],      
                    assignPublicIp: "ENABLED",
                  },
                },
                overrides: {
                  containerOverrides: [
                    {
                      name: "h2hadv-server",               
                      environment: [
                        { name: "LOBBY_URL", value: lobby_url },
                      ],
                    },
                  ],
                },
                count: 1,
              };
              const command = new RunTaskCommand(ecs_params);
              const response = await ecsClient.send(command);
              console.log("Task started:", response.tasks[0].taskArn);
                    
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
