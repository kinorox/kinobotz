import Vue from 'vue'

let signalR = require('@microsoft/signalr')

export default {

    connection: null,

    connect() {

        // tento inicializar a conexão com meu hub
        this.connection.start().then(() => {

            // no meu hub existe um método "SetUser(int userId)" que seta esse meu id de usuário num grupo especifico
            this.connection.invoke("SetUser", _userId);

            // no meu hub quando o usuário clica em uma das ações "play" ou "pause" eu emito um evento para todos os clientes registrados com o id de usuário setado acima
            this.connection.on("PlayPauseChange", action => {
                // quando chamado, eu emito um evento para todos os meus componentes que mudam os ícones de "play" e "pause" no sistema
                // esse método Vue.prototype.$eventHub.$emit é especifico do eventBus para o Vue 
                Vue.prototype.$eventHub.$emit('play-pause-change', action)
            });

        }, () => {
            // caso dê erro, o client continua tentando
            setTimeout(() => {
                this.connect()
            }, 5000);
        });

        // caso aconteça algum problema de conexão, o client tenta novamente
        this.connection.onclose(() => {
            this.connect();
        });

    },

    start() {

        // configura a conexão com seu hub do signalr
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(ENPOINT_SIGNALR_HUB)
            .build();

        // por conta de problemas de tempo de execução, eu defino um settimeout de 1 segundo
        setTimeout(() => {
            this.connect();
        }, 1000);
    }
}