[CmdletBinding()]
param (
    [Parameter(Mandatory=$true, ValueFromPipeline=$false, HelpMessage="Enter the path to the XLS file to convert.")]
    [string]$PathXLS
)

# Check if input file exists
if (-not (Test-Path $PathXLS -PathType Leaf)) {
    Write-Error "File '$PathXLS' does not exist or is not a file."
    exit 1
}

# Check file extension (optional, but good practice)
$extension = [System.IO.Path]::GetExtension($PathXLS)
if ($extension -ne ".xls") {
    Write-Warning "This script is optimized for .xls files. For other Excel types (.xlsx), different methods might be better."
}

$ExcelApp = $null
$Workbook = $null
$Worksheet = $null
$FullPathOfXLS = $null # Initialize to handle potential errors before assignment
$PathCSV = $null       # Initialize

try {
    Write-Host "[LOG] Attempting to open Excel application in the background..."
    $ExcelApp = New-Object -ComObject Excel.Application
    Write-Host "[LOG] Excel.Application object created: $($ExcelApp -ne $null)"
    $ExcelApp.Visible = $false
    Write-Host "[LOG] ExcelApp.Visible set to false"
    $ExcelApp.DisplayAlerts = $false
    Write-Host "[LOG] ExcelApp.DisplayAlerts set to false"
    $ExcelApp.Interactive = $false
    Write-Host "[LOG] ExcelApp.Interactive set to false"

    # Ensure $PathXLS is an absolute path
    if (-not ([System.IO.Path]::IsPathRooted($PathXLS))) {
        Write-Host "[LOG] PathXLS '$PathXLS' is relative. Converting to absolute."
        $FullPathOfXLS = Join-Path -Path (Get-Location).Path -ChildPath $PathXLS
        Write-Host "[LOG] Converted relative XLS path '$PathXLS' to absolute: '$FullPathOfXLS'"
    } else {
        $FullPathOfXLS = $PathXLS
        Write-Host "[LOG] PathXLS '$PathXLS' is already absolute."
    }
    Write-Host "[LOG] Absolute XLS path determined: '$FullPathOfXLS'"

    Write-Host "[LOG] Attempting to open workbook: '$FullPathOfXLS'..."
    $Workbook = $ExcelApp.Workbooks.Open($FullPathOfXLS)
    Write-Host "[LOG] Workbook opened: $($Workbook -ne $null). Name: $($Workbook.Name)"
    # Assuming we are interested in the first worksheet
    Write-Host "[LOG] Attempting to access worksheet 1..."
    $Worksheet = $Workbook.Worksheets.Item(1)
    Write-Host "[LOG] Worksheet 1 accessed: $($Worksheet -ne $null). Name: $($Worksheet.Name)"

    $FileName = [System.IO.Path]::GetFileNameWithoutExtension($FullPathOfXLS)
    $DirectoryName = [System.IO.Path]::GetDirectoryName($FullPathOfXLS)
    Write-Host "[LOG] FileName: $FileName, DirectoryName: $DirectoryName"
    
    $PathCSV = Join-Path -Path $DirectoryName -ChildPath ($FileName + ".csv")
    if (-not ([System.IO.Path]::IsPathRooted($PathCSV))) {
        Write-Host "[LOG] PathCSV '$PathCSV' is relative. Converting to absolute."
        $PathCSV = Join-Path -Path (Get-Location).Path -ChildPath $PathCSV
        Write-Host "[LOG] Ensured CSV path is absolute: '$PathCSV'"
    } else {
        Write-Host "[LOG] PathCSV '$PathCSV' is already absolute."
    }
    Write-Host "[LOG] Target CSV path determined: '$PathCSV'"


    Write-Host "[LOG] Converting file '$FullPathOfXLS' to '$PathCSV'..."
    
    if (Test-Path $PathCSV) {
        Write-Warning "[LOG] Target CSV file '$PathCSV' already exists. It will be deleted before writing."
        try {
            Remove-Item -Path $PathCSV -Force -ErrorAction Stop
            Write-Host "[LOG] Existing CSV file '$PathCSV' removed. Pausing briefly..."
            Start-Sleep -Milliseconds 500
        }
        catch {
            Write-Error "[LOG] Could not delete existing CSV file '$PathCSV': $($_.Exception.Message)"
        }
    }
    
    Write-Host "[LOG] Preparing to convert worksheet to CSV using Excel's SaveAs method..."
    
    try {
        $tempWorkbook = $null
        try {
            if (Test-Path $PathCSV) {
                Write-Host "[LOG] Ensuring target CSV file '$PathCSV' is removed before SaveAs (redundant check)..."
                try {
                    Remove-Item -Path $PathCSV -Force -ErrorAction Stop
                    Write-Host "[LOG] File '$PathCSV' removed (redundant check)."
                }
                catch {
                    Write-Warning "[LOG] Could not delete existing CSV file '$PathCSV' immediately before SaveAs (redundant check): $($_.Exception.Message)."
                }
            }
            Write-Host "[LOG] Attempting to copy worksheet '$($Worksheet.Name)' to a new temporary workbook..."
            $Worksheet.Copy()
            Write-Host "[LOG] Worksheet.Copy() command executed."
            $tempWorkbook = $ExcelApp.ActiveWorkbook
            Write-Host "[LOG] Active temporary workbook obtained: $($tempWorkbook -ne $null). Name: $($tempWorkbook.Name)"
            
            Write-Host "[LOG] Attempting to SaveAs temporary workbook to '$PathCSV' with FileFormat 6 and Local=true..."
            # Parameters for SaveAs: FileName, FileFormat, Password, WriteResPassword, ReadOnlyRecommended, CreateBackup, AccessMode, ConflictResolution, AddToMru, TextCodepage, TextVisualLayout, Local
            $tempWorkbook.SaveAs($PathCSV, [Type]::Missing, [Type]::Missing, [Type]::Missing, [Type]::Missing, [Type]::Missing, [Microsoft.Office.Interop.Excel.XlSaveAsAccessMode]::xlNoChange, [Type]::Missing, [Type]::Missing, [Type]::Missing, [Type]::Missing, $true)
            Write-Host "[LOG] tempWorkbook.SaveAs() command executed."
            
            Write-Host "[LOG] Conversion successful. File saved as '$PathCSV' (attempted with local settings for separator)."
        }
        catch {
            Write-Error "[LOG] Error in inner try (SaveAs process) for '$PathCSV': $($_.Exception.Message)"
            Write-Error "[LOG] StackTrace: $($_.ScriptStackTrace)"
            throw
        }
        finally {
            Write-Host "[LOG] Entering 'finally' block for temporary workbook cleanup..."
            if ($tempWorkbook -ne $null) {
                Write-Host "[LOG] Temporary workbook is not null. Attempting to close..."
                $tempWorkbook.Close($false)
                Write-Host "[LOG] Temporary workbook closed. Attempting to release COM object..."
                $ReleaseResult = [System.Runtime.InteropServices.Marshal]::ReleaseComObject($tempWorkbook)
                Write-Host "[LOG] COM object for temporary workbook released. Marshal.ReleaseComObject returned: $ReleaseResult"
                $tempWorkbook = $null
            } else {
                Write-Host "[LOG] Temporary workbook was null, no close/release needed in this specific finally block."
            }
            Write-Host "[LOG] Exiting 'finally' block for temporary workbook cleanup."
        }
    }
    catch {
        Write-Error "[LOG] Error in outer try (CSV conversion step): $($_.Exception.Message)"
        Write-Error "[LOG] StackTrace: $($_.ScriptStackTrace)"
        throw
    }
}
catch {
    $ErrorMessage = if ($_.Exception) { $_.Exception.Message } else { "Unknown error" }
    Write-Error "[LOG] Error in main try-catch block: $ErrorMessage"
    Write-Error "[LOG] StackTrace: $($_.ScriptStackTrace)"
    
    $ErrorFilePath = if ($FullPathOfXLS) { $FullPathOfXLS } else { $PathXLS }
    Write-Error "[LOG] Ensure Microsoft Excel is installed and the file '$ErrorFilePath' is not corrupted."
    Write-Error "[LOG] Additionally, check if the file '$ErrorFilePath' is not open in another program."
}
finally {
    Write-Host "[LOG] Entering main 'finally' block for resource cleanup..."
    if ($Worksheet -ne $null) {
        Write-Host "[LOG] Worksheet is not null. Attempting to release COM object..."
        $ReleaseResult = ([System.Runtime.InteropServices.Marshal]::ReleaseComObject($Worksheet))
        Write-Host "[LOG] COM object for worksheet released. Marshal.ReleaseComObject returned: $ReleaseResult"
        $Worksheet = $null
    } else {
        Write-Host "[LOG] Worksheet was null."
    }
    if ($Workbook -ne $null) {
        Write-Host "[LOG] Main workbook is not null. Attempting to close..."
        $Workbook.Close($false)
        Write-Host "[LOG] Main workbook closed. Attempting to release COM object..."
        $ReleaseResult = ([System.Runtime.InteropServices.Marshal]::ReleaseComObject($Workbook))
        Write-Host "[LOG] COM object for main workbook released. Marshal.ReleaseComObject returned: $ReleaseResult"
        $Workbook = $null
    } else {
        Write-Host "[LOG] Main workbook was null."
    }
    if ($ExcelApp -ne $null) {
        Write-Host "[LOG] ExcelApp is not null. Attempting to quit..."
        $ExcelApp.Quit()
        Write-Host "[LOG] Excel application quit command issued. Attempting to release COM object..."
        $ReleaseResult = ([System.Runtime.InteropServices.Marshal]::ReleaseComObject($ExcelApp))
        Write-Host "[LOG] COM object for Excel application released. Marshal.ReleaseComObject returned: $ReleaseResult"
        $ExcelApp = $null
    } else {
        Write-Host "[LOG] ExcelApp was null."
    }
    
    Write-Host "[LOG] Forcing garbage collection..."
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
    Write-Host "[LOG] Garbage collection complete."
    Write-Host "[LOG] Excel COM resources should now be released."
}