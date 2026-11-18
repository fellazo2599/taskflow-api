using FluentValidation;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Validators;

public sealed class PaginationRmValidator : AbstractValidator<PaginationRm>
{
    public PaginationRmValidator()
    {
        RuleFor(pagination => pagination.Page).InclusiveBetween(1, PaginationRm.MaxPage);
        RuleFor(pagination => pagination.PageSize).InclusiveBetween(1, PaginationRm.MaxPageSize);
    }
}
