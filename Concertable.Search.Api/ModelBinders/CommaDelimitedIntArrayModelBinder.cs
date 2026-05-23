using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Concertable.Search.Api.ModelBinders;

internal class CommaDelimitedIntArrayModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(Array.Empty<int>());
            return Task.CompletedTask;
        }

        try
        {
            var result = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToArray();

            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (FormatException)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid integer value in comma-delimited list.");
        }

        return Task.CompletedTask;
    }
}

internal class CommaDelimitedIntArrayBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(int[]))
            return new CommaDelimitedIntArrayModelBinder();

        if (context.Metadata.ModelType == typeof(Genre[]))
            return new CommaDelimitedGenreArrayModelBinder();

        return null;
    }
}

internal class CommaDelimitedGenreArrayModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(Array.Empty<Genre>());
            return Task.CompletedTask;
        }

        try
        {
            var result = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var i) ? (Genre)i : Enum.Parse<Genre>(s))
                .ToArray();

            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid genre value in comma-delimited list.");
        }

        return Task.CompletedTask;
    }
}
