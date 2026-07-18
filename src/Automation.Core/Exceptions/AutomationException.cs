namespace Automation.Core.Exceptions;

/// <summary>Base type for all framework-raised errors. Never used for product assertions.</summary>
public class AutomationException : Exception
{
    public AutomationException(string message)
        : base(message)
    {
    }

    public AutomationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Raised when strongly typed options fail validation before the first test.</summary>
public sealed class ConfigurationValidationException : AutomationException
{
    public ConfigurationValidationException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when a requested capability is still configured with template placeholders.</summary>
public sealed class NotConfiguredException : AutomationException
{
    public NotConfiguredException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when a candidate SQL command violates the read-only safety model.</summary>
public sealed class UnsafeSqlException : AutomationException
{
    public UnsafeSqlException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when a computed artifact path would escape the run root.</summary>
public sealed class UnsafeArtifactPathException : AutomationException
{
    public UnsafeArtifactPathException(string message)
        : base(message)
    {
    }
}
