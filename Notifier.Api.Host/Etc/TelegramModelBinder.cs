using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Notifier.Api.Host.Etc;

public class TelegramModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        using var reader = new StreamReader(bindingContext.HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);
        var value = JsonConvert.DeserializeObject(body, bindingContext.ModelType);
        
        bindingContext.Result = ModelBindingResult.Success(value);
    }
}