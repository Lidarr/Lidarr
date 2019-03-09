const reload = require('require-nocache')(module);
const browsers = require('./browsers');

module.exports = (ctx, configPath, options) => {
  const config = {
    plugins: {
      'postcss-mixins': {
        mixinsDir: [
          'frontend/src/Styles/Mixins'
        ]
      },
      'postcss-simple-vars': {
        variables: () =>
          ctx.options.cssVarsFiles.reduce((acc, vars) => {
            return Object.assign(acc, reload(vars));
          }, {})
      },
      'postcss-color-function': {},
      'postcss-nested': {},
      autoprefixer: {
        browsers
      }
    }
  };

  return config;
};
