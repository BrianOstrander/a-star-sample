using Ostrander.Data;
using UnityEngine;

namespace Ostrander.Navigation
{
    public class NavigationConnection
    {
        public Cell Begin { get; }
        public Cell End { get; }
        public float Cost { get; }
        public Vector3 Normal => (End.Position - (Vector3)Begin.Position).normalized;
        public byte Direction => (End.Position - Begin.Position).ToCollision();

        public NavigationConnection(
            Cell begin,
            Cell end,
            float cost
        )
        {
            Begin = begin;
            End = end;
            Cost = cost;
        }
    }
}