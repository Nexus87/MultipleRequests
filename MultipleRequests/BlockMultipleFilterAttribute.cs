using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MultipleRequests;

public class BlockMultipleFilterAttribute : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<string, Task<IActionResult?>> RunningRequests = new();
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var url = context.HttpContext.Request.GetDisplayUrl();
        var task = RunningRequests.GetOrAdd(url, async key =>
        {
            try
            {
                var result = await next();
                return result.Result;
            }
            finally
            {
                RunningRequests.TryRemove(key, out _);
            }
        });

        var response = await task;
        context.Result = response;
    }
}