using System;
using Microsoft.Extensions.Logging;

namespace RService.IO
{
    public static class RServiceMiddlewareLoggerExtensions
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Action<ILogger, Exception> _requestDidNotMatchServices;

        static RServiceMiddlewareLoggerExtensions()
        {
            _requestDidNotMatchServices = LoggerMessage.Define(
                LogLevel.Debug,
                1,
                "Request did not match any services.");
        }

        public static void RequestDidNotMatchServices(this ILogger logger)
        {
            _requestDidNotMatchServices(logger, null);
        }
    }
}