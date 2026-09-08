# Local Cursor helper: screenshot + mouse/keyboard on THIS PC only.
# Start it yourself. Close the terminal to stop. No network, no hidden process.
param(
    [Parameter(Position = 0)]
    [string]$Command = "help",
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Rest
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$CaptureDir = Join-Path $Root "captures"
$NativeCs = Join-Path $Root "AgentNative.cs"
New-Item -ItemType Directory -Force -Path $CaptureDir | Out-Null

if (-not ("AgentNative" -as [type])) {
    $code = Get-Content -Raw -Path $NativeCs
    Add-Type -TypeDefinition $code -ReferencedAssemblies @(
        "System.Drawing",
        "System.Windows.Forms",
        "System.Drawing.Primitives"
    ) -ErrorAction Stop
}

function Get-Vk([string]$Name) {
    $n = $Name.ToLowerInvariant()
    $map = @{
        "rshift" = 0xA1; "rightshift" = 0xA1; "right-shift" = 0xA1
        "lshift" = 0xA0; "shift" = 0x10
        "ctrl" = 0x11; "control" = 0x11; "lctrl" = 0xA2; "rctrl" = 0xA3
        "alt" = 0x12; "lalt" = 0xA4; "ralt" = 0xA5
        "win" = 0x5B
        "esc" = 0x1B; "escape" = 0x1B
        "enter" = 0x0D; "return" = 0x0D
        "space" = 0x20; "tab" = 0x09; "backspace" = 0x08
        "up" = 0x26; "down" = 0x28; "left" = 0x25; "right" = 0x27
        "insert" = 0x2D; "delete" = 0x2E; "home" = 0x24; "end" = 0x23
        "prior" = 0x21; "next" = 0x22; "pageup" = 0x21; "pagedown" = 0x22
        "f1" = 0x70; "f2" = 0x71; "f3" = 0x72; "f4" = 0x73
        "f5" = 0x74; "f6" = 0x75; "f7" = 0x76; "f8" = 0x77
        "f9" = 0x78; "f10" = 0x79; "f11" = 0x7A; "f12" = 0x7B
    }
    if ($map.ContainsKey($n)) { return [uint16]$map[$n] }
    if ($n.Length -eq 1) {
        $ch = $n[0]
        if ($ch -ge "a" -and $ch -le "z") { return [uint16][int][char]([string]$ch).ToUpperInvariant() }
        if ($ch -ge "0" -and $ch -le "9") { return [uint16][int][char]$ch }
    }
    throw "Unknown key '$Name'"
}

function Show-Help {
    @"
agent.ps1 - local screen/input helper for Cursor (this PC only)

  screenshot [path]              Capture the desktop to PNG
  shot-window <title> [path]     Capture a window (e.g. Minecraft)
  info                           Virtual screen size
  windows                        List windows with titles
  focus <title>                  Bring a window to the front
  move <x> <y>                   Move the cursor
  click <x> <y> [left|right]     Click
  dblclick <x> <y>               Double left click
  scroll <delta>                 Wheel (120 = one notch)
  key <name>                     Tap a key (rshift, esc, e, f1, ...)
  down <name> / up <name>        Hold or release
  hold <name> <ms>               Hold a key for N milliseconds
  type <text>                    Type ASCII text

Minecraft: windowed or borderless. Fullscreen exclusive often captures black.
Close this terminal / stop the command to end control.
"@
}

switch ($Command.ToLowerInvariant()) {
    "help" { Show-Help; break }
    "info" {
        Write-Output ([AgentNative]::ScreenInfo())
        break
    }
    "windows" {
        Write-Output ([AgentNative]::ListWindows())
        break
    }
    "focus" {
        $title = $Rest[0]
        if (-not $title) { throw "focus requires a window title substring" }
        $ok = [AgentNative]::FocusWindow($title)
        if (-not $ok) { throw "No window matched '$title'" }
        Write-Output "focused $title"
        break
    }
    "screenshot" {
        $path = $Rest[0]
        if (-not $path) { $path = Join-Path $CaptureDir "latest.png" }
        [AgentNative]::Screenshot($path, 0, 0, 0, 0)
        Write-Output $path
        break
    }
    "shot-window" {
        $title = $Rest[0]
        if (-not $title) { throw "shot-window requires a title substring" }
        $path = $Rest[1]
        if (-not $path) { $path = Join-Path $CaptureDir "latest.png" }
        $meta = [AgentNative]::WindowRect($title)
        if (-not $meta) { throw "No window matched '$title'" }
        $parts = $meta.Split("|")
        $rect = $parts[1].Split(",")
        [AgentNative]::Screenshot($path, [int]$rect[0], [int]$rect[1], [int]$rect[2], [int]$rect[3])
        Write-Output "$path"
        Write-Output $meta
        break
    }
    "move" {
        [AgentNative]::Move([int]$Rest[0], [int]$Rest[1])
        Write-Output "moved $($Rest[0]) $($Rest[1])"
        break
    }
    "click" {
        $button = "left"
        if ($Rest.Count -ge 3) { $button = $Rest[2] }
        [AgentNative]::Click([int]$Rest[0], [int]$Rest[1], $button)
        Write-Output "clicked $($Rest[0]) $($Rest[1]) $button"
        break
    }
    "dblclick" {
        [AgentNative]::Click([int]$Rest[0], [int]$Rest[1], "left")
        Start-Sleep -Milliseconds 80
        [AgentNative]::Click([int]$Rest[0], [int]$Rest[1], "left")
        Write-Output "dblclicked $($Rest[0]) $($Rest[1])"
        break
    }
    "scroll" {
        [AgentNative]::Scroll([int]$Rest[0])
        Write-Output "scrolled $($Rest[0])"
        break
    }
    "key" {
        $vk = Get-Vk $Rest[0]
        [AgentNative]::Key($vk, $true)
        Start-Sleep -Milliseconds 40
        [AgentNative]::Key($vk, $false)
        Write-Output "key $($Rest[0])"
        break
    }
    "down" {
        [AgentNative]::Key((Get-Vk $Rest[0]), $true)
        Write-Output "down $($Rest[0])"
        break
    }
    "up" {
        [AgentNative]::Key((Get-Vk $Rest[0]), $false)
        Write-Output "up $($Rest[0])"
        break
    }
    "hold" {
        $ms = [int]$Rest[1]
        $vk = Get-Vk $Rest[0]
        [AgentNative]::Key($vk, $true)
        Start-Sleep -Milliseconds $ms
        [AgentNative]::Key($vk, $false)
        Write-Output "held $($Rest[0]) ${ms}ms"
        break
    }
    "type" {
        $text = ($Rest -join " ")
        foreach ($ch in $text.ToCharArray()) {
            if ($ch -eq " ") {
                [AgentNative]::Key(0x20, $true); Start-Sleep -Milliseconds 20; [AgentNative]::Key(0x20, $false)
            }
            else {
                $upper = [char]::IsUpper($ch)
                $letter = $ch.ToString().ToUpperInvariant()
                if ($upper) { [AgentNative]::Key(0xA0, $true) }
                $vk = Get-Vk $letter
                [AgentNative]::Key($vk, $true)
                Start-Sleep -Milliseconds 20
                [AgentNative]::Key($vk, $false)
                if ($upper) { [AgentNative]::Key(0xA0, $false) }
            }
            Start-Sleep -Milliseconds 25
        }
        Write-Output "typed $text"
        break
    }
    default { throw "Unknown command '$Command'. Run without args for help." }
}
