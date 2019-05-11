using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Library.API.Helpers
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var expandoObjectList = new List<ExpandoObject>();
            var propertyInfoList = new List<PropertyInfo>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                var fieldsAfterSplit = fields.Split(",");
                foreach(var field in fieldsAfterSplit)
                {
                    var propertyName = field.Trim();

                    var propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} was not found on type {typeof(TSource)}");
                    }

                    propertyInfoList.Add(propertyInfo);
                }
            }

            foreach(TSource sourceObject in source)
            {
                var dataShapedObject = new ExpandoObject();
                foreach(var property in propertyInfoList)
                {
                    var propertyValue = property.GetValue(sourceObject);
                    ((IDictionary<string, object>)dataShapedObject).Add(property.Name, propertyValue);
                }

                expandoObjectList.Add(dataShapedObject);
            }

            return expandoObjectList;
        }
    }
}
