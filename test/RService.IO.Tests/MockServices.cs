using RService.IO.Abstractions;

namespace RService.IO.Tests
{
    public class SvcWithMethodRoute : IService
    {
        public const string RoutePath = "/Foobar";

        [Route(RoutePath)]
        public object Any()
        {
            return null;
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
            return null;
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
        
    }

    [Route(SvcWithMultParamRoutes.RoutePath1)]
    [Route(SvcWithMultParamRoutes.RoutePath2)]
    public class DtoForMultParamRoutes
    {
        
    }
}