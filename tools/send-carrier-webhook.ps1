<#
.SYNOPSIS
  Signs a carrier webhook body and posts it.
.EXAMPLE
  ./tools/send-carrier-webhook.ps1 -Secret "dev-secret" -File ./tools/samples/webhook-delivered.json
.EXAMPLE
  ./tools/send-carrier-webhook.ps1 -Secret "dev-secret" -File ./tools/samples/webhook-delivered.json -Timestamp 1700000000
.EXAMPLE
  ./tools/send-carrier-webhook.ps1 -Secret "dev-secret" -File ./tools/samples/webhook-delivered.json -Tamper
#>
param(
    [Parameter(Mandatory = $true)] [string] $Secret,
    [Parameter(Mandatory = $true)] [string] $File,
    [string] $Url = "http://localhost:5080/api/webhooks/carrier",
    [string] $Carrier = "lbc-express",
    [long] $Timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds(),
    [switch] $Tamper
)

$body = [System.IO.File]::ReadAllText((Resolve-Path $File))
$utf8 = [System.Text.Encoding]::UTF8
$hmac = [System.Security.Cryptography.HMACSHA256]::new($utf8.GetBytes($Secret))
try {
    $hash = $hmac.ComputeHash($utf8.GetBytes("$Timestamp.$body"))
}
finally {
    $hmac.Dispose()
}
$signature = -join ($hash | ForEach-Object { $_.ToString("x2") })

if ($Tamper) {
    $body = $body.Replace("Delivered", "Cancelled").Replace("InTransit", "Delivered")
}

$headers = @{
    "X-Courierly-Carrier"   = $Carrier
    "X-Courierly-Signature" = "t=$Timestamp,v1=$signature"
}

Write-Host "POST $Url  t=$Timestamp"
try {
    $response = Invoke-WebRequest -Uri $Url -Method Post -Headers $headers -ContentType "application/json" -Body $utf8.GetBytes($body) -UseBasicParsing
    Write-Host "$($response.StatusCode) $($response.StatusDescription)"
    Write-Host $response.Content
}
catch {
    if ($_.Exception.Response) {
        Write-Host "$([int]$_.Exception.Response.StatusCode) $($_.Exception.Response.StatusCode)"
    }
    else {
        throw
    }
}
