using System;
using System.Reflection;

namespace equiavia.components.Utilities
{

    public static class ClassIntrospect
    {

        /// <summary>
        /// https://stackoverflow.com/questions/1196991/get-property-value-from-string-using-reflection-in-c-sharp
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Object GetPropValue(this Object obj, String name)
        {
            foreach (String part in name.Split('.'))
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        public static T GetPropValue<T>(this Object obj, String name)
        {
            Object retval = GetPropValue(obj, name);
            if (retval == null) { return default(T); }

            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }

        public static bool HasProperty(this Object obj, String name)
        {
            Validate.IsNotNull(obj);
            Validate.IsNotNull(name);

            return obj.GetType().GetProperty(name) != null;
        }

        public static bool HasProperty(Type objType, String name)
        {
            Validate.IsNotNull(objType);
            Validate.IsNotNull(name);

            return objType.GetProperty(name) != null;
        }

        public static void SetPropValue<T>(this Object obj, String name, T value)
        {
            Validate.IsNotNull(obj);
            Validate.IsNotNull(name);

            var property = obj.GetType().GetProperty(name);
            if (property != null)
            {
                property.SetValue(obj, value);
            }
            else
            {
                Console.WriteLine(obj.GetType().Name + " does not have a property called " + name + ". Could not set the value.");
            }
        }
    }
}

