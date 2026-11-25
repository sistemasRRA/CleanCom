import { useSession } from '../../../shared/session/Session';

export const useCanEdit = () => useSession().can('operacion', 'edit');
