using System;

namespace Ostrander.Navigation
{
    public class NavigationResult
    {
        public enum States
        {
            Requesting,
            Processing,
            CompletedWithValidPath,
            CompletedWithInvalidPath,
            CompletedWithCancellation,
            CompletedWithTimeout,
            CompletedWithException,
        }
        
        public States State { get; private set; }
        public NavigationRequest Request { get; private set; }
        public NavigationPath Path { get; private set; }
        public Exception Exception { get; private set; }

        public bool IsCompleted { get; private set; }
        public bool IsValid { get; private set; }
        public long MillisecondsElapsed { get; set; }
        
        public NavigationResult UpdateState(
            States state,
            NavigationRequest request,
            NavigationPath path,
            Exception exception
        )
        {
            State = state;
            Request = request;
            Path = path;
            Exception = exception;

            switch (State)
            {
                case States.CompletedWithValidPath:
                {
                    IsValid = true;
                    break;
                }
                case States.CompletedWithInvalidPath:
                case States.CompletedWithCancellation:
                case States.CompletedWithTimeout:
                case States.CompletedWithException:
                {
                    IsValid = false;
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return this;
        }

        public NavigationResult UpdateState(
            States state
        )
        {
            State = state;

            switch (State)
            {
                case States.Requesting:
                case States.Processing:
                {
                    break;
                }
                case States.CompletedWithValidPath:
                case States.CompletedWithInvalidPath:
                case States.CompletedWithCancellation:
                case States.CompletedWithTimeout:
                case States.CompletedWithException:
                {
                    IsCompleted = true;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return this;
        }

        public NavigationResult Complete()
        {
            IsCompleted = true;

            return this;
        }
    }
}