﻿namespace ConnectMeBot
{
    internal class QueryInformation
    {
        public bool IsWhoQuestion
        {
            get; set;
        }

        public bool IsHelpQuestion
        {
            get; set;
        }

        public string Entity
        {
            get; set;
        }

        public string Role
        {
            get; set;
        }
    }
}