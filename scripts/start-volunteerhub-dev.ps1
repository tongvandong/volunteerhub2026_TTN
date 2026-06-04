param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Start-HiddenProcess {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string]$Command
    )

    Start-Process -FilePath "powershell" -WindowStyle Hidden -ArgumentList @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-Command",
        "Set-Location '$WorkingDirectory'; $Command"
    ) | Out-Null

    Write-Host "Started $Name"
}

Start-HiddenProcess "AuthService" "$Root\BaseCore.AuthService" "dotnet run --urls http://localhost:5002"
Start-HiddenProcess "EventService" "$Root\BaseCore.EventService" "dotnet run --urls http://localhost:5003"
Start-HiddenProcess "FinanceService" "$Root\BaseCore.FinanceService" "dotnet run --urls http://localhost:5004"
Start-HiddenProcess "ApiGateway" "$Root\BaseCore.ApiGateway" "dotnet run --urls http://localhost:5000"
Start-HiddenProcess "CertificateWorker" "$Root\BaseCore.CertificateWorker" "cargo run"
Start-HiddenProcess "WebClient" "$Root\BaseCore.WebClient" "npm run dev -- --host 127.0.0.1"

Write-Host "VolunteerHub dev stack is starting. Open http://localhost:3000"
