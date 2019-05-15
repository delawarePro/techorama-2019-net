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
            //await DeleteDb(client, dbName);
            //var collection = await EnsureDbAndCollection(client, dbName, collectionName);
            var collection = await GetCollection(client, dbName, collectionName);
            var bulkExecutor = await CreateBulkExecutor(collection);

            await WriteAndReadProducts(bulkExecutor, 5000, 25);
        }

        private static async Task WriteAndReadProducts(IBulkExecutor bulkExecutor, int nofProducts, int nofProperties)
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

            //Console.WriteLine("Reading products...");
            //context.Products.FirstOrDefault();
            //var counter = 0;
            //sw.Restart();
            //{
            //    IList<Product> batch = null;
            //    do
            //    {
            //        batch = context.Products.AsNoTracking().Include(x => x.Properties).OrderBy(x => x.Id).Skip(counter).Take(100).ToList();
            //        counter += batch.Count;
            //    } while (batch.Count == 100);
            //}
            //sw.Stop();
            //Console.WriteLine($"Read {counter} products with {nofProperties} properties in: {sw.Elapsed}");
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

        private static async Task DeleteDb(IDocumentClient client, string db)
        {
            var dbUri = UriFactory.CreateDatabaseUri(db);

            try
            {
                if ((await client.ReadDatabaseAsync(dbUri)) != null)
                    await client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(db));
            }
            catch (DocumentClientException)
            {
                // Read throws when db not present.
            }
        }

        private static async Task<DocumentCollection> EnsureDbAndCollection(IDocumentClient client, string db, string collection)
        {
            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = db });
            return await client.CreateDocumentCollectionIfNotExistsAsync(
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
                },
                new RequestOptions
                {
                    OfferThroughput = 150000
                });
        }

        private static async Task<DocumentCollection> GetCollection(IDocumentClient client, string db, string collection)
        {
            return await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(db, collection));
        }
    }
}