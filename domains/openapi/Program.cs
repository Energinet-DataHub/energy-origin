using System.Diagnostics;

namespace open_api;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Running Redocly CLI...");

        // Example of running a Redocly command
        RunRedoclyCommand("redocly lint");

        Console.WriteLine("Redocly CLI operation completed.");
    }

    static void RunRedoclyCommand(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error running Redocly command: " + ex.Message);
        }
    }
}
