import { AREAS, formatValue, type KpiHistory, lightName } from '../types/kpis';

/** The last weeks of each indicator, colored by the light each week had. */
export function KpiHistoryTable({ history }: { history: KpiHistory }) {
  return (
    <div className="table-wrap">
      <table className="kpi-history">
        <thead>
          <tr>
            <th>Indicador</th>
            {history.weeks.map((w) => (
              <th key={w.start} className="end" title={`${w.start} a ${w.end}`}>
                {w.label}
              </th>
            ))}
          </tr>
        </thead>
        {AREAS.map((area) => (
          <tbody key={area.key}>
            <tr className="kpi-history__area">
              <th colSpan={history.weeks.length + 1}>{area.name}</th>
            </tr>
            {history.indicadores
              .filter((r) => r.area === area.key)
              .map((r) => (
                <tr key={r.key}>
                  <td>{r.name}</td>
                  {r.cells.map((c, i) => (
                    <td key={i} className={`end kpi-cell light-${c.light}`} title={lightName(c.light)}>
                      {formatValue(r.unit, c.value)}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
        ))}
      </table>
    </div>
  );
}
