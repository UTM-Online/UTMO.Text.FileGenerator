namespace UTMO.Text.FileGenerator.Abstract;

public enum ValidationFailureType
{
    InvalidResource,
    InvalidResourceType,
    InvalidCategory,
    InvalidMessage,
    InvalidConfiguration,
    InternalError,
    RequiredPropertyMissing,
    MissingRequiredField,
    InvalidFormat,
}