using System.Collections.Concurrent;

namespace ConsoleApp1.Models;

public sealed class ProcessNode
{
    public int ProcessId { get; init; }
    public int ParentProcessId { get; init; }
    public string ProcessName { get; set; }
    public DateTime StartTime { get; set; }
    public ProcessNode? Parent { get; set; }
    public ConcurrentBag<ProcessNode> Children { get; } = new();
    public bool IsTerminated { get; set; }
    public Guid? CreationActivityId { get; set; }
}