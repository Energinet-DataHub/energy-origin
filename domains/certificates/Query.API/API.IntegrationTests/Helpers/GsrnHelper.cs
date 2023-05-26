using System;
using System.Text;

namespace API.IntegrationTests.Helpers;

public static class GsrnHelper
{
    public static string GenerateRandom()
    {
        var rand = new Random();
        var sb = new StringBuilder();
        sb.Append("57");
        for (var i = 0; i < 16; i++)
        {
            sb.Append(rand.Next(0, 9));
        }

        return sb.ToString();
    }
}
