using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Common;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Crm;

/// <summary>
/// Clientes y fuentes (docs/pantallas/oficina/clientes-y-fuentes.md): where the invitados come from and how much they
/// buy. Nuevos, recurrentes, recuperados and perdidos with the same 120 days as the CRM (Crm.DiasInactivo), the venta
/// of the tickets, the trend, the retention of each month's nuevos and the list of perdidos to call.
/// </summary>
public class ClientesFuentesService(ICleanComDbContext db, IBusinessClock clock, ISettingService settings)
{
    public const string NoSource = "Sin capturar";
    private const int TrendPeriods = 6;
    private const int CohortMonths = 6;

    private sealed record Person(int Id, string Name, string Phone, string Fuente, bool DoNotContact, DateOnly? CallRequestedOn, List<DateOnly> Services);

    private sealed record Sale(int InvitadoId, DateOnly Date, decimal Amount);

    private sealed record Data(IReadOnlyList<Person> People, IReadOnlyList<Sale> Sales, int InactiveDays, DateOnly Today);

    /// <summary>The indicators, the table by source and the trend of the period that contains <paramref name="date"/>.</summary>
    public async Task<ClientesFuentesDto> PeriodAsync(ClientesPeriod period, DateOnly? date, ClientesTipo tipo, CancellationToken ct)
    {
        var data = await LoadAsync(tipo, ct);
        var range = ClientesRules.Range(period, date ?? data.Today);
        var elapsed = KpiRules.Elapsed(range, data.Today);
        if (elapsed.Start > data.Today)
        {
            throw new DomainException("Ese periodo todavía no empieza.");
        }
        var comparison = ClientesRules.Comparison(period, range, elapsed);
        var totals = Totals(data, elapsed);

        var trend = new List<ClientesTrendDto>();
        var cursor = range;
        for (var i = 0; i < TrendPeriods; i++)
        {
            var e = KpiRules.Elapsed(cursor, data.Today);
            trend.Insert(0, new ClientesTrendDto(ClientesRules.Label(period, cursor), cursor.Start, cursor.End, e != cursor, Totals(data, e)));
            cursor = ClientesRules.Previous(period, cursor.Start);
        }

        return new ClientesFuentesDto(
            period, tipo, ClientesRules.Label(period, range), range.Start, range.End, elapsed != range,
            RangeLabel(comparison),
            totals,
            Totals(data, comparison),
            Fuentes(data, elapsed, totals.Venta),
            trend,
            data.InactiveDays);
    }

    /// <summary>The nuevos of each of the last 6 months (up to the month of <paramref name="date"/>) and how many repeated.</summary>
    public async Task<RetencionDto> RetentionAsync(DateOnly? date, ClientesTipo tipo, string? fuente, CancellationToken ct)
    {
        var data = await LoadAsync(tipo, ct);
        var day = date ?? data.Today;
        var lastMonth = new DateOnly(day.Year, day.Month, 1);
        var firstMonth = lastMonth.AddMonths(-(CohortMonths - 1));
        var withFirst = data.People.Where(p => p.Services.Count > 0).ToList();
        var inWindow = withFirst.Where(p => p.Services[0] >= firstMonth && p.Services[0] < lastMonth.AddMonths(1)).ToList();
        var fuentes = inWindow.Select(p => p.Fuente).Distinct().Order().ToList();
        var chosen = string.IsNullOrWhiteSpace(fuente) ? null : fuente.Trim();

        var cohorts = new List<CohortDto>();
        for (var month = lastMonth; month >= firstMonth; month = month.AddMonths(-1))
        {
            var members = inWindow.Where(p => p.Services[0].Year == month.Year && p.Services[0].Month == month.Month
                && (chosen is null || p.Fuente == chosen)).ToList();
            (int Base, int Repeated) Window(int days)
            {
                var results = members.Select(p => ClientesRules.RepeatedWithin(p.Services, days, data.Today)).Where(r => r is not null).ToList();
                return (results.Count, results.Count(r => r == true));
            }
            var (b30, r30) = Window(30);
            var (b60, r60) = Window(60);
            var (b90, r90) = Window(90);
            cohorts.Add(new CohortDto(month, ClientesRules.MonthLabel(month), members.Count, b30, r30, b60, r60, b90, r90, Pct(r30, b30), Pct(r60, b60), Pct(r90, b90)));
        }
        return new RetencionDto(tipo, chosen, cohorts, fuentes);
    }

    /// <summary>The invitados who became perdidos in the period, the most recent first.</summary>
    public async Task<PerdidosDto> LostAsync(ClientesPeriod period, DateOnly? date, ClientesTipo tipo, string? fuente, CancellationToken ct)
    {
        var data = await LoadAsync(tipo, ct);
        var range = ClientesRules.Range(period, date ?? data.Today);
        var elapsed = KpiRules.Elapsed(range, data.Today);
        var chosen = string.IsNullOrWhiteSpace(fuente) ? null : fuente.Trim();
        var sales = data.Sales.ToLookup(s => s.InvitadoId);
        var rows = data.People
            .Where(p => chosen is null || p.Fuente == chosen)
            .Select(p => (Person: p, Lost: ClientesRules.LostOn(p.Services, elapsed, data.InactiveDays, data.Today)))
            .Where(x => x.Lost is not null)
            .Select(x => new PerdidoDto(
                x.Person.Id, x.Person.Name, x.Person.Phone, x.Person.Fuente,
                x.Person.Services.Last(d => d < x.Lost!.Value),
                x.Lost!.Value,
                x.Person.Services.Count(d => d < x.Lost!.Value),
                sales[x.Person.Id].Sum(s => s.Amount),
                x.Person.DoNotContact,
                x.Person.CallRequestedOn is not null,
                x.Person.Services.Any(d => d > x.Lost!.Value)))
            .OrderByDescending(r => r.LostOn).ThenBy(r => r.Name)
            .ToList();
        return new PerdidosDto(ClientesRules.Label(period, range), rows);
    }

    /// <summary>
    /// Sends invitados to "Llamar hoy" (reactivation) from today, until someone registers a contact with them. Who
    /// asked not to be contacted is skipped.
    /// </summary>
    public async Task<LlamarPerdidosResultDto> CallAsync(LlamarPerdidosRequest r, CancellationToken ct)
    {
        var ids = (r.InvitadoIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
        {
            throw new DomainException("Elige a quién llamar.");
        }
        if (ids.Count > 1000)
        {
            throw new DomainException("Manda hasta 1,000 invitados a la vez.");
        }
        var today = clock.Today;
        var invitados = await db.Invitados.Where(i => ids.Contains(i.Id) && i.Active && !i.DoNotContact).ToListAsync(ct);
        foreach (var invitado in invitados)
        {
            invitado.CallRequestedOn ??= today;
        }
        await db.SaveChangesAsync(ct);
        var skipped = ids.Count - invitados.Count;
        var message = $"{invitados.Count} {(invitados.Count == 1 ? "invitado quedó" : "invitados quedaron")} en \"Llamar hoy\" de Clientes (CRM)."
            + (skipped > 0 ? $" {skipped} no, porque pidieron no ser contactados o ya no están activos." : "");
        return new LlamarPerdidosResultDto(invitados.Count, skipped, message);
    }

    private static ClientesTotalsDto Totals(Data data, DateRange range)
    {
        var close = range.End < data.Today ? range.End : data.Today;
        var kinds = data.People.Select(p => ClientesRules.Attended(p.Services, range, data.InactiveDays)).ToList();
        var nuevos = data.People.Where((_, i) => kinds[i] == AttendedKind.Nuevo).Select(p => p.Id).ToHashSet();
        var sales = data.Sales.Where(s => range.Contains(s.Date)).ToList();
        var venta = sales.Sum(s => s.Amount);
        return new ClientesTotalsDto(
            nuevos.Count,
            kinds.Count(k => k == AttendedKind.Recurrente),
            kinds.Count(k => k == AttendedKind.Recuperado),
            data.People.Count(p => ClientesRules.LostOn(p.Services, range, data.InactiveDays, data.Today) is not null),
            kinds.Count(k => k is not null),
            data.People.Count(p => ClientesRules.ActiveAt(p.Services, close, data.InactiveDays)),
            data.People.Sum(p => p.Services.Count(range.Contains)),
            venta,
            sales.Where(s => nuevos.Contains(s.InvitadoId)).Sum(s => s.Amount),
            sales.Count == 0 ? null : Math.Round(venta / sales.Count, 2, MidpointRounding.AwayFromZero));
    }

    private static IReadOnlyList<FuenteRowDto> Fuentes(Data data, DateRange range, decimal totalVenta)
    {
        var sales = data.Sales.Where(s => range.Contains(s.Date)).ToLookup(s => s.InvitadoId);
        return data.People.GroupBy(p => p.Fuente).Select(g =>
            {
                var people = g.ToList();
                var kinds = people.Select(p => ClientesRules.Attended(p.Services, range, data.InactiveDays)).ToList();
                var theirSales = people.SelectMany(p => sales[p.Id]).ToList();
                var venta = theirSales.Sum(s => s.Amount);
                return new FuenteRowDto(
                    g.Key,
                    kinds.Count(k => k == AttendedKind.Nuevo),
                    people.Where((_, i) => kinds[i] == AttendedKind.Nuevo).SelectMany(p => sales[p.Id]).Sum(s => s.Amount),
                    kinds.Count(k => k is not null),
                    people.Sum(p => p.Services.Count(range.Contains)),
                    venta,
                    theirSales.Count == 0 ? null : Math.Round(venta / theirSales.Count, 2, MidpointRounding.AwayFromZero),
                    totalVenta == 0 ? null : Math.Round(venta * 100 / totalVenta, 1, MidpointRounding.AwayFromZero),
                    people.Count(p => ClientesRules.LostOn(p.Services, range, data.InactiveDays, data.Today) is not null));
            })
            // Sources with nothing in the period are left out.
            .Where(f => f.Atendidos > 0 || f.Perdidos > 0 || f.Venta > 0)
            .OrderByDescending(f => f.Venta).ThenByDescending(f => f.Nuevos).ThenBy(f => f.Fuente)
            .ToList();
    }

    /// <summary>
    /// Invitados of the type (never the tiendas internas) with their services up to today, and the tickets with an
    /// amount. A service is a pedido not cancelled, on its recolección date, as in the CRM.
    /// </summary>
    private async Task<Data> LoadAsync(ClientesTipo tipo, CancellationToken ct)
    {
        var today = clock.Today;
        var inactiveDays = await settings.GetIntegerAsync(SettingKeys.CrmInactiveDays, ct);
        var invitados = db.Invitados.AsNoTracking().Where(i => i.Active && i.Type != InvitadoType.TiendaInterna);
        invitados = tipo switch
        {
            ClientesTipo.ClientesFinales => invitados.Where(i => i.Type == InvitadoType.ClienteFinal),
            ClientesTipo.Convenios => invitados.Where(i => i.Type == InvitadoType.Convenio || i.Type == InvitadoType.Empresa),
            _ => invitados,
        };
        var people = await invitados
            .Select(i => new { i.Id, i.Name, i.WhatsAppPhone, i.HowFoundUs, i.DoNotContact, i.CallRequestedOn })
            .ToListAsync(ct);
        var ids = people.Select(p => p.Id).ToHashSet();
        var services = (await db.Pedidos.AsNoTracking()
                .Where(p => p.Active && p.Etapa != PedidoEtapa.Cancelado && p.RecoleccionDate <= today)
                .Select(p => new { p.InvitadoId, p.RecoleccionDate })
                .ToListAsync(ct))
            .Where(s => ids.Contains(s.InvitadoId))
            .ToLookup(s => s.InvitadoId, s => s.RecoleccionDate);
        var sales = (await db.Tickets.AsNoTracking()
                .Where(t => t.Active && t.Amount != null && t.Pedido!.Active && t.Pedido.Etapa != PedidoEtapa.Cancelado)
                .Select(t => new { t.Pedido!.InvitadoId, t.CreatedAt, Amount = t.Amount!.Value })
                .ToListAsync(ct))
            .Where(t => ids.Contains(t.InvitadoId))
            .Select(t => new Sale(t.InvitadoId, BusinessTime.Date(t.CreatedAt), t.Amount))
            .ToList();

        return new Data(
            people.Select(p => new Person(
                p.Id, p.Name, p.WhatsAppPhone,
                string.IsNullOrWhiteSpace(p.HowFoundUs) ? NoSource : p.HowFoundUs.Trim(),
                p.DoNotContact, p.CallRequestedOn,
                services[p.Id].Order().ToList())).ToList(),
            sales, inactiveDays, today);
    }

    private static decimal? Pct(int part, int total) => total == 0 ? null : Math.Round(part * 100m / total, 1, MidpointRounding.AwayFromZero);

    /// <summary>"1 al 26 oct 2026".</summary>
    private static string RangeLabel(DateRange range) =>
        range.Start.Month == range.End.Month && range.Start.Year == range.End.Year
            ? $"{range.Start.Day} al {range.End.Day} {Month(range.End)} {range.End.Year}"
            : $"{range.Start.Day} {Month(range.Start)} {range.Start.Year} al {range.End.Day} {Month(range.End)} {range.End.Year}";

    private static string Month(DateOnly d) => d.ToString("MMM", new System.Globalization.CultureInfo("es-MX")).TrimEnd('.');
}
