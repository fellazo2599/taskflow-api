namespace TaskManager.Application.Common.Constants;

// Rule suffix of a validation error code: FIELD_RULE (e.g. TITLE_REQUIRED).
public static class FieldErrorCodes
{
    public const string Required = "REQUIRED";
    public const string Invalid = "INVALID";
    public const string TooLong = "TOO_LONG";
    public const string OutOfRange = "OUT_OF_RANGE";
    public const string InPast = "IN_PAST";
}
