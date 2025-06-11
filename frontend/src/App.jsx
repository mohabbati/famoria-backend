import { useEffect, useState } from 'react'
import axios from 'axios'
import './App.css'

axios.defaults.withCredentials = true
const API_BASE = 'https://localhost:5001'

function App() {
  const [user, setUser] = useState(null)
  const [loadingUser, setLoadingUser] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    fetchUser()
  }, [])

  const fetchUser = async () => {
    setLoadingUser(true)
    try {
      const res = await axios.get(`${API_BASE}/auth/bff/user`)
      setUser(res.data)
    } catch (err) {
      setUser(null)
    } finally {
      setLoadingUser(false)
    }
  }


  const handleGoogleLogin = () => {
    window.location.href = `${API_BASE}/auth/signin/google?returnUrl=${encodeURIComponent(window.location.origin)}`
  }

  const handleLinkGmail = () => {
    window.location.href = `${API_BASE}/connector/link/gmail?returnUrl=${encodeURIComponent(window.location.origin)}`
  }

  return (
    <div className="container">
      <h1>Famoria API Test</h1>
      {loadingUser ? (
        <p>Loading...</p>
      ) : user ? (
        <p>Signed in as {user.email || user.name}</p>
      ) : (
        <p>Not signed in</p>
      )}
      <div className="buttons">
        <button onClick={handleGoogleLogin}>Sign in with Google</button>
        <button onClick={handleLinkGmail} disabled={!user}>Link Gmail Account</button>
        <button onClick={fetchUser}>Refresh User Info</button>
      </div>
      
      {error && <p className="error">{error.toString()}</p>}
    </div>
  )
}

export default App
