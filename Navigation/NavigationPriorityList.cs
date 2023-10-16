using System;
using System.Collections.Generic;
using Ostrander.Data;

namespace Ostrander.Navigation
{
    public class NavigationPriorityList
    {
        List<NavigationNodeRecord> records = new();

        public int Count => records.Count;
        
        public NavigationNodeRecord GetLowestCostSoFar()
        {
            return records[0];
        }
        
        public void Add(
            NavigationNodeRecord record
        )
        {
            var index = 0;

            while (index < records.Count)
            {
                if (record.CostSoFar < records[index].CostSoFar)
                {
                    break;
                }
                
                index++;
            }
            
            records.Insert(
                index,
                record
            );
        }
        
        public void Remove(
            NavigationNodeRecord record
        )
        {
            if (!records.Remove(record))
            {
                throw new CannotRemoveMissingRecordFromPriorityListException(record);
            }
        }

        public bool TryGet(
            Cell node,
            out NavigationNodeRecord record
        )
        {
            for (var index = 0; index < records.Count; index++)
            {
                var current = records[index];
                
                if (current.Node == node)
                {
                    record = current;
                    return true;
                }
            }

            record = null;
            return false;
        }
        
        public NavigationNodeRecord Get(
            Cell node
        )
        {
            if (TryGet(node, out var record))
            {
                return record;
            }

            throw new CannotFindCellInPriorityListException(node);
        }

        public void Clear() => records.Clear();

        public bool Contains(
            Cell node
        )
        {
            for (var index = 0; index < records.Count; index++)
            {
                var current = records[index];
                
                if (current.Node == node)
                {
                    return true;
                }
            }

            return false;
        }
        
        public IEnumerable<NavigationNodeRecord> All()
        {
            using var enumerator = records.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    public class CannotFindCellInPriorityListException : Exception
    {
        public Cell Cell { get; }

        public override string Message => $"Cannot find a {nameof(NavigationNodeRecord)} with cell {Cell} in {nameof(NavigationPriorityList)}";

        public CannotFindCellInPriorityListException(
            Cell cell
        )
        {
            Cell = cell;
        }
    }

    public class CannotRemoveMissingRecordFromPriorityListException : Exception
    {

        public NavigationNodeRecord Record { get; }

        public override string Message => $"Cannot remove record {Record} from {nameof(NavigationPriorityList)}, it does not exist.";
        
        public CannotRemoveMissingRecordFromPriorityListException(
            NavigationNodeRecord record
        )
        {
            Record = record;
        }
    }
}