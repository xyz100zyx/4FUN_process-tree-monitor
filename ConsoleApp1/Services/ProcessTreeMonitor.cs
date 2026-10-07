using ConsoleApp1.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Concurrent;

namespace ConsoleApp1.Services;

public class ProcessTreeMonitor : IDisposable
{

    private const string _sessionKey = "process-tree-monitor-custom-session";
    private readonly TraceEventSession _session;

    private readonly ConcurrentDictionary<int, ProcessNode> _processes = new();

    private ETWTraceEventSource _traceEventSource;

    private Task _eventProcessTask;

    private readonly CancellationTokenSource _cts = new();

    public Action<ProcessNode, EProcessAction> onProcessTreeChange;

    public ProcessTreeMonitor()
    {

        /* TODO:
            перед стратом сессии проверить то, существует ли уже сессия с таким ключом
         */
        _session = new(_sessionKey)
        {
            StopOnDispose = true,

        };

        _session.EnableKernelProvider(
            KernelTraceEventParser.Keywords.Process
        );
    }

    public void Start()
    {
        _traceEventSource = new ETWTraceEventSource(_sessionKey, TraceEventSourceType.Session);
        var kernelParser = new KernelTraceEventParser(_traceEventSource);

        kernelParser.ProcessStart += OnProcessStart;
        kernelParser.ProcessStop += OnProcessStop;
        kernelParser.ProcessDCStart += OnProcessDCStart;
        kernelParser.ProcessDCStop += OnProcessDCStop;

        _eventProcessTask = Task.Run(() =>
        {
            try
            {
                _traceEventSource.Process();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"event processing error: {ex.Message}");
            }
        }, _cts.Token);
    }

    public void OnProcessStart(ProcessTraceData data)
    {
        ProcessNode node = new()
        {
            ProcessId = data.ProcessID,
            ParentProcessId = data.ParentID,
            ProcessName = data.ImageFileName,
            StartTime = data.TimeStamp,
            CreationActivityId = data.ActivityID
        };

        var added = _processes.GetOrAdd(node.ProcessId, node);

        if (added != node)
        {
            added.ProcessName ??= node.ProcessName;
            added.StartTime = node.StartTime;
            added.CreationActivityId = node.CreationActivityId;
        }

        if (added != null)
        {
            LinkToParent(added);
        }
        onProcessTreeChange?.Invoke(added, EProcessAction.Created);

    }

    public void OnProcessStop(ProcessTraceData data)
    {
        if (_processes.TryRemove(data.ProcessID, out var node))
        {
            node.IsTerminated = true;

            if (node.Parent != null)
            {
                if (_processes.TryGetValue(node.Parent.ProcessId, out var parentNode))
                {
                    // TODO: Тейкнуть процесс из родительского либо делегировать это задачу на ProcessMonitorSessionManager;
                }
            }

            onProcessTreeChange?.Invoke(node, EProcessAction.Terminated);
        }
    }

    public void OnProcessDCStart(ProcessTraceData data)
    {
        Console.WriteLine("xyz100zyx");
        var node = new ProcessNode
        {
            ProcessId = data.ProcessID,
            ParentProcessId = data.ParentID,
            ProcessName = data.ImageFileName,
            StartTime = data.TimeStamp
        };

        _processes.TryAdd(node.ProcessId, node);
        onProcessTreeChange?.Invoke(node, EProcessAction.Created);
    }

    public void OnProcessDCStop(ProcessTraceData data)
    {
        if (_processes.TryRemove(data.ProcessID, out var node))
        {
            node.IsTerminated = true;
            onProcessTreeChange?.Invoke(node, EProcessAction.Terminated);
        }
    }

    private void LinkToParent(ProcessNode node)
    {
        if (_processes.TryGetValue(node.ParentProcessId, out var parent))
        {
            node.Parent = parent;

            if (!parent.Children.TryPeek(out var child))
            {
                parent.Children.Add(node);
            }
        }
        else
        {
            node.Parent = null;
        }
    }

    /// <summary>
    /// Получаем снимок дерева
    /// </summary>
    public IReadOnlyDictionary<int, ProcessNode> GetSnapshot()
    {
        return _processes;
    }

    /// <summary>
    /// Возвращает список корневых процессов (без родителя).
    /// </summary>
    public IEnumerable<ProcessNode> GetRoots()
    {
        return _processes.Values.Where(p => p.Parent == null && !p.IsTerminated);
    }

    /// <summary>
    /// [DRAFT]: ремейк дерева + реснепшот
    /// </summary>
    public void CaptureCurrentState()
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                using var rundownSession = new TraceEventSession($"ProcessTreeRundownSession_{Guid.NewGuid():N}");

                rundownSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

                rundownSession.Source.Kernel.ProcessDCStart += OnProcessDCStart;

                var rundownThread = new Thread(() => rundownSession.Source.Process()) { IsBackground = true };
                rundownThread.Start();
                Thread.Sleep(1500);
                rundownSession.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации Rundown состояния: {ex.Message}");
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _traceEventSource?.StopProcessing();
        }
        catch { /* Игнорируем ошибки при остановке */ }
        try
        {
            _eventProcessTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch { /* Игнорируем таймаут */ }

        _session?.Dispose();
        _traceEventSource?.Dispose();
        _cts.Dispose();
    }

}