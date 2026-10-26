# Issues a new refreshable chat token for the bot account and stores it on Heroku.
#
# Run it yourself in a terminal (never paste tokens into chats or logs):
#   .\scripts\get-bot-token.ps1
#
# The bot seeds its token store from bot_access_token/bot_refresh_token and refreshes the
# token on its own from then on (BotTokenService), so this is only needed once, or again
# if the token gets revoked.
param(
    [string]$App = "kinobotz",
    [string]$RedirectUri = "https://localhost"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Web

$config = heroku config -a $App --json | ConvertFrom-Json
$clientId = $config.twitch_client_id
$clientSecret = $config.twitch_client_secret
$botLogin = if ($config.twitch_username) { $config.twitch_username } else { "kinobotz" }

if (-not $clientId -or -not $clientSecret) { throw "twitch_client_id / twitch_client_secret not set on $App" }

$state = [guid]::NewGuid().ToString("N")
$authorizeUrl = "https://id.twitch.tv/oauth2/authorize?response_type=code&client_id=$clientId" +
    "&redirect_uri=$([uri]::EscapeDataString($RedirectUri))&scope=chat%3Aread+chat%3Aedit&force_verify=true&state=$state"

Write-Host ""
Write-Host "1. A browser window will open. Log in to Twitch as '$botLogin' (use a private window if you're logged in as someone else)."
Write-Host "2. Authorize. The page will fail to load - that's expected."
Write-Host "3. Copy the whole address from the address bar and paste it here."
Write-Host ""
Write-Host "If the browser doesn't open, visit:"
Write-Host $authorizeUrl
Start-Process $authorizeUrl

$redirected = Read-Host "Address bar URL"
$query = [System.Web.HttpUtility]::ParseQueryString(([uri]$redirected).Query)
if ($query["error"]) { throw "Twitch returned an error: $($query["error_description"])" }
if ($query["state"] -ne $state) { throw "State mismatch - paste the URL from this run's authorization" }
$code = $query["code"]
if (-not $code) { throw "No code in that URL" }

$token = Invoke-RestMethod -Method Post -Uri "https://id.twitch.tv/oauth2/token" -Body @{
    client_id     = $clientId
    client_secret = $clientSecret
    code          = $code
    grant_type    = "authorization_code"
    redirect_uri  = $RedirectUri
}

$validation = Invoke-RestMethod -Uri "https://id.twitch.tv/oauth2/validate" -Headers @{ Authorization = "OAuth $($token.access_token)" }
if ($validation.login -ne $botLogin) {
    throw "Token belongs to '$($validation.login)', not '$botLogin'. Log in as '$botLogin' and run again."
}

heroku config:set "bot_access_token=$($token.access_token)" "bot_refresh_token=$($token.refresh_token)" -a $App | Out-Null

Write-Host ""
Write-Host "Done: refreshable token for '$($validation.login)' (scopes: $($validation.scopes -join ', ')) saved on $App. The dynos will restart."
