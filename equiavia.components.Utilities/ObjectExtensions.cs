using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace equiavia.components.Utilities
{
    public static class ObjectExtensions
    {

        public static void ShallowCopyPropertiesTo(this Object obj, object target)
        {
            var fromProperties = obj.GetType().GetProperties();
            var toProperties = target.GetType().GetProperties();

            foreach (var fromProperty in fromProperties)
            {
                foreach (var toProperty in toProperties)
                {
                    if (fromProperty.Name == toProperty.Name && fromProperty.PropertyType == toProperty.PropertyType && toProperty.CanWrite)
                    {
                        toProperty.SetValue(target, fromProperty.GetValue(obj));
                        break;
                    }
                }
            }
        }

        public static void ShallowCopyPropertiesFrom(this Object obj, object source)
        {
            var fromProperties = source.GetType().GetProperties();
            var toProperties = obj.GetType().GetProperties();

            foreach (var fromProperty in fromProperties)
            {
                foreach (var toProperty in toProperties)
                {
                    if (fromProperty.Name == toProperty.Name && fromProperty.PropertyType == toProperty.PropertyType && toProperty.CanWrite)
                    {
                        toProperty.SetValue(obj, fromProperty.GetValue(source));
                        break;
                    }
                }
            }
        }
    }
}
