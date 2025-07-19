# Function to format the time
function FormatElapsedTime {
    param($timeSpan)

    if ($timeSpan.Hours -ne 0) {
        return "$($timeSpan.Hours.ToString("00")):$($timeSpan.Minutes.ToString("00")):$($timeSpan.Seconds.ToString("00")) hours"
    } elseif ($timeSpan.Minutes -ne 0) {
        return "$($timeSpan.Minutes.ToString("00")):$($timeSpan.Seconds.ToString("00")):$($timeSpan.Milliseconds.ToString("000")) minutes"
    } else {
        return "$($timeSpan.Seconds.ToString("00")):$($timeSpan.Milliseconds.ToString("000")) seconds"
    }
}

# Check if the correct number of arguments is provided
if ($args.Count -ne 6) {
    Write-Host "Usage: $PSCommandPath <path_to_dotnet_project> <number_of_executions> <path_to_data_file> <type_of_problem> <number_of_vehicles> <max_distance>"
    exit 1
}

# Assign command line arguments to variables
$projectPath = $args[0]
$numExecutions = [int]$args[1]
$locationsPath = $args[2]
$problemType = $args[3]
$numVehicles = $args[4]
$maxDistance = $args[5]

# Check if the project directory exists
if (-not (Test-Path $projectPath -PathType Container)) {
    Write-Host "Error: Project directory '$projectPath' not found."
    exit 1
}

# Navigate to the project directory
Set-Location $projectPath

# Array to store the Process objects of the started PowerShell processes
$processes = @()

# Capture the end time
$startTime = Get-Date

# Execute the .NET project concurrently for n times with additional arguments
for ($i = 1; $i -le $numExecutions; $i++) {
    $process = Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command dotnet run $locationsPath $problemType $numVehicles $maxDistance --no-build" -NoNewWindow -PassThru
    $processes += $process
	Start-Sleep -Milliseconds 1000
}

# Wait for all PowerShell processes to finish
$processes | ForEach-Object { $_.WaitForExit() }

# Capture the end time
$endTime = Get-Date

# Calculate the elapsed time
$timeElapsed = New-TimeSpan -Start $startTime -End $endTime

# Format the elapsed time
$formattedTimeElapsed = FormatElapsedTime $timeElapsed

# Display the formatted elapsed time
Write-Host "Total elapsed time for $numExecutions executions: $formattedTimeElapsed."