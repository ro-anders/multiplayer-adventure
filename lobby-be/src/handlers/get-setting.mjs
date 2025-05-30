// Create clients and set shared const values outside of the handler.

// Create a DocumentClient that represents the query to add an item
import { DynamoDBDocumentClient, GetCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * A simple example includes a HTTP get method to get one item by id from a DynamoDB table.
 */
export const getSettingByNameHandler = async (event) => {
  if (event.httpMethod !== 'GET') {
    throw new Error(`getMethod only accept GET method, you tried: ${event.httpMethod}`);
  }
  // All log statements are written to CloudWatch
  await CheckDDB();
 
  // Get setting_name from pathParameters from APIGateway because of `/{setting_name}` at template.yaml
  const setting_name = event.pathParameters.setting_name;
 
  // Get the item from the table
  // https://docs.aws.amazon.com/AWSJavaScriptSDK/latest/AWS/DynamoDB/DocumentClient.html#get-property
  var params = {
    TableName : "Settings"+process.env.ENVIRONMENT_TYPE,
    Key: { setting_name: setting_name },
  };

  try {
    const data = await ddbDocClient.send(new GetCommand(params));
    var item = ( data.Item ? data.Item : "" )
  } catch (err) {
    console.log("Error", err);
  }
 
  const response = {
    statusCode: 200,
    headers: {
      "Access-Control-Allow-Headers" : "Content-Type",
      "Access-Control-Allow-Origin": "*", // Allow from anywhere 
      "Access-Control-Allow-Methods": "PUT" // Allow only GET request 
    },
    body: JSON.stringify(item)
  };
 
  // All log statements are written to CloudWatch
  console.info(`response from: ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
  return response;
}
