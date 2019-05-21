﻿using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<BooksController> _logger;
        private readonly IUrlHelper _urlHelper;

        public BooksController(ILibraryRepository libraryRepository, ILogger<BooksController> logger, IUrlHelper urlHelper)
        {
            _libraryRepository = libraryRepository;
            _logger = logger;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var booksFromRepo = _libraryRepository.GetBooksForAuthor(authorId);
            var books = Mapper.Map<IEnumerable<BookDto>>(booksFromRepo);
            books = books.Select(book => { book = CreateLinksForAuthorBook(book); return book; });
            var booksWrapper = new LinkedCollectionResourceWrapperDto<BookDto>(books);

            return Ok(CreateLinksForAuthorBooks(booksWrapper));
        }

        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookFromRepo == null)
            {
                return NotFound();
            }

            var book = Mapper.Map<BookDto>(bookFromRepo);

            return Ok(CreateLinksForAuthorBook(book));
        }

        [HttpPost(Name = "CreateBookForAuthor")]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }
            if (book.Title == book.Description)
            {
                ModelState.AddModelError(nameof(BookForCreationDto), "The provided description should be different from the title.");
            }
            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookEntity = Mapper.Map<Book>(book);

            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"Creating a book for author {authorId} failed on save.");
            }

            var bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, CreateLinksForAuthorBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthor = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthor == null)
            {
                return NotFound();
            }

            _libraryRepository.DeleteBook(bookForAuthor);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"Deleting book {id} for author {authorId} failed on save.");
            }

            _logger.LogInformation($"Book {id} for author {authorId} was deleted.");

            return NoContent();
        }

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }
            if (book.Title == book.Description)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");
            }
            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Upserting book { id } for author { authorId } failed on save.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId, id }, bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id, [FromBody]JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto, ModelState);

                TryValidateModel(bookDto);
                if (bookDto.Title == bookDto.Description)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be fifferent from the title.");
                }
                if (!ModelState.IsValid)
                {
                    return new UnprocessableEntityObjectResult(ModelState);
                }

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Upserting book { id } for author { authorId } failed on save.");
                }

                return CreatedAtRoute("GetBookForAuthor", new { authorId, id }, bookDto);
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);
            patchDoc.ApplyTo(bookToPatch, ModelState);

            TryValidateModel(bookToPatch);
            if (bookToPatch.Title == bookToPatch.Description)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be fifferent from the title.");
            }
            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);
            if (!_libraryRepository.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }

        private BookDto CreateLinksForAuthorBook(BookDto bookDto)
        {
            bookDto.Links.Add(new LinkDto(_urlHelper.Link("GetBookForAuthor", new { id = bookDto.Id }), "self", "GET"));
            bookDto.Links.Add(new LinkDto(_urlHelper.Link("DeleteBookForAuthor", new { id = bookDto.Id }), "delete_book", "DELETE"));
            bookDto.Links.Add(new LinkDto(_urlHelper.Link("UpdateBookForAuthor", new { id = bookDto.Id }), "update_book", "POST"));
            bookDto.Links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor", new { id = bookDto.Id }), "partially_update_book", "PATCH"));

            return bookDto;
        }

        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForAuthorBooks(LinkedCollectionResourceWrapperDto<BookDto> books)
        {
            books.Links.Add(new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { }), "self", "GET"));

            return books;
        }
    }
}
