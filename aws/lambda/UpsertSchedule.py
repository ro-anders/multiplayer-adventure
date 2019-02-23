import boto3
import json
import random
import time

'''
Update (or insert if PK/SK pair does not already exist) a scheduled game object 
in Dynamo

Copy this file into UpsertSchedule lambda
'''
def lambda_handler(event, context):
    sk = event['SK']
    host = event['Host']
    starttime = event['Time']
    duration = event['Duration']
    others = event['Others']
    comments = event['Comments']
    dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
    
    table = dynamodb.Table('global')
    if not sk:
        # Generate a sort key for this new scheduled game
        # Mostly sort on start time but throw some things on the end for uniqueness
        sk = "{}-{}-{}".format(starttime, int(time.time()), random.randint(0, 999))
    
    response = table.put_item(
        Item={
            'PK': 'Schedule',
            'SK': sk,
            'Host': host,
            'Time': starttime,
            'Duration': duration,
            'Comments': comments,
            'Others': others
        }
    )

    return {
        'statusCode': 200,
        'body': 'PutItem succeeded:' + json.dumps(response)
    }

