using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gcpe.Hub.API.Controllers
{
    /// <summary>
    /// The base class for all API controllers
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger logger;

        /// <summary>
        /// Construct the BaseController, it requires an ILogger object
        /// </summary>
        /// <param name="logger"></param>
        protected BaseController(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// HandleModifiedSince, caching support
        /// </summary>
        /// <param name="model"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        protected IActionResult HandleModifiedSince(ref DateTime? lastModified, ref DateTime lastModifiedNextCheck, Func<DateTime?> lastModifiedCheckFn)
        {
            var now = DateTime.Now;
            ResponseHeaders responseHeaders = Response?.GetTypedHeaders();
            var cacheMaxAge = responseHeaders?.CacheControl?.MaxAge;

            // Response cache headers may not be populated yet when this runs.
            if (lastModifiedNextCheck <= now && cacheMaxAge.HasValue)
            {
                lastModified = lastModifiedCheckFn();
                lastModifiedNextCheck = now.Add(cacheMaxAge.Value);
            }
            if (lastModified.HasValue)
            {
                var modifiedSpan = lastModified - Request.GetTypedHeaders().IfModifiedSince;

                // Ignore milliseconds because browsers are not supposed to store them (HTTP-date: https://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1)
                if (modifiedSpan.HasValue && Math.Abs(modifiedSpan.Value.TotalMilliseconds) < 1000)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
                responseHeaders.LastModified = lastModified;
            }
            return null;
        }

        protected IActionResult BadRequest(string error, Exception ex)
        {
            logger.LogError(error + ": " + ex.ToString());
            return BadRequest(error);
        }
    }
}
