import { createRoot } from 'react-dom/client'
import 'archon-ui/styles'
import './index.css'
import App from './App.tsx'

const loader = document.getElementById('initial-loader')
if (loader) loader.remove()

createRoot(document.getElementById('root')!).render(
  <App />
)
