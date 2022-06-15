import boto3
import json
import random
import time

'''
Mostly just insert or update in Dynamo an email/text to be notified, but also
does some business logic.  If the Unsubscribe option is set, will delete 
instead of update and if the existing table has blacklist set, won't do anything

Copy this file into UpsertSchedule lambda
'''
def lambda_handler(event, context):
    
    try:
        print("event = " + json.dumps(event))
    
        client = boto3.client('dynamodb')
        table = 'global'

        item = {
                'PK': {'S': 'Subscription'},
                'SK': {'S': event['Contact']},
                'Contact': {'S': event['Contact']},
                'Type': {'S': event['Type']},
                'OnCallOut': {'BOOL': event['OnCallOut']},
                'OnNewSchedule': {'BOOL': event['OnNewSchedule']},
                'Blacklist': {'BOOL': event['Blacklist']}
        }
        
        # Need to first see if this user is blacklisted
        key={
            'PK': {
                'S': 'Subscription'
                },
            'SK': {
                'S': event['Contact']
               }
            }
        response = client.get_item(TableName=table, Key=key)
        print("check blacklist response = {}".format(json.dumps(response)))
        blacklisted = (
            'Item' in response and 
            response['Item'] and 
            response['Item']['Blacklist'] and
            response['Item']['Blacklist']['BOOL']
        )
        if blacklisted:
            return {
                'statusCode': 200,
                'body': 'Upsert request ignored'
            }

        if event['Unsubscribe']:
            # Delete the entry from the table
            print("Deleting item with key {}".format(json.dumps(key)))
            response = client.delete_item(TableName=table, Key=key)
            return {
                'statusCode': 200,
                'body': 'Delete succeeded'
            }

        else:
            print("Upserting item with key {}".format(json.dumps(key)))
            response = client.put_item(TableName=table, Item=item)
            return {
                'statusCode': 200,
                'body': 'Upsert succeeded'
            }

    except Exception as e:
        message = 'Upsert failed: {}'.format(e)
        print("Hit exception.  Returning: " + message)
        return {
            'statusCode': 500,
            'body': message
        }
