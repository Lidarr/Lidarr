using System;
using Microsoft.AspNetCore.Mvc;

namespace Lidarr.Http.REST.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RestDeleteByIdAttribute : HttpDeleteAttribute
    {
        public RestDeleteByIdAttribute()
            : base("{id:int}")
        {
        }
    }
}
