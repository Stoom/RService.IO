using RService.IO.Abstractions;

namespace RServiceSample.Models
{
    [Route("api/Values/{id}", RestVerbs.Delete)]
    public class RemoveItemValueReq
    {
        public int Id { get; set; }
    }
}
