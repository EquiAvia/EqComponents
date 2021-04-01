using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace equiavia.components.Library
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class TreeViewJSInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public TreeViewJSInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
               "import", "./_content/Library/TreeViewJsInterop.js").AsTask());
        }

        public async Task<bool> ScrollToElement(string elementid)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<bool>("scrollToElementId", elementid);
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
