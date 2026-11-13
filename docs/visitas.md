# Visitas de solicitudes aceptadas

En Mis solicitudes o Solicitudes recibidas, abrir Ver visitas. El vendedor propone una fecha futura con notas opcionales de hasta 1000 caracteres. El comprador confirma asistencia. Ambos participantes pueden cancelar una visita futura Solicitada o Confirmada, conservando el historial.

Las fechas se almacenan en UTC y se introducen/muestran en horario de Republica Dominicana (UTC-04:00). Se usa la tabla Visitas existente; no requiere migracion nueva.

Solo los participantes pueden consultar o cambiar visitas. Programar/confirmar requiere solicitud Aceptada, propiedad disponible sin archivar y publicacion activa. Solo existe una visita Solicitada/Confirmada por solicitud; el bloqueo por propiedad evita duplicados concurrentes. Una visita pasada no se confirma ni cancela. Para cambiar fecha de una futura, cancelar primero y proponer otra.

Este avance no implementa disponibilidad horaria entre distintas solicitudes, duracion de visitas, recordatorios ni cierre como Realizada/NoAsistio. Tampoco crea reservas ni contratos.

Verificacion: pruebas unitarias de fecha/notas/autorizacion y recorrido HTTP/SQL de propuesta concurrente, confirmacion y cancelacion.
