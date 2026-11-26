package com.inmobiliaria.android

import android.content.ContentResolver
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.net.Uri
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import org.json.JSONTokener
import java.io.IOException
import java.io.OutputStream
import java.net.HttpURLConnection
import java.net.URI
import java.time.Instant
import java.time.OffsetDateTime
import java.time.LocalDateTime
import java.time.ZoneOffset
import java.util.UUID

internal class ApiError(val status: Int, message: String) : IOException(message)

internal fun json(vararg pairs: Pair<String, Any?>) = JSONObject().apply {
    pairs.forEach { (key, value) -> put(key, value ?: JSONObject.NULL) }
}

internal fun JSONObject.text(key: String) = if (isNull(key)) "" else optString(key)
internal fun JSONArray.objects() = List(length()) { getJSONObject(it) }

internal fun apiBase(value: String): String {
    val url = URI(value.trim())
    require(url.host != null && url.userInfo == null && url.query == null && url.fragment == null &&
        (url.scheme == "https" || (BuildConfig.DEBUG && url.scheme == "http"))) {
        "Introduce una URL HTTPS válida. La versión de depuración también admite HTTP."
    }
    return url.toString().trimEnd('/') + "/"
}

internal fun utc(value: String): Instant =
    runCatching { OffsetDateTime.parse(value).toInstant() }
        .getOrElse { LocalDateTime.parse(value).toInstant(ZoneOffset.UTC) }

internal class Api(private val base: String, private val token: String?) {
    suspend fun list(path: String) = JSONArray(request("GET", path)).objects()
    suspend fun item(path: String) = JSONObject(request("GET", path))

    suspend fun send(path: String, data: JSONObject? = null, method: String = "POST"): String =
        request(method, path, data)

    private suspend fun request(method: String, path: String, data: JSONObject? = null): String =
        connect(method, path, if (data != null) "application/json; charset=utf-8" else null,
            data?.let { body -> { stream -> stream.write(body.toString().toByteArray()) } }) { connection ->
            connection.inputStream.bufferedReader().use { it.readText() }
        }

    suspend fun image(path: String): Bitmap? = connect("GET", path.trimStart('/')) { connection ->
        connection.inputStream.use { BitmapFactory.decodeStream(it) }
    }

    suspend fun upload(path: String, resolver: ContentResolver, files: List<Uri>) {
        // Se conserva la selección múltiple, enviando una imagen de hasta 5 MB por petición.
        for (file in files) {
            val boundary = UUID.randomUUID().toString()
            connect("POST", path, "multipart/form-data; boundary=$boundary", { stream ->
                val bytes = resolver.openInputStream(file)?.use { input ->
                    val buffer = java.io.ByteArrayOutputStream()
                    val chunk = ByteArray(8192)
                    while (true) {
                        val count = input.read(chunk)
                        if (count < 0) break
                        require(buffer.size() + count <= 5 * 1024 * 1024) {
                            "Cada imagen puede ocupar hasta 5 MB."
                        }
                        buffer.write(chunk, 0, count)
                    }
                    buffer.toByteArray()
                } ?: error("No se pudo abrir la imagen seleccionada.")
                require(bytes.isNotEmpty()) { "La imagen seleccionada está vacía." }
                val mime = resolver.getType(file)?.takeIf { it in setOf("image/jpeg", "image/png", "image/webp") }
                    ?: "application/octet-stream"
                stream.write(("--$boundary\r\nContent-Disposition: form-data; name=\"archivos\"; " +
                    "filename=\"imagen\"\r\nContent-Type: $mime\r\n\r\n").toByteArray())
                stream.write(bytes)
                stream.write("\r\n".toByteArray())
                stream.write("--$boundary--\r\n".toByteArray())
            }) { }
        }
    }

    private suspend fun <T> connect(
        method: String,
        path: String,
        contentType: String? = null,
        body: ((OutputStream) -> Unit)? = null,
        read: (HttpURLConnection) -> T
    ): T = withContext(Dispatchers.IO) {
        require(!path.contains(':') && !path.contains('\\') && !path.startsWith('/'))
        val url = URI(base + "api/v1/" + path.removePrefix("api/v1/")).toURL()
        val connection = url.openConnection() as HttpURLConnection
        try {
            connection.requestMethod = method
            connection.instanceFollowRedirects = false
            connection.connectTimeout = 12_000
            connection.readTimeout = 20_000
            token?.let { connection.setRequestProperty("Authorization", "Bearer $it") }
            if (body != null) {
                connection.setRequestProperty("Content-Type", contentType)
                connection.doOutput = true
                if (contentType?.startsWith("multipart/") == true) connection.setChunkedStreamingMode(8192)
                connection.outputStream.use(body)
            }
            val status = connection.responseCode
            if (status !in 200..299) {
                val response = connection.errorStream?.bufferedReader()?.use { it.readText() }.orEmpty()
                throw ApiError(status, errorText(status, response))
            }
            read(connection)
        } finally {
            connection.disconnect()
        }
    }

    private fun errorText(status: Int, response: String): String {
        if (status == 401) return "Credenciales incorrectas o sesión vencida."
        if (status == 403) return "Tu cuenta no tiene permiso para esta operación o está pendiente de aprobación."
        val data = runCatching { JSONTokener(response).nextValue() }.getOrNull() as? JSONObject
        data?.text("mensaje")?.takeIf { it.isNotBlank() }?.let { return it }
        data?.optJSONObject("errors")?.let { errors ->
            val messages = errors.keys().asSequence().flatMap { key ->
                val values = errors.optJSONArray(key) ?: JSONArray()
                (0 until values.length()).asSequence().map { values.optString(it) }
            }.joinToString(" ")
            if (messages.isNotBlank()) return messages
        }
        return data?.text("title")?.takeIf { it.isNotBlank() } ?: "La API respondió con el error $status."
    }
}
