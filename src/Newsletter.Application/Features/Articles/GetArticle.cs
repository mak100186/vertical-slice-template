using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

using Newsletter.Application.Shared;
using Newsletter.Domain.Database;

namespace Newsletter.Application.Features.Articles;

public class ArticleResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? PublishedOnUtc { get; set; }
}

public static class GetArticle
{
    public class Query : IRequest<Result<ArticleResponse>>
    {
        public Guid Id { get; set; }
    }

    internal sealed class Handler : IRequestHandler<Query, Result<ArticleResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<ArticleResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var articleResponse = await _dbContext
                .Articles
                .AsNoTracking()
                .Where(article => article.Id == request.Id)
                .Select(article => new ArticleResponse
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    Tags = article.Tags,
                    CreatedOnUtc = article.CreatedOnUtc,
                    PublishedOnUtc = article.PublishedOnUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (articleResponse is null)
            {
                return Result.Failure<ArticleResponse>(new Error(
                    "GetArticle.Null",
                    "The article with the specified ID was not found"));
            }

            return articleResponse;
        }
    }
}

public class GetArticleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/articles/{id}", async (Guid id, ISender sender) =>
        {
            var query = new GetArticle.Query { Id = id };

            var result = await sender.Send(query);

            return result.IsFailure
                ? Results.NotFound(result.Error)
                : Results.Ok(result.Value);
        });
    }
}
