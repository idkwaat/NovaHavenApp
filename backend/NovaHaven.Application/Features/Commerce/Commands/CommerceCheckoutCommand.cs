using NovaHaven.Application.Commerce;

namespace NovaHaven.Application.Features.Commerce.Commands;

public sealed record CommerceCheckoutCommand(IReadOnlyList<CommerceCheckoutLineInput>? Items);
