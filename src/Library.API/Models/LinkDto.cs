namespace Library.API.Models
{
    public class LinkDto
    {
        public string Url { get; set; }
        public string Rel { get; set; }
        public string Method { get; set; }

        public LinkDto(string url, string rel, string method)
        {
            Url = url;
            Rel = rel;
            Method = method;
        }
    }
}
