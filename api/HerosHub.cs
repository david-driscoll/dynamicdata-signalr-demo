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

            var changes = new List<Change<Hero, string>>();
            var iterator = container.GetItemLinqQueryable<Hero>().ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                var resultSet = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in resultSet)
                {
                    changes.Add(new Change<Hero, string>(ChangeReason.Add, item.Id, item));
                }
            }

            await Clients.Client(invocationContext.ConnectionId).SendAsync("heroesStream", new ChangeSet<Hero, string>(changes), cancellationToken);
            logger.LogInformation($"{invocationContext.ConnectionId} has connected");
        }

        // [FunctionName(nameof(HerosHub) + nameof(Heros))]
        // public ChannelReader<IChangeSet<Hero, Guid>> Heros([SignalRTrigger] InvocationContext invocationContext, CancellationToken cancellationToken)
        // {

        //     var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
        //     container.GetItemLinqQueryable<Hero>()
        //     return PersonData.Instance.GetPeople().ConnectToChannel(cancellationToken, TaskPoolScheduler.Default);
        // }

        [FunctionName(nameof(Broadcast))]
        public async Task Broadcast([SignalRTrigger] InvocationContext invocationContext, string message, ILogger logger)
        {
            // await Clients.All.SendAsync(NewMessageTarget, new NewMessage(invocationContext, message));
            logger.LogInformation($"{invocationContext.ConnectionId} broadcast {message}");
        }

        [FunctionName(nameof(OnDisconnected))]
        public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext)
        {
        }
    }

    // public class OtherHub : ServerlessHub
    // {
    //     [FunctionName(nameof(OtherHub) + "negotiate")]
    //     public SignalRConnectionInfo Negotiate([HttpTrigger(AuthorizationLevel.Anonymous, Route = "other/negotiate")] HttpRequest req)
    //     {
    //         return Negotiate();
    //     }

    //     [FunctionName(nameof(OtherHub) + nameof(OnConnected))]
    //     public async Task OnConnected([SignalRTrigger] InvocationContext invocationContext, ILogger logger)
    //     {
    //         // await Clients.All.SendAsync(NewConnectionTarget, new NewConnection(invocationContext.ConnectionId));
    //         logger.LogInformation($"{invocationContext.ConnectionId} has connected");
    //     }

    //     [FunctionName(nameof(OtherHub) + nameof(Broadcast))]
    //     public async Task Broadcast([SignalRTrigger] InvocationContext invocationContext, string message, ILogger logger)
    //     {
    //         // await Clients.All.SendAsync(NewMessageTarget, new NewMessage(invocationContext, message));
    //         logger.LogInformation($"{invocationContext.ConnectionId} broadcast {message}");
    //     }

    //     [FunctionName(nameof(OtherHub) + nameof(OnDisconnected))]
    //     public void OnDisconnected([SignalRTrigger] InvocationContext invocationContext)
    //     {
    //     }
    // }
}
