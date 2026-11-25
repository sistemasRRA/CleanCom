import { Reasons } from '../components/Reasons';
import { ParadaTypes } from '../components/ParadaTypes';
import { LineasServicio } from '../components/LineasServicio';

export function OtherCatalogs() {
  return (
    <div className="stack">
      <Reasons />
      <ParadaTypes />
      <LineasServicio />
    </div>
  );
}
