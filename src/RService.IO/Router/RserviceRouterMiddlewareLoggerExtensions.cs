using System;
using Microsoft.Extensions.Logging;

namespace RService.IO.Router
{
    public static class RserviceRouterMiddlewareLoggerExtensions
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Action<ILogger, Exception> _requestDidNotMatchRoutes;

        static RserviceRouterMiddlewareLoggerExtensions()
        {
            _requestDidNotMatchRoutes = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "Request did not match any routes.");
        }

        public static void RequestDidNotMatchRoutes(this ILogger logger)
        {
            _requestDidNotMatchRoutes(logger, null);
        }
    }
}