﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Gcpe.Hub.API.Data;
using Gcpe.Hub.API.Helpers;
using Gcpe.Hub.API.ViewModels;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gcpe.Hub.API.Controllers
{
    // TODO: Re-enable this ==> [Authorize]
    [Route("api/[Controller]")]
    [ApiController]
    [Produces("application/json")]
    public class NewsReleasesController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly ILogger<NewsReleasesController> _logger;
        private readonly IMapper _mapper;

        public NewsReleasesController(IRepository repository,
            ILogger<NewsReleasesController> logger,
            IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        [NonAction]
        public IEnumerable<NewsRelease> GetResultsPage(NewsReleaseParams newsReleaseParams)
        {
            var newsReleases = _repository.GetAllReleases();
            var pagedNewsReleases = PagedList<NewsRelease>.Create(newsReleases, newsReleaseParams.PageNumber, newsReleaseParams.PageSize);
            return pagedNewsReleases;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetAll([FromQuery] NewsReleaseParams newsReleaseParams)
        {
            try
            {
                var count = _repository.GetAllReleases().Count();
                var pagedNewsReleases = this.GetResultsPage(newsReleaseParams);
                Response.AddPagination(newsReleaseParams.PageNumber, newsReleaseParams.PageSize, count, 10);

                return Ok(pagedNewsReleases);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get releases: {ex}");
                return BadRequest("Failed to get releases");
            }
        }

        [HttpGet("{id}", Name = "GetRelease")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetById(string id)
        {
            try
            {
                var release = _repository.GetReleaseByKey(id);

                if (release != null)
                {
                    return Ok(_mapper.Map<NewsRelease, NewsReleaseViewModel>(release));
                }
                else return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get release: {ex}");
                return BadRequest("Failed to get release");
            }
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public IActionResult Post([FromBody]NewsReleaseViewModel model)
        {
            try
            {
                var newRelease = _mapper.Map<NewsReleaseViewModel, NewsRelease>(model);

                _repository.AddEntity(newRelease);
                // can assume that this always works against an in memory dataset
                return StatusCode(201);

                // can be un-commented when working with a db
                // return CreatedAtRoute("GetRelease", new { id = newRelease.Id }, newRelease);
                //if (_repository.SaveAll())
                //{
                //    return CreatedAtRoute("GetRelease", new { id = newRelease.Id }, newRelease);
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save a new release: {ex}");
            }

            return BadRequest("Failed to save new release");
        }

        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult Put(string id, [FromBody] NewsReleaseViewModel model)
        {
            try
            {
                if (_repository.GetReleaseByKey(id) == null)
                {
                    return NotFound($"Could not find a release with an id of {id}");
                }
                var release = _mapper.Map<NewsReleaseViewModel, NewsRelease>(model);
                _repository.Update(id, release);
                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return BadRequest("Couldn't update release");
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Delete(string id)
        {
            try
            {
                var oldRelease = _repository.GetReleaseByKey(id);
                if (oldRelease == null)
                {
                    return NotFound($"Could not find release with id of {id}");
                }
                _repository.Delete(oldRelease);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return BadRequest("Could not delete release");
        }

    }
}

