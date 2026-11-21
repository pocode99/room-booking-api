param(
    [string]$BaseUrl = "http://localhost:5016",
    [int]$Requests = 20,
    [string]$Email = "alice@example.com",
    [string]$Password = "Passw0rd!123"
)

# Windows PowerShell 5.1 limits concurrent connections per host to 2 by default.
Add-Type -AssemblyName System.Net.Http
[System.Net.ServicePointManager]::DefaultConnectionLimit = 100

# Log in once and reuse the token for every request.
$loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" `
    -ContentType "application/json" -Body $loginBody
$token = $login.accessToken

# Pick a random day in 2028 so leftovers from earlier runs never collide with this run.
$date = (Get-Date "2028-01-01").AddDays((Get-Random -Minimum 0 -Maximum 300)).ToString("yyyy-MM-dd")
$json = @{
    roomId = 4
    title  = "Race test"
    start  = "${date}T09:00:00Z"
    end    = "${date}T10:00:00Z"
} | ConvertTo-Json

$client = New-Object System.Net.Http.HttpClient
$client.DefaultRequestHeaders.Authorization =
    New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)

Write-Host "Sending $Requests identical booking requests at once (room 4, $date 09:00-10:00)..."

# Start every request without waiting, so they all hit the API at nearly the same time.
$tasks = @()
1..$Requests | ForEach-Object {
    $content = New-Object System.Net.Http.StringContent(
        $json, [System.Text.Encoding]::UTF8, "application/json")
    $tasks += $client.PostAsync("$BaseUrl/api/bookings", $content)
}
[System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]$tasks)

# Count the status codes and remember the ids of the bookings that were created.
$statuses = @{}
$createdIds = @()
foreach ($task in $tasks) {
    $response = $task.Result
    $code = [int]$response.StatusCode
    $statuses[$code] = 1 + $statuses[$code]

    if ($code -eq 201) {
        $booking = $response.Content.ReadAsStringAsync().Result | ConvertFrom-Json
        $createdIds += $booking.id
    }
}

Write-Host ""
Write-Host "Status codes:"
$statuses.GetEnumerator() | Sort-Object Name | ForEach-Object {
    Write-Host ("  {0}: {1}" -f $_.Name, $_.Value)
}

Write-Host ""
if ($createdIds.Count -gt 1) {
    Write-Host "RACE CONDITION: $($createdIds.Count) bookings were created for the same slot." -ForegroundColor Red
} elseif ($createdIds.Count -eq 1) {
    Write-Host "OK: exactly one booking was created." -ForegroundColor Green
} else {
    Write-Host "No booking was created. Check the status codes above." -ForegroundColor Yellow
}

# Clean up, so the script can be run again without leaving data behind.
$headers = @{ Authorization = "Bearer $token" }
foreach ($id in $createdIds) {
    Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/bookings/$id" -Headers $headers | Out-Null
}