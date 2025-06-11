# Famoria API Test Frontend

This simple React application is used to manually test the `Famoria.Api` backend.
It relies on cookie based authentication provided by the API and exposes a minimal UI
for signing in with Google and linking a Gmail account.

## Available Scripts

```bash
npm install
npm run dev
```

The development server runs on <http://localhost:3000> by default. Ensure the backend
is running on `https://localhost:5001` so the API calls succeed.

## Features

- Checks `/auth/bff/user` on load to display the current user
- "Sign in with Google" button redirects to `/auth/signin/google`
- "Link Gmail Account" button calls `/connector/link/gmail` (only when signed in)
- Uses `axios` with `withCredentials` enabled so cookies are sent with every request.

The "Refresh User Info" button can be used to re-fetch the current user details.
