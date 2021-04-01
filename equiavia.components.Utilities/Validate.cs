using System;
using System.Collections.Generic;
using System.Text;

namespace equiavia.components.Utilities 
{ 
    public class ValidationException:Exception
    {
       public ValidationException(String ErrorMessage):base(ErrorMessage)
       {
       }
    }

    public static class Validate
    {
        public static void IsTheSame(object valueToCheck, object valueToCheckAgainst)
        {
            if (!valueToCheck.Equals(valueToCheckAgainst))
            {
                throw new ValidationException("The values provided are not the same but are expected to be.");
            }
        }
        public static void IsNull(object valueToCheck)
        {
            if (valueToCheck != null)
            {
                throw new ValidationException("The value provided is expected to be null but is not.");
            }
        }

        public static void IsNotNull(object valueToCheck)
        {
            if (valueToCheck == null)
            {
                throw new ValidationException("The value provided is not allowed to be null.");
            }
        }

        public static void StringHasValue(string valueToCheck)
        {
            if (valueToCheck == null)
            {
                throw new ValidationException("The value provided is expected to have a value but contains a null.");
            }

            if(valueToCheck.Length == 0)
            {
                throw new ValidationException("The string provided is expected to have a value but is empty.");
            }
        }
    }
}
