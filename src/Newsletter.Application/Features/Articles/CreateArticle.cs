using Carter;

using FluentValidation;

using Mapster;

using MediatR;

using Newsletter.Application.Shared;
using Newsletter.Domain.Database;
using Newsletter.Domain.Entities;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Newsletter.Application.Features.Articles;

public class CreateArticleRequest
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();
}

public static class CreateArticle
{
    public class Command : IRequest<Result<Guid>>
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Title).NotEmpty();
            RuleFor(c => c.Content).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<Guid>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return Result.Failure<Guid>(new Error(
                    "CreateArticle.Validation",
                    validationResult.ToString()));
            }

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Tags = request.Tags,
                CreatedOnUtc = DateTime.UtcNow
            };

            _dbContext.Add(article);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return article.Id;
        }
    }
}

public class CreateArticleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/articles", async (CreateArticleRequest request, ISender sender) =>
        {
            var command = request.Adapt<CreateArticle.Command>();

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result.Error)
                : Results.Ok(result.Value);
        });
    }
}
