using Library.API.AutoMapperConfiguration.Profiles;

namespace Library.API.AutoMapperConfiguration
{
    public class AutoMapperConfiguration
    {
        public static void Initialize()
        {
            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new AuthorProfile());
                cfg.AddProfile(new BookProfile());
            });
        }
    }
}
