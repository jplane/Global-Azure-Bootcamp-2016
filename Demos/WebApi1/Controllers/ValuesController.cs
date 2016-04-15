using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Actor1.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace WebApi1.Controllers
{
    public class ValuesController : ApiController
    {
        private static ConcurrentDictionary<int, string> _values = new ConcurrentDictionary<int, string>();

        // GET api/values/5 
        public string Get(int id)
        {
            string value = null;

            _values.TryGetValue(id, out value);

            return value;
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value)
        {
            _values.AddOrUpdate(id, value, (k, v) => v);
        }

        // DELETE api/values/5 
        public void Delete(int id)
        {
            string ignored;
            _values.TryRemove(id, out ignored);
        }
    }
}
