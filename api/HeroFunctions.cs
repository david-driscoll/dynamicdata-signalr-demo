using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.AspNetCore.SignalR;
using DynamicData;
using System.Linq;
using Microsoft.Azure.Documents;
using System.Net.Http;
using Bogus;
using Bogus.Extensions;

namespace api.Functions
{
    public class HeroFunctions
    {
        private const int Max_Page_Size = 2;
        private readonly CosmosClient cosmosClient;
        private readonly IHttpClientFactory httpClientFactory;
        private static Faker faker = new Faker()
        {
            Random = new Randomizer(1337)
        };

        public HeroFunctions(CosmosClient cosmosClient, IHttpClientFactory httpClientFactory)
        {
            this.cosmosClient = cosmosClient;
            this.httpClientFactory = httpClientFactory;
        }

        [FunctionName(nameof(ListHeros))]
        public async Task<ActionResult<IEnumerable<Hero>>> ListHeros([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, CancellationToken cancellationToken)
        {
            List<Hero> changes = await GetHeros(cancellationToken);

            return new OkObjectResult(changes);
        }

        private async Task<List<Hero>> GetHeros(CancellationToken cancellationToken)
        {
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
            var changes = new List<Hero>();
            var iterator = container.GetItemLinqQueryable<Hero>(requestOptions: new QueryRequestOptions() { MaxItemCount = Max_Page_Size }).ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                var resultSet = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in resultSet)
                {
                    changes.Add(item);
                }
            }

            return changes;
        }

        [FunctionName(nameof(GetApiHero))]
        public async Task<ActionResult<SuperHeroRecord>> GetApiHero([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, CancellationToken cancellationToken)
        {
            var id = req.Query["id"].ToString();
            if (string.IsNullOrWhiteSpace(id))
            {
                id = faker.Random.Number(1, 731).ToString();
            }

            var client = httpClientFactory.CreateClient("SuperHeroApi");
            var result = await client.GetAsync(id);
            var record = await result.Content.ReadAsAsync<SuperHeroRecord>();

            return new OkObjectResult(record);
        }

        [FunctionName(nameof(CreateHero))]
        public async Task<IActionResult> CreateHero([HttpTrigger(AuthorizationLevel.Anonymous, "post")] Hero hero, CancellationToken cancellationToken)
        {
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
            hero.Id = Guid.NewGuid().ToString();
            hero.Created = DateTimeOffset.Now.ToString("u");
            var result = await container.CreateItemAsync(hero, cancellationToken: cancellationToken);

            var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));
            await hub.Clients.All.SendAsync(
                    "herosStream",
                    new ChangeSet<Hero, string>() { new Change<Hero, string>(ChangeReason.Add, result.Resource.Id, result.Resource) },
                    cancellationToken
                );

            return new OkObjectResult(result.Resource);
        }

        [FunctionName(nameof(UpdateHero))]
        public async Task<IActionResult> UpdateHero([HttpTrigger(AuthorizationLevel.Anonymous, "post")] Hero hero, CancellationToken cancellationToken)
        {
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
            hero.Updated = DateTimeOffset.Now.ToString("u");


            var originalHero = (await container.GetItemLinqQueryable<Hero>(requestOptions: new QueryRequestOptions() { MaxItemCount = Max_Page_Size })
            .Where(z => z.Id == hero.Id)
            .ToFeedIterator()
            .ReadNextAsync(cancellationToken))?.SingleOrDefault();

            var result = await container.UpsertItemAsync(hero, cancellationToken: cancellationToken);

            var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));
            await hub.Clients.All.SendAsync(
                    "herosStream",
                    new ChangeSet<Hero, string>() { new Change<Hero, string>(ChangeReason.Update, result.Resource.Id, result.Resource, originalHero) },
                    cancellationToken
                );

            return new OkObjectResult(result.Resource);
        }

        [FunctionName(nameof(DeleteHero))]
        public async Task<IActionResult> DeleteHero(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequest req,
            CancellationToken cancellationToken)
        {
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
            var id = req.Query["id"].ToString();
            var partition = req.Query["partition"].ToString();
            await container.DeleteItemAsync<Hero>(id, new Microsoft.Azure.Cosmos.PartitionKey(null), cancellationToken: cancellationToken);

            var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));
            await hub.Clients.All.SendAsync("herosStream", new ChangeSet<Hero, string>() { new Change<Hero, string>(ChangeReason.Remove, id, null) }, cancellationToken);
            return new OkResult();
        }

        [FunctionName(nameof(RandomHeros))]
        public async Task<ActionResult<IEnumerable<Hero>>> RandomHeros([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, CancellationToken cancellationToken)
        {
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
            var count = req.Query["count"];
            var client = httpClientFactory.CreateClient("SuperHeroApi");
            var records = new List<Hero>();
            var heros = await GetHeros(cancellationToken);
            foreach (var id in Enumerable.Range(0, int.Parse(count)).Select(_ => faker.Random.Number(1, 731)))
            {
                Hero parent = null;
                if (heros.Count > 0)
                {
                    parent = faker.PickRandom(heros).OrNull(faker, 0.1f);
                }
                records.Add(await GenerateHero(container, client, id.ToString(), parent?.Id, cancellationToken));
            }

            var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));

            foreach (var items in records.Buffer(Max_Page_Size))
            {
                await hub.Clients.All.SendAsync(
                    "herosStream",
                    new ChangeSet<Hero, string>(items.Select(hero => new Change<Hero, string>(ChangeReason.Add, hero.Id, hero))),
                    cancellationToken
                );
                await Task.Delay(400); // simulate a delay for effect!
            }

            return new OkObjectResult(records);
        }

        [FunctionName(nameof(ImportHeros))]
        public async Task<ActionResult<IEnumerable<Hero>>> ImportHeros([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, CancellationToken cancellationToken)
        {
            var container = cosmosClient.GetDatabase("Heros").GetContainer("Heros");
            var ids = req.Query["id"].SelectMany(z => z.Split(','));

            var heros = await GetHeros(cancellationToken);
            var client = httpClientFactory.CreateClient("SuperHeroApi");
            var records = new List<Hero>();
            foreach (var id in ids)
            {
                Hero parent = null;
                if (heros.Count > 0)
                {
                    parent = faker.PickRandom(heros).OrNull(faker, 0.1f);
                }
                records.Add(await GenerateHero(container, client, id, parent?.Id, cancellationToken));
            }

            var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));
            await hub.Clients.All.SendAsync(
                    "herosStream",
                    new ChangeSet<Hero, string>(
                        records.Select(hero => new Change<Hero, string>(ChangeReason.Add, hero.Id, hero))
                    ),
                    cancellationToken
                );

            return new OkObjectResult(records);
        }

        [FunctionName(nameof(ClearHeros))]
        public async Task<ActionResult> ClearHeros([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, CancellationToken cancellationToken)
        {
            var database = cosmosClient.GetDatabase("Heros");
            var container = database.GetContainer("Heros");
            var iterator = container.GetItemLinqQueryable<Hero>(requestOptions: new QueryRequestOptions() { MaxItemCount = Max_Page_Size }).ToFeedIterator();
            var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));
            while (iterator.HasMoreResults)
            {
                var resultSet = await iterator.ReadNextAsync(cancellationToken);
                var deletions = new List<Hero>();
                foreach (var item in resultSet)
                {
                    deletions.Add(item);
                    await container.DeleteItemAsync<Hero>(item.Id, new Microsoft.Azure.Cosmos.PartitionKey(item.Race), cancellationToken: cancellationToken);
                    await Task.Delay(150);
                }

                await hub.Clients.All.SendAsync("herosStream", new ChangeSet<Hero, string>(deletions.Select(z => new Change<Hero, string>(ChangeReason.Remove, z.Id, z))), cancellationToken);
            }


            return new OkResult();
        }

        private static async Task<Hero> GenerateHero(Container container, HttpClient client, string id, string parentId, CancellationToken cancellationToken)
        {
            var result = await client.GetAsync(id);
            var record = await result.Content.ReadAsAsync<SuperHeroRecord>();

            var groupAffiliation = record.Connections.GroupAffiliation;
            var items = groupAffiliation.Split(new char[] { ';', ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var teams = items
                .Select(z => z.IndexOf('(', StringComparison.OrdinalIgnoreCase) > -1 ? z.Substring(0, z.IndexOf('(', StringComparison.OrdinalIgnoreCase)) : z)
                .Where(z => !z.Contains("formerly", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("former", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("was once", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("of the", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("ally", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("and", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("Captain", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("None", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("Luke", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("Misty", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("Inc", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("leader", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("United States", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("the", StringComparison.OrdinalIgnoreCase))
                .Where(z => !z.Contains("of America", StringComparison.OrdinalIgnoreCase))
                .Select(z => z.Trim())
                .Where(z => z.Length > 3)
                .ToList();

            if (teams.Count == 0)
            {
                teams.Add("Loner");
            }

            var hero = new Hero()
            {
                Id = record.Id.ToString(),
                LeaderId = parentId,
                Alignment = record.Biography.Alignment,
                AvatarUrl = record.Image?.Url.ToString(),
                Created = DateTimeOffset.Now.ToString("u"),
                Gender = record.Appearance.Gender == "null" ? "Unknown" : record.Appearance.Gender,
                Race = record.Appearance.Race == "null" ? "Unknown" : record.Appearance.Race,
                Powerstats = record.Powerstats,
                Name = record.Name,
                RealName = record.Biography.FullName,
                SuperHeroApiId = record.Id,
                Publisher = record.Biography.Publisher,
                Teams = teams
            };
            var created = await container.UpsertItemAsync(hero, cancellationToken: cancellationToken);
            return created.Resource;
        }

        // [FunctionName(nameof(CosmosTrigger))]
        // public async Task CosmosTrigger(
        //     [CosmosDBTrigger(
        //     databaseName: "Heros",
        //     collectionName: "Heros",
        //     ConnectionStringSetting = "CosmosDbConnectionString",
        //     LeaseCollectionName = "heros",
        //     CreateLeaseCollectionIfNotExists = true)]
        //     IReadOnlyList<Document> heros,
        //     CancellationToken cancellationToken)
        // {
        //     if (heros != null && heros.Count > 0)
        //     {
        //         var hub = await StaticServiceHubContextStore.Get().GetAsync(nameof(HerosHub));

        //         await hub.Clients.All.SendAsync(
        //             "herosStream",
        //             new ChangeSet<Hero, string>(
        //                 heros
        //                     .Select(document => JsonConvert.DeserializeObject<Hero>(document.ToString()))
        //                     .Select(hero => hero.Updated.HasValue ? new Change<Hero, string>(ChangeReason.Update, hero.Id, hero, hero) : new Change<Hero, string>(ChangeReason.Add, hero.Id, hero))
        //             ),
        //             cancellationToken
        //         );
        //     }
        // }
    }
}
