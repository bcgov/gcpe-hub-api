﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Gcpe.Hub.API.ViewModels;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gcpe.Hub.API.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    [Produces("application/json")]
    public class SocialMediaController : ControllerBase
    {
        private readonly HubDbContext dbContext;
        private readonly ILogger<SocialMediaController> logger;
        private readonly IMapper mapper;

        public SocialMediaController(HubDbContext dbContext, ILogger<SocialMediaController> logger, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.logger = logger;
            this.mapper = mapper;
        }

        private string ErrorMessage(string operation, bool plural = false)
        {
            return $"Failed to {operation} social media post{(plural ? "(s)" : "")}";
        }

        [HttpGet]
        [Produces(typeof(SocialMediaPostViewModel[]))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            try
            {
                var posts = dbContext.SocialMediaPost.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
                return Ok(mapper.Map<List<SocialMediaPost>, List<SocialMediaPostViewModel>>(posts));
            }
            catch (Exception ex)
            {
                logger.LogError(ErrorMessage("retrieve", true) + $": {ex}");
                return BadRequest(ErrorMessage("retrieve", true));
            }
        }

        [HttpPost]
        [Produces(typeof(SocialMediaPostViewModel))]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public IActionResult Post(SocialMediaPostViewModel postVM)
        {
            try
            {
                if (postVM.Id != Guid.Empty)
                {
                    throw new ValidationException("Invalid parameter (id)");
                }
                SocialMediaPost post = mapper.Map<SocialMediaPostViewModel, SocialMediaPost>(postVM);
                post.Id = Guid.NewGuid();
                post.IsActive = true;
                dbContext.SocialMediaPost.Add(post);
                dbContext.SaveChanges();
                return CreatedAtRoute("GetPost", new { id = post.Id }, mapper.Map<SocialMediaPost, SocialMediaPostViewModel>(post));
            }
            catch (Exception ex)
            {
                logger.LogError(ErrorMessage("create") + $": {ex}");
                return BadRequest(ErrorMessage("create"));
            }
        }

        [HttpGet("{id}", Name = "GetPost")]
        [Produces(typeof(SocialMediaPostViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult Get(Guid id)
        {
            try
            {
                var post = dbContext.SocialMediaPost.Find(id);
                if (post != null && post.IsActive)
                {
                    return Ok(mapper.Map<SocialMediaPost, SocialMediaPostViewModel>(post));
                }
                return NotFound($"Social media post not found with id: {id}");
            }
            catch (Exception ex)
            {
                logger.LogError(ErrorMessage("retrieve") + $": {ex}");
                return BadRequest(ErrorMessage("retrieve"));
            }
        }

        [HttpPut("{id}")]
        [Produces(typeof(SocialMediaPostViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult Put(Guid id, SocialMediaPostViewModel postVM)
        {
            try
            {
                SocialMediaPost dbPost = dbContext.SocialMediaPost.Find(id);
                if (dbPost != null && dbPost.IsActive)
                {
                    dbPost = mapper.Map(postVM, dbPost);
                    dbPost.Timestamp = DateTime.Now;
                    dbPost.Id = id;
                    dbContext.SocialMediaPost.Update(dbPost);
                    dbContext.SaveChanges();
                    return Ok(mapper.Map<SocialMediaPost, SocialMediaPostViewModel>(dbPost));
                }
                return NotFound($"Social media post not found with id: {id}");
            }
            catch (Exception ex)
            {
                logger.LogError(ErrorMessage("update") + $": {ex}");
                return BadRequest(ErrorMessage("update"));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult Delete(Guid id)
        {
            try
            {
                SocialMediaPost dbPost = dbContext.SocialMediaPost.Find(id);
                if (dbPost != null && dbPost.IsActive)
                {
                    dbPost.IsActive = false;
                    dbPost.Timestamp = DateTime.Now;
                    dbContext.SocialMediaPost.Update(dbPost);
                    dbContext.SaveChanges();
                    return new NoContentResult();
                }
                return NotFound($"Social media post not found with id: {id}");
            }
            catch (Exception ex)
            {
                logger.LogError(ErrorMessage("delete") + $": {ex}");
                return BadRequest(ErrorMessage("delete"));
            }
        }
    }
}
