using RService.IO.Abstractions;

namespace RServiceSample.Models
{
    [Route("api/Values/{id}", RestVerbs.Get)]
    public class GetItemValueReq
    {
        public int Id { get; set; }
    }
}
