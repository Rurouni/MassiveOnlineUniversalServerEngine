using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOUSE.Unity
{
    public enum FutureState
    {
        InProgress,
        Completed,
        Error
    }

    public class Future
    {
        public ushort ErrorCode { get; protected set; }
        public string Error { get; protected set; }
        public FutureState State { get; protected set; }
        public DateTime StartTime { get; protected set; }

        protected Action OnCompleted;
        protected Action<ushort, string> OnError;

        public Future(Action onCompleted, Action<ushort, string> onError)
        {
            State = FutureState.InProgress;
            StartTime = DateTime.UtcNow;
            OnCompleted = onCompleted;
            OnError = onError;
        }

        public Future() : this(null, null)
        {}

        public void SetCompleted()
        {
            State = FutureState.Completed;
            if (OnCompleted != null)
                OnCompleted();
        }

        public void SetError(ushort errorCode, string error)
        {
            State = FutureState.Error;
            Error = error;
            ErrorCode = errorCode;
            if (OnError != null)
                OnError(errorCode, error);
        }
    }

    public class Future<T> : Future
    {
        private T _result;

        public T Result 
        {
            get 
            {
                if(State == FutureState.Error)
                    throw new Exception(Error);
                else if(State == FutureState.InProgress)
                    throw new Exception("result is not ready yet, this API doesnt block so you should always check state of future first");
                else
                    return _result;
            }
        }

        new protected Action<T> OnCompleted;

        public Future(Action<T> onCompleted, Action<ushort, string> onError)
            : base(null, onError)
        {
            OnCompleted = onCompleted;
        }

        public Future()
            : base(null, null)
        {}

        public void SetResult(T val)
        {
            _result = val;
            SetCompleted();

            if (OnCompleted != null)
                OnCompleted(val);
        }
    }
}
