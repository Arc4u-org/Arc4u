using FluentValidation;
using FluentValidation.Resources;
using FluentValidation.Validators;

namespace Arc4u.FluentValidation.Rules;

internal sealed class IsUtcDateOnlyRuleValidator<T, TProperty> : PropertyValidator<T, TProperty> where T : class where TProperty : struct
{

    const string ruleName = "IsUtcDateOnlyRuleValidator";
    static IsUtcDateOnlyRuleValidator()
    {
        var lgMgr = ValidatorOptions.Global.LanguageManager as LanguageManager;
        lgMgr?.AddTranslation("en", ruleName, "Property must be a DateTime with no Time {PropertyValue}.");
    }

    public override string Name => ruleName;

    public override bool IsValid(ValidationContext<T> context, TProperty value)
    {
        var dt = value as DateTime?;

        return dt.HasValue && dt.Value.TimeOfDay.Equals(TimeSpan.Zero);

    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return Localized(errorCode, Name);
    }
}
