﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace WordCount.WebService.Controllers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    /// <summary>
    /// Default controller.
    /// </summary>
    [RoutePrefix("api")]
    public class DefaultController : ApiController
    {
        private const int MaxQueryRetryCount = 20;

        private static readonly Uri _serviceUri;
        private static readonly TimeSpan _backoffQueryDelay;
        private static readonly FabricClient _fabricClient;
        private static readonly HttpCommunicationClientFactory _communicationFactory;

        static DefaultController()
        {
            _serviceUri = new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/WordCountService");

            _backoffQueryDelay = TimeSpan.FromSeconds(3);

            _fabricClient = new FabricClient();

            _communicationFactory = new HttpCommunicationClientFactory(new ServicePartitionResolver(() => _fabricClient));
        }

        [HttpGet]
        [Route("Count")]
        public async Task<HttpResponseMessage> Count()
        {
            // For each partition client, keep track of partition information and the number of words
            var totals = new ConcurrentDictionary<Int64RangePartitionInformation, long>();

            foreach (var partition in await this.GetServicePartitionKeysAsync())
            {
                try
                {
                    var partitionClient =
                        new ServicePartitionClient<HttpCommunicationClient>(_communicationFactory, _serviceUri, new ServicePartitionKey(partition.LowKey));

                    await partitionClient.InvokeWithRetryAsync(
                        async client =>
                        {
                            var response = await client.HttpClient.GetAsync(new Uri(client.Url, "Count"));
                            var content = await response.Content.ReadAsStringAsync();
                            totals[partition] = Int64.Parse(content.Trim());
                        });
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.OperationFailed(ex.Message, "Count - run web request");
                }
            }

            var sb = new StringBuilder();
            sb.Append("<h1> Total:");
            sb.Append(totals.Aggregate<KeyValuePair<Int64RangePartitionInformation, long>, long>(0, (total, next) => next.Value + total));
            sb.Append("</h1>");
            sb.Append("<table><tr><td>Partition ID</td><td>Key Range</td><td>Total</td></tr>");
            foreach (var partitionData in totals.OrderBy(partitionData => partitionData.Key.LowKey))
            {
                sb.Append("<tr><td>");
                sb.Append(partitionData.Key.Id);
                sb.Append("</td><td>");
                sb.AppendFormat("{0} - {1}", partitionData.Key.LowKey, partitionData.Key.HighKey);
                sb.Append("</td><td>");
                sb.Append(partitionData.Value);
                sb.Append("</td></tr>");
            }

            sb.Append("</table>");

            return new HttpResponseMessage()
            {
                Content = new StringContent(sb.ToString(), Encoding.UTF8, "text/html")
            };
        }

        [HttpPost]
        [Route("AddWord/{word}")]
        public async Task<HttpResponseMessage> AddWord(string word)
        {
            // Determine the partition key that should handle the request
            var partitionKey = GetPartitionKey(word);

            var partitionClient =
                new ServicePartitionClient<HttpCommunicationClient>(_communicationFactory, _serviceUri, new ServicePartitionKey(partitionKey));

            await
                partitionClient.InvokeWithRetryAsync(
                    async client => { await client.HttpClient.PutAsync(new Uri(client.Url, "AddWord/" + word), new StringContent(String.Empty)); });

            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    $"<h1>{word}</h1> added to partition with key <h2>{partitionKey}</h2>",
                    Encoding.UTF8,
                    "text/html")
            };
        }

        /// <summary>
        /// Gets the partition key which serves the specified word.
        /// Note that the sample only accepts Int64 partition scheme. 
        /// </summary>
        /// <param name="word">The word that needs to be mapped to a service partition key.</param>
        /// <returns>A long representing the partition key.</returns>
        private static long GetPartitionKey(string word)
        {
            return ((long) char.ToUpper(word[0])) - 64;
        }

        /// <summary>
        /// Returns a list of service partition clients pointing to one key in each of the WordCount service partitions.
        /// The returned representative key is the min key served by each partition.
        /// </summary>
        /// <returns>The service partition clients pointing at a key in each of the WordCount service partitions.</returns>
        private async Task<IList<Int64RangePartitionInformation>> GetServicePartitionKeysAsync()
        {
            for (var i = 0; i < MaxQueryRetryCount; i++)
            {
                try
                {
                    // Get the list of partitions up and running in the service.
                    var partitionList = await _fabricClient.QueryManager.GetPartitionListAsync(_serviceUri);

                    // For each partition, build a service partition client used to resolve the low key served by the partition.
                    IList<Int64RangePartitionInformation> partitionKeys = new List<Int64RangePartitionInformation>(partitionList.Count);

                    foreach (var partition in partitionList)
                    {
                        var partitionInfo = partition.PartitionInformation as Int64RangePartitionInformation;

                        if (partitionInfo == null)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "The service {0} should have a uniform Int64 partition. Instead: {1}",
                                    _serviceUri.ToString(),
                                    partition.PartitionInformation.Kind));
                        }

                        partitionKeys.Add(partitionInfo);
                    }

                    return partitionKeys;
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.OperationFailed(ex.Message, "create representative partition clients");

                    if (i == MaxQueryRetryCount - 1)
                    {
                        throw;
                    }
                }

                await Task.Delay(_backoffQueryDelay);
            }

            throw new TimeoutException("Retry timeout is exhausted and creating representative partition clients wasn't successful");
        }
    }
}