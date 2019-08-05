const gulp = require('gulp');
const webpackStream = require('webpack-stream');
const livereload = require('gulp-livereload');
const path = require('path');
const webpack = require('webpack');
const errorHandler = require('./helpers/errorHandler');
const OptimizeCssAssetsPlugin = require('optimize-css-assets-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

const uiFolder = 'UI';
const frontendFolder = path.join(__dirname, '..');
const srcFolder = path.join(frontendFolder, 'src');
const isProduction = process.argv.indexOf('--production') > -1;

console.log('Source Folder:', srcFolder);
console.log('isProduction:', isProduction);

const cssVarsFiles = [
  '../src/Styles/Variables/colors',
  '../src/Styles/Variables/dimensions',
  '../src/Styles/Variables/fonts',
  '../src/Styles/Variables/animations',
  '../src/Styles/Variables/zIndexes'
].map(require.resolve);

const plugins = [
  new OptimizeCssAssetsPlugin({}),

  new webpack.DefinePlugin({
    __DEV__: !isProduction,
    'process.env.NODE_ENV': isProduction ? JSON.stringify('production') : JSON.stringify('development')
  }),

  new MiniCssExtractPlugin({
    filename: path.join('_output', uiFolder, 'Content', 'styles.css')
  })
];

const config = {
  mode: isProduction ? 'production' : 'development',
  devtool: '#source-map',

  stats: {
    children: false
  },

  watchOptions: {
    ignored: /node_modules/
  },

  entry: {
    preload: 'preload.js',
    vendor: 'vendor.js',
    index: 'index.js'
  },

  resolve: {
    modules: [
      srcFolder,
      path.join(srcFolder, 'Shims'),
      'node_modules'
    ],
    alias: {
      jquery: 'jquery/src/jquery'
    }
  },

  output: {
    filename: path.join('_output', uiFolder, '[name].js'),
    sourceMapFilename: '[file].map'
  },

  optimization: {
    chunkIds: 'named'
  },

  plugins,

  resolveLoader: {
    modules: [
      'node_modules',
      'frontend/gulp/webpack/'
    ]
  },

  module: {
    rules: [
      {
        test: /\.js?$/,
        exclude: /(node_modules|JsLibraries)/,
        use: [
          {
            loader: 'babel-loader',
            options: {
              configFile: `${frontendFolder}/babel.config.js`,
              envName: isProduction ? 'production' : 'development',
              presets: [
                [
                  '@babel/preset-env',
                  {
                    modules: false,
                    loose: true,
                    debug: false,
                    useBuiltIns: 'entry',
                    corejs: 3
                  }
                ]
              ]
            }
          }
        ]
      },

      // CSS Modules
      {
        test: /\.css$/,
        exclude: /(node_modules|globals.css)/,
        use: [
          { loader: MiniCssExtractPlugin.loader },
          {
            loader: 'css-loader',
            options: {
              importLoaders: 1,
              modules: {
                localIdentName: '[name]/[local]/[hash:base64:5]'
              }
            }
          },
          {
            loader: 'postcss-loader',
            options: {
              ident: 'postcss',
              config: {
                ctx: {
                  cssVarsFiles
                },
                path: 'frontend/postcss.config.js'
              }
            }
          }
        ]
      },

      // Global styles
      {
        test: /\.css$/,
        include: /(node_modules|globals.css)/,
        use: [
          'style-loader',
          {
            loader: 'css-loader'
          }
        ]
      },

      // Fonts
      {
        test: /\.woff(2)?(\?v=[0-9]\.[0-9]\.[0-9])?$/,
        use: [
          {
            loader: 'url-loader',
            options: {
              limit: 10240,
              mimetype: 'application/font-woff',
              emitFile: false,
              name: 'Content/Fonts/[name].[ext]'
            }
          }
        ]
      },

      {
        test: /\.(ttf|eot|eot?#iefix|svg)(\?v=[0-9]\.[0-9]\.[0-9])?$/,
        use: [
          {
            loader: 'file-loader',
            options: {
              emitFile: false,
              name: 'Content/Fonts/[name].[ext]'
            }
          }
        ]
      }
    ]
  }
};

gulp.task('webpack', () => {
  return webpackStream(config, webpack)
    .pipe(gulp.dest('./'));
});

gulp.task('webpackWatch', () => {
  config.watch = true;

  return webpackStream(config, webpack)
    .on('error', errorHandler)
    .pipe(gulp.dest('./'))
    .on('error', errorHandler)
    .pipe(livereload())
    .on('error', errorHandler);
});
