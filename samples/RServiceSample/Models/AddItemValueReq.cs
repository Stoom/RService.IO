using RService.IO.Abstractions;

namespace RServiceSample.Models
{
    [Route("api/Values", RestVerbs.Post)]
    public class AddItemValueReq
    {
        public string Value { get; set; }
    }
}
