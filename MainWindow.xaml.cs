using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace SimpleTaiwanIITE;

public partial class MainWindow
{
    private int _above70Count; // 年滿 70 歲人數

    private int _ble; // 基本生活費總額
    private int _bled; // 基本生活費差額

    // 免稅額
    private int _exemption;

    // 綜合所得總額
    private int _gci;
    private int _interestIncome; // 利息所得
    private int _lcDeduction; // 長期照顧特別扣除額
    private int _leaseIncome; // 租賃所得

    private double _nci; // 綜合所得淨額
    private int _otherIncome; // 其他所得

    private int _peopleCount; // 戶口人數;
    private int _pmcDeduction; // 身心障礙特別扣除額
    private int _pscDeduction; // 幼兒學前特別扣除額
    private int _selfWageIncome; // 本人薪資所得
    private int _siDeduction; // 儲蓄投資特別扣除額

    // 特別扣除額
    private int _specialDeduction;
    private int _spouseWageIncome; // 配偶薪資所得

    // 一般扣除額
    private int _standardDeduction; // 標準扣除額
    private double _tax; // 應納稅額
    private int _wageDeduction; // 薪資所得特別扣除額

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Single.IsChecked = true;

        const string currentVersion = "23.9.0";
        const string endpointUrl = "https://gist.githubusercontent.com/ccpl17/4d405f115dacc50d9dc955bb9d27be28/raw/194ba74550bccb6da576d2cb59eec88d5ca2378e/simple-taiwan-iite.json";

        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync(endpointUrl);

            var jsonContent = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
            var data = jsonContent != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent) : null;
            var remoteVersion = data != null
                ? data.TryGetValue("version", out var versionValue) ? versionValue : currentVersion
                : currentVersion;

            var result = Version.Parse(currentVersion) < Version.Parse(remoteVersion)
                ? MessageBox.Show("有可用的更新，你想要現在下載嗎", "有可用的更新", MessageBoxButton.YesNo, MessageBoxImage.Question,
                    MessageBoxResult.Yes)
                : MessageBoxResult.No;
            if (result == MessageBoxResult.Yes)
                Process.Start("explorer",
                    "https://github.com/ccpl17/simple-taiwan-iite/releases");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        _standardDeduction = Single.IsChecked == true ? 124000 : 248000;
        SpouseWageIncome.IsEnabled = Married.IsChecked == true;
        SpouseWageIncome.Text = Single.IsChecked == true ? "" : SpouseWageIncome.Text;
    }

    private void Calculate(object sender, RoutedEventArgs e)
    {
        _peopleCount = int.TryParse(PeopleCount.Text, out _peopleCount) ? _peopleCount : 0;
        _above70Count = int.TryParse(Above70Count.Text, out _above70Count) ? _above70Count : 0;
        _pmcDeduction = int.TryParse(DisabledCount.Text, out _pmcDeduction) ? 207000 * _pmcDeduction : 0;
        _pscDeduction = int.TryParse(Under5ChildCount.Text, out _pscDeduction) ? 120000 * _pscDeduction : 0;
        _lcDeduction = int.TryParse(LongTermCareCount.Text, out _lcDeduction) ? 120000 * _lcDeduction : 0;
        _selfWageIncome = int.TryParse(SelfWageIncome.Text, out _selfWageIncome) ? _selfWageIncome : 0;
        _spouseWageIncome = int.TryParse(SpouseWageIncome.Text, out _spouseWageIncome) ? _spouseWageIncome : 0;
        _interestIncome = int.TryParse(InterestIncome.Text, out _interestIncome) ? _interestIncome : 0;
        _leaseIncome = int.TryParse(LeaseIncome.Text, out _leaseIncome) ? _leaseIncome : 0;
        _otherIncome = int.TryParse(OtherIncome.Text, out _otherIncome) ? _otherIncome : 0;

        _gci = _selfWageIncome + _spouseWageIncome + _interestIncome + _leaseIncome + _otherIncome;

        _exemption = _above70Count * 138000 + (_peopleCount - _above70Count) * 92000;

        _wageDeduction = 0;
        switch (_selfWageIncome)
        {
            case >= 207000:
                _wageDeduction += 207000;
                break;
            case >= 0:
                _wageDeduction += _selfWageIncome;
                break;
        }

        if (Married.IsChecked == true)
            switch (_spouseWageIncome)
            {
                case >= 207000:
                    _wageDeduction += 207000;
                    break;

                case >= 0:
                    _wageDeduction += _spouseWageIncome;
                    break;
            }

        _siDeduction = 0;
        switch (_interestIncome)
        {
            case >= 270000:
                _siDeduction += 270000;
                break;

            case >= 0:
                _siDeduction += _interestIncome;
                break;
        }

        _specialDeduction = _wageDeduction + _siDeduction + _pmcDeduction + _pscDeduction + _lcDeduction;

        _ble = 196000 * _peopleCount;
        _bled = _ble - _exemption - _standardDeduction - (_specialDeduction - _wageDeduction);

        _nci = _bled < 0
            ? _gci - _exemption - _standardDeduction - _specialDeduction
            : _gci - _exemption - _standardDeduction - _specialDeduction - _bled;

        _tax = _nci switch
        {
            <= 560000 => _nci * .05,
            <= 1260000 => _nci * .12 - 39200,
            <= 2520000 => _nci * .2 - 140000,
            <= 4720000 => _nci * .3 - 392000,
            _ => _nci * .4 - 864000
        };
        Result.Content = _tax <= 0 ? "無應納稅額" : $"應納稅額：{Math.Floor(_tax)}";
    }

    private void OpenWebsite(object sender, RoutedEventArgs e)
    {
        Process.Start("explorer",
            "https://ccpl17.github.io/simple-taiwan-iite/");
    }

    private void ShowAboutWindow(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog();
    }
}