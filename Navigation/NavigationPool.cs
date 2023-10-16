using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Lunra.Deep;
using Debug = UnityEngine.Debug;

namespace Ostrander.Navigation
{
    public class NavigationPool : Injectable
    {
        public NavigationService.States State { get; private set; }

        ConcurrentQueue<NavigationRequestHandle> requests;
        
        NavigationService navigation;
        Stopwatch stopwatch;
        
        public async UniTask Initialize(
            ConcurrentQueue<NavigationRequestHandle> requests
        )
        {
            this.requests = requests;
            
            navigation = Container.Get<NavigationService>();
            
            Process().Forget();

            while (State == NavigationService.States.Initializing)
            {
                await UniTask.Delay(100, DelayType.Realtime);
            }
        }

        async UniTask Process()
        {
            await UniTask.SwitchToThreadPool();

            await Update();
            
            await UniTask.SwitchToMainThread();
            
            State = NavigationService.States.Stopped;
        }

        async UniTask Update()
        {
            while (true)
            {
                switch (navigation.State)
                {
                    case NavigationService.States.Initializing:
                    case NavigationService.States.Paused:
                    {
                        await UniTask.Delay(100, DelayType.Realtime);
                        await UniTask.SwitchToThreadPool();
                        State = NavigationService.States.Paused;
                        break;
                    }
                    case NavigationService.States.Running:
                    {
                        State = NavigationService.States.Running;
                        await OnUpdate();
                        break;
                    }
                    case NavigationService.States.Stopped:
                    {
                        return;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        UniTask OnUpdate()
        {
            if (!requests.TryDequeue(out var handle))
            {
                return UniTask.CompletedTask;
            }

            if (handle.Request.IsDebugging)
            {
                stopwatch ??= new Stopwatch();
                stopwatch.Start();
            }

            var operation = new NavigationOperation();
            operation.Initialize(handle);
            
            operation.Process();

            if (handle.Result.State == NavigationResult.States.CompletedWithException)
            {
                Debug.LogException(handle.Result.Exception);
            }

            if (handle.Request.IsDebugging)
            {
                handle.Result.MillisecondsElapsed = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
            }

            handle.Result.Complete();

            return UniTask.CompletedTask;
        }
    }
}