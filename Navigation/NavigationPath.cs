using System.Collections.Generic;
using Ostrander.Data;
using UnityEngine;

namespace Ostrander.Navigation
{
    public class NavigationPath
    {
        public List<NavigationConnection> Connections { get; }
        public NavigationPriorityList Open { get; }
        public NavigationPriorityList Closed { get; }

        public NavigationPath(
            List<NavigationConnection> connections,
            NavigationPriorityList open,
            NavigationPriorityList closed
        )
        {
            Connections = connections;
            Open = open;
            Closed = closed;
        }
        
        public void DebugDraw(
            int index = 0,
            Color? color = null,
            float duration = 0.017f
        )
        {
            for (var i = index; i < Connections.Count; i++)
            {
                Debug.DrawLine(
                    (Connections[i].Begin.Position + Vector3Directions.CellCenter).ToScene(),
                    (Connections[i].End.Position + Vector3Directions.CellCenter).ToScene(),
                    color ?? Color.white,
                    duration
                );
            }
        }
    }
}