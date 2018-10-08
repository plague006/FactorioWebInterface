using System.Collections.Generic;

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
        private static readonly Result success = new Result(true, new Error[0]);

        public static Result OK => success;

        public static Result Failure(Error error) => new Result(false, new Error[] { error });
        public static Result Failure(IReadOnlyList<Error> errors) => new Result(false, errors);
        public static Result Failure(string key, string description = "") => Failure(new Error(key, description));

        public bool Success { get; }
        public IReadOnlyList<Error> Errors { get; }

        private Result(bool success, IReadOnlyList<Error> errors)
        {
            Success = success;
            Errors = errors;
        }
    }
}
