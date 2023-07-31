using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gcpe.Hub.API.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MinistriesController : BaseController
    {
        private readonly HubDbContext dbContext;
        private readonly IMapper mapper;
        static DateTime? lastModified = null;
        static DateTime lastModifiedNextCheck = DateTime.Now;

        public MinistriesController(HubDbContext dbContext, ILogger<MinistriesController> logger, IMapper mapper) : base(logger)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        [HttpGet]
        [Produces(typeof(IEnumerable<Models.Ministry>))]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ResponseCache(Duration = 5)]
        public IActionResult GetAllMinistries()
        {
            try
            {
                IQueryable<Ministry> dbMinistries = dbContext.Ministry;

                IActionResult res = HandleModifiedSince(ref lastModified, ref lastModifiedNextCheck, () => dbMinistries.OrderByDescending(p => p.Timestamp).FirstOrDefault()?.Timestamp);
                return res ?? Ok(mapper.Map<List<Models.Ministry>>(dbMinistries.Where(m => (m.IsActive && !m.Abbreviation.Equals("JIC"))).OrderBy(m => m.SortOrder).ThenBy(m => m.DisplayName).ToList()));
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to retrieve ministries", ex);
            }
        }
    }
}
