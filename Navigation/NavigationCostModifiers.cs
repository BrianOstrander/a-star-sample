namespace Ostrander.Navigation
{
    public class NavigationCostModifiers
    {
        public static NavigationCostModifiers Default { get; } = new()
        {
            Door = 10f,
            Entity = 5f,
        };
        
        public float Door;
        public float Entity;
    }
}