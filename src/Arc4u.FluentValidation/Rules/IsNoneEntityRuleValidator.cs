using Arc4u.Data;
using FluentValidation;
using FluentValidation.Resources;
using FluentValidation.Validators;

namespace Arc4u.FluentValidation.Rules;

/// <summary>
/// FluentValidation property validator that succeeds when the value is <see cref="PersistChange.None"/>. Registered by <c>IsNone</c>.
/// </summary>
/// <typeparam name="T">The type of the validated object.</typeparam>
/// <typeparam name="TProperty">The enum type of the property.</typeparam>
public class IsNoneEntityRuleValidator<T, TProperty> : PropertyValidator<T, TProperty> where T : IPersistEntity where TProperty : Enum
{
    const string ruleName = "IsNoneRuleValidator";
    /// <inheritdoc/>
    public override string Name => ruleName;

    static IsNoneEntityRuleValidator()
    {
        var lgMgr = ValidatorOptions.Global.LanguageManager as LanguageManager;
        lgMgr?.AddTranslation("en", ruleName, "PersistEntity is expected to be set as None and is {PropertyValue}.");
    }
    /// <inheritdoc/>
    public override bool IsValid(ValidationContext<T> context, TProperty value)
    {
        return value.Equals(PersistChange.None);
    }

    /// <inheritdoc/>
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return Localized(errorCode, Name);
    }
}
