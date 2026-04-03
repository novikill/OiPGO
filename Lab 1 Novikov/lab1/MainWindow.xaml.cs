using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace lab1
{
    // Класс для хранения информации о спрайте
    public class GameSprite
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }

    // Класс шаблона игрового юнита
    public class UnitTemplate
    {
        [JsonInclude]
        private string unitName;

        [JsonInclude]
        private string spriteFileName;

        [JsonInclude]
        private int healthBase;

        [JsonInclude]
        private double healthMultiplier;

        [JsonInclude]
        private int goldBase;

        [JsonInclude]
        private double goldMultiplier;

        [JsonInclude]
        private double spawnRate;

        public UnitTemplate(string name, string sprite, int hpBase,
                           double hpMult, int goldBaseVal, double goldMult,
                           double rate)
        {
            this.unitName = name;
            this.spriteFileName = sprite;
            this.healthBase = hpBase;
            this.healthMultiplier = hpMult;
            this.goldBase = goldBaseVal;
            this.goldMultiplier = goldMult;
            this.spawnRate = rate;
        }

        public string GetUnitName() { return unitName; }
        public string GetSpriteName() { return spriteFileName; }
        public int GetHealthBase() { return healthBase; }
        public double GetHealthMultiplier() { return healthMultiplier; }
        public int GetGoldBase() { return goldBase; }
        public double GetGoldMultiplier() { return goldMultiplier; }
        public double GetSpawnRate() { return spawnRate; }
    }

    // Класс для управления коллекцией юнитов
    public class UnitCollection
    {
        private List<UnitTemplate> units;

        public UnitCollection()
        {
            units = new List<UnitTemplate>();
        }

        public void AddUnit(string name, string sprite, int hpBase,
                           double hpMult, int goldBaseVal, double goldMult,
                           double rate)
        {
            units.Add(new UnitTemplate(name, sprite, hpBase,
                                      hpMult, goldBaseVal, goldMult,
                                      rate));
        }

        public UnitTemplate FindUnitByName(string name)
        {
            return units.Find(u => u.GetUnitName() == name);
        }

        public UnitTemplate GetUnitByPosition(int index)
        {
            if (index >= 0 && index < units.Count)
                return units[index];
            return null;
        }

        public void RemoveUnitByName(string name)
        {
            var target = FindUnitByName(name);
            if (target != null)
                units.Remove(target);
        }

        public void RemoveUnitByPosition(int index)
        {
            if (index >= 0 && index < units.Count)
                units.RemoveAt(index);
        }

        public List<string> GetAllUnitNames()
        {
            List<string> names = new List<string>();
            foreach (var unit in units)
            {
                names.Add(unit.GetUnitName());
            }
            return names;
        }

        public List<UnitTemplate> FetchAllUnits()
        {
            return units;
        }

        public void ExportToJson(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(units, options);
                File.WriteAllText(filePath, jsonContent);
                MessageBox.Show("Данные успешно сохранены!", "Готово",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи: {ex.Message}", "Сбой",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ImportFromJson(string filePath)
        {
            try
            {
                string rawJson = File.ReadAllText(filePath);
                JsonDocument document = JsonDocument.Parse(rawJson);

                units.Clear();

                foreach (JsonElement item in document.RootElement.EnumerateArray())
                {
                    string name = item.GetProperty("unitName").GetString();
                    string sprite = item.GetProperty("spriteFileName").GetString();
                    int hpBase = item.GetProperty("healthBase").GetInt32();
                    double hpMult = item.GetProperty("healthMultiplier").GetDouble();
                    int goldBaseVal = item.GetProperty("goldBase").GetInt32();
                    double goldMult = item.GetProperty("goldMultiplier").GetDouble();
                    double rate = item.GetProperty("spawnRate").GetDouble();

                    units.Add(new UnitTemplate(name, sprite, hpBase,
                                               hpMult, goldBaseVal,
                                               goldMult, rate));
                }

                MessageBox.Show("Данные успешно загружены!", "Готово",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения: {ex.Message}", "Сбой",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Основное окно приложения
    public partial class MainWindow : Window
    {
        private UnitCollection unitStorage;
        private List<GameSprite> spriteLibrary;
        private string activeSprite = "";
        private string cachedFolderPath = "";

        public MainWindow()
        {
            InitializeComponent();
            unitStorage = new UnitCollection();
            spriteLibrary = new List<GameSprite>();
        }

        // Выбор директории со спрайтами
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var pathDialog = new Window
            {
                Title = "Укажите путь к спрайтам",
                Width = 420,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var layout = new StackPanel { Margin = new Thickness(12) };

            var instruction = new TextBlock
            {
                Text = "Путь к папке с изображениями:",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var pathInput = new TextBox { Margin = new Thickness(0, 0, 0, 12) };

            var actionBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var confirmBtn = new Button { Content = "Применить", Width = 70, Margin = new Thickness(6) };
            var cancelBtn = new Button { Content = "Закрыть", Width = 70, Margin = new Thickness(6) };

            actionBar.Children.Add(confirmBtn);
            actionBar.Children.Add(cancelBtn);

            layout.Children.Add(instruction);
            layout.Children.Add(pathInput);
            layout.Children.Add(actionBar);

            pathDialog.Content = layout;

            confirmBtn.Click += (s, args) =>
            {
                if (Directory.Exists(pathInput.Text))
                {
                    cachedFolderPath = pathInput.Text;
                    LoadSpritesFromFolder(pathInput.Text);
                    pathDialog.Close();
                }
                else
                {
                    MessageBox.Show("Папка не найдена!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelBtn.Click += (s, args) => pathDialog.Close();

            pathDialog.ShowDialog();
        }

        // Загрузка спрайтов из директории
        private void LoadSpritesFromFolder(string folderPath)
        {
            try
            {
                spriteLibrary.Clear();
                IconsListBox.Items.Clear();

                string[] imageFiles = Directory.GetFiles(folderPath, "*.png");

                foreach (string file in imageFiles)
                {
                    spriteLibrary.Add(new GameSprite
                    {
                        FileName = System.IO.Path.GetFileName(file),
                        FullPath = file
                    });
                }

                foreach (GameSprite sprite in spriteLibrary)
                {
                    Image thumbnail = new Image()
                    {
                        Source = new BitmapImage(new Uri(sprite.FullPath)),
                        Height = 60,
                        Width = 60,
                        Margin = new Thickness(4),
                        Stretch = Stretch.Uniform
                    };

                    StackPanel card = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(4)
                    };

                    TextBlock caption = new TextBlock
                    {
                        Text = sprite.FileName,
                        FontSize = 9,
                        TextAlignment = TextAlignment.Center
                    };

                    card.Children.Add(thumbnail);
                    card.Children.Add(caption);

                    IconsListBox.Items.Add(card);
                }

                if (spriteLibrary.Count == 0)
                {
                    MessageBox.Show("PNG файлы не обнаружены!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Сбой",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработка выбора спрайта
        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconsListBox.SelectedItem != null)
            {
                int idx = IconsListBox.SelectedIndex;
                if (idx >= 0 && idx < spriteLibrary.Count)
                {
                    activeSprite = spriteLibrary[idx].FileName;
                    SelectedIconTextBox.Text = activeSprite;
                }
            }
        }

        // Добавление юнита
        private void AddEnemy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Укажите название!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(activeSprite))
                {
                    MessageBox.Show("Выберите спрайт!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(BaseLifeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LifeModifierTextBox.Text) ||
                    string.IsNullOrWhiteSpace(BaseGoldTextBox.Text) ||
                    string.IsNullOrWhiteSpace(GoldModifierTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SpawnChanceTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string unitName = NameTextBox.Text;
                int hpBase = int.Parse(BaseLifeTextBox.Text);
                double hpMult = double.Parse(LifeModifierTextBox.Text);
                int goldBaseVal = int.Parse(BaseGoldTextBox.Text);
                double goldMult = double.Parse(GoldModifierTextBox.Text);
                double rate = double.Parse(SpawnChanceTextBox.Text);

                if (rate < 0 || rate > 1)
                {
                    MessageBox.Show("Значение шанса от 0 до 1!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (unitStorage.FindUnitByName(unitName) != null)
                {
                    MessageBox.Show("Такое имя уже существует!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                unitStorage.AddUnit(unitName, activeSprite, hpBase, hpMult,
                                   goldBaseVal, goldMult, rate);

                RefreshUnitList();
                ResetForm();

                MessageBox.Show("Юнит добавлен!", "Готово",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (FormatException)
            {
                MessageBox.Show("Неверный формат чисел!", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Сбой",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление выбранного юнита
        private void DeleteEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem != null)
            {
                string targetName = EnemiesListBox.SelectedItem.ToString();

                MessageBoxResult confirmation = MessageBox.Show($"Удалить '{targetName}'?",
                                                          "Подтверждение",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);

                if (confirmation == MessageBoxResult.Yes)
                {
                    unitStorage.RemoveUnitByName(targetName);
                    RefreshUnitList();
                    ResetForm();
                }
            }
            else
            {
                MessageBox.Show("Сначала выберите юнит!", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Обновление списка в UI
        private void RefreshUnitList()
        {
            EnemiesListBox.Items.Clear();
            List<string> names = unitStorage.GetAllUnitNames();
            foreach (string name in names)
            {
                EnemiesListBox.Items.Add(name);
            }
        }

        // Сброс полей ввода
        private void ResetForm()
        {
            NameTextBox.Clear();
            SelectedIconTextBox.Clear();
            BaseLifeTextBox.Clear();
            LifeModifierTextBox.Clear();
            BaseGoldTextBox.Clear();
            GoldModifierTextBox.Clear();
            SpawnChanceTextBox.Clear();
            activeSprite = "";
            IconsListBox.SelectedItem = null;
        }

        // Загрузка данных юнита в форму
        private void EnemiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem != null)
            {
                string selected = EnemiesListBox.SelectedItem.ToString();
                UnitTemplate target = unitStorage.FindUnitByName(selected);

                if (target != null)
                {
                    NameTextBox.Text = target.GetUnitName();
                    activeSprite = target.GetSpriteName();
                    SelectedIconTextBox.Text = activeSprite;
                    BaseLifeTextBox.Text = target.GetHealthBase().ToString();
                    LifeModifierTextBox.Text = target.GetHealthMultiplier().ToString();
                    BaseGoldTextBox.Text = target.GetGoldBase().ToString();
                    GoldModifierTextBox.Text = target.GetGoldMultiplier().ToString();
                    SpawnChanceTextBox.Text = target.GetSpawnRate().ToString();

                    for (int i = 0; i < spriteLibrary.Count; i++)
                    {
                        if (spriteLibrary[i].FileName == activeSprite)
                        {
                            IconsListBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        // Экспорт в JSON
        private void SaveToJson_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON (*.json)|*.json";
            dialog.DefaultExt = "json";
            dialog.FileName = "units_backup.json";

            if (dialog.ShowDialog() == true)
            {
                unitStorage.ExportToJson(dialog.FileName);
            }
        }

        // Импорт из JSON
        private void LoadFromJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON (*.json)|*.json";
            dialog.DefaultExt = "json";

            if (dialog.ShowDialog() == true)
            {
                unitStorage.ImportFromJson(dialog.FileName);
                RefreshUnitList();
                ResetForm();
            }
        }

        // Полная очистка
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmation = MessageBox.Show("Удалить всех юнитов?",
                                                           "Подтверждение",
                                                           MessageBoxButton.YesNo,
                                                           MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                unitStorage = new UnitCollection();
                RefreshUnitList();
                ResetForm();
            }
        }
    }
}