using System;
using System.Text;

namespace API.AppTests.Helpers;

public static class GsrnHelper
{
    public static string GenerateRandom()
    {
        var rand = new Random();
        var sb = new StringBuilder();
        for (var i = 0; i < 18; i++)
        {
            sb.Append(rand.Next(0, 9));
        }

        return sb.ToString();
    }
}
