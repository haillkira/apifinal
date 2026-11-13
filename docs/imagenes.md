# Imagenes de propiedades y publicaciones

Desde Mis propiedades, pulsa Imagenes. Tambien hay un enlace en Publicar alquiler. Se pueden seleccionar varios archivos y eliminar fotos individualmente. El catalogo muestra una galeria con botones Anterior/Siguiente; la primera foto por orden es la portada.

Limites: 8 fotos en total por propiedad (compartidas por sus publicaciones), 5 MB por archivo y 24 megapixeles. Formatos de entrada: JPEG, PNG y WebP. La API comprueba el contenido real, corrige la orientacion, limita el lado mayor a 1600 px y guarda JPEG sin conservar los metadatos originales. Valida la propiedad del vendedor y serializa las cargas por propiedad en SQL para respetar el limite con peticiones concurrentes. Una carga con archivos invalidos no agrega ninguna foto del lote.

Se utiliza la tabla existente ImagenesPropiedades, sin crear o borrar tablas. Se corrigio el mapeo EF generado para que la relacion con Propiedades sea uno a muchos; conservar esta correccion si se vuelve a generar el DbContext. Los archivos se guardan en api/Inmobiliaria.Api/App_Data/imagenes, excluidos de Git. Se puede cambiar la carpeta con Imagenes:Carpeta (ruta absoluta). Respaldar tanto SQL como esta carpeta.

Las imagenes privadas se consultan con el token del vendedor. Los endpoints publicos solo sirven archivos de publicaciones disponibles de vendedores activos y aprobados. Al pausar una publicacion dejan de servirse por esa ruta.

El almacenamiento actual es local, no Azure Blob Storage. Para desplegar varias instancias o usar un sistema de archivos efimero, sustituirlo por almacenamiento compartido persistente. SkiaSharp incluye los componentes nativos para Windows; un despliegue Linux requiere incluir sus assets nativos compatibles.

Despues de actualizar el codigo, reiniciar API y Web con recorrido-local.cmd. No volver a ejecutar los scripts de esquema sobre la base existente.

Pruebas: RecorridoTests verifica carga real, limites, concurrencia, contenido invalido, permisos, descarga JPEG, eliminacion y ocultacion de una publicacion pausada. RecorridoUiTests verifica navegacion de la galeria. Las pruebas usan una base temporal en SQLEXPRESS y una carpeta temporal, eliminadas al terminar.
