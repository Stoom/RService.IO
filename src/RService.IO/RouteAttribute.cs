using System;

namespace RService.IO
{
    /// <summary>
    /// Used to decorate Request DTO's or service method to associate a RESTful 
    /// request path mapping with a service.  Multiple attributes can be applied 
    /// to each request DTO or web service method, to map multiple paths to the service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : Attribute, IEquatable<RouteAttribute>
    {
        /// <summary>
        /// The path template that maps to the request.
        /// </summary>
        /// <remarks>
        ///		<para>Some examples of valid paths are:</para>
        /// 
        ///		<list>
        ///			<item>"/Inventory"</item>
        ///			<item>"/Inventory/{Category}/{ItemId}"</item>
        ///			<item>"/Inventory/{ItemPath*}"</item>
        ///		</list>
        /// 
        ///		<para>Variables are specified within "{}"
        ///		brackets.  Each variable in the path is mapped to the same-named property 
        ///		on the request DTO.  At runtime, ServiceR will parse the request URL, 
        ///     extract the variable values, instantiate the request DTO,
        ///		and assign the variable values into the corresponding request properties,
        ///		prior to passing the request DTO to the service object for processing.</para>
        /// 
        ///		<para>Please note that while it is possible to specify property values
        ///		in the query string, it is generally considered to be less RESTful and
        ///		less desirable than to specify them as variables in the path.  Using the 
        ///		query string to specify property values may also interfere with HTTP
        ///		caching. *RFC3986</para>
        /// </remarks> 
        public string Path { get; }
        /// <summary>
        /// Gets a list of flags of HTTP verbs supported by the service endpoint.
        /// </summary>
        public RestVerbs Verbs { get; }

        /// <summary>
        /// Initializes an instance of <see cref="RouteAttribute"/>.
        /// </summary>
        /// <param name="path">Path template to map to request.</param>
        /// <param name="verbs">A list of flags of HTTP verbs supported by the service.</param>
        /// <remarks>If no verbs are specified GET, POST, PUT, PATCH, DELETE and OPTIONS are routed.</remarks>
        public RouteAttribute(String path, RestVerbs verbs = RestVerbs.Any)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentException(nameof(path));

            Path = path;
            Verbs = verbs;
        }

        #region IEquatable
        /// <Inheritdoc/>
        public bool Equals(RouteAttribute other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return Path.Equals(other.Path)
                   && Verbs.Equals(other.Verbs);
        }

        /// <Inheritdoc/>
        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, null))
                return false;
            return ReferenceEquals(other, this) || Equals(other as RouteAttribute);
        }

        /// <Inheritdoc/>
        public override int GetHashCode()
        {
            var hash = 13;
            hash = (hash * 79) + Path.GetHashCode();
            hash = (hash * 79) + Verbs.GetHashCode();
            return hash;
        }
        #endregion
    }
}
