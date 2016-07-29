using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ConnectMeBot.Models
{
    public static class ContactLookup
    {
        private static readonly string AADInstance = "https://login.windows.net"; //login.microsoftonline.com works too?
        private static readonly string Tenant = "microsoft.com";

        private static AuthenticationContext authContext;
        private static ClientCredential clientCredential;
        public static async Task<string> Lookup(string keyword)
        {
            Lazy<string> authToken = new Lazy<string>(() =>
            {
                var result = authContext.AcquireTokenAsync("https://graph.windows.net", clientCredential).Result;
                return result.AccessToken;
            });

            string authenticationAuthority = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    AADInstance,
                    Tenant);


            authContext = new AuthenticationContext(authenticationAuthority);
            clientCredential = new ClientCredential(Secrets.ClientId, Secrets.AppKey);

            var activeDirectoryClient = new ActiveDirectoryClient(new Uri("https://graph.windows.net/microsoft.com"), () =>
            {
                return Task.Run(() => { return authToken.Value; });
            });
            var objectIds = await FindPotentialGroupsAsync(keyword);

            var users = new List<IDirectoryObject>();
            var tasks = new List<Task<IPagedCollection<IDirectoryObject>>>();
            foreach (var objectId in objectIds)
            {
                tasks.Add(activeDirectoryClient.Groups.GetByObjectId(objectId.ToString()).Members.ExecuteAsync());
            };

            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                users.AddRange(task.Result.CurrentPage.Where(o => o.ObjectType == "User"));
            }

            var user = users.GroupBy(u => u.ObjectId).Select(group => new
            {
                User = group.First(),
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count).First().User;
            return ((User)user).UserPrincipalName;
        }

        private static async Task<IEnumerable<Guid>> FindPotentialGroupsAsync(string keyword)
        {
            var groups = new List<Tuple<Guid, string>>();
            using (var connection = new SqlConnection(Secrets.DatabaseConnectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("SELECT ObjectId, Description FROM Groups WHERE Contains(Description, @Keyword)", connection))
                {
                    command.Parameters.Add("Keyword", SqlDbType.NVarChar);
                    command.Parameters["Keyword"].Value = keyword;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            groups.Add(new Tuple<Guid, string>(reader.GetGuid(0), reader.GetString(1)));
                        }
                    }
                }
            }
            return groups.Where(g => (g.Item2 != null)).Select(g => g.Item1);
        }
    }
}