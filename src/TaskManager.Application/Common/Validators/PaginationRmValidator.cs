using FluentValidation;
using TaskManager.Application.Common.Models;

namespace TaskManager.Application.Common.Validators;

public sealed class PaginationRmValidator : AbstractValidator<PaginationRm>
{
    public PaginationRmValidator()
    {
        RuleFor(pagination => pagination.Page).GreaterThanOrEqualTo(1);
        RuleFor(pagination => pagination.PageSize).InclusiveBetween(1, PaginationRm.MaxPageSize);
    }
}
