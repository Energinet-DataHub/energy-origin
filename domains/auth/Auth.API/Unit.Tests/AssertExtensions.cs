
namespace Unit.Tests.AssertExtensions;

public static class AssertExtensions
{
    public static bool ele(this Assert assert, int number)
    {
        if (number == 0)
        {
            return true;
        }
        return false;
    }
}

