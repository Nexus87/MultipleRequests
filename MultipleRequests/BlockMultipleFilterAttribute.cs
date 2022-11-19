using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MultipleRequests;

public class BlockMultipleFilterAttribute : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<IActionResult?>>> RunningRequests = new();
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method != HttpMethods.Get)
        {
            await next();
            return;
        }
        var url = context.HttpContext.Request.GetDisplayUrl();
        var task = RunningRequests.GetOrAdd(url, CreateLazy(url, next));

        var response = await task.Value;
        context.Result = response;
    }

    private static Lazy<Task<IActionResult?>> CreateLazy(string key, ActionExecutionDelegate next) =>
        new(async () =>
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
}