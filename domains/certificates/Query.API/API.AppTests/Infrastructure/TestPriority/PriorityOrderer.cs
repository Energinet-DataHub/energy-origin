using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace API.AppTests.Infrastructure.TestPriority;

public class PriorityOrderer : ITestCaseOrderer
{
    private const string @namespace = "API.AppTests.Infrastructure.TestPriority";
    public const string TypeName = $"{@namespace}.{nameof(PriorityOrderer)}";

    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var sortedMethods = new SortedDictionary<int, List<TTestCase>>();
        const int priorityForTestCasesWithoutAttribute = int.MaxValue;

        foreach (var testCase in testCases)
        {
            var testPriorityAttribute = testCase.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute))
                .FirstOrDefault();
            
            var priority = testPriorityAttribute?.GetNamedArgument<int>(nameof(TestPriorityAttribute.Priority))
                           ?? priorityForTestCasesWithoutAttribute;

            sortedMethods.TryAdd(priority, new List<TTestCase>());
            sortedMethods[priority].Add(testCase);
        }

        return sortedMethods.Keys.SelectMany(priority => sortedMethods[priority].OrderBy(MethodName));
    }

    private static string MethodName<TTestCase>(TTestCase testCase)
        where TTestCase : ITestCase
        => testCase.TestMethod.Method.Name;
}
