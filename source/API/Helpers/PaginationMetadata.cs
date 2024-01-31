namespace Saitynai2.Helpers
{
    public record PaginationMetadata(int totalCount, int pageSize, int currentPage, int totalPages, string? previousPageLink, string? nextPageLink);
}
