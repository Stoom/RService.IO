using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RService.IO
{
    public class RServiceMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public RServiceMiddleware(RequestDelegate next, ILoggerFactory logFactory)
        {
            _next = next;
            _logger = logFactory.CreateLogger<RServiceMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            await _next.Invoke(context);
        }
    }
}