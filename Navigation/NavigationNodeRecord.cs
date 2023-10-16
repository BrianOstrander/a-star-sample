using Ostrander.Data;

namespace Ostrander.Navigation
{
    public class NavigationNodeRecord
    {
        public Cell Node { get; set; }
        public NavigationConnection Connection { get; set; }
        public float CostSoFar { get; set; }
        public float EstimatedTotalCost { get; set; }

        public override string ToString()
        {
            var result = $"{nameof(NavigationNodeRecord)} for {Node}";
            result += $"\n\t{nameof(CostSoFar)} : {CostSoFar:N2}";
            result += $"\n\t{nameof(EstimatedTotalCost)} : {EstimatedTotalCost:N2}";
            result += $"\n\t{nameof(Connection)} : {Connection}";

            return result;
        }
    }
}