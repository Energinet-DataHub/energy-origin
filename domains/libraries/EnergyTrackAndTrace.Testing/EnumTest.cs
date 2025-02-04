using System;
using Xunit;

namespace EnergyTrackAndTrace.Testing;

public class EnumTest
{
    public static void AssertEquivalentEnums<TEnum1, TEnum2>() where TEnum1 : struct, Enum where TEnum2 : struct, Enum
    {
        var enum1Values = Enum.GetValues<TEnum1>();
        var enum2Values = Enum.GetValues<TEnum2>();

        Assert.Equal(enum1Values.Length, enum2Values.Length);

        foreach (var enum1Value in enum1Values)
        {
            var enum1Name = Enum.GetName(typeof(TEnum1), enum1Value);
            var enum2Name = Enum.GetName(typeof(TEnum2), enum1Value);

            Assert.NotNull(enum1Name);
            Assert.NotNull(enum2Name);
            Assert.Equal(enum1Name, enum2Name);
        }
    }
}
