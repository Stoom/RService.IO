using RService.IO.Abstractions;

namespace RServiceSample.Models
{
    [Route("api/Values/{id}", RestVerbs.Put)]
    public class UpdateItemValueReq
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
