using System.Reflection;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// Metadata about a RService.IO web service.
    /// </summary>
    public class ServiceMetadata
    {
        /// <summary>
        /// The unique identifier.
        /// </summary>
        public string Ident { get; set; }
        /// <summary>
        /// Metadata on the service.
        /// </summary>
        public TypeInfo Service { get; set; }
        /// <summary>
        /// Metadata on the method.
        /// </summary>
        public MethodInfo Method { get; set; }
    }
}
