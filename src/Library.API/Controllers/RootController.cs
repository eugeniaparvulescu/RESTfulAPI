using Library.API.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api")]
    public class RootController : Controller
    {
        private readonly IUrlHelper _urlHelper;

        public RootController(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetRoot")]
        public IActionResult GetRoot([FromHeader(Name = "Accept")] string contentType)
        {
            if (contentType != "application/vnd.parvulescu.hateoas+json")
            {
                return NoContent();
            }

            var links = new List<LinkDto>();
            links.Add(new LinkDto(_urlHelper.Link("GetRoot", new { }), "self", "GET"));
            links.Add(new LinkDto(_urlHelper.Link("GetAuthors", new { }), "GetAuthors", "GET"));
            links.Add(new LinkDto(_urlHelper.Link("CreateAuthor", new { }), "CreateAuthor", "POST"));

            return Ok(links);
        }
    }
}
