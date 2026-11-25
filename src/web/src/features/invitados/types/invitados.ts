import type { Schemas } from '../../../shared/api/client';
import { parsePoint } from '../../../shared/components/AddressLocation';
import { optional } from '../../../shared/utils/format';

export type Invitado = Schemas['InvitadoDto'];

export type InvitadoType = Schemas['InvitadoType'];

/** Tienda interna is not offered for new invitados; one that already is keeps it. */
export const INVITADO_TYPES: { key: InvitadoType; name: string }[] = [
  { key: 'ClienteFinal', name: 'Cliente final (servicio a domicilio)' },
  { key: 'Convenio', name: 'Convenio' },
  { key: 'Empresa', name: 'Empresa' },
  { key: 'TiendaInterna', name: 'Tienda interna' },
];

export type Direccion = Schemas['AddressDto'];

export function addressLine(d: Direccion) {
  const number = [d.exteriorNumber, d.interiorNumber && `int. ${d.interiorNumber}`].filter(Boolean).join(' ');
  return [`${d.street} ${number}`.trim(), d.neighborhood, `CP ${d.postalCode}`].filter(Boolean).join(', ');
}

export type DireccionFields = {
  label: string;
  street: string;
  exteriorNumber: string;
  interiorNumber: string;
  postalCode: string;
  neighborhood: string;
  borough: string;
  references: string;
  coordinates: string;
};

export const emptyDireccion: DireccionFields = {
  label: '',
  street: '',
  exteriorNumber: '',
  interiorNumber: '',
  postalCode: '',
  neighborhood: '',
  borough: '',
  references: '',
  coordinates: '',
};

export function toDireccionRequest(f: DireccionFields): Schemas['SaveAddressRequest'] {
  const point = parsePoint(f.coordinates);
  const lat = point?.lat;
  const lng = point?.lng;
  const hasPoint = point !== null;
  return {
    label: optional(f.label),
    street: f.street.trim(),
    exteriorNumber: optional(f.exteriorNumber),
    interiorNumber: optional(f.interiorNumber),
    postalCode: f.postalCode.trim(),
    neighborhood: optional(f.neighborhood),
    borough: optional(f.borough),
    references: optional(f.references),
    lat: hasPoint ? lat! : null,
    lng: hasPoint ? lng! : null,
    zonaId: null,
  };
}

/** The saved address in the form's fields; the point goes as "lat, lng" for the map. */
export function toDireccionFields(d: Direccion): DireccionFields {
  return {
    label: d.label ?? '',
    street: d.street,
    exteriorNumber: d.exteriorNumber ?? '',
    interiorNumber: d.interiorNumber ?? '',
    postalCode: d.postalCode,
    neighborhood: d.neighborhood ?? '',
    borough: d.borough ?? '',
    references: d.references ?? '',
    coordinates: d.lat != null && d.lng != null ? `${d.lat}, ${d.lng}` : '',
  };
}
