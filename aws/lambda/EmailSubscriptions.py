import boto3
import decimal
import json
from boto3.dynamodb.conditions import Key, Attr

'''
Run through all subscriptions and send a message

Copy this file into EmailSubscriptions lambda
'''

def retrieve_email_subscriptions(event):
    '''
    Query dynamo for the list of email addresses to email about an event
    event 'CallOut' or 'NewSchedule'
    :return: list of emails to notify about this event
    :rtype: string[]:
    '''
    dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
    
    table = dynamodb.Table('global')
    attribute = 'On' + event
    response = table.query(
        KeyConditionExpression=Key('PK').eq('Schedule')
        #QueryFilter=Attr(attribute).eq('true') + Attr('Type').eq('EMAIL')
    )
    return [sub['Contact'] for sub in response['Items'] if sub['Type']=='EMAIL' && sub[attribute]]

def send_emails(recipients, subject, message):
    message = create_message("ro.c.anders@gmail.com", "robert.antonucci@gmail.com", "Testing H2HAdventure Notify", "This works!")
    response = send_message(service, 'me', message)
    print("Sent message.  Response = " + json.dumps(response))
    
    
def lambda_handler(event, context):

    retrieve_email_subscriptions()
    return {
        'statusCode': 200,
        'body': json.dumps(response['Items'], cls=DecimalEncoder)
    }
