using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace equiavia.components.Library
{
    public static class EqComponents
    {
        public static void Initialize(IServiceCollection services)
        {
            services.AddScoped<TreeViewJSInterop, TreeViewJSInterop>();
        }
    }
}
