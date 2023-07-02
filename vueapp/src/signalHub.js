import Vue from 'vue'

let signalR = require('@microsoft/signalr')

export default {

    connection: null,

    connect() {

        // tento inicializar a conex�o com meu hub
        this.connection.start().then(() => {

            // no meu hub existe um m�todo "SetUser(int userId)" que seta esse meu id de usu�rio num grupo especifico
            this.connection.invoke("SetUser", _userId);

            // no meu hub quando o usu�rio clica em uma das a��es "play" ou "pause" eu emito um evento para todos os clientes registrados com o id de usu�rio setado acima
            this.connection.on("PlayPauseChange", action => {
                // quando chamado, eu emito um evento para todos os meus componentes que mudam os �cones de "play" e "pause" no sistema
                // esse m�todo Vue.prototype.$eventHub.$emit � especifico do eventBus para o Vue 
                Vue.prototype.$eventHub.$emit('play-pause-change', action)
            });

        }, () => {
            // caso d� erro, o client continua tentando
            setTimeout(() => {
                this.connect()
            }, 5000);
        });

        // caso aconte�a algum problema de conex�o, o client tenta novamente
        this.connection.onclose(() => {
            this.connect();
        });

    },

    start() {

        // configura a conex�o com seu hub do signalr
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(ENPOINT_SIGNALR_HUB)
            .build();

        // por conta de problemas de tempo de execu��o, eu defino um settimeout de 1 segundo
        setTimeout(() => {
            this.connect();
        }, 1000);
    }
}