using System.Reflection;

namespace Library.API.Services
{
    public class TypeHelperService : ITypeHelperService
    {
        public bool TypeHasProperties<T>(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            var fieldsAfterSplit = fields.Split(",");
            foreach (var field in fieldsAfterSplit)
            {
                var trimmedField = field.Trim();
                var propertyInfo = typeof(T).GetProperty(trimmedField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
