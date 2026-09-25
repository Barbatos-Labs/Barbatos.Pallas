# This Source Code Form is subject to the terms of the MIT License.
# If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
# Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
# All Rights Reserved.

<#
.SYNOPSIS
Copies the base64 of a key file to the clipboard, to paste as a GitHub secret, without showing it anywhere.

.DESCRIPTION
Two of the release secrets are files (docs/RELEASING.md): STRONG_NAME_KEY is the base64 of src/barbatos.snk and
SIGNING_CERTIFICATE_PFX the base64 of packaging/certificates/barbatos-codesign.pfx. Both hold a private key, so the
value goes to the clipboard only - never to the screen, a log or a file - and is kept out of Windows' clipboard
history and cloud clipboard, which would otherwise keep it after it has been pasted.

What it says instead is enough to tell that the right file was taken:
- for a .snk, the public key token, which for Barbatos.Pallas is 1c94c30b213a8345;
- for a .pfx, the certificate it holds - opened with the password in the <name>.password.txt beside it, as
  Barbatos.PackagingEngine keeps it, when that file is there - and whether it holds its private key;
- for any file, its size and the length of the base64, to compare with what GitHub shows after pasting.

Run it in Windows PowerShell 5.1 (powershell.exe), whose console is the single-threaded apartment the clipboard
needs. Clear the clipboard afterwards with -Clear.

.PARAMETER Path
The key file: a .snk or a .pfx.

.PARAMETER Clear
Empties the clipboard, once the secret has been pasted.

.EXAMPLE
powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Path src/barbatos.snk

.EXAMPLE
powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Path packaging/certificates/barbatos-codesign.pfx

.EXAMPLE
powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Clear
#>
[CmdletBinding(DefaultParameterSetName = 'Copy')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Copy', Position = 0)] [string] $Path,
    [Parameter(Mandatory, ParameterSetName = 'Clear')] [switch] $Clear
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    throw 'The clipboard needs a single-threaded apartment. Run this with: powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 ...'
}

Add-Type -AssemblyName System.Windows.Forms

if ($Clear) {
    [System.Windows.Forms.Clipboard]::Clear()
    Write-Host 'The clipboard is empty.'
    return
}

$file = Get-Item -LiteralPath $Path
$bytes = [IO.File]::ReadAllBytes($file.FullName)
if ($bytes.Length -eq 0) { throw "$($file.Name) is empty." }

switch ($file.Extension.ToLowerInvariant()) {
    '.snk' {
        # A key pair is a CryptoAPI PRIVATEKEYBLOB: type 7, version 2, the key's algorithm, "RSA2", the bit length, the
        # public exponent and the modulus. A file of type 6 holds a public key alone, which cannot sign.
        if ($bytes.Length -lt 20 -or $bytes[0] -ne 0x07 -or [Text.Encoding]::ASCII.GetString($bytes, 8, 4) -ne 'RSA2') {
            throw "$($file.Name) is not a strong-name key pair: a .snk that can sign starts with an RSA private key blob."
        }

        $modulusLength = [BitConverter]::ToUInt32($bytes, 12) / 8

        # The public key an assembly carries, built as the compiler builds it: signature algorithm RSA (0x2400), hash
        # SHA-1 (0x8004), the blob's length, then a PUBLICKEYBLOB of the same key. The algorithm is written as RSA
        # signing whatever the key file says - a key RSACryptoServiceProvider made says key exchange (0xA400), and
        # StrongNameKeyPair.PublicKey keeps that, which gave the wrong token when this was measured (25 Sep 2026).
        $publicKey = New-Object byte[] (32 + $modulusLength)
        [BitConverter]::GetBytes([uint32] 0x2400).CopyTo($publicKey, 0)
        [BitConverter]::GetBytes([uint32] 0x8004).CopyTo($publicKey, 4)
        [BitConverter]::GetBytes([uint32] (20 + $modulusLength)).CopyTo($publicKey, 8)
        $publicKey[12] = 0x06
        $publicKey[13] = 0x02
        [BitConverter]::GetBytes([uint32] 0x2400).CopyTo($publicKey, 16)
        [Text.Encoding]::ASCII.GetBytes('RSA1').CopyTo($publicKey, 20)
        [Array]::Copy($bytes, 12, $publicKey, 24, 8 + $modulusLength)

        # The token is the last eight bytes of the SHA-1 of that public key, in reverse.
        $sha1 = [Security.Cryptography.SHA1]::Create()
        try { $hash = $sha1.ComputeHash($publicKey) } finally { $sha1.Dispose() }
        $token = (($hash[19..12]) | ForEach-Object { $_.ToString('x2') }) -join ''
        Write-Host "Strong-name key pair of $($modulusLength * 8) bits, public key token $token."
    }
    '.pfx' {
        $passwordFile = Join-Path $file.DirectoryName ($file.BaseName + '.password.txt')
        if (Test-Path -LiteralPath $passwordFile) {
            $password = (Get-Content -LiteralPath $passwordFile -Raw).Trim()
            try {
                $certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList $file.FullName, $password
            }
            catch {
                throw "$($file.Name) does not open with the password in $(Split-Path $passwordFile -Leaf): $($_.Exception.Message)"
            }

            if (-not $certificate.HasPrivateKey) { throw "$($file.Name) holds no private key, so it cannot sign." }
            Write-Host "Certificate $($certificate.Subject), issued by $($certificate.Issuer)"
            Write-Host "  thumbprint $($certificate.Thumbprint), expires $($certificate.NotAfter.ToString('yyyy-MM-dd')), private key included."
            if ($certificate.NotAfter -lt (Get-Date)) { throw 'The certificate has expired.' }
        }
        else {
            Write-Host "No $(Split-Path $passwordFile -Leaf) beside it, so the certificate was not opened."
        }
    }
    default {
        throw "$($file.Name) is neither a .snk nor a .pfx."
    }
}

$base64 = [Convert]::ToBase64String($bytes)

# Read back before copying: what is pasted must decode to the very bytes of the file.
$sha256 = [Security.Cryptography.SHA256]::Create()
try {
    $same = [BitConverter]::ToString($sha256.ComputeHash([Convert]::FromBase64String($base64))) -eq [BitConverter]::ToString($sha256.ComputeHash($bytes))
}
finally {
    $sha256.Dispose()
}

if (-not $same) { throw 'The base64 does not decode to the file.' }

# Text, marked so that Windows keeps it out of the clipboard history (Win+V) and the cloud clipboard, as password
# managers mark what they copy: a private key pasted once should not be there to paste again next week.
$data = New-Object System.Windows.Forms.DataObject
$data.SetData([System.Windows.Forms.DataFormats]::UnicodeText, $base64)
$data.SetData('ExcludeClipboardContentFromMonitorProcessing', (New-Object IO.MemoryStream -ArgumentList (, [byte[]] (1, 0, 0, 0))))
$data.SetData('CanIncludeInClipboardHistory', (New-Object IO.MemoryStream -ArgumentList (, [byte[]] (0, 0, 0, 0))))
$data.SetData('CanUploadToCloudClipboard', (New-Object IO.MemoryStream -ArgumentList (, [byte[]] (0, 0, 0, 0))))
[System.Windows.Forms.Clipboard]::SetDataObject($data, $true)

Write-Host "The base64 of $($file.Name) ($($bytes.Length) bytes, $($base64.Length) characters) is on the clipboard."
Write-Host 'Paste it as the secret, then empty the clipboard: powershell -STA -NoProfile -ExecutionPolicy Bypass -File build/Copy-SecretToClipboard.ps1 -Clear'
