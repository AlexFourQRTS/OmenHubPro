$out = 'C:\Users\Brahma\Desktop\OmenCloude\probe\smart.txt'
$ns = 'root\Microsoft\Windows\Storage'
$lines = New-Object System.Collections.ArrayList
try {
  foreach ($r in (Get-CimInstance -Namespace $ns -ClassName MSFT_StorageReliabilityCounter)) {
    [void]$lines.Add(('Disk {0}: temp={1}C wear={2}% hours={3} readErr={4}/{5}unc writeErr={6}/{7}unc' -f $r.DeviceId,$r.Temperature,$r.Wear,$r.PowerOnHours,$r.ReadErrorsTotal,$r.ReadErrorsUncorrected,$r.WriteErrorsTotal,$r.WriteErrorsUncorrected))
  }
  foreach ($p in (Get-CimInstance -Namespace $ns -ClassName MSFT_Partition)) {
    [void]$lines.Add(('Partition disk={0} letter={1} GB={2} type={3}' -f $p.DiskNumber,$p.DriveLetter,[math]::Round($p.Size/1GB),$p.Type))
  }
} catch { [void]$lines.Add('ERR ' + $_.Exception.Message) }
[IO.File]::WriteAllLines($out, [string[]]$lines.ToArray())
