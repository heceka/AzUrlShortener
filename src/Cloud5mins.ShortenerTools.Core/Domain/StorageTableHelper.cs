using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Cloud5mins.ShortenerTools.Core.Domain.Models;

namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class StorageTableHelper
    {
        const string TableUrlsDetails = "UrlsDetails";
        const string TableClickStats = "ClickStats";

        //private readonly string _storageConnectionString;
        private readonly TableServiceClient _tableServiceClient;

        public StorageTableHelper() { }
        public StorageTableHelper(string storageConnectionString)
        {
            _tableServiceClient = new TableServiceClient(storageConnectionString);
        }

        private TableClient GetTableClient(string tableName)
        {
            // Create a new table. The TableItem class stores properties of the created table.
            //TableItem table = _tableServiceClient.CreateTableIfNotExists(tableName);

            // Get a reference to the TableClient from the service client instance.
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            // Create the table if it doesn't exist.
            tableClient.CreateIfNotExists();

            return tableClient;
        }

        private TableClient GetUrlsTable()
        {
            var table = GetTableClient(TableUrlsDetails);
            return table;
        }

        private TableClient GetStatsTable()
        {
            var table = GetTableClient(TableClickStats);
            return table;
        }

        public async Task<ShortUrlEntity> GetShortUrlEntityAsync(ShortUrlEntity row, CancellationToken cancellationToken = default)
        {
            Response<ShortUrlEntity> result = null;
            try
            {
                var tableClient = GetUrlsTable();
                result = await tableClient.GetEntityAsync<ShortUrlEntity>(row.PartitionKey, row.RowKey, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine(ex);
            }
            return result?.Value;
        }

        public async Task<IList<ShortUrlEntity>> GetAllShortUrlsAsync(CancellationToken cancellationToken = default)
        {
            var entities = new List<ShortUrlEntity>();
            var tableClient = GetUrlsTable();

            var pages = tableClient.QueryAsync<ShortUrlEntity>(filter: "RowKey ne \'KEY\'", cancellationToken: cancellationToken);
            //var pages = tableClient.QueryAsync<ShortUrlEntity>(x => x.RowKey != "KEY", cancellationToken: cancellationToken);// Alternative
            await foreach (var page in pages)
            {
                entities.Add(page);
            }
            return entities;
        }

        public async Task<ShortUrlEntity> GetShortUrlEntityByVanityAsync(string vanity, CancellationToken cancellationToken = default)
        {
            var entities = new List<ShortUrlEntity>();
            var tableClient = GetUrlsTable();

            var pages = tableClient.QueryAsync<ShortUrlEntity>(x => x.RowKey == vanity, cancellationToken: cancellationToken);// Alternative           
            await foreach (var page in pages)
            {
                entities.Add(page);
            }
            var shortUrlEntity = entities.FirstOrDefault();
            return shortUrlEntity;
        }

        public async Task SaveClickStatsEntityAsync(ClickStatsEntity newStats, CancellationToken cancellationToken = default)
        {
            var tableClient = GetStatsTable();
            _ = await tableClient.UpsertEntityAsync(newStats, cancellationToken: cancellationToken);
        }

        public async Task<ShortUrlEntity> SaveShortUrlEntityAsync(ShortUrlEntity newShortUrl, CancellationToken cancellationToken = default)
        {
            var tableClient = GetUrlsTable();
            var x = await tableClient.UpsertEntityAsync(newShortUrl, cancellationToken: cancellationToken);
            return newShortUrl;//TODO: hkilic - Calismayabilir!
        }

        public async Task<bool> IfShortUrlEntityExistByVanityAsync(string vanity)
        {
            var shortUrlEntity = await GetShortUrlEntityByVanityAsync(vanity);
            return (shortUrlEntity != null);
        }

        public async Task<bool> IfShortUrlEntityExistAsync(ShortUrlEntity row)
        {
            ShortUrlEntity eShortUrl = await GetShortUrlEntityAsync(row);
            return (eShortUrl != null);
        }

        public async Task<int> GetNextTableIdAsync(CancellationToken cancellationToken = default)
        {
            var tableClient = GetUrlsTable();
            NextId entity = null;

            try
            {
                //Get current ID
                var result = await tableClient.GetEntityAsync<NextId>("1", "KEY", cancellationToken: cancellationToken);
                entity = result.Value;
            }
            catch { }

            entity ??=
                new NextId
                {
                    PartitionKey = "1",
                    RowKey = "KEY",
                    Id = 1024
                };

            entity.Id++;

            //UpdateOrInsert
            _ = await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);

            return entity.Id;
        }

        public async Task<ShortUrlEntity> UpdateShortUrlEntityAsync(ShortUrlEntity urlEntity, CancellationToken cancellationToken = default)
        {
            ShortUrlEntity originalUrl = await GetShortUrlEntityAsync(urlEntity, cancellationToken);
            if (originalUrl == null)
                return null;

            originalUrl.Url = urlEntity.Url;
            originalUrl.Title = urlEntity.Title;
            originalUrl.SchedulesPropertyRaw = JsonSerializer.Serialize(urlEntity.Schedules);

            var result = await SaveShortUrlEntityAsync(originalUrl, cancellationToken);
            return result;
        }

        public async Task<IList<ClickStatsEntity>> GetAllStatsByVanityAsync(string vanity, CancellationToken cancellationToken = default)
        {
            var entities = new List<ClickStatsEntity>();
            var tableClient = GetStatsTable();

            var pages = tableClient.QueryAsync<ClickStatsEntity>(filter: $"PartitionKey eq \'{vanity}\'", cancellationToken: cancellationToken);
            await foreach (var page in pages)
            {
                entities.Add(page);
            }
            return entities;
        }

        public async Task<ShortUrlEntity> ArchiveShortUrlEntityAsync(ShortUrlEntity urlEntity, CancellationToken cancellationToken = default)
        {
            ShortUrlEntity originalUrl = await GetShortUrlEntityAsync(urlEntity, cancellationToken);
            originalUrl.IsArchived = true;

            var result = await SaveShortUrlEntityAsync(originalUrl, cancellationToken);
            return result;
        }
    }
}