# Archivado y retiro

En Mis propiedades, abrir Retirar publicaciones y archivar propiedades.

Retirar una publicacion cambia su estado a Pausada y la oculta del catalogo. Puede reactivarse si la propiedad sigue disponible. Archivar una propiedad asigna EliminadoEn en UTC, pausa sus publicaciones activas y la oculta de los listados y endpoints normales, incluidas las imagenes. Se conservan filas SQL, archivos e historial de solicitudes.

La vista de archivadas permite restaurar propiedades propias; restaurar no reactiva automaticamente sus anuncios. El historial de solicitudes indica cuando una publicacion esta retirada.

El archivado se bloquea con reservas Pendientes/Confirmadas y contratos que no tengan un estado terminal (Cancelado, Finalizado, Completado, Rescindido, Expirado o Rechazado). Esta comprobacion es conservadora y requiere resolver esos compromisos primero.

004_archivado.sql agrega una columna nullable y actualiza la vista publica. Es idempotente y se aplica al iniciar la API mediante recorrido-local.cmd, exclusivamente en la base local de demostracion. Para otro entorno, aplicar esa actualizacion antes de desplegar el codigo. No ejecutar nuevamente 001 sobre bases existentes.
