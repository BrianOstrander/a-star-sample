using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Ostrander.Data;
using Lunra.Deep;

namespace Ostrander.Navigation
{
    public class NavigationOperation
    {
        public NavigationRequestHandle Handle { get; private set; }
        public NavigationRequest Request => Handle.Request;
        public NavigationResult Result => Handle.Result;

        DependencyContainer operationContainer;
        GetNeighborArgs getNeighborArgs;
        NavigationCostModifiers costModifiers;
        INavigationHeuristic heuristic;
        NavigationGraph graph;

        NavigationNodeRecord beginRecord;
        NavigationPriorityList open;
        NavigationPriorityList closed;

        Stopwatch stopwatch;
        
        public void Initialize(NavigationRequestHandle handle)
        {
            operationContainer = new DependencyContainer();
            
            Handle = operationContainer.Bind(handle);
            
            getNeighborArgs = operationContainer.Bind(
                new GetNeighborArgs
                {
                    StrictDiagonals = true,
                    CardinalFilter = new NeighborFilter
                    {
                        IncludeBlockedByDoors = true,
                        IncludeBlockedByEntities = true,
                    }
                }
            );
            
            // TODO: These should probably be defined in the 
            costModifiers = operationContainer.Bind(Request.CostModifiers);
            heuristic = operationContainer.Bind(Request.Heuristic);
            
            graph = operationContainer.Bind(new NavigationGraph());
            
            heuristic.Initialize();
            graph.Initialize();
        }

        public void Process()
        {
            Result.UpdateState(NavigationResult.States.Processing);

            try
            {
                var isValid = OnProcess(out var path);

                Result.UpdateState(
                    isValid ? NavigationResult.States.CompletedWithValidPath : NavigationResult.States.CompletedWithInvalidPath,
                    Handle.Request,
                    path,
                    null
                );
            }
            catch (NavigationOperationCanceledException)
            {
                Result.UpdateState(
                    NavigationResult.States.CompletedWithCancellation,
                    Request,
                    null,
                    null
                );
            }
            catch (NavigationOperationTimeoutException)
            {
                Result.UpdateState(
                    NavigationResult.States.CompletedWithTimeout,
                    Request,
                    null,
                    null
                );
            }
            catch (Exception ex)
            {
                Result.UpdateState(
                    NavigationResult.States.CompletedWithException,
                    Request,
                    null,
                    ex
                );
            }
        }
        
        bool OnProcess(out NavigationPath pathResult)
        {
            // Initialize the record for the start node
            beginRecord = new NavigationNodeRecord();
            beginRecord.Node = Handle.Begin;
            beginRecord.EstimatedTotalCost = heuristic.GetEstimate(Handle.Begin);

            // Initialize the open and closed lists
            open = new NavigationPriorityList();
            open.Add(beginRecord);
            closed = new NavigationPriorityList();

            var currentRecord = new NavigationNodeRecord();

            stopwatch = Stopwatch.StartNew();
            
            // Iterate through processing each node
            while (0 < open.Count)
            {
                ThrowIfHalted();
                
                // Find the smallest element in the open list (using the estimatedTotalCost)
                currentRecord = open.GetLowestCostSoFar();

                // If it is the end node, then terminate
                if (currentRecord.Node == Handle.End)
                {
                    break;
                }

                // Otherwise get its outgoing connections
                var connections = graph.GetConnections(currentRecord.Node);
                
                // Loop through each connection in turn
                for (var connectionIndex = 0; connectionIndex < connections.Count; connectionIndex++)
                {
                    ThrowIfHalted();
                    
                    var connection = connections[connectionIndex];

                    // Get the cost estimate for the end node
                    var endNode = connection.End;
                    var endNodeCost = currentRecord.CostSoFar + connection.Cost;

                    NavigationNodeRecord endRecord;
                    float endEstimate;
                    
                    if (closed.TryGet(endNode, out endRecord))
                    {
                        // If the node is closed we may have to skip, or remove it from the closed list.
                        
                        // If we didn't find a shorter route, skip
                        if (endRecord.CostSoFar <= endNodeCost)
                        {
                            continue;
                        }
                        
                        // Otherwise remove it from the closed list
                        closed.Remove(endRecord);
                        
                        // We can use the node's old cost values to calculate its heuristic without calling the possibly
                        // expensive heuristic function
                        endEstimate = endRecord.EstimatedTotalCost - endRecord.CostSoFar;
                    }
                    else if (open.TryGet(endNode, out endRecord))
                    {
                        // Skip if the node is open and we've not found a better route
                        
                        // If our route is no better, then skip
                        if (endRecord.CostSoFar <= endNodeCost)
                        {
                            continue;
                        }
                        
                        // We can use the node's old cost values to calculate its heuristic without calling the possibly
                        // expensive heuristic function
                        endEstimate = endRecord.EstimatedTotalCost - endRecord.CostSoFar;
                    }
                    else
                    {
                        // Otherwise we know we've got an unvisited node, so make a record for it
                        endRecord = new NavigationNodeRecord();
                        endRecord.Node = endNode;
                        
                        // We'll need to calculate the heuristic value using the function, since we don't have an
                        // existing record to use
                        endEstimate = heuristic.GetEstimate(endNode);
                    }
                    
                    // We're here if we need to update the node
                    // Update the cost, estimate, and connection
                    endRecord.CostSoFar = endNodeCost;
                    endRecord.Connection = connection;
                    endRecord.EstimatedTotalCost = endNodeCost + endEstimate;

                    if (!open.Contains(endNode))
                    {
                        open.Add(endRecord);
                    }
                }
                
                // We've finished looking at the connections for the current node, so add it to the closed list and
                // remove it from the open list
                open.Remove(currentRecord);
                closed.Add(currentRecord);
            }
            
            // We're here if we've either found the goal, or if we have no more node's to search, find which.
            if (currentRecord.Node != Handle.End)
            {
                if (Handle.Request.IsDebugging)
                {
                    pathResult = new NavigationPath(
                        null,
                        open,
                        closed
                    );
                }
                else
                {
                    pathResult = new NavigationPath(
                        null,
                        null,
                        null
                    );
                }

                return false;
            }
            
            // Compile the list of connections in the path
            var path = new List<NavigationConnection>();

            while (currentRecord.Node != Handle.Begin)
            {
                path.Add(currentRecord.Connection);
                currentRecord = closed.Get(currentRecord.Connection.Begin);
            }

            // Reverse the path, and return it
            path.Reverse();
            pathResult = new NavigationPath(
                path,
                open,
                closed
            );

            return true;
        }

        void ThrowIfHalted()
        {
            if (Handle.IsCanceled)
            {
                throw new NavigationOperationCanceledException();
            }
            
            if (Request.MillisecondsTimeout <= stopwatch.ElapsedMilliseconds)
            {
                throw new NavigationOperationTimeoutException(
                    Request.MillisecondsTimeout,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }
    }
    
    public class NavigationOperationCanceledException : Exception {}

    public class NavigationOperationTimeoutException : Exception
    {
        public long MillisecondsTimeout { get; }
        public long MillisecondsElapsed { get; }
        
        public NavigationOperationTimeoutException(
            long millisecondsTimeout,
            long millisecondsElapsed
        )
        {
            MillisecondsTimeout = millisecondsTimeout;
            MillisecondsElapsed = millisecondsElapsed;
        }
    }
}