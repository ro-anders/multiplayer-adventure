import boto3
import json
import random
import time

'''
Called by running games simply stores the current time in a record.
Then someone knows when the last running game was.

Copy this file into GamePing lambda
'''
def lambda_handler(event, context):
    
    try:    
        dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
        table = dynamodb.Table('global')
        item = {
                'PK': 'Ping',
                'SK': 'Ping',
                'Time': int(time.time())
        }
        response = table.put_item(Item=item)
    
        return {
            'statusCode': 200,
            'body': 'Ping succeeded'
        }
    except Exception as e:
        message = 'PutItem failed: {}'.format(e)
        print("Hit exception.  Returning: " + message)
        return {
            'statusCode': 500,
            'body': message
        }
