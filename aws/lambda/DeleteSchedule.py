import boto3
import json
import random
import time

'''
Delete a scheduled game object from Dynamo

Copy this file into DeleteSchedule lambda
'''
def lambda_handler(event, context):
    sk = event['SK']
    dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
    table = dynamodb.Table('global')

    response = table.delete_item(
        Key={
            'PK': 'Schedule',
            'SK': sk
        }
    )

    return {
        'statusCode': 200,
        'body': 'DeleteItem succeeded'
    }

