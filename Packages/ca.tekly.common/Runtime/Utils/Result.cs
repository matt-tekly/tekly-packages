namespace Tekly.Common.Utils
{
    /// <summary>
    /// Stores the result of some operation. Commonly used to store the result of asynchronous operations.
    /// </summary>
    public class Result
    {
        public bool Success => ErrorCode == 0 && Error == null;
        public readonly string Error;
        public readonly int ErrorCode;

        public bool Failure => !Success;

        protected Result(string error = null, int errorCode = 0)
        {
            Error = error;
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            if (Success)
            {
                return "[Success]";
            }

            return $"[Failure][{ErrorCode}] " + Error;
        }

        public static Result Okay()
        {
            return new Result();
        }

        public static Result<T> Okay<T>(T data)
        {
            return new Result<T>(data);
        }

        public static Result Fail(string error, int errorCode = 0)
        {
            return new Result(error, errorCode);
        }

        public static Result<T> Fail<T>(string error, int errorCode = 0)
        {
            return new Result<T>(error, errorCode);
        }

        public static Result<T> Fail<T>(Result result)
        {
            return new Result<T>(result.Error, result.ErrorCode);
        }
    }

    public class Result<T> : Result
    {
        public readonly T Data;

        public Result(T data)
        {
            Data = data;
        }

        public Result(string error = null, int errorCode = 0) : base(error, errorCode)
        {
        }
    }
}