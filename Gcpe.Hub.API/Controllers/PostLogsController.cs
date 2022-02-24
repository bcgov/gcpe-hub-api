using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gcpe.Hub.API.Controllers
{
    [Route("/api/Posts/{newsreleaseid}/logs")]
    [ApiController]
    public class PostLogsController : BaseController
    {
        private readonly HubDbContext dbContext;
        private readonly IMapper mapper;
        public PostLogsController(HubDbContext dbContext,
          ILogger<PostLogsController> logger,
          IMapper mapper) : base(logger)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        [HttpGet("{postKey}")]
        [Produces(typeof(IEnumerable<Models.PostLog>))]
        [ResponseCache(Duration = 30)]
        public IActionResult GetPostLogs(string postKey)
        {
            var dbPost = dbContext.NewsRelease.Include(p => p.NewsReleaseLog).FirstOrDefault(p => p.Key == postKey);

            if (dbPost != null)
            {
                return Ok(mapper.Map<IEnumerable<Models.PostLog>>(dbPost.NewsReleaseLog));
            }
            return NotFound();
        }

        [HttpPost]
        public IActionResult AddPostLog([FromBody]Models.PostLog logEntry)
        {
            try
            {
                var dbPost = dbContext.NewsRelease.Include(p => p.NewsReleaseLog).FirstOrDefault(p => p.Key == logEntry.PostKey);
                var dbPostLog = new NewsReleaseLog { DateTime = DateTimeOffset.Now };
                dbContext.Entry(dbPostLog).CurrentValues.SetValues(logEntry);
                dbPost.NewsReleaseLog.Add(dbPostLog);
                dbContext.SaveChanges();
                return CreatedAtRoute("GetPostLogs", new { key = logEntry.PostKey }, mapper.Map<Models.PostLog>(dbPostLog));
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to save a new post log entry", ex);
            }
        }
    }
}
