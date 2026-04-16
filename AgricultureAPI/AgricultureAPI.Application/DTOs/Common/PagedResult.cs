namespace AgricultureAPI.Application.DTOs.Common;

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int Limit { get; set; }
    public long Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / Limit);
}