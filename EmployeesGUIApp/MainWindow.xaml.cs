using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace EmployeesGUIApp
{
    public partial class MainWindow : Window
    {
        private string _filePath;
        private readonly List<SalaryRecord> _allRecords = new List<SalaryRecord>();

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
            _allRecords.Clear();

            // 2.1 (XSLT)
            try
            {
                //load the Xml doc
                XPathDocument myXPathDoc = new XPathDocument(_filePath);

                XslCompiledTransform myXslTrans = new XslCompiledTransform();

                //load the Xsl 
                myXslTrans.Load("../../data/any_data_to_Employees.xsl");

                //create the output stream
                XmlTextWriter myWriter = new XmlTextWriter
                    ("result.xml", null);

                //do the actual transform of Xml
                myXslTrans.Transform(myXPathDoc, null, myWriter);

                myWriter.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "при трансформации", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 2.2
            try
            {
                //load the Xml doc
                XDocument doc = XDocument.Load("result.xml");

                // Культура, в которой запятая – разделитель дробной части
                var ruCulture = new CultureInfo("ru-RU");

                foreach (var emp in doc.Root.Elements("Employee"))
                {
                    // Все salary‑элементы текущего сотрудника
                    var salaries = emp.Elements("salary");

                    double sum = salaries
                        .Select(s =>
                        {
                        // Берём строку атрибута amount
                        string txt = s.Attribute("amount").Value;

                        // Заменяем точку на запятую (и наоборот) — делаем валидным для Parse
                        txt = txt.Replace('.', ',').Replace(',', System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator[0]);

                        // Парсим в double, учитывая локаль
                        return double.Parse(txt, NumberStyles.Any, ruCulture);
                        })
                        .Sum();

                    // Добавляем атрибут totalSalary
                    emp.SetAttributeValue("totalSalary", sum.ToString("F2", CultureInfo.InvariantCulture));
                }

                // Сохраняем в файл:
                doc.Save("employees_with_total.xml");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "при добавлении totalSalary", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 2.3
            try
            {
                //load the Xml doc
                XDocument doc = XDocument.Load(_filePath);

                if (doc.Root?.Elements().FirstOrDefault()?.Name.LocalName == "item")
                {
                    // Используем InvariantCulture
                    var inv = CultureInfo.InvariantCulture;

                    double total = 0.0;

                    foreach (var item in doc.Root.Elements("item"))
                    {
                        string amountStr = item.Attribute("amount").Value;

                        // Заменяем запятую на точку, чтобы double.Parse понял любой вариант
                        amountStr = amountStr.Replace(',', '.');

                        if (double.TryParse(amountStr, NumberStyles.Any, inv, out double amount))
                        {
                            total += amount;
                        }
                        else
                        {
                            Console.WriteLine($"Не удалось распознать amount: {amountStr}");
                        }
                    }

                    // Добавляем атрибут total к <Pay>
                    doc.Root.SetAttributeValue("total", total.ToString("F2", inv));

                    // Сохраняем в файл:
                    doc.Save("pay_with_total.xml");
                }
                else
                {
                    MessageBox.Show("Выбран файл НЕ data1. Total не добавлен.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "при добавлении total к <Pay>", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // GUI output
            try
            {
                XDocument doc = XDocument.Load("result.xml");

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
                            _allRecords.Add(new SalaryRecord
                            {
                                FullName = fullName,
                                MonthEn = monthEn,
                                MonthRu = monthRu,
                                Amount = amount
                            });
                        }
                    }
                }
                UpdateUI();
                MessageBox.Show($"Загружено {_allRecords.Count} записей.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "XML", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPayment_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtSurname.Text) ||
                string.IsNullOrWhiteSpace(txtAmount.Text) ||
                string.IsNullOrWhiteSpace(txtMonthEn.Text))
            {
                MessageBox.Show("Заполните все поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string amountStr = txtAmount.Text.Trim().Replace(",", ".");
            if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
            {
                MessageBox.Show("Неверный формат суммы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string monthEn = txtMonthEn.Text.Trim().ToLower();
            string monthRu = TranslateMonth(monthEn);

            _allRecords.Add(new SalaryRecord
            {
                FullName = $"{txtName.Text.Trim()} {txtSurname.Text.Trim()}".Trim(),
                MonthEn = monthEn,
                MonthRu = monthRu,
                Amount = amount
            });

            UpdateUI();
            ClearForm();
            MessageBox.Show("Выплата добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateUI()
        {
            // === Список сотрудников ===
            var employees = _allRecords
                    .Select(r => r.FullName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            EmployeesListBox.ItemsSource = employees;

            // === Сумма по месяцам ===
            var monthly = _allRecords
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

        private void ClearForm()
        {
            txtName.Text = "";
            txtSurname.Text = "";
            txtAmount.Text = "";
            txtMonthEn.Text = "";
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