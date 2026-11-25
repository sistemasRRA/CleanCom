import './paradasFijas.css';
// Public API of the paradasFijas feature: other features and the app import only from here.
export { PasoEditor } from './components/PasoEditor';
export { ParadasFijas } from './pages/ParadasFijas';
export { ACTIONS, CATEGORIES, PLACEMENTS, TURNOS, categoryOf, emptyPaso, toPasoRequest } from './types/fijas';
export type { PasoForm } from './types/fijas';
