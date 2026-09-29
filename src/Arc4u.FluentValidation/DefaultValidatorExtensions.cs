using Arc4u.Data;
using Arc4u.FluentValidation.Rules;
using Arc4u.Results.Validation;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Severity = FluentValidation.Severity;

namespace Arc4u.Validation;

/// <summary>
/// FluentValidation extensions for Arc4u: rules on the <see cref="PersistChange"/> state of an <see cref="IPersistEntity"/>, rules on <see cref="DateTime"/> properties,
/// and the bridge between a FluentValidation validation and a FluentResults <see cref="Result{TValue}"/>.
/// </summary>
/// <example>
/// <code>
/// public class OrderValidator : AbstractValidator&lt;Order&gt;
/// {
///     public OrderValidator()
///     {
///         RuleFor(o =&gt; o.PersistChange).IsInsert();
///         RuleFor(o =&gt; o.CreatedOn).IsUtcDateTime();
///     }
/// }
///
/// Result&lt;Order&gt; result = await new OrderValidator().ValidateWithResultAsync(order);
/// </code>
/// </example>
public static class DefaultValidatorExtensions
{
    /// <summary>
    /// Requires the property, which holds a <see cref="PersistChange"/>, to be <see cref="PersistChange.Insert"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The enum type of the property.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options, to chain further configuration.</returns>
    public static IRuleBuilderOptions<T, TProperty> IsInsert<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder) where T : IPersistEntity where TProperty : Enum
    {
        return ruleBuilder.SetValidator(new IsInsertEntityRuleValidator<T, TProperty>());
    }

    /// <summary>
    /// Requires the property, which holds a <see cref="PersistChange"/>, to be <see cref="PersistChange.Update"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The enum type of the property.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options, to chain further configuration.</returns>
    public static IRuleBuilderOptions<T, TProperty> IsUpdate<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder) where T : IPersistEntity where TProperty : Enum
    {
        return ruleBuilder.SetValidator(new IsUpdateEntityRuleValidator<T, TProperty>());
    }

    /// <summary>
    /// Requires the property, which holds a <see cref="PersistChange"/>, to be <see cref="PersistChange.Delete"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The enum type of the property.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options, to chain further configuration.</returns>
    public static IRuleBuilderOptions<T, TProperty> IsDelete<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder) where T : IPersistEntity where TProperty : Enum
    {
        return ruleBuilder.SetValidator(new IsDeleteEntityRuleValidator<T, TProperty>());
    }

    /// <summary>
    /// Requires the property, which holds a <see cref="PersistChange"/>, to be <see cref="PersistChange.None"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The enum type of the property.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options, to chain further configuration.</returns>
    public static IRuleBuilderOptions<T, TProperty> IsNone<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder) where T : IPersistEntity where TProperty : Enum
    {
        return ruleBuilder.SetValidator(new IsNoneEntityRuleValidator<T, TProperty>());
    }

    /// <summary>
    /// Requires the property to be a <see cref="DateTime"/> whose <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    /// <remarks>The rule fails when the value is not a <see cref="DateTime"/> (or a nullable <see cref="DateTime"/>) or is <see langword="null"/>.</remarks>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options, to chain further configuration.</returns>
    public static IRuleBuilderOptions<T, TProperty> IsUtcDateTime<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder) where T : class where TProperty : struct
    {
        return ruleBuilder.SetValidator(new IsUtcDateTimeRuleValidator<T, TProperty>());
    }

    /// <summary>
    /// Requires the property to be a <see cref="DateTime"/> with no time part (<see cref="DateTime.TimeOfDay"/> is zero).
    /// </summary>
    /// <remarks>The rule fails when the value is not a <see cref="DateTime"/> (or a nullable <see cref="DateTime"/>) or is <see langword="null"/>. The <see cref="DateTime.Kind"/> is not checked.</remarks>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>The rule builder options, to chain further configuration.</returns>
    public static IRuleBuilderOptions<T, TProperty> IsDateOnly<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder) where T : class where TProperty : struct
    {
        return ruleBuilder.SetValidator(new IsUtcDateOnlyRuleValidator<T, TProperty>());
    }

    /// <summary>
    /// Builds a dictionary describing a FluentValidation failure with the keys <c>Code</c>, <c>State</c>, <c>PropertyName</c> and <c>Severity</c>.
    /// </summary>
    /// <param name="failure">The failure to describe.</param>
    /// <returns>The dictionary, filled from the error code, custom state, property name and severity of the failure.</returns>
    public static Dictionary<string, object> ToMetadata(this ValidationFailure failure)
    {
        var metadata = new Dictionary<string, object>
        {
            { "Code", failure.ErrorCode },
            { "State", failure.CustomState },
            { "PropertyName", failure.PropertyName },
            { "Severity", failure.Severity }
        };
        return metadata;
    }

    /// <summary>
    /// Converts FluentValidation failures to Arc4u <see cref="ValidationError"/>s.
    /// </summary>
    /// <param name="failures">The failures to convert.</param>
    /// <returns>One <see cref="ValidationError"/> per failure, converted by <see cref="ToValidationError(ValidationFailure)"/>.</returns>
    public static List<ValidationError> ToResultErrors(this IEnumerable<ValidationFailure> failures)
    {
        return failures.Select(failure => failure.ToValidationError()).ToList();
    }

    /// <summary>
    /// Converts a FluentValidation failure to an Arc4u <see cref="ValidationError"/>.
    /// </summary>
    /// <remarks>The message, the error code and the severity are carried over; FluentValidation <c>Info</c> severities map to <see cref="Arc4u.Results.Validation.Severity.Info"/>.</remarks>
    /// <param name="failure">The failure to convert.</param>
    /// <returns>The validation error.</returns>
    public static ValidationError ToValidationError(this ValidationFailure failure)
    {
        var error = ValidationError.Create(failure.ErrorMessage)
                                   .WithSeverity(failure.Severity.ToSeverity())
                                   .WithCode(failure.ErrorCode);

        return error;
    }

    private static Results.Validation.Severity ToSeverity(this Severity severity)
    {
        return severity switch
        {
            Severity.Error => Results.Validation.Severity.Error,
            Severity.Warning => Results.Validation.Severity.Warning,
            _ => Arc4u.Results.Validation.Severity.Info
        };
    }

    /// <summary>
    /// Validates a value asynchronously and returns the outcome as a <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated value.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>A successful result carrying <paramref name="value"/> when it is valid; otherwise a failed result holding one <see cref="ValidationError"/> per failure.</returns>
    public static async ValueTask<Result<T>> ValidateWithResultAsync<T>(this IValidator<T> validator, T value)
    {
        return await validator.ValidateWithResultAsync(value, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates a value asynchronously and returns the outcome as a <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated value.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="cancellation">A token to cancel the validation.</param>
    /// <returns>A successful result carrying <paramref name="value"/> when it is valid; otherwise a failed result holding one <see cref="ValidationError"/> per failure.</returns>
    public static async ValueTask<Result<T>> ValidateWithResultAsync<T>(this IValidator<T> validator, T value, CancellationToken cancellation)
    {
        var validationResult = await validator.ValidateAsync(value, cancellation).ConfigureAwait(false);

        if (validationResult.IsValid)
        {
            return Result.Ok(value);
        }

        return Result.Fail(validationResult.Errors.ToResultErrors());
    }

    /// <summary>
    /// Validates a value synchronously and returns the outcome as a <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the validated value.</typeparam>
    /// <param name="validator">The validator to run.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>A successful result carrying <paramref name="value"/> when it is valid; otherwise a failed result holding one <see cref="ValidationError"/> per failure.</returns>
    public static Result<T> ValidateWithResult<T>(this IValidator<T> validator, T value)
    {
        var validationResult = validator.Validate(value);

        if (validationResult.IsValid)
        {
            return Result.Ok(value);
        }

        return Result.Fail(validationResult.Errors.ToResultErrors());
    }

}
