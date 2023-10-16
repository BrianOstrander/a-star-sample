using System;
using System.Collections.Generic;
using Data;
using Ostrander.Data;
using Lunra.Deep;

namespace Ostrander.Navigation
{
    public class NavigationGraph : Injectable
    {
        INavigationHeuristic heuristic;
        GetNeighborArgs args;

        public void Initialize()
        {
            heuristic = Container.Get<INavigationHeuristic>();
            args = Container.Get<GetNeighborArgs>();
        }

        public List<NavigationConnection> GetConnections(
            Cell cell
        )
        {
            var neighbors = cell.GetNeighbors(
                args
            );

            var results = new List<NavigationConnection>();

            var neighborIndex = 0;
            var validNeighborsRemaining = neighbors.AccessibleCount;

            while (0 < validNeighborsRemaining && neighborIndex < neighbors.Count)
            {
                if (IsValid(neighbors.Entries[neighborIndex]))
                {
                    var neighborCell = neighbors.Entries[neighborIndex].Neighbor; 
                    results.Add(
                        new NavigationConnection(
                            cell,
                            neighborCell,
                            heuristic.GetEstimate(
                                cell,
                                neighborCell
                            )
                        )
                    );

                    validNeighborsRemaining--;
                }

                neighborIndex++;
            }

            return results;
        }

        bool IsValid(
            NeighborAccessibility neighborInfo
        )
        {
            if (!neighborInfo.IsAccessible)
            {
                return false;
            }

            switch (neighborInfo.Neighbor.Chunk.SpawnState)
            {
                case SpawnStates.Spawned:
                {
                    break;
                }
                case SpawnStates.Spawning:
                case SpawnStates.Despawned:
                case SpawnStates.Despawning:
                {
                    return false;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return true;
        }
    }
}