using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace EmployeesGUIApp
{
    public partial class MainWindow : Window
    {
        private string _filePath;

        // Словарь для перевода месяцев
        private static readonly Dictionary<string, string> MonthEnToRu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "january",   "Январь" },
            { "february",  "Февраль" },
            { "march",     "Март" },
            { "april",     "Апрель" },
            { "may",       "Май" },
            { "june",      "Июнь" },
            { "july",      "Июль" },
            { "august",    "Август" },
            { "september", "Сентябрь" },
            { "october",   "Октябрь" },
            { "november",  "Ноябрь" },
            { "december",  "Декабрь" }
        };

        // Порядок месяцев
        private static readonly Dictionary<string, int> MonthOrder = new Dictionary<string, int>
        {
            { "Январь", 1 }, { "Февраль", 2 }, { "Март", 3 },
            { "Апрель", 4 }, { "Май", 5 }, { "Июнь", 6 },
            { "Июль", 7 }, { "Август", 8 }, { "Сентябрь", 9 },
            { "Октябрь", 10 }, { "Ноябрь", 11 }, { "Декабрь", 12 }
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "XML files|*.xml|All files|*.*",
                Title = "Выберите XML-файл"
            };

            if (dialog.ShowDialog() == true)
            {
                _filePath = dialog.FileName;
                FilePathTextBox.Text = _filePath;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                MessageBox.Show("Файл не выбран!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(_filePath);
                var records = new List<SalaryRecord>();

                foreach (XElement emp in doc.Root.Elements("Employee"))
                {
                    string name = emp.Attribute("name")?.Value ?? "";
                    string surname = emp.Attribute("surname")?.Value ?? "";
                    string fullName = (name + " " + surname).Trim();

                    foreach (XElement salary in emp.Elements("salary"))
                    {
                        string amountStr = salary.Attribute("amount")?.Value ?? "0";
                        string monthEn = salary.Attribute("mount")?.Value ?? "";

                        // Заменяем запятую на точку
                        amountStr = amountStr.Replace(",", ".");

                        if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        {
                            string monthRu = TranslateMonth(monthEn);
                            records.Add(new SalaryRecord
                            {
                                FullName = fullName,
                                MonthEn = monthEn,
                                MonthRu = monthRu,
                                Amount = amount
                            });
                        }
                    }
                }

                // === Список сотрудников ===
                var employees = records
                    .Select(r => r.FullName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                EmployeesListBox.ItemsSource = employees;

                // === Сумма по месяцам ===
                var monthly = records
                    .GroupBy(r => r.MonthEn)
                    .Select(g =>
                    {
                        string monthRu = TranslateMonth(g.Key);
                        decimal total = g.Sum(x => x.Amount);
                        return new { Month = monthRu, TotalAmount = total };
                    })
                    .OrderBy(x => GetMonthOrder(x.Month))
                    .ToList();

                MonthlyTotalsGrid.ItemsSource = monthly;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "XML", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Перевод месяца
        private string TranslateMonth(string en)
        {
            if (string.IsNullOrEmpty(en)) return "Неизвестно";

            foreach (var pair in MonthEnToRu)
            {
                if (string.Equals(pair.Key, en, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
            return en; // если не нашли
        }

        // Порядок месяца
        private int GetMonthOrder(string ru)
        {
            if (MonthOrder.TryGetValue(ru, out int order))
                return order;
            return 99;
        }
    }

    // Простая модель
    public class SalaryRecord
    {
        public string FullName { get; set; }
        public string MonthEn { get; set; }
        public string MonthRu { get; set; }
        public decimal Amount { get; set; }
    }
}