import boto3
import json
import time
from boto3.dynamodb.conditions import Key, Attr

'''
Return if the most recent ping was within the last minute

Copy this file into CheckGames lambda
'''

def lambda_handler(event, context):

    dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
    table = dynamodb.Table('global')
    
    response = table.query(
        KeyConditionExpression=Key('PK').eq('Ping')
    )

    found = False;
    if 'Items' in response and len(response['Items'])>0:
        then = response['Items'][0]['Time']
        now = int(time.time())
        found = (now-then) < 70
    response = {
        'Found': found
    }
    return {
        'statusCode': 200,
        'body': json.dumps(response)
    }
