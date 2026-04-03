import { createRoot } from 'react-dom/client'
import 'archon-ui/styles'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <App />
)
