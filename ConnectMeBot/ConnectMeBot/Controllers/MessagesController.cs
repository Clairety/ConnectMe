﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace ConnectMeBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                QueryInformation queryInfo = GetQueryInformation(activity.Text);

                // calculate something for us to return
                //int length = (activity.Text ?? string.Empty).Length;

                Activity reply = activity.CreateReply($"You sent {activity.Text}, and we found role {queryInfo.Role} and entity {queryInfo.Entity}, and IsWhoQuestion is: {queryInfo.IsWhoQuestion}");

                // return our reply to the user
                //Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        internal QueryInformation GetQueryInformation(string message)
        {
            QueryInformation queryInfo = new QueryInformation();

            string url = "https://api.projectoxford.ai/luis/v1/application?id=a105d502-33b6-4560-b628-6046230ec680&subscription-key=bd33f80fe71f486198fc133d819598a2&q=";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Concat(url, message));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseFromServer;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                responseFromServer = reader.ReadToEnd();
            }

            response.Close();

            JObject json = JObject.Parse(responseFromServer);

            JObject whoData = json["intents"].Values<JObject>()
            .Where(m => m["intent"].Value<string>() == "WhoTech")
            .FirstOrDefault();

            var score = whoData["score"];

            queryInfo.IsWhoQuestion = (double)score > 0.5;

            JToken actionData = whoData["actions"].FirstOrDefault();

            SortedList roleData = new SortedList();
            GetData(roleData, actionData, "Role");

            if (roleData.Count > 0)
            {
                queryInfo.Role = roleData.GetByIndex(0).ToString();
            }
            else
            {
                queryInfo.Role = string.Empty;
            }

            SortedList entityData = new SortedList();
            GetData(entityData, actionData, "Technology");
            GetData(entityData, actionData, "Feature");
            GetData(entityData, actionData, "Product");

            if (entityData.Count > 0)
            {
                queryInfo.Entity = entityData.GetByIndex(0).ToString();
            }
            else
            {
                queryInfo.Entity = string.Empty;
            }

            return queryInfo;
        }

        private static void GetData(SortedList list, JToken actionData, string paramName)
        {
            JObject techData = actionData["parameters"].Values<JObject>()
            .Where(m => m["name"].Value<string>() == paramName)
            .FirstOrDefault();

            if (techData != null)
            {
                foreach (JObject roleInfo in techData["value"])
                {
                    list.Add(1 - ((double)roleInfo["score"]), (string)roleInfo["entity"]);
                }
            }
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}