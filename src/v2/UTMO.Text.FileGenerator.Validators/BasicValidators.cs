namespace UTMO.Text.FileGenerator.Validators;

using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;

public static class BasicValidators
{
    public static (ITemplateModel model, List<ValidationFailedException> errors) ValidateNotNull(this (ITemplateModel model, List<ValidationFailedException> errors) hook, object? value, string parameterName)
    {
        if (value is not null)
        {
            return hook;
        }
        
        var (model, errors) = hook;

        var error = new ValidationFailedException(model.ResourceName, model.ResourceTypeName, ValidationFailureType.InvalidResource, $"Parameter {parameterName} cannot be null.");
        errors.Add(error);

        return (model, errors);
    }
    
    public static (ITemplateModel model, List<ValidationFailedException> errors) ValidateStringNotNullOrEmpty(this (ITemplateModel model, List<ValidationFailedException> errors) hook, string? value, string parameterName)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return hook;
        } 
        
        var (model, errors) = hook;
        
        var error = new ValidationFailedException(model.ResourceName, model.ResourceTypeName, ValidationFailureType.InvalidResource, $"Parameter {parameterName} cannot be null or empty.");
        errors.Add(error);
        
        return (model, errors);
    }
    
    public static (ITemplateModel model, List<ValidationFailedException> errors) ValidateArrayNotNullOrEmpty<T>(this (ITemplateModel model, List<ValidationFailedException> errors) hook, IEnumerable<T>? value, string parameterName)
    {
        if (value is not null && value.Any())
        {
            return hook;
        }
        
        var (model, errors) = hook;

        var error = new ValidationFailedException(model.ResourceName, model.ResourceTypeName, ValidationFailureType.InvalidResource, $"Parameter {parameterName} cannot be null or empty.");
        errors.Add(error);

        return (model, errors);
    }
    
    public static (ITemplateModel model, List<ValidationFailedException> errors) ValidationBuilder(this ITemplateModel model) => new(model, new List<ValidationFailedException>());
    
    
}