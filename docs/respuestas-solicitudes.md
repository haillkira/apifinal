# Responder solicitudes

El vendedor abre Solicitudes recibidas, pulsa Responder y acepta o rechaza. El rechazo requiere un motivo de hasta 1000 caracteres. La respuesta y su fecha UTC aparecen en el listado del comprador al actualizar.

Solo el vendedor aprobado propietario puede responder una solicitud Pendiente de una publicacion activa y propiedad disponible sin archivar. Una decision guardada no puede sobrescribirse; las peticiones concurrentes se serializan por propiedad. Aceptar no crea reservas ni contratos, ni oculta el anuncio.

005_respuesta_solicitudes.sql agrega Respuesta nullable a la tabla existente, sin modificar mensajes del comprador. Aplicada a la base local InmobiliariaDB. Para otras bases existentes, ejecutar este script antes de usar el codigo nuevo. Recorrido-local aplica la actualizacion al arrancar su API de demostracion.
