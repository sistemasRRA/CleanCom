using CleanCom.Application.Crm;

namespace CleanCom.Tests.Crm;

/// <summary>Pages of the Clientes (CRM) list.</summary>
public class CrmPagingTests
{
    private static readonly int[] Rows = Enumerable.Range(1, 120).ToArray();

    [Fact]
    public void The_second_page_of_50_has_rows_51_to_100()
    {
        var (rows, page, size) = CrmService.Paginate(Rows, 2, 50);
        Assert.Equal(2, page);
        Assert.Equal(50, size);
        Assert.Equal(Enumerable.Range(51, 50), rows);
    }

    [Fact]
    public void A_page_past_the_end_returns_the_last_one()
    {
        var (rows, page, _) = CrmService.Paginate(Rows, 9, 50);
        Assert.Equal(3, page);
        Assert.Equal(Enumerable.Range(101, 20), rows);
    }

    [Fact]
    public void Page_and_size_are_clamped()
    {
        var (_, page, size) = CrmService.Paginate(Rows, 0, 5000);
        Assert.Equal(1, page);
        Assert.Equal(CrmService.MaxPageSize, size);
    }

    [Fact]
    public void An_empty_list_is_page_1()
    {
        var (rows, page, _) = CrmService.Paginate(Array.Empty<int>(), 3, 50);
        Assert.Equal(1, page);
        Assert.Empty(rows);
    }
}
