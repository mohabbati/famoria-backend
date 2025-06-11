# Famoria Backend

This repository contains the Famoria API along with several workers and a small Blazor WebAssembly test client.

## Running the Test Client

1. Start the API and its proxy. In separate terminals run:

```bash
dotnet run --project src/Famoria.Api

dotnet run --project src/Famoria.Api.Proxy
```

The proxy exposes the API at `https://localhost:5001`.

2. Run the test client:

```bash
dotnet run --project src/Famoria.TestClient
```

The Blazor development server will open a browser window on `https://localhost:19759`. Use the `/test` page to sign in and link Gmail accounts.

