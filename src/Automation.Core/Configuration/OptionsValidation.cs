using System.ComponentModel.DataAnnotations;
using Automation.Core.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Automation.Core.Configuration;

/// <summary>
/// Binds and validates strongly typed options up front. All options MUST validate before the
/// first test runs (guide section 11); a placeholder URL is allowed, but sample integration
/// tests skip with an explicit reason until real values are supplied.
/// </summary>
public static class OptionsValidation
{
    /// <summary>Binds <paramref name="section"/> to <typeparamref name="T"/> and validates data annotations.</summary>
    public static T BindAndValidate<T>(IConfiguration configuration, string section)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        var instance = new T();
        configuration.GetSection(section).Bind(instance);
        Validate(instance, section);
        return instance;
    }

    /// <summary>Validates an already-constructed options object, throwing on any violation.</summary>
    public static void Validate<T>(T instance, string context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(instance);
        if (!Validator.TryValidateObject(instance, validationContext, results, validateAllProperties: true))
        {
            string details = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ConfigurationValidationException(
                $"Configuration section '{context}' is invalid: {details}");
        }
    }
}
