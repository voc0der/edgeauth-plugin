# EdgeAuth (Jellyfin plugin POC)

A minimal Jellyfin plugin that exposes a localhost-only HTTP endpoint the reverse proxy can call via `auth_request` to validate clients by **Jellyfin token** or a **short-lived IP allow** (for onboarding without mTLS).

> This is a POC: it hosts a tiny Kestrel server inside the plugin on `127.0.0.1:5577`. Later, you can map routes directly into Jellyfin's ASP.NET Core pipeline and remove Kestrel.

## Endpoints

- `GET http://127.0.0.1:5577/validate`
  - Headers:
    - `X-Forwarded-For: <client-ip>`
    - One of: `X-Emby-Token`, `X-MediaBrowser-Token`, or `Authorization: MediaBrowser Token="<token>"`
  - 200 if token valid OR IP is temporarily allowed; 401 otherwise.
  - Response headers: `X-Auth-Method: token|ip-temp`

- `POST http://127.0.0.1:5577/allow`
  - Headers: `X-Admin-Secret: <secret>` (configured in plugin)
  - Body: `{"ip":"203.0.113.45","ttlSeconds":300}`

## NGINX (auth_request) example

```
location = /_auth {
  internal;
  proxy_pass http://127.0.0.1:5577/validate;
  proxy_set_header X-Forwarded-For $remote_addr;
  proxy_set_header Authorization $http_authorization;
  proxy_set_header X-Emby-Token $http_x_emby_token;
  proxy_set_header X-MediaBrowser-Token $http_x_mediabrowser_token;
  proxy_pass_request_body off;
  proxy_set_header Content-Length "";
}

location / {
  if ($ssl_client_verify = SUCCESS) { proxy_pass https://jellyfin_backend; break; }
  auth_request /_auth;
  proxy_pass https://jellyfin_backend;
}
```

## Build

- Requires .NET 8 SDK.
- The `MediaBrowser.*` package versions in the `.csproj` **must match** your Jellyfin server. Adjust as needed.
- Build:
  ```bash
  dotnet build src/EdgeAuth/EdgeAuth.csproj -c Release
  ```

## Install

Create `plugins/EdgeAuth/` under your Jellyfin config/plugins path and copy:
```
EdgeAuth.dll
plugin.json
```

Restart Jellyfin. Configure `AdminSecret` and TTL in the plugin settings (or edit `PluginConfiguration` default and rebuild).

## GitHub Actions

This repo includes `.github/workflows/build.yml` to build and upload a zip artifact on pushes/tags.

## Security notes

- The internal Kestrel listener binds to `127.0.0.1:5577` only. Your reverse proxy must be on the same host or use a localhost bind mount/network namespace approach.
- Keep IP TTL short (2–5 min).
- Prefer mTLS or valid tokens; IP allow is for onboarding only.