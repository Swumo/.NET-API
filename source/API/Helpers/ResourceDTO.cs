namespace Saitynai2.Helpers
{
    public record ResourceDTO<T>(T Resource, IReadOnlyCollection<LinkDTO> _links);
}

