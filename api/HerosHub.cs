using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.AspNetCore.SignalR;
using DynamicData;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace api
{
    public class HerosHub : ServerlessHub
    {
        private const int Max_Page_Size = 2;
        private readonly CosmosClient cosmosClient;

        public HerosHub(CosmosClient cosmosClient)
        {
            this.cosmosClient = cosmosClient;
        }

        [FunctionName("negotiate")]
        public SignalRConnectionInfo Negotiate([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
        {
            return Negotiate();
        }

        [FunctionName(nameof(OnConnected))]
        public async Task OnConnected([SignalRTrigger] InvocationContext invocationContext, ILogger logger, CancellationToken cancellationToken)
        {
            // await Clients.All.SendAsync(NewConnectionTarget, new NewConnection(invocationContext.ConnectionId));
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");

            var iterator = container.GetItemLinqQueryable<Hero>(requestOptions: new QueryRequestOptions() { MaxItemCount = Max_Page_Size }).ToFeedIterator();
            // Dump results to users
            while (iterator.HasMoreResults)
            {
                var changes = new List<Change<Hero, string>>();
                var resultSet = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in resultSet)
                {
                    changes.Add(new Change<Hero, string>(ChangeReason.Add, item.Id, item));
                }
                await Clients.Client(invocationContext.ConnectionId).SendAsync("herosStream", new ChangeSet<Hero, string>(changes), cancellationToken);
            }

            logger.LogInformation($"{invocationContext.ConnectionId} has connected");
        }

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext)
        {
        }
    }
}
