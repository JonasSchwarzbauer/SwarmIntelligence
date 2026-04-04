/**
 * plugins/vuetify.ts
 */

import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'

import { createVuetify } from 'vuetify'

export default createVuetify({
  theme: {
    defaultTheme: 'swarm',
    themes: {
      swarm: {
        dark: true,
        colors: {
          background:          '#060b13',
          surface:             '#0a1628',
          'surface-variant':   '#0d1e36',
          'surface-bright':    '#142240',
          primary:             '#00c8ff',
          'primary-darken-1':  '#0090bf',
          secondary:           '#a78bfa',
          success:             '#10b981',
          'success-darken-1':  '#059669',
          warning:             '#f59e0b',
          error:               '#ef4444',
          info:                '#60a5fa',
          'on-background':     '#c8e0f8',
          'on-surface':        '#c8e0f8',
          'on-surface-variant':'#7aafcc',
          'on-primary':        '#00080f',
          'on-secondary':      '#0a0018',
          'on-success':        '#001a10',
          'on-warning':        '#1a0d00',
          'on-error':          '#1a0000',
        },
      },
    },
  },
})
