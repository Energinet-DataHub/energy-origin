using System;

namespace ClaimAutomation.Worker.Automation;

public interface IShuffler
{
    int Next();
}

public class Shuffler : IShuffler
{
    private readonly Random random;
    public Shuffler()
    {
        random = new Random();
    }
    public Shuffler(int seed)
    {
        random = new Random(seed);
    }

    public int Next()
    {
        return random.Next();
    }
}
