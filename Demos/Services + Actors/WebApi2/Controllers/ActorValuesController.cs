using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Actor1.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace WebApi2.Controllers
{
    public class ActorValuesController : ApiController
    {
        private static Uri serviceUri = new Uri("fabric:/DemoApp/Actor1ActorService");

        // GET api/values/5 
        public async Task<string> Get(int id)
        {
            var actor = ActorProxy.Create<IActor1>(new ActorId(id), serviceUri);
            return await actor.GetValueAsync();
        }

        // PUT api/values/5 
        public Task Put(int id, [FromBody]string value)
        {
            var actor = ActorProxy.Create<IActor1>(new ActorId(id), serviceUri);
            return actor.SetValueAsync(value);
        }

        // DELETE api/values/5 
        public Task Delete(int id)
        {
            var actorId = new ActorId(id);
            var proxy = ActorServiceProxy.Create(serviceUri, actorId);
            return proxy.DeleteActorAsync(actorId, CancellationToken.None);
        }
    }
}
