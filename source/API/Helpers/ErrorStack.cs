using Microsoft.AspNetCore.Identity;
using System;
using System.Dynamic;

namespace Saitynai2.Helpers
{
    public class ErrorStack
    {

        public static Object CreateErrorStackFromIdentityErrors(IEnumerable<IdentityError> errors)
        {
            dynamic errorsObject = new ExpandoObject();
            errorsObject.errors = new List<string>();
            foreach (var error in errors)
            {
                errorsObject.errors.Add(error.Description);
            }
            return errorsObject;
        }
    }
}
