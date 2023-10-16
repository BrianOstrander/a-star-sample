using Ostrander.Data;
using UnityEngine;

namespace Ostrander.Navigation
{
    public class NavigationRequest
    {
        public const long DefaultMillisecondsTimeout = 1000L;
        
        public static NavigationRequest Euclidean(
            Vector3Int begin,
            Vector3Int end,
            NavigationCostModifiers costModifiers = null,
            long millisecondsTimeout = DefaultMillisecondsTimeout,
            bool isDebugging = false
        )
        {
            return new NavigationRequest(
                begin,
                end,
                costModifiers ?? NavigationCostModifiers.Default,
                new EuclideanHeuristic(),
                millisecondsTimeout,
                isDebugging
            );
        }
        
        public Vector3Int Begin { get; }
        public Vector3Int End { get; }
        public NavigationCostModifiers CostModifiers { get; }
        public INavigationHeuristic Heuristic { get; }
        public long MillisecondsTimeout { get; }
        public bool IsDebugging { get; }

        NavigationRequest(
            Vector3Int begin,
            Vector3Int end,
            NavigationCostModifiers costModifiers,
            INavigationHeuristic heuristic,
            long millisecondsTimeout,
            bool isDebugging
        )
        {
            Begin = begin;
            End = end;
            CostModifiers = costModifiers;
            Heuristic = heuristic;
            MillisecondsTimeout = millisecondsTimeout;
            IsDebugging = isDebugging;
        }
    }
}