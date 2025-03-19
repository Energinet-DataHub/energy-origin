using System;
using System.Reflection;
using Xunit.v3;

namespace API.IntegrationTests.Attributes;

internal class WriteToConsoleAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        Console.WriteLine();
        Console.WriteLine($"[Starting {methodUnderTest.DeclaringType}.{methodUnderTest.Name}]");
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        Console.WriteLine($"[Finished {methodUnderTest.DeclaringType}.{methodUnderTest.Name}]");
        Console.WriteLine();
    }
}
