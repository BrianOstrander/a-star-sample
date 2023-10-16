using System;
using Ostrander.Data;

namespace Ostrander.Navigation
{
    public class NavigationRequestHandle
    {
        public NavigationRequest Request { get; }
        public Cell Begin { get; }
        public Cell End { get; }
        public NavigationResult Result { get; }
        
        public bool IsCanceled { get; private set; }

        public NavigationRequestHandle(
            NavigationRequest request,
            Cell begin,
            Cell end,
            NavigationResult result
        )
        {
            Request = request;
            Begin = begin;
            End = end;
            Result = result;
        }
        
        public void Cancel()
        {
            IsCanceled = true;
        }
    }
}