const { defineConfig } = require('@vue/cli-service');

module.exports = defineConfig({
    transpileDependencies: true,
    productionSourceMap: false,
    configureWebpack: {
        devtool: process.env.NODE_ENV === 'production' ? false : 'source-map'
    }
})