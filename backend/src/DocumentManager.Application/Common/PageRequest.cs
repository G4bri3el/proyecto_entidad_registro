namespace DocumentManager.Application.Common;

public record PageRequest(int Page = 1, int PageSize = 25)
{
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
}
