using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Lab4._1
{
    // Класс для хранения информации об иконке
    public class EnemyIcon
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
    }

    //ИНТЕРФЕЙС ДЛЯ СОХРАНЕНИЯ
    public interface ISaveList<T>
    {
        T Load(string path);
        void Save(T data, string path);
    }

    //АБСТРАКТНЫЙ КЛАСС ШАБЛОНА ПРОТИВНИКА
    [JsonConverter(typeof(EnemyTemplateConverter))]
    public abstract class CEnemyTemplate
    {
        private string name;
        private string iconName;
        private int baseLife;
        private double lifeModifier;
        private int baseGold;
        private double goldModifier;
        private double spawnChance;

        // Свойства вместо геттеров/сеттеров
        public string Name
        {
            get => name;
            set => name = !string.IsNullOrWhiteSpace(value) ? value : "unknown";
        }

        public string IconName
        {
            get => iconName;
            set => iconName = value ?? "default.png";
        }

        public int BaseLife
        {
            get => baseLife;
            set => baseLife = value > 0 ? value : 1;
        }

        public double LifeModifier
        {
            get => lifeModifier;
            set => lifeModifier = value >= 0 ? value : 0;
        }

        public int BaseGold
        {
            get => baseGold;
            set => baseGold = value >= 0 ? value : 0;
        }

        public double GoldModifier
        {
            get => goldModifier;
            set => goldModifier = value >= 0 ? value : 0;
        }

        public double SpawnChance
        {
            get => spawnChance;
            set => spawnChance = value < 0 ? 0 : (value > 1 ? 1 : value);
        }

        protected CEnemyTemplate() { }

        protected CEnemyTemplate(string name, string iconName, int baseLife,
                                 double lifeModifier, int baseGold, double goldModifier,
                                 double spawnChance)
        {
            Name = name;
            IconName = iconName;
            BaseLife = baseLife;
            LifeModifier = lifeModifier;
            BaseGold = baseGold;
            GoldModifier = goldModifier;
            SpawnChance = spawnChance;
        }

        // Виртуальный метод для получения типа противника (для отображения)
        public virtual string GetEnemyType() => "Обычный";
    }

    //ОБЫЧНЫЙ ПРОТИВНИК
    public class CNormalEnemyTemplate : CEnemyTemplate
    {
        public CNormalEnemyTemplate() : base() { }

        public CNormalEnemyTemplate(string name, string iconName, int baseLife,
                                    double lifeModifier, int baseGold, double goldModifier,
                                    double spawnChance)
            : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance)
        {
        }

        public override string GetEnemyType() => "Обычный";
    }

    //БРОНИРОВАННЫЙ ПРОТИВНИК
    public class CArmoredEnemyTemplate : CEnemyTemplate
    {
        private double armor;

        public double Armor
        {
            get => armor;
            set => armor = value > 0 ? value : 25;
        }

        public CArmoredEnemyTemplate() : base() { }

        public CArmoredEnemyTemplate(string name, string iconName, int baseLife,
                                     double lifeModifier, int baseGold, double goldModifier,
                                     double spawnChance, double armor = 25)
            : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance)
        {
            Armor = armor;
        }

        public override string GetEnemyType() => "Бронированный";
    }

    //ЛЕЧАЩИЙСЯ ПРОТИВНИК
    public class CHealingEnemyTemplate : CEnemyTemplate
    {
        private double healChance;
        private int healAmount;

        public double HealChance
        {
            get => healChance;
            set => healChance = value < 0 ? 0 : (value > 1 ? 1 : value);
        }

        public int HealAmount
        {
            get => healAmount;
            set => healAmount = value > 0 ? value : 10;
        }

        public CHealingEnemyTemplate() : base() { }

        public CHealingEnemyTemplate(string name, string iconName, int baseLife,
                                     double lifeModifier, int baseGold, double goldModifier,
                                     double spawnChance, double healChance = 0.3, int healAmount = 10)
            : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance)
        {
            HealChance = healChance;
            HealAmount = healAmount;
        }

        public override string GetEnemyType() => "Лечащийся";
    }

    //ОСЛАБЛЯЮЩИЙ ПРОТИВНИК
    public class CWeakeningEnemyTemplate : CEnemyTemplate
    {
        private double weakenFactor;

        public double WeakenFactor
        {
            get => weakenFactor;
            set => weakenFactor = value < 0.1 ? 0.1 : (value > 0.9 ? 0.9 : value);
        }

        public CWeakeningEnemyTemplate() : base() { }

        public CWeakeningEnemyTemplate(string name, string iconName, int baseLife,
                                       double lifeModifier, int baseGold, double goldModifier,
                                       double spawnChance, double weakenFactor = 0.5)
            : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance)
        {
            WeakenFactor = weakenFactor;
        }

        public override string GetEnemyType() => "Ослабляющий";
    }

    //КАСТОМНЫЙ КОНВЕРТЕР ДЛЯ ПОЛИМОРФНОЙ СЕРИАЛИЗАЦИИ
    public class EnemyTemplateConverter : JsonConverter<CEnemyTemplate>
    {
        public override CEnemyTemplate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                string type = jsonDoc.RootElement.GetProperty("$type").GetString();

                switch (type)
                {
                    case nameof(CNormalEnemyTemplate):
                        return JsonSerializer.Deserialize<CNormalEnemyTemplate>(jsonDoc.RootElement.GetRawText(), options);
                    case nameof(CArmoredEnemyTemplate):
                        return JsonSerializer.Deserialize<CArmoredEnemyTemplate>(jsonDoc.RootElement.GetRawText(), options);
                    case nameof(CHealingEnemyTemplate):
                        return JsonSerializer.Deserialize<CHealingEnemyTemplate>(jsonDoc.RootElement.GetRawText(), options);
                    case nameof(CWeakeningEnemyTemplate):
                        return JsonSerializer.Deserialize<CWeakeningEnemyTemplate>(jsonDoc.RootElement.GetRawText(), options);
                    default:
                        throw new NotSupportedException($"Unknown type: {type}");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, CEnemyTemplate value, JsonSerializerOptions options)
        {
            string type = value.GetType().Name;
            string json = JsonSerializer.Serialize(value, value.GetType(), options);

            using (var jsonDoc = JsonDocument.Parse(json))
            {
                writer.WriteStartObject();
                writer.WriteString("$type", type);

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
        }
    }

    //СЕРИАЛИЗАТОР ДЛЯ СПИСКА ПРОТИВНИКОВ
    public class JsonEnemySaver : ISaveList<List<CEnemyTemplate>>
    {
        private readonly JsonSerializerOptions _options;

        public JsonEnemySaver()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new EnemyTemplateConverter() }
            };
        }

        public List<CEnemyTemplate> Load(string path)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<CEnemyTemplate>>(json, _options) ?? new List<CEnemyTemplate>();
            }
            return new List<CEnemyTemplate>();
        }

        public void Save(List<CEnemyTemplate> data, string path)
        {
            string json = JsonSerializer.Serialize(data, _options);
            File.WriteAllText(path, json);
            MessageBox.Show("Список противников успешно сохранен!", "Успех",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    //КЛАСС ДЛЯ УПРАВЛЕНИЯ СПИСКОМ ПРОТИВНИКОВ
    public class CEnemyTemplateList
    {
        private List<CEnemyTemplate> enemies;
        private readonly ISaveList<List<CEnemyTemplate>> _serializer;

        public CEnemyTemplateList()
        {
            enemies = new List<CEnemyTemplate>();
            _serializer = new JsonEnemySaver();
        }

        // Добавление обычного противника
        public void AddNormalEnemy(string name, string iconName, int baseLife,
                                   double lifeModifier, int baseGold, double goldModifier,
                                   double spawnChance)
        {
            enemies.Add(new CNormalEnemyTemplate(name, iconName, baseLife,
                                                lifeModifier, baseGold, goldModifier,
                                                spawnChance));
        }

        // Добавление бронированного противника
        public void AddArmoredEnemy(string name, string iconName, int baseLife,
                                    double lifeModifier, int baseGold, double goldModifier,
                                    double spawnChance, double armor)
        {
            enemies.Add(new CArmoredEnemyTemplate(name, iconName, baseLife,
                                                 lifeModifier, baseGold, goldModifier,
                                                 spawnChance, armor));
        }

        // Добавление лечащегося противника
        public void AddHealingEnemy(string name, string iconName, int baseLife,
                                    double lifeModifier, int baseGold, double goldModifier,
                                    double spawnChance, double healChance, int healAmount)
        {
            enemies.Add(new CHealingEnemyTemplate(name, iconName, baseLife,
                                                 lifeModifier, baseGold, goldModifier,
                                                 spawnChance, healChance, healAmount));
        }

        // Добавление ослабляющего противника
        public void AddWeakeningEnemy(string name, string iconName, int baseLife,
                                      double lifeModifier, int baseGold, double goldModifier,
                                      double spawnChance, double weakenFactor)
        {
            enemies.Add(new CWeakeningEnemyTemplate(name, iconName, baseLife,
                                                   lifeModifier, baseGold, goldModifier,
                                                   spawnChance, weakenFactor));
        }

        public CEnemyTemplate GetEnemyByName(string name)
        {
            return enemies.Find(e => e.Name == name);
        }

        public CEnemyTemplate GetEnemyByIndex(int index)
        {
            if (index >= 0 && index < enemies.Count)
                return enemies[index];
            return null;
        }

        public void DeleteEnemyByName(string name)
        {
            var enemy = GetEnemyByName(name);
            if (enemy != null)
                enemies.Remove(enemy);
        }

        public void DeleteEnemyByIndex(int index)
        {
            if (index >= 0 && index < enemies.Count)
                enemies.RemoveAt(index);
        }

        public List<string> GetListOfEnemyNames()
        {
            List<string> names = new List<string>();
            foreach (var enemy in enemies)
            {
                names.Add(enemy.Name);
            }
            return names;
        }

        public List<CEnemyTemplate> GetAllEnemies()
        {
            return enemies;
        }

        // Использование интерфейса для сохранения
        public void SaveToJson(string path)
        {
            try
            {
                _serializer.Save(enemies, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Использование интерфейса для загрузки
        public void LoadFromJson(string path)
        {
            try
            {
                enemies = _serializer.Load(path);
                MessageBox.Show("Список противников успешно загружен!", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Clear()
        {
            enemies.Clear();
        }
    }

    //ОСНОВНОЕ ОКНО ПРИЛОЖЕНИЯ
    public partial class MainWindow : Window
    {
        private CEnemyTemplateList enemyList;
        private List<EnemyIcon> enemyIcons;
        private string currentSelectedIcon = "";
        private string lastFolderPath = "";
        private string selectedEnemyType = "normal";

        public MainWindow()
        {
            InitializeComponent();
            enemyList = new CEnemyTemplateList();
            enemyIcons = new List<EnemyIcon>();
            InitializeTypeComboBox();
        }

        private void InitializeTypeComboBox()
        {
            EnemyTypeComboBox.Items.Add(new ComboBoxItem { Content = "Обычный", Tag = "normal" });
            EnemyTypeComboBox.Items.Add(new ComboBoxItem { Content = "Бронированный", Tag = "armored" });
            EnemyTypeComboBox.Items.Add(new ComboBoxItem { Content = "Лечащийся", Tag = "healing" });
            EnemyTypeComboBox.Items.Add(new ComboBoxItem { Content = "Ослабляющий", Tag = "weakening" });
            EnemyTypeComboBox.SelectedIndex = 0;
        }

        // Выбор папки с иконками
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new Window
            {
                Title = "Выберите папку с иконками",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var label = new TextBlock
            {
                Text = "Введите путь к папке с иконками:",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var okButton = new Button { Content = "OK", Width = 60, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 60, Margin = new Thickness(5) };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            inputDialog.Content = stackPanel;

            okButton.Click += (s, args) =>
            {
                if (Directory.Exists(textBox.Text))
                {
                    lastFolderPath = textBox.Text;
                    LoadIconsFromFolder(textBox.Text);
                    inputDialog.Close();
                }
                else
                {
                    MessageBox.Show("Указанная папка не существует!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, args) => inputDialog.Close();

            inputDialog.ShowDialog();
        }

        // Загрузка иконок из папки
        private void LoadIconsFromFolder(string path)
        {
            try
            {
                enemyIcons.Clear();
                IconsListBox.Items.Clear();

                string[] files = Directory.GetFiles(path, "*.png");

                foreach (string file in files)
                {
                    enemyIcons.Add(new EnemyIcon
                    {
                        Name = System.IO.Path.GetFileName(file),
                        ImagePath = file
                    });
                }

                foreach (EnemyIcon icon in enemyIcons)
                {
                    Image image = new Image()
                    {
                        Source = new BitmapImage(new Uri(icon.ImagePath)),
                        Height = 64,
                        Width = 64,
                        Margin = new Thickness(5),
                        Stretch = Stretch.Uniform
                    };

                    StackPanel panel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(5)
                    };

                    TextBlock textBlock = new TextBlock
                    {
                        Text = icon.Name,
                        FontSize = 10,
                        TextAlignment = TextAlignment.Center
                    };

                    panel.Children.Add(image);
                    panel.Children.Add(textBlock);

                    IconsListBox.Items.Add(panel);
                }

                if (enemyIcons.Count == 0)
                {
                    MessageBox.Show("В выбранной папке не найдено PNG изображений!",
                                   "Предупреждение", MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке иконок: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработка выбора иконки
        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconsListBox.SelectedItem != null)
            {
                int index = IconsListBox.SelectedIndex;
                if (index >= 0 && index < enemyIcons.Count)
                {
                    currentSelectedIcon = enemyIcons[index].Name;
                    SelectedIconTextBox.Text = currentSelectedIcon;
                }
            }
        }

        // Обработка выбора типа противника
        private void EnemyTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnemyTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedEnemyType = selectedItem.Tag.ToString();
                UpdateAdditionalFields();
            }
        }

        // Обновление дополнительных полей в зависимости от типа противника
        private void UpdateAdditionalFields()
        {
            AdditionalFieldsPanel.Children.Clear();

            switch (selectedEnemyType)
            {
                case "armored":
                    AdditionalFieldsPanel.Children.Add(new TextBlock { Text = "Броня:", Margin = new Thickness(0, 5, 0, 0) });
                    AdditionalFieldsPanel.Children.Add(new TextBox { Name = "ArmorTextBox", Margin = new Thickness(0, 0, 0, 5), Text = "25" });
                    break;
                case "healing":
                    AdditionalFieldsPanel.Children.Add(new TextBlock { Text = "Шанс лечения (0-1):", Margin = new Thickness(0, 5, 0, 0) });
                    AdditionalFieldsPanel.Children.Add(new TextBox { Name = "HealChanceTextBox", Margin = new Thickness(0, 0, 0, 5), Text = "0.3" });
                    AdditionalFieldsPanel.Children.Add(new TextBlock { Text = "Количество лечения:", Margin = new Thickness(0, 5, 0, 0) });
                    AdditionalFieldsPanel.Children.Add(new TextBox { Name = "HealAmountTextBox", Margin = new Thickness(0, 0, 0, 5), Text = "10" });
                    break;
                case "weakening":
                    AdditionalFieldsPanel.Children.Add(new TextBlock { Text = "Множитель ослабления (0.1-0.9):", Margin = new Thickness(0, 5, 0, 0) });
                    AdditionalFieldsPanel.Children.Add(new TextBox { Name = "WeakenFactorTextBox", Margin = new Thickness(0, 0, 0, 5), Text = "0.5" });
                    break;
            }
        }

        // Добавление противника
        private void AddEnemy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Введите название противника!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentSelectedIcon))
                {
                    MessageBox.Show("Выберите иконку противника!", "Ошибка",
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

                string name = NameTextBox.Text;
                int baseLife = int.Parse(BaseLifeTextBox.Text);
                double lifeModifier = double.Parse(LifeModifierTextBox.Text);
                int baseGold = int.Parse(BaseGoldTextBox.Text);
                double goldModifier = double.Parse(GoldModifierTextBox.Text);
                double spawnChance = double.Parse(SpawnChanceTextBox.Text);

                if (spawnChance < 0 || spawnChance > 1)
                {
                    MessageBox.Show("Шанс появления должен быть в диапазоне от 0 до 1!",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (enemyList.GetEnemyByName(name) != null)
                {
                    MessageBox.Show("Противник с таким именем уже существует!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Добавление в зависимости от типа
                switch (selectedEnemyType)
                {
                    case "normal":
                        enemyList.AddNormalEnemy(name, currentSelectedIcon, baseLife,
                                                lifeModifier, baseGold, goldModifier, spawnChance);
                        break;
                    case "armored":
                        double armor = double.Parse((AdditionalFieldsPanel.Children[1] as TextBox).Text);
                        enemyList.AddArmoredEnemy(name, currentSelectedIcon, baseLife,
                                                 lifeModifier, baseGold, goldModifier, spawnChance, armor);
                        break;
                    case "healing":
                        double healChance = double.Parse((AdditionalFieldsPanel.Children[1] as TextBox).Text);
                        int healAmount = int.Parse((AdditionalFieldsPanel.Children[3] as TextBox).Text);
                        enemyList.AddHealingEnemy(name, currentSelectedIcon, baseLife,
                                                 lifeModifier, baseGold, goldModifier, spawnChance,
                                                 healChance, healAmount);
                        break;
                    case "weakening":
                        double weakenFactor = double.Parse((AdditionalFieldsPanel.Children[1] as TextBox).Text);
                        enemyList.AddWeakeningEnemy(name, currentSelectedIcon, baseLife,
                                                   lifeModifier, baseGold, goldModifier, spawnChance,
                                                   weakenFactor);
                        break;
                }

                UpdateEnemiesList();
                ClearForm();

                MessageBox.Show("Противник успешно добавлен!", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (FormatException)
            {
                MessageBox.Show("Проверьте правильность ввода числовых значений!",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление выбранного противника
        private void DeleteEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem != null)
            {
                string selectedName = EnemiesListBox.SelectedItem.ToString();

                MessageBoxResult result = MessageBox.Show($"Удалить противника '{selectedName}'?",
                                                          "Подтверждение",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    enemyList.DeleteEnemyByName(selectedName);
                    UpdateEnemiesList();
                    ClearForm();
                }
            }
            else
            {
                MessageBox.Show("Выберите противника для удаления!", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Обновление списка противников в интерфейсе
        private void UpdateEnemiesList()
        {
            EnemiesListBox.Items.Clear();
            foreach (var enemy in enemyList.GetAllEnemies())
            {
                EnemiesListBox.Items.Add($"{enemy.Name} [{enemy.GetEnemyType()}]");
            }
        }

        // Очистка формы
        private void ClearForm()
        {
            NameTextBox.Clear();
            SelectedIconTextBox.Clear();
            BaseLifeTextBox.Clear();
            LifeModifierTextBox.Clear();
            BaseGoldTextBox.Clear();
            GoldModifierTextBox.Clear();
            SpawnChanceTextBox.Clear();
            currentSelectedIcon = "";
            IconsListBox.SelectedItem = null;
            EnemyTypeComboBox.SelectedIndex = 0;
            AdditionalFieldsPanel.Children.Clear();
        }

        // Выбор противника из списка для редактирования
        private void EnemiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem != null)
            {
                string selectedName = EnemiesListBox.SelectedItem.ToString();
                // Убираем суффикс с типом
                if (selectedName.Contains(" ["))
                    selectedName = selectedName.Substring(0, selectedName.IndexOf(" ["));

                CEnemyTemplate enemy = enemyList.GetEnemyByName(selectedName);

                if (enemy != null)
                {
                    NameTextBox.Text = enemy.Name;
                    currentSelectedIcon = enemy.IconName;
                    SelectedIconTextBox.Text = currentSelectedIcon;
                    BaseLifeTextBox.Text = enemy.BaseLife.ToString();
                    LifeModifierTextBox.Text = enemy.LifeModifier.ToString();
                    BaseGoldTextBox.Text = enemy.BaseGold.ToString();
                    GoldModifierTextBox.Text = enemy.GoldModifier.ToString();
                    SpawnChanceTextBox.Text = enemy.SpawnChance.ToString();

                    // Установка типа в комбобоксе
                    switch (enemy)
                    {
                        case CArmoredEnemyTemplate armored:
                            SetComboBoxByTag("armored");
                            break;
                        case CHealingEnemyTemplate healing:
                            SetComboBoxByTag("healing");
                            break;
                        case CWeakeningEnemyTemplate weakening:
                            SetComboBoxByTag("weakening");
                            break;
                        default:
                            SetComboBoxByTag("normal");
                            break;
                    }

                    // Подсветка выбранной иконки
                    for (int i = 0; i < enemyIcons.Count; i++)
                    {
                        if (enemyIcons[i].Name == currentSelectedIcon)
                        {
                            IconsListBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void SetComboBoxByTag(string tag)
        {
            for (int i = 0; i < EnemyTypeComboBox.Items.Count; i++)
            {
                var item = EnemyTypeComboBox.Items[i] as ComboBoxItem;
                if (item.Tag.ToString() == tag)
                {
                    EnemyTypeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        // Сохранение в JSON
        private void SaveToJson_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JSON files (*.json)|*.json";
            saveDialog.DefaultExt = "json";
            saveDialog.FileName = "enemies.json";

            if (saveDialog.ShowDialog() == true)
            {
                enemyList.SaveToJson(saveDialog.FileName);
            }
        }

        // Загрузка из JSON
        private void LoadFromJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "JSON files (*.json)|*.json";
            openDialog.DefaultExt = "json";

            if (openDialog.ShowDialog() == true)
            {
                enemyList.LoadFromJson(openDialog.FileName);
                UpdateEnemiesList();
                ClearForm();
            }
        }

        // Очистка всех данных
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить всех противников?",
                                                      "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                enemyList.Clear();
                UpdateEnemiesList();
                ClearForm();
            }
        }
    }
}
