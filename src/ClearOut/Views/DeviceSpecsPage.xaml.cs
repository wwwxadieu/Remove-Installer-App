using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ClearOut.ViewModels;

namespace ClearOut.Views;

public sealed partial class DeviceSpecsPage : Page
{
    public DeviceSpecsViewModel ViewModel { get; }

    public DeviceSpecsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DeviceSpecsViewModel>();
        DrivesItemsControl.ItemsSource = ViewModel.Drives;
        // Registry reads + DriveInfo.GetDrives() are cheap, and drive usage changes over time,
        // so refresh every time the page is shown rather than guarding it to run once.
        Loaded += (_, _) =>
        {
            ViewModel.Load();
            ApplySpecs();
        };
    }

    private void ApplySpecs()
    {
        var specs = ViewModel.Specs;
        if (specs is null)
        {
            return;
        }

        OsNameText.Text = specs.OsDisplayName;
        OsVersionText.Text = specs.OsVersionText;
        ArchitectureText.Text = specs.Architecture;
        ComputerNameText.Text = specs.MachineName;
        CpuText.Text = specs.CpuName ?? Unknown;
        CoresText.Text = specs.LogicalProcessorCount.ToString();
        RamText.Text = specs.DisplayTotalRam;
        ManufacturerText.Text = specs.Manufacturer ?? Unknown;
        ModelText.Text = specs.Model ?? Unknown;
    }

    private const string Unknown = "—";
}
