using Avalonia.Logging;

namespace ChatAAC.Helpers
{
    public static class AppLogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The content of the log.</param>
        /// <param name="className">Name of class</param>
        /// <remarks>
        /// This method uses Avalonia's logging system to log an informational message.
        /// The message is tagged with the "MainViewModel" category and logged at the Information level.
        /// </remarks>
        public static void LogInfo(string message, string className = "AppLogger")
        {
            Logger.TryGet(LogEventLevel.Information, className)
                ?.Log(LogEventLevel.Information, message);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The content of the log. It should describe the error that occurred.</param>
        /// <param name="className">Name of the class where the error occurred. The default value is "AppLogger".</param>
        /// <returns>This method does not return any value.</returns>
        /// <remarks>
        /// This method uses Avalonia's logging system to log an informational message.
        /// The message is tagged with the "MainViewModel" category and logged at the Information level.
        /// </remarks>
        public static void LogError(string message, string className = "AppLogger")
        {
            Logger.TryGet(LogEventLevel.Error, className)
                ?.Log(LogEventLevel.Error, message);
        }

    }
}