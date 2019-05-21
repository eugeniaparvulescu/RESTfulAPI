using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper, 
            IPropertyMappingService propertyMappingService, ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters,
            [FromHeader(Name ="Accept")] string mediaType)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);
            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            if (mediaType == "application/vnd.parvulescu.hateoas+json")
            {
                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    totalPages = authorsFromRepo.TotalPages,
                    currentPage = authorsFromRepo.CurrentPage,
                    pageSize = authorsFromRepo.PageSize,
                };

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

                var links = CreateLinksForAuthors(authorsResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);
                var shapedAuthors = authors.ShapeData<AuthorDto>(authorsResourceParameters.Fields);
                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    authorAsDictionary.Add("links", CreateLinksForAuthor((Guid)authorAsDictionary["Id"], authorsResourceParameters.Fields));
                    return authorAsDictionary;
                });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links = links
                };

                return Ok(linkedCollectionResource);
            }

            var previousPageLink = authorsFromRepo.HasPrevious ?
                           CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) :
                           null;
            var nextPageLink = authorsFromRepo.HasNext ?
                CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) :
                null;

            var paginationMetadata2 = new
            {
                totalCount = authorsFromRepo.TotalCount,
                totalPages = authorsFromRepo.TotalPages,
                currentPage = authorsFromRepo.CurrentPage,
                pageSize = authorsFromRepo.PageSize,
                previousPageLink,
                nextPageLink
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata2));

            return Ok(authors.ShapeData<AuthorDto>(authorsResourceParameters.Fields));
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new {
                            orderBy = authorsResourceParameters.OrderBy,
                            genre = authorsResourceParameters.Genre,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            orderBy = authorsResourceParameters.OrderBy,
                            genre = authorsResourceParameters.Genre,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.Current:
                default:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            orderBy = authorsResourceParameters.OrderBy,
                            genre = authorsResourceParameters.Genre,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields, [FromHeader(Name = "Accept")] string mediaType)
        {
            if(!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            } 

            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            if (mediaType == "application/vnd.parvulescu.hateoas+json")
            {
                var links = CreateLinksForAuthor(id, fields);
                var linkedResourceToReturn = author.ShapeData<AuthorDto>(fields) as IDictionary<string, object>;
                linkedResourceToReturn.Add("links", links);

                return Ok(linkedResourceToReturn);
            }

            return Ok(author.ShapeData<AuthorDto>(fields));
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesContentType("Content-Type", new[] { "application/vnd.parvulescu.author.full+json", "application/vnd.parvulescu.author.full+xml" })]
        public IActionResult SaveAuthor([FromBody] AuthorForCreationDto author, [FromHeader(Name = "Accept")] string contentType)
        {
            if (author == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(author);

            _libraryRepository.AddAuthor(authorEntity);
            if(!_libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save.");
                //return StatusCode(500, "A problem happened with handling your request.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            if (contentType == "application/vnd.parvulescu.hateoas+json")
            {
                var links = CreateLinksForAuthor(authorToReturn.Id, null);
                var linkedAuthorToReturn = authorToReturn.ShapeData<AuthorDto>(null) as IDictionary<string, object>;
                linkedAuthorToReturn.Add("links", links);

                return CreatedAtRoute("GetAuthor", new { id = linkedAuthorToReturn["Id"] }, linkedAuthorToReturn);
            }

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn.ShapeData<AuthorDto>(null));
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesContentType("Content-Type", 
            new[] { "application/vnd.parvulescu.authorwithdateofdeath.full+json",
                    "application/vnd.parvulescu.authorwithdateofdeath.full+xml" })]
        //[RequestHeaderMatchesContentType("Accept", "")]
        public IActionResult SaveAuthorWithDateOfDeath([FromBody] AuthorForCreationWithDateOfDeathDto author, [FromHeader(Name = "Accept")] string contentType)
        {
            if (author == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(author);

            _libraryRepository.AddAuthor(authorEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save.");
                //return StatusCode(500, "A problem happened with handling your request.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            if (contentType == "application/vnd.parvulescu.hateoas+json")
            {
                var links = CreateLinksForAuthor(authorToReturn.Id, null);
                var linkedAuthorToReturn = authorToReturn.ShapeData<AuthorDto>(null) as IDictionary<string, object>;
                linkedAuthorToReturn.Add("links", links);

                return CreatedAtRoute("GetAuthor", new { id = linkedAuthorToReturn["Id"] }, linkedAuthorToReturn);
            }

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn.ShapeData<AuthorDto>(null));
        }

 
        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_libraryRepository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }

        [HttpDelete("{id}", Name = "DeleteAuthor")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }

            _libraryRepository.DeleteAuthor(authorFromRepo);
            if(!_libraryRepository.Save())
            {
                throw new Exception($"Deleting author { id } failed on save.");
            }

            return NoContent();
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            links.Add(new LinkDto(_urlHelper.Link("GetAuthor", new { id, fields }), "self", "GET"));
            links.Add(new LinkDto(_urlHelper.Link("DeleteAuthor", new { id }), "delete_author", "DELETE"));
            links.Add(new LinkDto(_urlHelper.Link("CreateBookForAuthor", new { authorId = id }), "create_book_for_author", "POST"));
            links.Add(new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { authorId = id }), "get_books_for_author", "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();

            links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current), "self", "GET"));
            if (hasNext)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage), "nextPage", "GET"));
            }
            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage), "previousPage", "GET"));
            }

            return links;
        }
    }
}
