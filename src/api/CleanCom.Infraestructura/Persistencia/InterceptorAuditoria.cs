using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanCom.Infraestructura.Persistencia;

/// <summary>
/// Llena CreadoEn/CreadoPor y ModificadoEn/ModificadoPor (en UTC) al guardar.
/// </summary>
public class InterceptorAuditoria(IRelojNegocio reloj, IUsuarioActual usuario) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Auditar(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Auditar(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Auditar(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        var ahora = reloj.AhoraUtc;
        foreach (var entrada in contexto.ChangeTracker.Entries<Entidad>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.CreadoEn = ahora;
                    entrada.Entity.CreadoPor = usuario.Id;
                    break;
                case EntityState.Modified:
                    entrada.Entity.ModificadoEn = ahora;
                    entrada.Entity.ModificadoPor = usuario.Id;
                    entrada.Property(e => e.CreadoEn).IsModified = false;
                    entrada.Property(e => e.CreadoPor).IsModified = false;
                    break;
            }
        }
    }
}
