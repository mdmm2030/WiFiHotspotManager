using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace WiFiHotspotManager;

public partial class MainWindow : Window
{
    private NetworkOperatorTetheringManager? _manager;
    private readonly ObservableCollection<ClientRow> _clients = new();
    private const string RulePrefix = "WiFiHotspotManager-Block-";

    public MainWindow()
    {
        InitializeComponent();
        ClientsGrid.ItemsSource = _clients;
        PasswordBox.Password = "ChangeMe12345";
        RefreshBlockedCount();
        RefreshClients();
    }

    private void EnsureManager()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile is null) throw new InvalidOperationException(
            "لم يعثر Windows على اتصال إنترنت نشط. Connect to Wi-Fi or Ethernet first.");
        _manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureManager();
            if (!string.IsNullOrWhiteSpace(SsidBox.Text) && !string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                var config = new NetworkOperatorTetheringAccessPointConfiguration
                {
                    Ssid = SsidBox.Text.Trim(),
                    Passphrase = PasswordBox.Password
                };
                await _manager!.ConfigureAccessPointAsync(config);
            }
            var result = await _manager!.StartTetheringAsync();
            StatusText.Text = result.Status.ToString();
            StatusDetail.Text = result.AdditionalErrorMessage ?? "Windows Mobile Hotspot returned a status.";
            FooterText.Text = "Start request completed • تم إرسال طلب التشغيل";
            RefreshClients();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureManager();
            var result = await _manager!.StopTetheringAsync();
            StatusText.Text = result.Status.ToString();
            StatusDetail.Text = result.AdditionalErrorMessage ?? "Hotspot stop request completed.";
            FooterText.Text = "Stop request completed • تم إرسال طلب الإيقاف";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureManager();
            var config = new NetworkOperatorTetheringAccessPointConfiguration
            {
                Ssid = SsidBox.Text.Trim(),
                Passphrase = PasswordBox.Password
            };
            await _manager!.ConfigureAccessPointAsync(config);
            FooterText.Text = "Settings sent to Windows • أُرسلت الإعدادات إلى Windows";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshClients();

    private void RefreshClients()
    {
        try
        {
            EnsureManager();
            _clients.Clear();
            var arp = ReadArpTable();
            foreach (var client in _manager!.GetTetheringClients())
            {
                var mac = client.MacAddress ?? "";
                var ip = arp.FirstOrDefault(x => NormalizeMac(x.Value) == NormalizeMac(mac)).Key ?? "";
                _clients.Add(new ClientRow
                {
                    Name = string.IsNullOrWhiteSpace(client.HostName) ? "Unknown device" : client.HostName,
                    Mac = mac,
                    Ip = ip,
                    Status = IsBlocked(ip) ? "Blocked" : "Connected"
                });
            }
            ClientCount.Text = _clients.Count.ToString();
            RefreshBlockedCount();
            FooterText.Text = "Client list refreshed • تم تحديث قائمة الأجهزة";
        }
        catch (Exception ex)
        {
            FooterText.Text = "Client discovery unavailable • تعذر اكتشاف الأجهزة: " + ex.Message;
        }
    }

    private void BlockSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ClientsGrid.SelectedItem is not ClientRow row)
        {
            MessageBox.Show("Select a connected device first • اختر جهازاً أولاً");
            return;
        }
        if (!IPAddress.TryParse(row.Ip, out _))
        {
            MessageBox.Show("No IPv4 address was matched to this device. Refresh the list and try again.\nلم يتم العثور على عنوان IP لهذا الجهاز.");
            return;
        }
        try
        {
            var safeIp = row.Ip;
            var ruleName = RulePrefix + safeIp.Replace('.', '_');
            RunPowerShell($"if (-not (Get-NetFirewallRule -DisplayName '{ruleName}' -ErrorAction SilentlyContinue)) {{ New-NetFirewallRule -DisplayName '{ruleName}' -Direction Outbound -Action Block -RemoteAddress '{safeIp}' -Profile Any | Out-Null }}");
            FooterText.Text = $"Firewall block rule created for {safeIp} • أُنشئت قاعدة حظر";
            RefreshClients();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void UnblockSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ClientsGrid.SelectedItem is not ClientRow row || !IPAddress.TryParse(row.Ip, out _))
        {
            MessageBox.Show("Select a device with a detected IP address first.");
            return;
        }
        try
        {
            var ruleName = RulePrefix + row.Ip.Replace('.', '_');
            RunPowerShell($"Get-NetFirewallRule -DisplayName '{ruleName}' -ErrorAction SilentlyContinue | Remove-NetFirewallRule");
            FooterText.Text = $"Firewall rule removed for {row.Ip} • أزيلت قاعدة الحظر";
            RefreshClients();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowBlocked_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var output = RunPowerShell($"Get-NetFirewallRule -DisplayName '{RulePrefix}*' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty DisplayName");
            MessageBox.Show(string.IsNullOrWhiteSpace(output) ? "No block rules found • لا توجد قواعد حظر" : output,
                "Blocked devices • الأجهزة المحظورة");
            RefreshBlockedCount();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RefreshBlockedCount()
    {
        try
        {
            var output = RunPowerShell($"(Get-NetFirewallRule -DisplayName '{RulePrefix}*' -ErrorAction SilentlyContinue | Measure-Object).Count");
            BlockedCount.Text = output.Trim().Length == 0 ? "0" : output.Trim();
        }
        catch { BlockedCount.Text = "—"; }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("ms-settings:network-mobilehotspot") { UseShellExecute = true });
    }

    private static Dictionary<string, string> ReadArpTable()
    {
        var output = RunPowerShell("arp -a");
        var map = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(output, @"(?<ip>\d{1,3}(?:\.\d{1,3}){3})\s+(?<mac>[0-9a-fA-F-]{17})\s+\w+"))
            map[m.Groups["ip"].Value] = m.Groups["mac"].Value;
        return map;
    }

    private static string NormalizeMac(string value) => Regex.Replace(value ?? "", "[^0-9A-Fa-f]", "").ToUpperInvariant();

    private static bool IsBlocked(string ip)
    {
        if (!IPAddress.TryParse(ip, out _)) return false;
        try { return RunPowerShell($"Get-NetFirewallRule -DisplayName '{RulePrefix}{ip.Replace('.', '_')}' -ErrorAction SilentlyContinue").Length > 0; }
        catch { return false; }
    }

    private static string RunPowerShell(string command)
    {
        var psi = new ProcessStartInfo("powershell.exe")
        {
            Arguments = "-NoProfile -ExecutionPolicy Bypass -Command " + Quote(command),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start PowerShell.");
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
            throw new InvalidOperationException(stderr.Trim());
        return stdout;
    }

    private static string Quote(string s) => "\"" + s.Replace("\"", "`\"") + "\"";

    private static void ShowError(Exception ex) =>
        MessageBox.Show(ex.Message, "WiFi Hotspot Manager • Error", MessageBoxButton.OK, MessageBoxImage.Warning);
}

public class ClientRow
{
    public string Name { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Mac { get; set; } = "";
    public string Status { get; set; } = "";
}
