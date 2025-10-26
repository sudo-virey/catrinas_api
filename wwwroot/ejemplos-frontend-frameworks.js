// ===============================================
// EJEMPLO PARA REACT.JS
// ===============================================

import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useState, useEffect } from 'react';

const CatrinasVotingComponent = () => {
    const [connection, setConnection] = useState(null);
    const [jwtToken, setJwtToken] = useState(null);
    const [isConnected, setIsConnected] = useState(false);
    const [votingData, setVotingData] = useState(null);
    const [voteScores, setVoteScores] = useState({
        atuendo: 3,
        maquillaje: 3,
        tradiciones: 3,
        pasarela: 3,
        interaccion: 3
    });

    // PASO 1: Obtener Token JWT
    const obtenerToken = async (accessCode) => {
        try {
            const response = await fetch('http://localhost:5001/api/validar-acceso', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ usuario: accessCode })
            });

            if (response.ok) {
                const data = await response.json();
                setJwtToken(data.token);
                return data.token;
            } else {
                throw new Error('Código de acceso inválido');
            }
        } catch (error) {
            console.error('Error obteniendo token:', error);
            throw error;
        }
    };

    // PASO 2: Conectar SignalR
    const conectarSignalR = async (token) => {
        try {
            const newConnection = new HubConnectionBuilder()
                .withUrl('http://localhost:5001/chatHub', {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

            // Configurar eventos
            newConnection.on('NuevaVotacionParaVotar', (data) => {
                console.log('Nueva votación:', data);
                setVotingData(data);
            });

            newConnection.on('VotoConfirmado', (data) => {
                console.log('Voto confirmado:', data);
                alert(`Voto confirmado para ${data.participante}. Puntuación: ${data.puntosFinal}/10`);
                setVotingData(null); // Ocultar formulario de votación
            });

            newConnection.on('Error', (error) => {
                console.error('Error del servidor:', error);
                alert(`Error: ${error}`);
            });

            newConnection.on('VotacionTerminada', (data) => {
                console.log('Votación terminada:', data);
                setVotingData(null);
            });

            // Conectar
            await newConnection.start();
            setConnection(newConnection);
            setIsConnected(true);
            console.log('Conectado a SignalR');

        } catch (error) {
            console.error('Error conectando SignalR:', error);
            throw error;
        }
    };

    // PASO 3: Enviar Voto
    const enviarVoto = async () => {
        if (!connection || !isConnected) {
            alert('No hay conexión activa');
            return;
        }

        try {
            await connection.invoke(
                'EnviarVoto',
                voteScores.atuendo,
                voteScores.maquillaje,
                voteScores.tradiciones,
                voteScores.pasarela,
                voteScores.interaccion
            );
        } catch (error) {
            console.error('Error enviando voto:', error);
            alert(`Error enviando voto: ${error.message}`);
        }
    };

    // Desconectar al desmontar el componente
    useEffect(() => {
        return () => {
            if (connection) {
                connection.stop();
            }
        };
    }, [connection]);

    return (
        <div>
            <h2>🎭 Sistema de Votación Catrinas</h2>
            
            {/* Formulario de acceso */}
            {!jwtToken && (
                <div>
                    <input 
                        type="text" 
                        placeholder="Código de acceso (ej: JUE001)"
                        onKeyPress={async (e) => {
                            if (e.key === 'Enter') {
                                try {
                                    const token = await obtenerToken(e.target.value);
                                    await conectarSignalR(token);
                                } catch (error) {
                                    alert('Error de acceso: ' + error.message);
                                }
                            }
                        }}
                    />
                </div>
            )}

            {/* Estado de conexión */}
            <div>
                Estado: {isConnected ? '✅ Conectado' : '❌ Desconectado'}
            </div>

            {/* Formulario de votación */}
            {votingData && (
                <div>
                    <h3>Votando por: {votingData.participante}</h3>
                    
                    {Object.keys(voteScores).map(categoria => (
                        <div key={categoria}>
                            <label>{categoria.charAt(0).toUpperCase() + categoria.slice(1)}:</label>
                            <select 
                                value={voteScores[categoria]}
                                onChange={(e) => setVoteScores({
                                    ...voteScores,
                                    [categoria]: parseInt(e.target.value)
                                })}
                            >
                                {[1,2,3,4,5].map(num => (
                                    <option key={num} value={num}>{num}</option>
                                ))}
                            </select>
                        </div>
                    ))}
                    
                    <button onClick={enviarVoto}>🗳️ ENVIAR VOTO</button>
                </div>
            )}
        </div>
    );
};

export default CatrinasVotingComponent;

// ===============================================
// EJEMPLO PARA VUE.JS 3
// ===============================================

// composables/useCatrinas.js
import { ref, onUnmounted } from 'vue';
import { HubConnectionBuilder } from '@microsoft/signalr';

export function useCatrinas() {
    const connection = ref(null);
    const jwtToken = ref(null);
    const isConnected = ref(false);
    const votingData = ref(null);
    const voteScores = ref({
        atuendo: 3,
        maquillaje: 3,
        tradiciones: 3,
        pasarela: 3,
        interaccion: 3
    });

    const obtenerToken = async (accessCode) => {
        const response = await fetch('http://localhost:5001/api/validar-acceso', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuario: accessCode })
        });

        if (response.ok) {
            const data = await response.json();
            jwtToken.value = data.token;
            return data.token;
        } else {
            throw new Error('Código de acceso inválido');
        }
    };

    const conectarSignalR = async (token) => {
        const newConnection = new HubConnectionBuilder()
            .withUrl('http://localhost:5001/chatHub', {
                accessTokenFactory: () => token
            })
            .build();

        newConnection.on('NuevaVotacionParaVotar', (data) => {
            votingData.value = data;
        });

        newConnection.on('VotoConfirmado', (data) => {
            alert(`Voto confirmado para ${data.participante}`);
            votingData.value = null;
        });

        newConnection.on('Error', (error) => {
            alert(`Error: ${error}`);
        });

        await newConnection.start();
        connection.value = newConnection;
        isConnected.value = true;
    };

    const enviarVoto = async () => {
        if (!connection.value) return;

        await connection.value.invoke(
            'EnviarVoto',
            voteScores.value.atuendo,
            voteScores.value.maquillaje,
            voteScores.value.tradiciones,
            voteScores.value.pasarela,
            voteScores.value.interaccion
        );
    };

    onUnmounted(() => {
        if (connection.value) {
            connection.value.stop();
        }
    });

    return {
        jwtToken,
        isConnected,
        votingData,
        voteScores,
        obtenerToken,
        conectarSignalR,
        enviarVoto
    };
}

// ===============================================
// EJEMPLO PARA VANILLA JAVASCRIPT
// ===============================================

class CatrinasAPI {
    constructor(baseUrl = 'http://localhost:5001') {
        this.baseUrl = baseUrl;
        this.connection = null;
        this.jwtToken = null;
        this.isConnected = false;
    }

    // PASO 1: Obtener Token JWT
    async obtenerToken(accessCode) {
        try {
            const response = await fetch(`${this.baseUrl}/api/validar-acceso`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ usuario: accessCode })
            });

            if (response.ok) {
                const data = await response.json();
                this.jwtToken = data.token;
                return data;
            } else {
                throw new Error(`HTTP ${response.status}: ${await response.text()}`);
            }
        } catch (error) {
            console.error('Error obteniendo token:', error);
            throw error;
        }
    }

    // PASO 2: Conectar SignalR
    async conectar() {
        if (!this.jwtToken) {
            throw new Error('Primero debes obtener un token JWT');
        }

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(`${this.baseUrl}/chatHub`, {
                    accessTokenFactory: () => this.jwtToken
                })
                .withAutomaticReconnect()
                .build();

            // Configurar eventos
            this.connection.on('NuevaVotacionParaVotar', (data) => {
                this.onNuevaVotacion(data);
            });

            this.connection.on('VotoConfirmado', (data) => {
                this.onVotoConfirmado(data);
            });

            this.connection.on('Error', (error) => {
                this.onError(error);
            });

            this.connection.on('VotacionTerminada', (data) => {
                this.onVotacionTerminada(data);
            });

            // Conectar
            await this.connection.start();
            this.isConnected = true;
            console.log('✅ Conectado a Catrinas API');

        } catch (error) {
            console.error('❌ Error conectando:', error);
            throw error;
        }
    }

    // PASO 3: Enviar Voto
    async enviarVoto(atuendo, maquillaje, tradiciones, pasarela, interaccion) {
        if (!this.isConnected || !this.connection) {
            throw new Error('No hay conexión activa');
        }

        // Validar rangos
        const scores = [atuendo, maquillaje, tradiciones, pasarela, interaccion];
        if (scores.some(score => score < 1 || score > 5)) {
            throw new Error('Todas las puntuaciones deben estar entre 1 y 5');
        }

        try {
            await this.connection.invoke('EnviarVoto', atuendo, maquillaje, tradiciones, pasarela, interaccion);
        } catch (error) {
            console.error('❌ Error enviando voto:', error);
            throw error;
        }
    }

    // Desconectar
    async desconectar() {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            console.log('📡 Desconectado de Catrinas API');
        }
    }

    // Eventos que puedes sobrescribir
    onNuevaVotacion(data) {
        console.log('🚀 Nueva votación:', data);
    }

    onVotoConfirmado(data) {
        console.log('✅ Voto confirmado:', data);
    }

    onError(error) {
        console.error('❌ Error:', error);
    }

    onVotacionTerminada(data) {
        console.log('🏁 Votación terminada:', data);
    }
}

// Uso de la clase
/*
const api = new CatrinasAPI();

// Personalizar eventos
api.onNuevaVotacion = (data) => {
    document.getElementById('participante').textContent = data.participante;
    document.getElementById('formulario-voto').style.display = 'block';
};

api.onVotoConfirmado = (data) => {
    alert(`¡Voto confirmado! Puntuación: ${data.puntosFinal}/10`);
};

// Conectar y usar
async function inicializar() {
    try {
        await api.obtenerToken('JUE001');
        await api.conectar();
        
        // Enviar voto cuando sea necesario
        await api.enviarVoto(5, 4, 5, 3, 4);
    } catch (error) {
        console.error('Error:', error);
    }
}
*/