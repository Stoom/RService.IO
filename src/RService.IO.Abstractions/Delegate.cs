namespace RService.IO.Abstractions
{
    /// <summary>
    /// Delegates used in RService.IO.
    /// </summary>
    public static class Delegate
    {
        /// <summary>
        /// Activates method associated with a service.
        /// </summary>
        /// <param name="args">Arguments for method being activated.</param>
        /// <returns>The service's response.</returns>
        public delegate object Activator(object target, params object[] args);
        /// <summary>
        /// Creates a DTO based on JSON.
        /// </summary>
        /// <param name="json">JSON to create DTO from.</param>
        /// <returns>The DTO.</returns>
        public delegate object DtoCtor(string json);
    }
}