import { ECSClient, RunTaskCommand } from "@aws-sdk/client-ecs";
import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

export const createGameServerHandler = async (event) => {
    console.info('received:', event);
    console.log(`Calling ${event.httpMethod} ${event.path} while ENVIRONMENT_TYPE=${process.env.ENVIRONMENT_TYPE}` +
      `fullpath=https://${event.requestContext.domainName}/${event.requestContext.path}`
    )
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


            // The full path is probably something like /Prod/setting/game_server_ip and we want the "/Prod" part.
            // So pull the REST path off the end of the full path to get that.
            const path_prefix = ( event.requestContext.path.endsWith(event.path) ?
              event.requestContext.path.slice(0, -event.path.length) : event.requestContext.path);
            const lobby_url = `https://${event.requestContext.domainName}${path_prefix}`
            const accountId = event.requestContext.accountId
            // Then issue the AWS commands to spawn a server in a fargate task
            // aws ecs run-task \
            // --cluster h2hadv-serverCluster \ 
            // --task-definition arn:aws:ecs:us-east-2:637423607158:task-definition/h2hadv-serverTaskDefinition \
            // --launch-type FARGATE \
            // --network-configuration "awsvpcConfiguration={subnets=[subnet-0d46ce42b6ae7a1ee,subnet-011083badbc3f216e],securityGroups=[sg-07539077994dfb96c],assignPublicIp=ENABLED}" \
            // --overrides '{ "containerOverrides": [ { "name": "h2hadv-server", "environment": [ { "name": "LOBBY_URL", "value": "https://g0g3vzs6qf.execute-api.us-east-2.amazonaws.com/Prod" } ] } ] }'
            // Couple of things are hard-coded that we eventually want to make dynamic
            const subnets = ["subnet-0d46ce42b6ae7a1ee","subnet-011083badbc3f216e"]
            const security_group = "sg-07539077994dfb96c"
            const region = `us-east-2`
            const ecsClient = new ECSClient();
            const ecs_params = {
                cluster: "h2hadv-serverCluster",              
                taskDefinition: `arn:aws:ecs:${region}:${accountId}:task-definition/h2hadv-serverTaskDefinition`,    
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
              console.log(`Spawning game server with LOBBY_URL=${lobby_url}`)
              const command = new RunTaskCommand(ecs_params);
              const response = await ecsClient.send(command);
              console.log("Task started:", response.tasks[0].taskArn);
                    
        } catch (error) {
            if (error.name === "ConditionalCheckFailedException") {
                console.error("Aborting.  Game server already in starting or running state.");
                status_code = 302;
            } else {
                console.error("Unexpected error recording game server as starting.", error);
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
