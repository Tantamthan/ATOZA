using Microsoft.EntityFrameworkCore;
using ATOZA.Infrastructure.Persistence;
using ATOZA.Application.Abstractions.Persistence;

namespace ATOZA.Test.Helpers;

public static class TestDbContextFactory
{
    public static ATOZADbContext Create()
    {
        var options = new DbContextOptionsBuilder<ATOZADbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ATOZADbContext(options);
    }
}