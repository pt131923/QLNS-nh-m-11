
namespace API.Interfaces
{
    public class IMapper
    {
        public AutoMapper.IConfigurationProvider ConfigurationProvider { get; internal set; }
        public AutoMapper.IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            throw new NotImplementedException();
        }
    }
}