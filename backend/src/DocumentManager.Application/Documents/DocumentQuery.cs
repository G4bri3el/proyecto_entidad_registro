using DocumentManager.Application.Common;

namespace DocumentManager.Application.Documents;

public sealed record DocumentQuery(int Page = 1, int PageSize = 25) : PageRequest(Page, PageSize);
