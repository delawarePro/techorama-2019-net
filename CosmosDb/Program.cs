using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.DataGeneration;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb
{
    class Program
    {
        private static string CosmosUri { get; set; }
        private static string CosmosKey { get; set; }

        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            CosmosUri = config["cosmosUri"];
            CosmosKey = config["cosmosKey"];
            const string dbName = "Techorama";
            const string collectionName = "2019";

            var client = CreateDocumentClient();
            var collection = await RecreateCollection(client, dbName, collectionName);
            var bulkExecutor = await CreateBulkExecutor(collection);

            await WriteProducts(bulkExecutor, 5000, 25);
        }

        private static async Task WriteProducts(IBulkExecutor bulkExecutor, int nofProducts, int nofProperties)
        {
            Console.WriteLine("Writing products...");
            var sw = Stopwatch.StartNew();
            {
                await bulkExecutor.BulkImportAsync(
                    ProductGenerator.GenerateProducts(nofProducts, nofProperties)
                    .Select(x => JObject.FromObject(x, Serializer)),
                    enableUpsert: true);
            }
            sw.Stop();
            Console.WriteLine($"Written {nofProducts} products with {nofProperties} properties in: {sw.Elapsed}");
            var rps = (nofProducts + nofProducts * nofProperties) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"That is {rps:0.##} records/s");
        }

        private static DocumentClient CreateDocumentClient()
        {
            return new DocumentClient(new Uri(CosmosUri), CosmosKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });
        }

        private static async Task<IBulkExecutor> CreateBulkExecutor(DocumentCollection collection)
        {
            var client = CreateDocumentClient();
            var bulkExectuor = new BulkExecutor(client, collection);

            // Set retries to 0 to pass complete control to bulk executor.
            client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
            client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

            await bulkExectuor.InitializeAsync();

            return bulkExectuor;
        }

        private static async Task<DocumentCollection> RecreateCollection(IDocumentClient client, string db, string collection)
        {
            try
            {
                await client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(db, collection));
            }
            catch
            {
                // Not found throws.
            }
            return await client.CreateDocumentCollectionAsync(
                UriFactory.CreateDatabaseUri(db),
                new DocumentCollection
                {
                    Id = collection,
                    PartitionKey =
                    {
                        Paths =
                        {
                            "/partition"
                        }
                    }
                });
        }
    }
}