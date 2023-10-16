using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ostrander.Data;
using Lunra.Deep;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ostrander.Navigation
{
    public class NavigationService : Bindable
    {
        public class Data
        {
            public static Data Default()
            {
                var result = new Data();
                
                result.ThreadCount = 1;
                
                return result;
            }
            
            public int ThreadCount { get; private set; }
        }
        
        public enum States
        {
            Initializing,
            Paused,
            Running,
            Stopped,
        }
        
        public States State { get; private set; }
        public Data InstanceData { get; private set; }
        ConcurrentQueue<NavigationRequestHandle> requests = new();

        List<NavigationPool> pools = new();

        GameMain main;
        GameTime time;
        MapService map;
        
        public NavigationService()
        {
            UnBinded += OnUnBinded;
        }
        
        public async UniTask Initialize()
        {
            main = Container.Get<GameMain>();
            time = Container.Get<GameTime>();
            map = Container.Get<MapService>();

            main.GameEnding.Add(OnGameEnding);
            time.IsTickingUpdated += OnIsTickingUpdated;
            
            // Eventually this should probably be obtained from a container so it can be edited by a settings menu.
            InstanceData = Data.Default();

            if (InstanceData.ThreadCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(InstanceData.ThreadCount),
                    $"ThreadCount of {InstanceData.ThreadCount} is invalid, must be greater than zero"
                );
            }

            for (var i = 0; i < InstanceData.ThreadCount; i++)
            {
                var pool = new NavigationPool();
                pools.Add(pool);
                Container.Inject(pool);
                await pool.Initialize(
                    requests
                );
            }

            State = States.Paused;
        }

        void OnIsTickingUpdated(
            object sender,
            IsTickingUpdatedEventArgs e
        )
        {
            switch (State)
            {
                case States.Paused:
                {
                    if (e.IsTicking)
                    {
                        State = States.Running;
                        return;
                    }

                    break;
                }
                case States.Running:
                {
                    if (!e.IsTicking)
                    {
                        State = States.Paused;
                    }

                    break;
                }
                case States.Initializing:
                case States.Stopped:
                {
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Debug.LogWarning($"{nameof(NavigationService)} in unexpected {nameof(State)} {State} while {nameof(e.IsTicking)} is {e.IsTicking}");
        }
        
        async UniTask OnGameEnding(
            object sender,
            GameEndingEventArgs e
        )
        {
            State = States.Stopped;
            
            var poolIndex = 0;
            
            while (poolIndex < pools.Count)
            {
                if (pools[poolIndex].State == States.Stopped)
                {
                    poolIndex++;
                }
                else
                {
                    await UniTask.Delay(100, DelayType.Realtime);
                }
            }
        }

        void OnUnBinded()
        {
            time.IsTickingUpdated -= OnIsTickingUpdated;
        }

        public NavigationRequestHandle Process(
            NavigationRequest request
        )
        {
            NavigationRequestHandle handle;
            
            if (!map.TryGetCell(request.Begin, out var begin))
            {
                handle = new NavigationRequestHandle(
                    request,
                    null,
                    null,
                    new NavigationResult()
                        .UpdateState(
                            NavigationResult.States.CompletedWithException,
                            request,
                            null,
                            new NavigationRequestBeginIsInvalid(
                                request,
                                request.Begin
                            )
                        )
                        .Complete()
                );

                return handle;
            }
            
            if (!map.TryGetCell(request.End, out var end))
            {
                handle = new NavigationRequestHandle(
                    request,
                    null,
                    null,
                    new NavigationResult()
                        .UpdateState(
                            NavigationResult.States.CompletedWithException,
                            request,
                            null,
                            new NavigationRequestEndIsInvalid(
                                request,
                                request.End
                            )
                        )
                        .Complete()
                );

                return handle;
            }
            
            handle = new NavigationRequestHandle(
                request,
                begin,
                end,
                new NavigationResult()
            );

            requests.Enqueue(handle);

            return handle;
        }

        public async UniTask<NavigationResult> ProcessAsync(
            NavigationRequest request
        )
        {
            var handle = Process(request);

            while (!handle.Result.IsCompleted)
            {
                await UniTask.NextFrame();
            }

            return handle.Result;
        }
    }
    
    public class NavigationRequestBeginIsInvalid : Exception
    {
        public NavigationRequest Request { get; }
        public Vector3Int Begin { get; }
        public override string Message => $"Request {Request} specified invalid {nameof(Request.Begin)} of {Begin}";

        public NavigationRequestBeginIsInvalid(
            NavigationRequest request,
            Vector3Int begin
        )
        {
            Request = request;
            Begin = begin;
        }
    }

    public class NavigationRequestEndIsInvalid : Exception
    {
        public NavigationRequest Request { get; }
        public Vector3Int End { get; }
        public override string Message => $"Request {Request} specified invalid {nameof(Request.End)} of {End}";

        public NavigationRequestEndIsInvalid(
            NavigationRequest request,
            Vector3Int end
        )
        {
            Request = request;
            End = end;
        }
    }
}