import { useQuery } from '@tanstack/react-query';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { usuariosApi } from '../services/usuariosApi';

/** Roles the current user can assign. The API leaves out Invitado, and SuperUser unless the user is SuperUser. */
function useOfficeRoles() {
  const roles = useQuery({ queryKey: keys.assignableRoles, queryFn: () => usuariosApi.assignableRoles() });
  return roles.data ?? [];
}

export function RoleOptions() {
  return useOfficeRoles().map((r) => (
    <option key={r.key} value={r.key}>
      {r.name}
    </option>
  ));
}
