# 🎭 Catrinas API - Documentación para Frontend Externo

## 📋 Resumen

La **Catrinas API** utiliza **SignalR (WebSocket)** para comunicación en tiempo real entre el frontend y el backend. No es una REST API tradicional, sino un sistema de eventos bidireccional.

## 🔐 Autenticación

### 1. Obtener Token JWT

**Endpoint REST**: `POST /api/validar-acceso`

**Request Body**:
```json
{
    "usuario": "JUE001"
}
```

**Response (Éxito)**:
```json
{
    "exito": true,
    "mensaje": "Acceso válido",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "rol": "Publico",
    "tiempoExpiracion": "2024-10-26T15:30:00Z"
}
```

**Response (Error)**:
```json
{
    "exito": false,
    "mensaje": "Código de acceso no válido o inactivo"
}
```

## 🔌 Conexión SignalR

### URL de Conexión
```
ws://localhost:5001/chatHub
```

### Configuración de Conexión
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5001/chatHub", {
        accessTokenFactory: () => jwtToken
    })
    .withAutomaticReconnect()
    .build();
```

## 📡 Eventos SignalR

### 📤 Eventos que ENVÍA el Cliente

#### `EnviarVoto`
Envía un voto para el participante actualmente en votación.

**Parámetros**:
- `atuendo` (int): Puntuación 1-5
- `maquillaje` (int): Puntuación 1-5  
- `tradiciones` (int): Puntuación 1-5
- `pasarela` (int): Puntuación 1-5
- `interaccion` (int): Puntuación 1-5

**Ejemplo**:
```javascript
await connection.invoke('EnviarVoto', 5, 4, 5, 3, 4);
```

### 📥 Eventos que RECIBE el Cliente

#### `NuevaVotacionParaVotar`
Se dispara cuando el administrador inicia una nueva votación.

**Estructura de datos**:
```json
{
    "idParticipante": 1,
    "participante": "Ana García Martínez",
    "tiempoVotacion": 30,
    "mensaje": "Nueva votación disponible"
}
```

**Uso**:
```javascript
connection.on('NuevaVotacionParaVotar', function (data) {
    console.log('Nueva votación:', data);
    mostrarFormularioVotacion(data.participante);
});
```

#### `VotoConfirmado`
Se dispara cuando el voto se guarda exitosamente.

**Estructura de datos**:
```json
{
    "participante": "Ana García Martínez",
    "criterios": {
        "atuendo": 5,
        "maquillaje": 4,
        "tradiciones": 5,
        "pasarela": 3,
        "interaccion": 4
    },
    "puntosFinal": 8.4,
    "mensaje": "Tu voto ha sido registrado exitosamente"
}
```

**Uso**:
```javascript
connection.on('VotoConfirmado', function (data) {
    console.log('Voto confirmado:', data);
    alert(`¡Voto confirmado! Puntuación: ${data.puntosFinal}/10`);
});
```

#### `Error`
Se dispara cuando ocurre un error.

**Estructura de datos**:
```json
"No hay votación activa en este momento"
```

**Posibles errores**:
- `"Acceso no válido"`
- `"No hay votación activa en este momento"`
- `"Ya has votado por este participante"`
- `"Todas las puntuaciones deben estar entre 1 y 5"`

**Uso**:
```javascript
connection.on('Error', function (error) {
    console.error('Error:', error);
    alert('Error: ' + error);
});
```

#### `VotacionTerminada`
Se dispara cuando la votación actual termina.

**Estructura de datos**:
```json
{
    "participante": "Ana García Martínez",
    "mensaje": "Votación terminada"
}
```

**Uso**:
```javascript
connection.on('VotacionTerminada', function (data) {
    console.log('Votación terminada:', data);
    ocultarFormularioVotacion();
});
```

#### `CuentaRegresiva`
Se dispara cada segundo durante la votación activa.

**Estructura de datos**:
```json
{
    "tiempoRestante": 25,
    "mensaje": "Tiempo restante para votar: 25 segundos"
}
```

**Uso**:
```javascript
connection.on('CuentaRegresiva', function (data) {
    document.getElementById('timer').textContent = data.tiempoRestante;
});
```

## 🔄 Flujo Completo de Votación

### 1. Autenticación
```javascript
// 1. Obtener token JWT
const response = await fetch('http://localhost:5001/api/validar-acceso', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ usuario: 'JUE001' })
});
const { token } = await response.json();
```

### 2. Conexión WebSocket
```javascript
// 2. Conectar SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5001/chatHub', {
        accessTokenFactory: () => token
    })
    .build();

await connection.start();
```

### 3. Configurar Eventos
```javascript
// 3. Configurar listeners
connection.on('NuevaVotacionParaVotar', (data) => {
    // Mostrar formulario de votación
    mostrarFormulario(data.participante);
});

connection.on('VotoConfirmado', (data) => {
    // Confirmar voto enviado
    alert(`Voto confirmado: ${data.puntosFinal}/10`);
});

connection.on('Error', (error) => {
    // Manejar errores
    alert('Error: ' + error);
});
```

### 4. Enviar Voto
```javascript
// 4. Cuando el usuario complete el formulario
async function enviarVoto() {
    const atuendo = parseInt(document.getElementById('atuendo').value);
    const maquillaje = parseInt(document.getElementById('maquillaje').value);
    const tradiciones = parseInt(document.getElementById('tradiciones').value);
    const pasarela = parseInt(document.getElementById('pasarela').value);
    const interaccion = parseInt(document.getElementById('interaccion').value);
    
    await connection.invoke('EnviarVoto', atuendo, maquillaje, tradiciones, pasarela, interaccion);
}
```

## 🎯 Estructura de Datos de Voto

### Entrada (Lo que envía el frontend)
```javascript
{
    atuendo: 5,     // int 1-5
    maquillaje: 4,  // int 1-5
    tradiciones: 5, // int 1-5
    pasarela: 3,    // int 1-5
    interaccion: 4  // int 1-5
}
```

### Procesamiento en el Backend
```csharp
// Cálculo automático del total
var totalCriterios = atuendo + maquillaje + tradiciones + pasarela + interaccion; // 21
var totalPuntos = (decimal)totalCriterios * 10 / 25; // 8.4 puntos sobre 10
```

### Almacenamiento en Base de Datos
```sql
INSERT INTO Evaluaciones (
    Id_Participante, Id_Acceso, 
    Atuendo, Maquillaje, Tradiciones, Pasarela, Interaccion,
    Total, Activo, FechaEvaluacion
) VALUES (
    1, 123, 
    5, 4, 5, 3, 4,
    8.4, 1, '2024-10-26 15:30:00'
);
```

## ⚠️ Validaciones y Restricciones

### Cliente (Frontend)
- ✅ Validar que todas las puntuaciones estén entre 1-5
- ✅ Verificar conexión SignalR antes de enviar
- ✅ Manejar estados de carga y errores

### Servidor (Backend)
- ✅ Token JWT válido y no expirado
- ✅ Rol "Publico" en el token
- ✅ Código de acceso existe en BD
- ✅ Hay una votación activa (participante en estado 2)
- ✅ El votante no ha votado ya por este participante
- ✅ Todas las puntuaciones están entre 1-5

## 🚫 Errores Comunes

### Error de Autenticación
```
"Unauthorized" (401)
```
**Solución**: Verificar que el token JWT sea válido y no haya expirado.

### Error de Conexión SignalR
```
"Failed to start the connection"
```
**Solución**: Verificar URL, token JWT y que el servidor esté ejecutándose.

### Error de Voto Duplicado
```
"Ya has votado por este participante"
```
**Solución**: Cada código de acceso solo puede votar una vez por participante.

### Error de Validación
```
"Todas las puntuaciones deben estar entre 1 y 5"
```
**Solución**: Verificar que todos los valores sean enteros entre 1 y 5.

## 🛠️ Herramientas de Desarrollo

### Navegador
- Usar DevTools → Network → WS para ver tráfico WebSocket
- Console para ver eventos SignalR en tiempo real

### Postman
- Probar endpoint `/api/validar-acceso` para obtener tokens
- No puede probar SignalR directamente

### Herramientas SignalR
- [SignalR Connection Debugger](https://github.com/aspnet/SignalR-samples)
- Logs en consola del navegador para debug

## 📚 Recursos Adicionales

- [Documentación SignalR Client](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [Ejemplos de código en GitHub](./ejemplos-frontend-frameworks.js)
- [HTML de prueba completo](./ejemplo-frontend-externo.html)