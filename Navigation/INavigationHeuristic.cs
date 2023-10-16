using Ostrander.Data;

namespace Ostrander.Navigation
{
    public interface INavigationHeuristic
    {
        public void Initialize();
        
        public float GetEstimate(
            Cell begin
        );
        
        public float GetEstimate(
            Cell begin,
            Cell end
        );
    }
}