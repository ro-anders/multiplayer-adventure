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

def get_creds():
    dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
    table = dynamodb.Table('global')
    response = table.query(
      KeyConditionExpression=Key('PK').eq('GmailToken')
    )
    if 'Items' not in response or len(response['Items']!=1):
      raise ValueError("Could not read GMail Token from database")
    token = response['Items']
    
def send_emails(recipients, subject, message):
    with open('token.pickle', 'rb') as token:
        creds = pickle.load(token)
    if creds and creds.expired:
        raise ValueError("GMail Token expired")
    if not creds: 
        raise ValueError("No GMail Token")
    if not creds.valid:
        raise ValueError("Invalid GMail Token")

    service = build('gmail', 'v1', credentials=creds)
    for recipient in recipients:
        message = create_message("ro.c.anders@gmail.com", "robert.antonucci@gmail.com", subject, message)
        response = send_message(service, 'me', message)
        print("Sent message to {}.  Response = {}".format(recipeint, json.dumps(response))

def create_message(sender, to, subject, message_text):
  """Create a message for an email.

  Args:
    sender: Email address of the sender.
    to: Email address of the receiver.
    subject: The subject of the email message.
    message_text: The text of the email message.

  Returns:
    An object containing a base64url encoded email object.
  """
  message = MIMEText(message_text)
  message['to'] = to
  message['from'] = sender
  message['subject'] = subject
  return {'raw': base64.urlsafe_b64encode(message.as_string())}

def send_message(service, user_id, message):
  """Send an email message.

  Args:
    service: Authorized Gmail API service instance.
    user_id: User's email address. The special value "me"
    can be used to indicate the authenticated user.
    message: Message to be sent.

  Returns:
    Sent Message.
  """
  try:
    message = (service.users().messages().send(userId=user_id, body=message)
               .execute())
    print('Message Id: %s' % message['id'])
    return message
  except HttpError as error:
    print('An error occurred: %s' % error)


    
    
def lambda_handler(event, context):

    recipients = retrieve_email_subscriptions()
    creds = get_creds()

    return {
        'statusCode': 200,
        'body': json.dumps(response['Items'], cls=DecimalEncoder)
    }
