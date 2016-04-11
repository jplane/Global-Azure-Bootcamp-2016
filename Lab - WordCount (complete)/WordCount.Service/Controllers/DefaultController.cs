// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;

namespace WordCountService.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    /// <summary>
    /// Default controller.
    /// </summary>
    public class DefaultController : ApiController
    {
        private readonly IReliableStateManager stateManager;

        public DefaultController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        [HttpGet]
        [Route("Count")]
        public async Task<IHttpActionResult> Count()
        {
            var statsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("statsDictionary");

            using (var tx = this.stateManager.CreateTransaction())
            {
                var result = await statsDictionary.TryGetValueAsync(tx, "Number of Words Processed");

                if (result.HasValue)
                {
                    return this.Ok(result.Value);
                }
            }

            return this.Ok(0);
        }

        // TODO: add FindDups() service method...

        [HttpGet]
        [Route("FindDups")]
        public async Task<IHttpActionResult> FindDups()
        {
            var wordCountDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("wordCountDictionary");

            using (var tx = this.stateManager.CreateTransaction())
            {
                var enumerator = (await wordCountDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                var dups = new List<Tuple<string, long>>();

                while(await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var item = enumerator.Current;

                    if (item.Value > 1)
                    {
                        dups.Add(Tuple.Create(item.Key, item.Value));
                    }
                }

                return this.Ok(dups.ToArray());
            }
        }

        [HttpPut]
        [Route("AddWord/{word}")]
        public async Task<IHttpActionResult> AddWord(string word)
        {
            var queue = await this.stateManager.GetOrAddAsync<IReliableQueue<string>>("inputQueue");

            using (var tx = this.stateManager.CreateTransaction())
            {
                await queue.EnqueueAsync(tx, word);

                await tx.CommitAsync();
            }

            return this.Ok();
        }
    }
}