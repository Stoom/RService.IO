using RService.IO.Abstractions;

namespace RService.IO.Tests
{
    public class SvcWithMethodRoute : IService
    {
        public const string RoutePath = "/Foobar";
        public const string GetPath = "/Foobar/Get";
        public const string PostPath = "/Foobar/Post";
        public bool HasAnyBeenCalled { get; set; }
        public string GetResponse { get; set; }
        public int PostResponse { get; set; }

        [Route(RoutePath)]
        public object Any()
        {
            HasAnyBeenCalled = true;
            return null;
        }

        [Route(GetPath, RestVerbs.Get)]
        public string Get()
        {
            return GetResponse;
        }

        [Route(PostPath, RestVerbs.Post)]
        public int Post()
        {
            return PostResponse;
        }
    }

    public class SvcWithMultMethodRoutes : IService
    {
        public const string RoutePath1 = "/Foobar/Llamas";
        public const string RoutePath2 = "/Foobar/Eats";

        [Route(RoutePath1)]
        [Route(RoutePath2)]
        public object Any()
        {
            return null;
        }
    }

    public class SvcWithParamRoute : IService
    {
        public const string RoutePath = "/Llamas";

        public object Any(DtoForParamRoute dto)
        {
            return dto.Foobar;
        }
    }

    public class SvcWithMultParamRoutes : IService
    {
        public const string RoutePath1 = "/Llamas/Eats";
        public const string RoutePath2 = "/Llamas/Hands";

        public object Any(DtoForMultParamRoutes dto)
        {
            return null;
        }
    }

    [Route(SvcWithParamRoute.RoutePath)]
    public class DtoForParamRoute
    {
        public string Foobar { get; set; }
    }

    [Route(SvcWithMultParamRoutes.RoutePath1)]
    [Route(SvcWithMultParamRoutes.RoutePath2)]
    public class DtoForMultParamRoutes
    {
        
    }
}