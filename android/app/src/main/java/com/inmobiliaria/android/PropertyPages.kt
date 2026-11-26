package com.inmobiliaria.android

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.math.BigDecimal

@Composable
internal fun PropertiesPage(api: Api, open: (String, Int) -> Unit) {
    val op = remember { Operation() }
    var properties by remember { mutableStateOf<List<JSONObject>?>(null) }
    val load: suspend () -> Unit = { properties = api.list("propiedades") }
    LaunchedEffect(Unit) { op.run(load) }
    val list = properties.orEmpty()
    Page(list.size, {
        Heading("Mis propiedades")
        Refresh(op, load)
        if (properties != null) Button(enabled = !op.busy, onClick = { open("editar", 0) }) { Text("Crear propiedad") }
        if (properties?.isEmpty() == true) Text("Todavía no tienes propiedades registradas.")
    }) { index ->
        val property = list[index]
        val id = property.getInt("idPropiedad")
        Heading(property.text("titulo"))
        Text("${property.text("sector")}, ${property.text("ciudad")}")
        Text(property.text("estadoPropiedad"))
        OutlinedButton(onClick = { open("editar", id) }) { Text("Editar propiedad") }
        OutlinedButton(onClick = { open("imagenes", id) }) { Text("Administrar imágenes") }
        Button(onClick = { open("publicar", id) }) { Text("Publicar alquiler") }
    }
}

private data class PropertyField(val key: String, val label: String, val limit: Int, val keyboard: KeyboardType = KeyboardType.Text)
private val propertyFields = listOf(
    PropertyField("titulo", "Título", 150), PropertyField("descripcion", "Descripción", 4000),
    PropertyField("ciudad", "Ciudad", 100), PropertyField("sector", "Sector", 100),
    PropertyField("direccion", "Dirección", 300), PropertyField("habitaciones", "Habitaciones", 3, KeyboardType.Number),
    PropertyField("banos", "Baños", 3, KeyboardType.Number), PropertyField("parqueos", "Parqueos", 3, KeyboardType.Number),
    PropertyField("metrosCuadrados", "Metros cuadrados", 16, KeyboardType.Decimal)
)

@Composable
internal fun EditPropertyPage(api: Api, id: Int, saved: () -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    val values = remember { mutableStateMapOf<String, String>() }
    var types by remember { mutableStateOf(emptyList<JSONObject>()) }
    var provinces by remember { mutableStateOf(emptyList<JSONObject>()) }
    var loaded by remember { mutableStateOf(false) }
    var type by remember { mutableStateOf("") }
    var province by remember { mutableStateOf("") }
    val load: suspend () -> Unit = {
        api.list("propiedades")
        types = api.list("catalogos/tipos")
        provinces = api.list("catalogos/provincias")
        if (id != 0) {
            val property = api.item("propiedades/$id")
            type = property.text("idTipoPropiedad")
            province = property.text("idProvincia")
            propertyFields.forEach { values[it.key] = property.text(it.key) }
        } else {
            listOf("habitaciones", "banos", "parqueos").forEach { values[it] = "0" }
        }
        loaded = true
    }
    LaunchedEffect(Unit) { op.run(load) }
    Page(header = {
        Feedback(op)
        if (!loaded && !op.busy) OutlinedButton(onClick = { scope.launch { op.run(load) } }) { Text("Volver a cargar") }
        if (loaded) {
            Choice("Tipo de propiedad", type, types.map { it.text("idTipoPropiedad") to it.text("nombre") }) { type = it }
            Choice("Provincia", province, provinces.map { it.text("idProvincia") to it.text("nombre") }) { province = it }
            propertyFields.forEach { field ->
                TextBox(field.label, values[field.key].orEmpty(), { values[field.key] = it }, field.limit,
                    multiline = field.key == "descripcion", keyboard = field.keyboard)
            }
            Button(enabled = !op.busy, onClick = {
                scope.launch {
                    op.run {
                        require(type.isNotBlank() && province.isNotBlank()) { "Selecciona el tipo y la provincia." }
                        val body = json("idTipoPropiedad" to type.toInt(), "idProvincia" to province.toInt())
                        propertyFields.forEach { field ->
                            val value = values[field.key].orEmpty().trim()
                            require(value.isNotBlank()) { "Completa el campo ${field.label}." }
                            val parsed = when (field.keyboard) {
                                KeyboardType.Number -> value.toIntOrNull()?.takeIf { it in 0..100 }
                                    ?: throw IllegalArgumentException("${field.label} debe estar entre 0 y 100.")
                                KeyboardType.Decimal -> decimal(value, "Metros cuadrados", allowZero = false, maxScale = null)
                                else -> value
                            }
                            body.put(field.key, parsed)
                        }
                        api.send(if (id == 0) "propiedades" else "propiedades/$id", body, if (id == 0) "POST" else "PUT")
                        saved()
                    }
                }
            }) { Text("Guardar propiedad") }
        }
    })
}

@Composable
internal fun Choice(label: String, selected: String, options: List<Pair<String, String>>, choose: (String) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    Text(label)
    androidx.compose.foundation.layout.Box {
        OutlinedButton(onClick = { expanded = true }) {
            Text(options.find { it.first == selected }?.second ?: "Seleccionar")
        }
        DropdownMenu(expanded, onDismissRequest = { expanded = false }) {
            options.forEach { (id, name) ->
                DropdownMenuItem(text = { Text(name) }, onClick = { choose(id); expanded = false })
            }
        }
    }
}

internal fun decimal(value: String, label: String, allowZero: Boolean, maxScale: Int? = 2): BigDecimal {
    val number = value.trim().replace(',', '.').toBigDecimalOrNull()
    require(number != null && number >= (if (allowZero) BigDecimal.ZERO else BigDecimal("0.01")) &&
        number <= BigDecimal("999999999") && (maxScale == null || number.stripTrailingZeros().scale() <= maxScale)) {
        "Revisa el valor de $label." + if (maxScale == 2) " Usa hasta dos decimales." else ""
    }
    return number
}

@Composable
internal fun PublishPage(api: Api, id: Int, addImages: () -> Unit, saved: () -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    var property by remember { mutableStateOf<JSONObject?>(null) }
    var price by remember { mutableStateOf("") }
    var currency by remember { mutableStateOf("DOP") }
    var deposit by remember { mutableStateOf("") }
    var maintenance by remember { mutableStateOf("") }
    val load: suspend () -> Unit = { property = api.item("propiedades/$id") }
    LaunchedEffect(Unit) { op.run(load) }
    Page(header = {
        Refresh(op, load)
        property?.let {
            Heading(it.text("titulo"))
            OutlinedButton(onClick = addImages) { Text("Administrar imágenes") }
            TextBox("Precio mensual", price, { price = it }, 16, keyboard = KeyboardType.Decimal)
            Choice("Moneda", currency, listOf("DOP" to "DOP", "USD" to "USD")) { currency = it }
            TextBox("Depósito (opcional)", deposit, { deposit = it }, 16, keyboard = KeyboardType.Decimal)
            TextBox("Mantenimiento mensual (opcional)", maintenance, { maintenance = it }, 16, keyboard = KeyboardType.Decimal)
            Button(enabled = !op.busy, onClick = {
                scope.launch {
                    op.run {
                        api.send("publicaciones", json("idPropiedad" to id, "tipoOperacion" to "Alquiler", "moneda" to currency,
                            "precio" to decimal(price, "precio", allowZero = false),
                            "deposito" to deposit.takeIf(String::isNotBlank)?.let { value -> decimal(value, "depósito", allowZero = true) },
                            "mantenimientoMensual" to maintenance.takeIf(String::isNotBlank)?.let { value ->
                                decimal(value, "mantenimiento", allowZero = true)
                            }))
                        saved()
                    }
                }
            }) { Text("Publicar alquiler") }
        }
    })
}

@Composable
internal fun PropertyImagesPage(api: Api, id: Int, publish: () -> Unit, notify: (String) -> Unit) {
    val op = remember { Operation() }
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val path = "propiedades/$id/imagenes"
    var images by remember { mutableStateOf<List<JSONObject>?>(null) }
    var deleting by remember { mutableStateOf<Int?>(null) }
    val load: suspend () -> Unit = { images = api.list(path).sortedBy { it.optInt("orden") } }
    LaunchedEffect(Unit) { op.run(load) }
    val list = images.orEmpty()
    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenMultipleDocuments()) { files ->
        if (files.isNotEmpty()) scope.launch {
            op.run {
                require(files.size + list.size <= 8) { "Puedes guardar hasta 8 imágenes por propiedad." }
                try {
                    api.upload(path, context.contentResolver, files)
                    notify("Imágenes guardadas.")
                } finally {
                    load()
                }
            }
        }
    }
    Page(list.size, {
        Text("${list.size} de 8 imágenes · JPG, PNG o WebP · Máximo 5 MB por imagen")
        Refresh(op, load)
        Button(enabled = !op.busy && images != null && list.size < 8, onClick = {
            picker.launch(arrayOf("image/jpeg", "image/png", "image/webp"))
        }) { Text("Seleccionar imágenes") }
        OutlinedButton(onClick = publish) { Text("Continuar a publicar") }
    }) { index ->
        val imageId = list[index].getInt("idImagen")
        RemoteImage(api, "$path/$imageId/archivo", "Fotografía ${index + 1}")
        TextButton(enabled = !op.busy, onClick = { deleting = imageId }) { Text("Eliminar imagen") }
    }
    deleting?.let { imageId ->
        Confirm("Eliminar imagen", "La imagen dejará de aparecer en las publicaciones de esta propiedad.",
            close = { deleting = null }) {
            deleting = null
            scope.launch {
                op.run {
                    api.send("$path/$imageId", method = "DELETE")
                    notify("Imagen eliminada.")
                    load()
                }
            }
        }
    }
}

private data class ArchiveEntry(val category: String, val item: JSONObject)
private data class ArchiveAction(val title: String, val detail: String, val path: String, val method: String = "POST")

@Composable
internal fun ArchivePage(api: Api, notify: (String) -> Unit) {
    val op = remember { Operation() }
    val scope = rememberCoroutineScope()
    var entries by remember { mutableStateOf<List<ArchiveEntry>?>(null) }
    var action by remember { mutableStateOf<ArchiveAction?>(null) }
    val load: suspend () -> Unit = {
        entries = api.list("propiedades").map { ArchiveEntry("Propiedad activa", it) } +
            api.list("propiedades/archivadas").map { ArchiveEntry("Propiedad archivada", it) } +
            api.list("publicaciones/mias").map { ArchiveEntry("Publicación", it) }
    }
    LaunchedEffect(Unit) { op.run(load) }
    val list = entries.orEmpty()
    Page(list.size, {
        Heading("Propiedades y publicaciones")
        Refresh(op, load)
        if (entries?.isEmpty() == true) Text("No hay propiedades ni publicaciones para administrar.")
    }) { index ->
        val (category, item) = list[index]
        val id = item.optInt("idPropiedad")
        val publication = item.optInt("idPublicacion")
        val state = item.text("estado")
        Text(if (state.isBlank()) category else "$category · $state")
        Heading(item.text("titulo"))
        val change = when {
            category == "Propiedad activa" -> ArchiveAction("Archivar propiedad",
                "Se ocultará la propiedad y se retirarán sus publicaciones. Se conserva el historial.", "propiedades/$id", "DELETE")
            category == "Propiedad archivada" -> ArchiveAction("Restaurar propiedad",
                "Podrás administrar la propiedad y reactivar sus publicaciones por separado.", "propiedades/$id/restaurar")
            state == "Publicada" -> ArchiveAction("Retirar publicación", "El anuncio dejará de aparecer en el catálogo.",
                "publicaciones/$publication/retirar")
            state == "Pausada" -> ArchiveAction("Volver a publicar", "El anuncio volverá a aparecer en el catálogo.",
                "publicaciones/$publication/reactivar")
            else -> null
        }
        if (change != null) OutlinedButton(enabled = !op.busy, onClick = { action = change }) { Text(change.title) }
    }
    action?.let { change ->
        Confirm(change.title, change.detail, close = { action = null }) {
            action = null
            scope.launch {
                op.run {
                    api.send(change.path, method = change.method)
                    notify("Cambio guardado.")
                    load()
                }
            }
        }
    }
}
