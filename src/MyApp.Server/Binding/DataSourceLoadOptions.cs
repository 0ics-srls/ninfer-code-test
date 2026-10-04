using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyApp.Server.Binding;

[ModelBinder(BinderType = typeof(DataSourceLoadOptionsBinder))]
public sealed class DataSourceLoadOptions : DataSourceLoadOptionsBase
{
    /// <summary>Testable core: parse from a key→value getter (query-string style, JSON-encoded complex values).</summary>
    /// <remarks>Parser treats null and empty as "absent" (IsNullOrEmpty guards) — coalesce keeps zero-warning NRT flow.</remarks>
    public static DataSourceLoadOptions FromValues(Func<string, string?> valueGetter)
    {
        var options = new DataSourceLoadOptions();
        DataSourceLoadOptionsParser.Parse(options, key => valueGetter(key) ?? string.Empty);
        return options;
    }
}

public sealed class DataSourceLoadOptionsBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var options = DataSourceLoadOptions.FromValues(
            key => bindingContext.ValueProvider.GetValue(key).FirstOrDefault());
        bindingContext.Result = ModelBindingResult.Success(options);
        return Task.CompletedTask;
    }
}
