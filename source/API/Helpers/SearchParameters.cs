namespace Saitynai2.Helpers
{
    public class SearchParameters
    {
        private int _pageSize = 50;
        private int _pageNumber = 1;

        private const int MaxPageSize = 50;

        public int? PageNumber
        {
            get => _pageNumber <= 0 ? 1 : _pageNumber;
            set => _pageNumber = value ?? _pageNumber;
        }
        public int? PageSize
        {
            get => _pageSize > MaxPageSize ? MaxPageSize : _pageSize;
            set => _pageSize = value ?? _pageSize;
        }
    }
}
