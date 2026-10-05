using ConsoleApp1.Models;
using ConsoleApp1.Services;

class Program
{
    static void Main(string[] args)
    {
        using var monitor = new ProcessTreeMonitor();

        monitor._onProcessTreeChange += (node, changeType) =>
        {
            Console.WriteLine(
                $"[{changeType,-10}] PID={node.ProcessId,-6} PPID={node.ParentProcessId,-6} {node.ProcessName}"
            );
        };

        monitor.Start();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Q)
                break;

            if (key.Key == ConsoleKey.Enter)
            {
                PrintTree(monitor);
            }
        }
    }

    static void PrintTree(ProcessTreeMonitor monitor)
    {
        Console.WriteLine("\n=== Tree start ===");
        foreach (var root in monitor.GetRoots().OrderBy(r => r.ProcessId))
        {
            PrintNode(root, 0);
        }
        Console.WriteLine("\n=== Tree end ===");
    }

    static void PrintNode(ProcessNode node, int depth)
    {
        var indent = new string(' ', depth * 2);
        var status = node.IsTerminated ? " [terminated]" : "";
        Console.WriteLine($"{indent}└─ {node.ProcessId} ({node.ProcessName}){status}");
        foreach (var child in node.Children.OrderBy(c => c.ProcessId))
        {
            PrintNode(child, depth + 1);
        }
    }
}