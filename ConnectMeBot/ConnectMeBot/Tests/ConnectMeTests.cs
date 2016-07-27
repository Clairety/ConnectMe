using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectMeBot.Tests
{
    public class ConnectMeTests
    {
        public void TestQueryRetriever()
        {
            var messagesController = new MessagesController();

            messagesController.GetQueryInformation("Who owns OSI?");
        }
    }
}