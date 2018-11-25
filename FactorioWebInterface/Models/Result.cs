using System.Collections.Generic;
using System.Text;

namespace FactorioWebInterface.Models
{
    public class Error
    {
        public string Key { get; }
        public string Description { get; }

        public Error(string key, string description = "")
        {
            Key = key;
            Description = description;
        }
    }

    public class Result
    {
        public static Result OK { get; } = new Result(true, new Error[0]);

        public static Result Failure(Error error) => new Result(false, new Error[] { error });
        public static Result Failure(IReadOnlyList<Error> errors) => new Result(false, errors);
        public static Result Failure(string key, string description = "") => Failure(new Error(key, description));

        public bool Success { get; }
        public IReadOnlyList<Error> Errors { get; }

        protected Result(bool success, IReadOnlyList<Error> errors)
        {
            Success = success;
            Errors = errors;
        }

        public override string ToString()
        {
            if (Success)
            {
                return "OK";
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var error in Errors)
                {
                    sb.Append(error.Key).Append(": ").AppendLine(error.Description);
                }
                return sb.ToString();
            }
        }
    }

    public class Result<T> : Result
    {
        public static new Result<T> OK(T value) => new Result<T>(value);
        public static new Result<T> Failure(Error error) => new Result<T>(new Error[] { error });
        public static new Result<T> Failure(IReadOnlyList<Error> errors) => new Result<T>(errors);
        public static new Result<T> Failure(string key, string description = "") => Failure(new Error(key, description));

        private Result(IReadOnlyList<Error> errors) : base(false, errors)
        {
        }

        private Result(T value) : base(true, new Error[0])
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}
