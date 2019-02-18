using FactorioWebInterface.Data.GitHub;
using FactorioWebInterface.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace FactorioWebInterface.Controllers
{
    public class GitHubController : ControllerBase
    {
        private readonly string filePath;

        public GitHubController(IConfiguration config)
        {
            filePath = config[Constants.GitHubCallbackFilePathKey];
        }

        [GitHubWebHook]
        public IActionResult GitHub(string id, string @event, JObject data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (@event == "push")
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok();
                }

                var push = data.ToObject<PushEvent>();
                string @ref = push.Ref;

                if (@ref.Length < 12)
                {
                    return Ok();
                }

                string branch = push.Ref.Substring(11);

                _ = ProcessHelper.RunProcessToEndAsync(filePath, branch);
            }

            return Ok();
        }
    }
}