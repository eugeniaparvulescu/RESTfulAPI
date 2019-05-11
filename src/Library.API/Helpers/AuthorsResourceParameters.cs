namespace Library.API.Helpers
{
    public class AuthorsResourceParameters
    {
        private const int maxPageSize = 20;
        private int pageSize = 10;

        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get { return pageSize; }
            set { pageSize = (value > maxPageSize) ? maxPageSize : value; }
        }
        public string Genre { get; set; }
        public string SearchQuery { get; set; }
        public string OrderBy { get; set; } = "Name";
        public string Fields { get; set; }
    }
}
