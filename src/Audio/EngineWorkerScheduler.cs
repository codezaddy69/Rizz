using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DJMixMaster.Audio
{
    public class EngineWorkerScheduler
    {
        private readonly ILogger<EngineWorkerScheduler> _logger;
        private readonly ConcurrentQueue<Action> _taskQueue = new();
        private readonly Thread[] _workerThreads;
        private volatile bool _running = true;

        public EngineWorkerScheduler(ILogger<EngineWorkerScheduler> logger, int threadCount = 2)
        {
            _logger = logger;
            _workerThreads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                _workerThreads[i] = new Thread(WorkerLoop);
                _workerThreads[i].Start();
            }
        }

        public void ScheduleTask(Action task)
        {
            _taskQueue.Enqueue(task);
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                if (_taskQueue.TryDequeue(out var task))
                {
                    try
                    {
                        task();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Task execution failed");
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void Shutdown()
        {
            _running = false;
            foreach (var thread in _workerThreads)
            {
                thread.Join();
            }
        }
    }
}