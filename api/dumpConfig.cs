using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Collections;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using DynamicData;
using System.Threading;
using System.Collections.Generic;
using DynamicData.Kernel;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Company.Function
{
    public static class dumpConfig
    {
        [FunctionName("dumpConfig")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            {
                sb.AppendLine($"{item.Key}: {item.Value}");
            }
            return new OkObjectResult(sb.ToString());
        }
    }

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

            var changes = new List<Change<Hero, Guid>>();
            var iterator = container.GetItemLinqQueryable<Hero>().ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                var resultSet = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in resultSet)
                {
                    changes.Add(new Change<Hero, Guid>(ChangeReason.Add, item.Id, item));
                }
            }

            await Clients.Client(invocationContext.ConnectionId).SendAsync("heroesStream", new ChangeSet<Hero, Guid>(changes), cancellationToken);
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

    public class Hero
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        public string Universe { get; set; }
        public List<string> SuperPowers { get; set; } = new List<string>();
        public string AvatarUrl { get; set; }
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
