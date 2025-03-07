import nodemailer from 'nodemailer'
import { DynamoDBDocumentClient, ScanCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'


const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Run a database query to get the emails of everyone who wants to be notified
 * @param {str} notification_event_type the type of notification (sendcall or newscheduledevent)
 */
const getSubscribers = async (notification_event_type) => {
    var params = {
        TableName : "Subscriptions"
    };

    const data = await ddbDocClient.send(new ScanCommand(params));
    var items = data.Items;
    // Filter out subscribers who aren't subscribed to this notification type
    const subscribed = items.filter(item => notification_event_type == "sendcall" ? item.on_send_call : item.on_new_event)
    // Pull out the emails
    const emails = subscribed.map(item => item.address)
    return emails
}

/**
 * Generates the email to send to each user
 * @param {str} notification_event_type the type of notification (sendcall or newscheduledevent)
 * @returns a mail object like {
 *  from: "h2h.atari.adventure@gmail.com",
 *  subject: "Testing Mailer",
 *  text: "This is a test email sent from H2H Atari Adventure.",
 * }
 * The mail object needs a "to" entry before it can be sent
 */
const generateEmail = (notification_event_type) => {
    if (notification_event_type === "sendcall") {
        return {
            from: "h2h.atari.adventure@gmail.com",
            subject: "Someone wants to play H2H Atari Adventure",
            text: "You indicated you want to be notified when someone is online and wants to play.",
        }
    } else if (notification_event_type == "newscheduledevent") {
        return {
            from: "h2h.atari.adventure@gmail.com",
            subject: "A new H2H Atari Adventure event has been scheduled",
            text: "You indicated you want to be notified when someone schedules a new event.",
        }
    } else {
        return null
    }
}

/**
 * Sends an email to all the email addresses
 * @param {string[]} emails the email addresses to send this message to
 * @param {*} mail the mail object of the form {
            from: "h2h.atari.adventure@gmail.com",
            subject: "A new H2H Atari Adventure event has been scheduled",
            text: "You indicated you want to be notified when someone schedules a new event.",
        }
        but it needs a "to" field before it can be sent
 */
const emailSubscribers = (emails, mail) => {

  const gmail_password = process.env.GMAIL_PASSWORD
  console.log("Creating transporter");
  const transporter = nodemailer.createTransport({
    service: "Gmail",
    host: "smtp.gmail.com",
    port: 465,
    secure: true,
    auth: {
      user: "h2h.atari.adventure@gmail.com",
      pass: gmail_password,
    },
  });
  
  for (const email of emails) {
    console.log(`Sending email to ${email}`)
    const mail_to_send = {...mail}
    mail_to_send.to = email
    console.log(`Sending email to ${email}`)
    transporter.sendMail(mail_to_send, (error, info) => {
        if (error) {
          console.error("Error sending email: ", error);
        } else {
          console.log("Email sent: ", info.response);
        }
      });
    
  }
}

/**
 * This generates and sends email for everyone who is subscribed to the given event.
 * This does not modify the database.
 * @param {*} event 
 * @returns 
 */
export const createNotificationEventHandler = async (event) => {
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }
    // All log statements are written to CloudWatch
    console.info('received:', event);

    await CheckDDB();

    // Get body of the request
    const body = JSON.parse(event.body);
    console.info('received JSON body:', body);

    // body will be of the form 
    // { 
    //   "notification_event_type": "sendcall",
    // }
    if (!!!body.notification_event_type) {
        throw new Error(`Cannot notify without notification_event_type: ${event.body}`)
    }

    const subscriber_list = await getSubscribers(body.notification_event_type)
    const email = generateEmail(body.notification_event_type)
    if (email) {
        emailSubscribers(subscriber_list, email)
    }

    // We will need the GMail password to send emails
    console.log(`Running in ${process.env.ENVIRONMENT_TYPE} environment`)
    console.log(`Using GMAIL_PASSWORD=${process.env.GMAIL_PASSWORD}`);

    const response = {
        statusCode: 200,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "PUT" // Allow only GET request 
        }
    };

    // All log statements are written to CloudWatch
    console.info(`response from: ${event.httpMethod} ${event.path} statusCode: ${response.statusCode} body: ${response.body}`);
    return response;
};
