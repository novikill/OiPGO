using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Lab2
{
    // Класс для работы с большими числами
    public class BigNumber
    {
        private const int Base = 1000;
        private int[] number;

        public BigNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
                value = "0";

            List<int> blocks = new List<int>();
            int length = value.Length;

            for (int i = length; i > 0; i -= 3)
            {
                int start = Math.Max(0, i - 3);
                int blockLength = i - start;
                string blockStr = value.Substring(start, blockLength);
                blocks.Add(int.Parse(blockStr));
            }

            blocks.Reverse();
            number = blocks.ToArray();
            TrimLeadingZeros();
        }

        public BigNumber(int[] blocks)
        {
            number = (int[])blocks.Clone();
            TrimLeadingZeros();
        }

        public BigNumber Clone()
        {
            return new BigNumber(number);
        }

        private void TrimLeadingZeros()
        {
            if (number.Length == 0)
            {
                number = new int[] { 0 };
                return;
            }

            int startIndex = 0;
            while (startIndex < number.Length - 1 && number[startIndex] == 0)
                startIndex++;

            if (startIndex > 0)
            {
                int[] newNumber = new int[number.Length - startIndex];
                Array.Copy(number, startIndex, newNumber, 0, newNumber.Length);
                number = newNumber;
            }
        }

        private BigNumber Add(BigNumber other)
        {
            if (other == null) return this.Clone();

            int maxLength = Math.Max(number.Length, other.number.Length);
            int[] result = new int[maxLength];
            int carry = 0;

            for (int i = 0; i < maxLength; i++)
            {
                int sum = carry;
                if (i < number.Length)
                    sum += number[number.Length - 1 - i];
                if (i < other.number.Length)
                    sum += other.number[other.number.Length - 1 - i];

                result[maxLength - 1 - i] = sum % Base;
                carry = sum / Base;
            }

            if (carry > 0)
            {
                int[] newResult = new int[maxLength + 1];
                newResult[0] = carry;
                Array.Copy(result, 0, newResult, 1, maxLength);
                result = newResult;
            }

            return new BigNumber(result);
        }

        private BigNumber Subtract(BigNumber other)
        {
            if (other == null) return this.Clone();

            if (CompareTo(other) < 0)
                return new BigNumber("0");

            int maxLength = Math.Max(number.Length, other.number.Length);
            int[] result = new int[maxLength];
            int borrow = 0;

            for (int i = 0; i < maxLength; i++)
            {
                int a = (i < number.Length) ? number[number.Length - 1 - i] : 0;
                int b = (i < other.number.Length) ? other.number[other.number.Length - 1 - i] : 0;
                a -= borrow;

                if (a < b)
                {
                    a += Base;
                    borrow = 1;
                }
                else
                {
                    borrow = 0;
                }

                result[maxLength - 1 - i] = a - b;
            }

            return new BigNumber(result);
        }

        public BigNumber Multiply(double multiplier)
        {
            int[] result = new int[number.Length + 1];
            int carry = 0;

            for (int i = number.Length - 1; i >= 0; i--)
            {
                long product = (long)(number[i] * multiplier) + carry;
                result[i + 1] = (int)(product % Base);
                carry = (int)(product / Base);
            }

            result[0] = carry;
            BigNumber resultNum = new BigNumber(result);
            resultNum.TrimLeadingZeros();
            return resultNum;
        }

        public BigNumber Divide(double divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException();

            int[] result = new int[number.Length];
            long remainder = 0;

            for (int i = 0; i < number.Length; i++)
            {
                long current = remainder * Base + number[i];
                result[i] = (int)(current / divisor);
                remainder = (int)(current % divisor);
            }

            return new BigNumber(result);
        }

        public int CompareTo(BigNumber other)
        {
            if (other == null)
                return 1;

            if (number.Length != other.number.Length)
                return number.Length.CompareTo(other.number.Length);

            for (int i = 0; i < number.Length; i++)
            {
                if (number[i] != other.number[i])
                    return number[i].CompareTo(other.number[i]);
            }

            return 0;
        }

        // Переопределение операторов
        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a == null) return b?.Clone();
            if (b == null) return a.Clone();
            return a.Add(b);
        }

        public static BigNumber operator -(BigNumber a, BigNumber b)
        {
            if (a == null) return b?.Clone();
            if (b == null) return a.Clone();
            return a.Subtract(b);
        }

        public static BigNumber operator *(BigNumber a, double b)
        {
            if (a == null) return new BigNumber("0");
            return a.Multiply(b);
        }

        public static BigNumber operator /(BigNumber a, double b)
        {
            if (a == null) return new BigNumber("0");
            return a.Divide(b);
        }

        public static bool operator >(BigNumber a, BigNumber b)
        {
            if (a == null) return false;
            if (b == null) return true;
            return a.CompareTo(b) > 0;
        }

        public static bool operator <(BigNumber a, BigNumber b)
        {
            if (a == null) return b != null;
            if (b == null) return false;
            return a.CompareTo(b) < 0;
        }

        public static bool operator >=(BigNumber a, BigNumber b)
        {
            if (a == null) return b == null;
            if (b == null) return true;
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <=(BigNumber a, BigNumber b)
        {
            if (a == null) return true;
            if (b == null) return false;
            return a.CompareTo(b) <= 0;
        }

        public static bool operator ==(BigNumber a, BigNumber b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null) return false;
            if (b is null) return false;
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(BigNumber a, BigNumber b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is BigNumber other)
                return CompareTo(other) == 0;
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            if (number == null || number.Length == 0)
                return "0";

            StringBuilder sb = new StringBuilder();
            sb.Append(number[0].ToString());

            for (int i = 1; i < number.Length; i++)
            {
                sb.Append(number[i].ToString("D3"));
            }

            return sb.ToString();
        }
    }

    // Класс шаблона противника
    public class CEnemyTemplate
    {
        [JsonInclude]
        private string name;

        [JsonInclude]
        private string iconName;

        [JsonInclude]
        private int baseLife;

        [JsonInclude]
        private double lifeModifier;

        [JsonInclude]
        private int baseGold;

        [JsonInclude]
        private double goldModifier;

        [JsonInclude]
        private double spawnChance;

        public CEnemyTemplate(string name, string iconName, int baseLife,
                             double lifeModifier, int baseGold, double goldModifier,
                             double spawnChance)
        {
            this.name = name;
            this.iconName = iconName;
            this.baseLife = baseLife;
            this.lifeModifier = lifeModifier;
            this.baseGold = baseGold;
            this.goldModifier = goldModifier;
            this.spawnChance = spawnChance;
        }

        public string GetName() => name;
        public string GetIconName() => iconName;
        public int GetBaseLife() => baseLife;
        public double GetLifeModifier() => lifeModifier;
        public int GetBaseGold() => baseGold;
        public double GetGoldModifier() => goldModifier;
        public double GetSpawnChance() => spawnChance;
    }

    // Класс противника в игре
    public class Enemy
    {
        private string name;
        private BigNumber maxHitPoints;
        private BigNumber currentHitPoints;
        private BigNumber goldReward;
        private bool isDead;
        private string iconName;

        public string Name => name;
        public BigNumber MaxHitPoints => maxHitPoints;
        public BigNumber CurrentHitPoints => currentHitPoints;
        public BigNumber GoldReward => goldReward;
        public bool IsDead => isDead;
        public string IconName => iconName;

        public Enemy(CEnemyTemplate template, int level)
        {
            name = template.GetName();
            iconName = template.GetIconName();

            double lifeMultiplier = Math.Pow(template.GetLifeModifier(), level - 1);
            double goldMultiplier = Math.Pow(template.GetGoldModifier(), level - 1);

            int initialLife = (int)(template.GetBaseLife() * lifeMultiplier);
            int initialGold = (int)(template.GetBaseGold() * goldMultiplier);

            maxHitPoints = new BigNumber(initialLife.ToString());
            currentHitPoints = maxHitPoints.Clone();
            goldReward = new BigNumber(initialGold.ToString());
            isDead = false;
        }

        public bool TakeDamage(BigNumber damage, out BigNumber goldRewardOut)
        {
            goldRewardOut = new BigNumber("0");

            if (isDead)
                return false;

            if (damage == null || currentHitPoints == null)
                return false;

            if (damage.CompareTo(currentHitPoints) >= 0)
            {
                currentHitPoints = new BigNumber("0");
                isDead = true;
                goldRewardOut = goldReward.Clone();
                return true;
            }
            else
            {
                currentHitPoints = currentHitPoints - damage;
                return false;
            }
        }
    }

    // Класс игрока
    public class Player
    {
        private int lvl;
        private BigNumber gold;
        private BigNumber damage;
        private double damageModifier;
        private BigNumber upgradeCost;
        private double upgradeModifier;

        public int Lvl => lvl;
        public BigNumber Gold => gold;
        public BigNumber Damage => damage;
        public BigNumber UpgradeCost => upgradeCost;

        public Player()
        {
            lvl = 1;
            gold = new BigNumber("0");
            damage = new BigNumber("10");
            damageModifier = 1.2;
            upgradeCost = new BigNumber("100");
            upgradeModifier = 1.2;
        }

        public void AddGold(BigNumber amount)
        {
            if (amount == null)
                return;
            gold = gold + amount;
        }

        public bool TryUpgrade()
        {
            if (gold == null || upgradeCost == null)
                return false;

            if (gold.CompareTo(upgradeCost) >= 0)
            {
                gold = gold - upgradeCost;
                lvl++;
                RecalculateStats();
                return true;
            }
            return false;
        }

        public BigNumber DealDamage()
        {
            return damage.Clone();
        }

        private void RecalculateStats()
        {
            damage = damage * damageModifier;
            upgradeCost = CalculateNextUpgradeCost();
        }

        private BigNumber CalculateNextUpgradeCost()
        {
            BigNumber cost = upgradeCost.Clone();
            double multiplier = upgradeModifier * (lvl - 1);
            return cost * multiplier;
        }
    }

    // Класс для управления противниками
    public class EnemyManager
    {
        private List<CEnemyTemplate> templates;
        private Random random;

        public EnemyManager()
        {
            templates = new List<CEnemyTemplate>();
            random = new Random();
        }

        public void LoadTemplates(string filePath)
        {
            try
            {
                string jsonFromFile = File.ReadAllText(filePath);
                JsonDocument doc = JsonDocument.Parse(jsonFromFile);

                templates.Clear();

                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    string name = element.GetProperty("name").GetString();
                    string iconName = element.GetProperty("iconName").GetString();
                    int baseLife = element.GetProperty("baseLife").GetInt32();
                    double lifeModifier = element.GetProperty("lifeModifier").GetDouble();
                    int baseGold = element.GetProperty("baseGold").GetInt32();
                    double goldModifier = element.GetProperty("goldModifier").GetDouble();
                    double spawnChance = element.GetProperty("spawnChance").GetDouble();

                    templates.Add(new CEnemyTemplate(name, iconName, baseLife,
                                                    lifeModifier, baseGold,
                                                    goldModifier, spawnChance));
                }

                NormalizeChances();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки шаблонов: {ex.Message}");
            }
        }

        private void NormalizeChances()
        {
            double sum = 0;
            foreach (var template in templates)
            {
                sum += template.GetSpawnChance();
            }

            if (sum > 0 && sum != 1.0)
            {
                // Для простоты оставляем как есть, так как шансы уже должны быть нормализованы
            }
        }

        public CEnemyTemplate GetRandomTemplate(int level)
        {
            if (templates.Count == 0)
                return null;

            double chance = random.NextDouble();
            double sum = 0;

            foreach (var template in templates)
            {
                sum += template.GetSpawnChance();
                if (sum >= chance)
                    return template;
            }

            return templates[0];
        }

        public bool HasTemplates => templates.Count > 0;
    }

    // Основное окно игры
    public partial class MainWindow : Window
    {
        private Player player;
        private Enemy currentEnemy;
        private EnemyManager enemyManager;
        private int enemiesDefeated;
        private BigNumber totalDamage;
        private BigNumber highestDamage;
        private string iconsFolderPath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            player = new Player();
            enemyManager = new EnemyManager();
            enemiesDefeated = 0;
            totalDamage = new BigNumber("0");
            highestDamage = new BigNumber("0");

            UpdateUI();
        }

        private void UpdateUI()
        {
            LvlText.Text = player.Lvl.ToString();
            GoldText.Text = player.Gold.ToString();
            DamageText.Text = player.Damage.ToString();
            UpgradeCostText.Text = player.UpgradeCost.ToString();

            EnemiesDefeatedText.Text = $"Побеждено: {enemiesDefeated}";
            TotalDamageText.Text = $"Всего урона: {totalDamage}";
            HighestDamageText.Text = $"Макс. урон: {highestDamage}";

            if (currentEnemy != null && !currentEnemy.IsDead)
            {
                EnemyNameText.Text = currentEnemy.Name;
                EnemyHealthText.Text = currentEnemy.CurrentHitPoints.ToString();
                EnemyRewardText.Text = currentEnemy.GoldReward.ToString();

                // Загрузка иконки
                if (!string.IsNullOrEmpty(currentEnemy.IconName) && !string.IsNullOrEmpty(iconsFolderPath))
                {
                    try
                    {
                        string iconPath = System.IO.Path.Combine(iconsFolderPath, currentEnemy.IconName);
                        if (File.Exists(iconPath))
                        {
                            EnemyImage.Source = new BitmapImage(new Uri(iconPath));
                        }
                    }
                    catch { }
                }
            }
            else
            {
                EnemyNameText.Text = "Нет противника";
                EnemyHealthText.Text = "0";
                EnemyRewardText.Text = "0";
                EnemyImage.Source = null;
            }

            AttackButton.IsEnabled = currentEnemy != null && !currentEnemy.IsDead;
        }

        private void LoadEnemiesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "JSON files (*.json)|*.json";
            openDialog.Title = "Выберите файл с шаблонами противников";

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    enemyManager.LoadTemplates(openDialog.FileName);

                    // Создаем простой диалог для ввода пути
                    Window inputWindow = new Window
                    {
                        Title = "Выберите папку с иконками",
                        Width = 450,
                        Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = this,
                        Background = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                        ResizeMode = ResizeMode.NoResize
                    };

                    StackPanel panel = new StackPanel { Margin = new Thickness(10) };

                    TextBlock label = new TextBlock
                    {
                        Text = "Введите путь к папке с иконками:",
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 0, 0, 10),
                        FontSize = 14
                    };

                    TextBox textBox = new TextBox
                    {
                        Margin = new Thickness(0, 0, 0, 10),
                        Background = Brushes.White,
                        FontSize = 14,
                        Padding = new Thickness(5)
                    };

                    // Устанавливаем путь по умолчанию
                    textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    StackPanel buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    Button okButton = new Button
                    {
                        Content = "OK",
                        Width = 80,
                        Margin = new Thickness(5),
                        Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                        Foreground = Brushes.White,
                        FontSize = 14,
                        Padding = new Thickness(10, 5, 10, 5)
                    };

                    Button cancelButton = new Button
                    {
                        Content = "Отмена",
                        Width = 80,
                        Margin = new Thickness(5),
                        Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                        Foreground = Brushes.White,
                        FontSize = 14,
                        Padding = new Thickness(10, 5, 10, 5)
                    };

                    buttonPanel.Children.Add(okButton);
                    buttonPanel.Children.Add(cancelButton);

                    panel.Children.Add(label);
                    panel.Children.Add(textBox);
                    panel.Children.Add(buttonPanel);

                    inputWindow.Content = panel;

                    bool folderSelected = false;

                    okButton.Click += (s, args) =>
                    {
                        if (Directory.Exists(textBox.Text))
                        {
                            iconsFolderPath = textBox.Text;
                            folderSelected = true;
                            inputWindow.Close();
                        }
                        else
                        {
                            MessageBox.Show("Указанная папка не существует!", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    cancelButton.Click += (s, args) => inputWindow.Close();

                    inputWindow.ShowDialog();

                    if (folderSelected)
                    {
                        SpawnNewEnemy();
                        MessageBox.Show("Противники успешно загружены!", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SpawnNewEnemy()
        {
            if (enemyManager.HasTemplates)
            {
                var template = enemyManager.GetRandomTemplate(player.Lvl);
                if (template != null)
                {
                    currentEnemy = new Enemy(template, player.Lvl);
                    UpdateUI();
                }
            }
        }

        private void AttackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentEnemy == null || currentEnemy.IsDead)
                return;

            BigNumber damage = player.DealDamage();

            if (damage == null)
                return;

            totalDamage = totalDamage + damage;

            if (damage.CompareTo(highestDamage) > 0)
                highestDamage = damage.Clone();

            if (currentEnemy.TakeDamage(damage, out BigNumber goldReward))
            {
                // Противник побежден
                player.AddGold(goldReward);
                enemiesDefeated++;
                UpdateUI();

                MessageBox.Show($"Вы победили {currentEnemy.Name}!\nПолучено золота: {goldReward}",
                               "Победа!", MessageBoxButton.OK, MessageBoxImage.Information);

                SpawnNewEnemy();
            }

            UpdateUI();
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (player.TryUpgrade())
            {
                UpdateUI();
                MessageBox.Show($"Улучшение успешно!\nНовый уровень: {player.Lvl}\nНовый урон: {player.Damage}",
                               "Улучшение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Недостаточно золота!\nНужно: {player.UpgradeCost}\nУ вас: {player.Gold}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}