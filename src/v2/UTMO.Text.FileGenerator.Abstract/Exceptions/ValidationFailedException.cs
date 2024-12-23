namespace UTMO.Text.FileGenerator.Abstract.Exceptions;

using System.Runtime.CompilerServices;

public class ValidationFailedException : ApplicationException
{
    public ValidationFailedException(string resourceName, string resourceTypeName, ValidationFailureType category, string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0) : base($"Validation of resource {resourceName} of type {resourceTypeName} failed. Failure Category: {category}. Failure Message: {message}")
    {
        this.ResourceName = resourceName;
        this.ResourceTypeName = resourceTypeName;
        this.Category = category;
        this.CallerFilePath = callerFilePath;
        this.CallerLineNumber = callerLineNumber;
    }
    
    public string ResourceName { get; set; }
    
    public string ResourceTypeName { get; set; }
    
    public ValidationFailureType Category { get; set; }
    
    public string CallerFilePath { get; set; }
    
    public int CallerLineNumber { get; set; }
}