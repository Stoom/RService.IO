namespace RService.IO.Tests
{
    public class SvcWithMethodRoute : IService
    {
        public const string RoutePath = "/Foobar";

        [Route(RoutePath)]
        public void Any() { }
    }

    public class SvcWithMultMethodRoutes : IService
    {
        public const string RoutePath1 = "/Foobar/Llamas";
        public const string RoutePath2 = "/Foobar/Eats";

        [Route(RoutePath1)]
        [Route(RoutePath2)]
        public void Any() { }
    }

    public class SvcWithParamRoute : IService
    {
        public const string RoutePath = "/Llamas";

        public void Any(DtoForParamRoute dto) { }
    }

    public class SvcWithMultParamRoutes : IService
    {
        public const string RoutePath1 = "/Llamas/Eats";
        public const string RoutePath2 = "/Llamas/Hands";

        public void Any(DtoForMultParamRoutes dto) { }
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