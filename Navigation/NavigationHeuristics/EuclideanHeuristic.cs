using Ostrander.Data;
using Lunra.Deep;
using UnityEngine;

namespace Ostrander.Navigation
{
    public class EuclideanHeuristic :
        Bindable,
        INavigationHeuristic
    {
        Cell goal;
        NavigationCostModifiers costModifiers;

        public void Initialize()
        {
            goal = Container.Get<NavigationRequestHandle>().End;
            costModifiers = Container.Get<NavigationCostModifiers>();
        }
        
        public float GetEstimate(
            Cell begin
        )
        {
            return Vector3Int.Distance(
                begin.Position,
                goal.Position
            );
        }

        public float GetEstimate(
            Cell begin,
            Cell end
        )
        {
            var cost = GetEstimate(begin);

            if (!begin.Position.TryGetDirectionTo(end.Position, out var direction))
            {
                // These two are not adjacent, so we can't estimate it properly
                return cost;
            }

            begin.GetCollisionTo(
                direction,
                out _,
                out var doorCollision,
                out var entityCollision
            );

            if (doorCollision != Collisions.None)
            {
                cost += costModifiers.Door;
            }

            if (entityCollision != Collisions.None)
            {
                cost += costModifiers.Entity;
            }

            return cost;
        }
    }
}