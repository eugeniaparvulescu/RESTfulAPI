using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API
{
    public class LibraryContextDbContextFactory : IDesignTimeDbContextFactory<LibraryContext>
    {
        public LibraryContext CreateDbContext(string[] args)
        {
            //IConfiguration configuration = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("appsettings.json")
            //    .Build();
            var builder = new DbContextOptionsBuilder<LibraryContext>();
           // var connectionString = configuration["connectionStrings: libraryDBConnectionString"];//.GetConnectionString("connectionStrings");
            builder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LibraryDB;Trusted_Connection=True;");
            return new LibraryContext(builder.Options);
        }
    }
}
