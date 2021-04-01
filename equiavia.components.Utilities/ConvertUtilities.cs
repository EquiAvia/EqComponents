using System;
using System.Reflection;

namespace equiavia.components.Utilities
{
    public static class Convert
    {
        public static int IntTryParse(string inValue, int defaultValue)
        {
            int outValue;
            bool success = Int32.TryParse(inValue, out outValue);
            if (success)
            {
                return outValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static int? IntorNullTryParse(string inValue)
        {
            int outValue;
            bool success = Int32.TryParse(inValue, out outValue);
            if (success)
            {
                return outValue;
            }
            else
            {
                return null;
            }
        }


        //With thanks from https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}

