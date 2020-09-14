using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Collections;

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
}
