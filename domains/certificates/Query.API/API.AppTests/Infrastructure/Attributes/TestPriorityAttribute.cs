using System;

namespace API.AppTests.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }

    public TestPriorityAttribute(int priority) => Priority = priority;
}
