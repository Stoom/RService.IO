using RService.IO.Abstractions;
using System.Collections.Generic;
using RServiceSample.Models;

namespace RServiceSample.Services
{
    public class ValuesService : ServiceBase
    {
        [Route("api/Values", RestVerbs.Get)]
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        public string Get(GetItemValueReq req)
        {
            return "value";
        }

        public void Post(AddItemValueReq req)
        {
        }

        public void Put(UpdateItemValueReq req)
        {
        }

        public void Delete(RemoveItemValueReq req)
        {
        }
    }
}
