using System;

namespace SpecificationPatternDemo.IntegrationTests;

internal class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("Integration test runner placeholder.");
        Console.WriteLine("Run the API locally and use the included PostsController endpoints to test.");
        Console.WriteLine("Examples:");
        Console.WriteLine("1) POST /api/auth/login with JSON { \"Username\": \"user\", \"Role\": \"User\" } to get token");
        Console.WriteLine("2) Use Authorization: Bearer <token> to call POST /api/posts");
        return 0;
    }
}
