using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Lab4._2
{
    //КЛАСС ДЛЯ РАБОТЫ С БОЛЬШИМИ ЧИСЛАМИ
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

    //КЛАСС ШАБЛОНА ПРОТИВНИКА
    public class CEnemyTemplate
    {
        private string name;
        private string iconName;
        private int baseLife;
        private double lifeModifier;
        private int baseGold;
        private double goldModifier;
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

    //ИНТЕРФЕЙС ДЛЯ СОХРАНЕНИЯ
    public interface ISaveList<T>
    {
        T Load(string path);
        void Save(T data, string path);
    }

    //КЛАСС ДЛЯ СОХРАНЕНИЯ ИГРОКА
    public class PlayerSaver : ISaveList<PlayerSaveData>
    {
        private readonly JsonSerializerOptions _options;

        public PlayerSaver()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        public PlayerSaveData Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<PlayerSaveData>(json, _options) ?? new PlayerSaveData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return new PlayerSaveData();
        }

        public void Save(PlayerSaveData data, string path)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _options);
                File.WriteAllText(path, json);
                MessageBox.Show("Игра успешно сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Класс для сохранения данных игрока
    public class PlayerSaveData
    {
        public int Lvl { get; set; }
        public string Gold { get; set; }
        public string Damage { get; set; }
        public string UpgradeCost { get; set; }
        public double AttackCooldown { get; set; }
        public int EnemiesDefeated { get; set; }
        public string TotalDamage { get; set; }
        public string HighestDamage { get; set; }

        public PlayerSaveData()
        {
            Lvl = 1;
            Gold = "0";
            Damage = "10";
            UpgradeCost = "100";
            AttackCooldown = 1.0;
            EnemiesDefeated = 0;
            TotalDamage = "0";
            HighestDamage = "0";
        }
    }

    //ИНТЕРФЕЙС ПРОТИВНИКА
    public interface IEnemy
    {
        string Name { get; }
        string IconName { get; }
        BigNumber MaxHitPoints { get; }
        BigNumber CurrentHitPoints { get; }
        BigNumber GoldReward { get; }
        bool IsDead { get; }
        bool TakeDamage(BigNumber damage, out BigNumber goldReward);
        void ApplyWeakenEffect(Action<double, double> applyWeakenCallback = null);
    }

    //АБСТРАКТНЫЙ КЛАСС ПРОТИВНИКА
    public abstract class CEnemy : IEnemy
    {
        protected string name;
        protected string iconName;
        protected BigNumber maxHitPoints;
        protected BigNumber currentHitPoints;
        protected BigNumber goldReward;
        protected bool isDead;

        public string Name => name;
        public string IconName => iconName;
        public BigNumber MaxHitPoints => maxHitPoints;
        public BigNumber CurrentHitPoints => currentHitPoints;
        public BigNumber GoldReward => goldReward;
        public bool IsDead => isDead;

        protected CEnemy(string name, string iconName, BigNumber maxHitPoints, BigNumber goldReward)
        {
            this.name = name;
            this.iconName = iconName;
            this.maxHitPoints = maxHitPoints;
            this.currentHitPoints = maxHitPoints.Clone();
            this.goldReward = goldReward;
            this.isDead = false;
        }

        public virtual bool TakeDamage(BigNumber damage, out BigNumber goldRewardOut)
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

        public virtual void ApplyWeakenEffect(Action<double, double> applyWeakenCallback = null)
        {
            // Базовый класс не применяет эффект ослабления
        }
    }

    //ОБЫЧНЫЙ ПРОТИВНИК
    public class CNormalEnemy : CEnemy
    {
        public CNormalEnemy(string name, string iconName, BigNumber maxHitPoints, BigNumber goldReward)
            : base(name, iconName, maxHitPoints, goldReward)
        {
        }
    }

    //БРОНИРОВАННЫЙ ПРОТИВНИК
    public class CArmoredEnemy : CEnemy
    {
        private BigNumber armor;

        public BigNumber Armor => armor;

        public CArmoredEnemy(string name, string iconName, BigNumber maxHitPoints,
                             BigNumber goldReward, BigNumber armor)
            : base(name, iconName, maxHitPoints, goldReward)
        {
            this.armor = armor;
        }

        public override bool TakeDamage(BigNumber damage, out BigNumber goldRewardOut)
        {
            goldRewardOut = new BigNumber("0");

            if (isDead)
                return false;

            BigNumber reducedDamage;
            if (damage.CompareTo(armor) > 0)
            {
                reducedDamage = damage - armor;
            }
            else
            {
                reducedDamage = new BigNumber("0");
            }

            if (reducedDamage.CompareTo(currentHitPoints) >= 0)
            {
                currentHitPoints = new BigNumber("0");
                isDead = true;
                goldRewardOut = goldReward.Clone();
                return true;
            }
            else
            {
                currentHitPoints = currentHitPoints - reducedDamage;
                return false;
            }
        }
    }

    //ЛЕЧАЩИЙСЯ ПРОТИВНИК
    public class CHealingEnemy : CEnemy
    {
        private double healChance;
        private int healAmount;
        private Random random;

        public double HealChance => healChance;
        public int HealAmount => healAmount;

        public CHealingEnemy(string name, string iconName, BigNumber maxHitPoints,
                             BigNumber goldReward, double healChance, int healAmount)
            : base(name, iconName, maxHitPoints, goldReward)
        {
            this.healChance = Math.Max(0, Math.Min(1, healChance));
            this.healAmount = healAmount;
            this.random = new Random();
        }

        public override bool TakeDamage(BigNumber damage, out BigNumber goldRewardOut)
        {
            goldRewardOut = new BigNumber("0");

            if (isDead)
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
            }

            if (!isDead && random.NextDouble() < healChance)
            {
                BigNumber healAmountBN = new BigNumber(healAmount.ToString());
                BigNumber healedHealth = currentHitPoints + healAmountBN;

                if (healedHealth.CompareTo(maxHitPoints) > 0)
                {
                    currentHitPoints = maxHitPoints.Clone();
                }
                else
                {
                    currentHitPoints = healedHealth;
                }
            }

            return false;
        }
    }

    //ОСЛАБЛЯЮЩИЙ ПРОТИВНИК
    public class CWeakeningEnemy : CEnemy
    {
        private double weakenFactor;
        private bool hasWeakened;

        public double WeakenFactor => weakenFactor;

        public CWeakeningEnemy(string name, string iconName, BigNumber maxHitPoints,
                               BigNumber goldReward, double weakenFactor)
            : base(name, iconName, maxHitPoints, goldReward)
        {
            this.weakenFactor = Math.Max(0.1, Math.Min(0.9, weakenFactor));
            this.hasWeakened = false;
        }

        public override bool TakeDamage(BigNumber damage, out BigNumber goldRewardOut)
        {
            goldRewardOut = new BigNumber("0");

            if (isDead)
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

        public override void ApplyWeakenEffect(Action<double, double> applyWeakenCallback = null)
        {
            if (!hasWeakened && applyWeakenCallback != null)
            {
                applyWeakenCallback(weakenFactor, 5.0);
                hasWeakened = true;
            }
        }
    }

    //ФАБРИКА ДЛЯ СОЗДАНИЯ ПРОТИВНИКОВ
    public static class EnemyFactory
    {
        public static IEnemy CreateEnemy(string typeName, params object[] args)
        {
            var type = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t => t.Name == typeName);

            if (type == null)
            {
                throw new ArgumentException($"Тип {typeName} не найден");
            }

            return (IEnemy)Activator.CreateInstance(type, args);
        }

        public static IEnemy CreateFromTemplate(CEnemyTemplate template, int level, string type = "CNormalEnemy",
                                                 double armor = 0, double healChance = 0, int healAmount = 0,
                                                 double weakenFactor = 0)
        {
            double lifeMultiplier = Math.Pow(template.GetLifeModifier(), level - 1);
            double goldMultiplier = Math.Pow(template.GetGoldModifier(), level - 1);

            int initialLife = (int)(template.GetBaseLife() * lifeMultiplier);
            int initialGold = (int)(template.GetBaseGold() * goldMultiplier);

            BigNumber maxHealth = new BigNumber(initialLife.ToString());
            BigNumber goldReward = new BigNumber(initialGold.ToString());

            switch (type)
            {
                case "CArmoredEnemy":
                    BigNumber armorBN = new BigNumber(armor.ToString());
                    return new CArmoredEnemy(template.GetName(), template.GetIconName(),
                                             maxHealth, goldReward, armorBN);
                case "CHealingEnemy":
                    return new CHealingEnemy(template.GetName(), template.GetIconName(),
                                             maxHealth, goldReward, healChance, healAmount);
                case "CWeakeningEnemy":
                    return new CWeakeningEnemy(template.GetName(), template.GetIconName(),
                                               maxHealth, goldReward, weakenFactor);
                default:
                    return new CNormalEnemy(template.GetName(), template.GetIconName(),
                                            maxHealth, goldReward);
            }
        }
    }

    //КЛАСС ИГРОКА
    public class Player
    {
        private int lvl;
        private BigNumber gold;
        private BigNumber damage;
        private double damageModifier;
        private BigNumber upgradeCost;
        private double upgradeModifier;
        private double attackCooldown;
        private double currentCooldown;
        private double damageBoostMultiplier;
        private double damageBoostDuration;
        private bool isDamageBoosted;
        private double weakenMultiplier;
        private double weakenDuration;
        private bool isWeakened;

        public int Lvl => lvl;
        public BigNumber Gold => gold;
        public BigNumber Damage => damage;
        public BigNumber UpgradeCost => upgradeCost;
        public double AttackCooldown => attackCooldown;
        public bool CanAttack => currentCooldown <= 0;

        public Player()
        {
            lvl = 1;
            gold = new BigNumber("0");
            damage = new BigNumber("10");
            damageModifier = 1.2;
            upgradeCost = new BigNumber("100");
            upgradeModifier = 1.2;
            attackCooldown = 1.0;
            currentCooldown = 0;
            damageBoostMultiplier = 1.0;
            damageBoostDuration = 0;
            isDamageBoosted = false;
            weakenMultiplier = 1.0;
            weakenDuration = 0;
            isWeakened = false;
        }

        public void AddGold(BigNumber amount)
        {
            if (amount == null) return;
            gold = gold + amount;
        }

        public bool TryUpgradeCooldown()
        {
            BigNumber cooldownUpgradeCost = new BigNumber(((int)(100 * Math.Pow(1.5, lvl - 1))).ToString());
            if (gold.CompareTo(cooldownUpgradeCost) >= 0)
            {
                gold = gold - cooldownUpgradeCost;
                attackCooldown = Math.Max(0.2, attackCooldown * 0.9);
                return true;
            }
            return false;
        }

        public bool TryUpgrade()
        {
            if (gold.CompareTo(upgradeCost) >= 0)
            {
                gold = gold - upgradeCost;
                lvl++;
                damage = damage * damageModifier;
                upgradeCost = upgradeCost * (upgradeModifier * (lvl - 1));
                return true;
            }
            return false;
        }

        public BigNumber DealDamage()
        {
            if (currentCooldown > 0)
                return new BigNumber("0");

            BigNumber finalDamage = damage.Clone();

            if (isDamageBoosted)
            {
                finalDamage = finalDamage * damageBoostMultiplier;
            }

            if (isWeakened)
            {
                finalDamage = finalDamage * weakenMultiplier;
            }

            currentCooldown = attackCooldown;
            return finalDamage;
        }

        public void UpdateCooldown(double deltaTime)
        {
            if (currentCooldown > 0)
            {
                currentCooldown -= deltaTime;
                if (currentCooldown < 0) currentCooldown = 0;
            }

            if (isDamageBoosted)
            {
                damageBoostDuration -= deltaTime;
                if (damageBoostDuration <= 0)
                {
                    isDamageBoosted = false;
                    damageBoostMultiplier = 1.0;
                }
            }

            if (isWeakened)
            {
                weakenDuration -= deltaTime;
                if (weakenDuration <= 0)
                {
                    isWeakened = false;
                    weakenMultiplier = 1.0;
                }
            }
        }

        public void ReduceAttackCooldown(double reduction)
        {
            attackCooldown = Math.Max(0.2, attackCooldown - reduction / 10.0);
            if (currentCooldown > attackCooldown)
                currentCooldown = attackCooldown;
        }

        public void ApplyDamageBoost(double multiplier, double duration)
        {
            damageBoostMultiplier = multiplier;
            damageBoostDuration = duration;
            isDamageBoosted = true;
        }

        public void ApplyWeaken(double multiplier, double duration)
        {
            weakenMultiplier = multiplier;
            weakenDuration = duration;
            isWeakened = true;
        }

        public string GetCooldownText()
        {
            if (currentCooldown > 0)
                return $"Перезарядка: {currentCooldown:F1} сек";
            return "Готов к атаке!";
        }

        public string GetDamageBoostText()
        {
            if (isDamageBoosted)
                return $"Урон увеличен x{damageBoostMultiplier:F1} ({damageBoostDuration:F1} сек)";
            return "";
        }

        public string GetWeakenText()
        {
            if (isWeakened)
                return $"Урон уменьшен x{weakenMultiplier:F1} ({weakenDuration:F1} сек)";
            return "";
        }

        public void LoadSaveData(PlayerSaveData data)
        {
            lvl = data.Lvl;
            gold = new BigNumber(data.Gold);
            damage = new BigNumber(data.Damage);
            upgradeCost = new BigNumber(data.UpgradeCost);
            attackCooldown = data.AttackCooldown;
            currentCooldown = 0;
        }
    }

    //КЛАСС ДЛЯ ЗАГРУЗКИ ШАБЛОНОВ ПРОТИВНИКОВ
    public class EnemyLoader
    {
        private List<CEnemyTemplate> templates;
        private Random random;

        public EnemyLoader()
        {
            templates = new List<CEnemyTemplate>();
            random = new Random();
        }

        public void LoadTemplates(string filePath)
        {
            string jsonFromFile = File.ReadAllText(filePath);
            JsonDocument doc = JsonDocument.Parse(jsonFromFile);
            templates.Clear();

            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                // ИСПРАВЛЕНО: теперь читаем поля с большой буквы (PascalCase)
                string name = element.GetProperty("Name").GetString();
                string iconName = element.GetProperty("IconName").GetString();
                int baseLife = element.GetProperty("BaseLife").GetInt32();
                double lifeModifier = element.GetProperty("LifeModifier").GetDouble();
                int baseGold = element.GetProperty("BaseGold").GetInt32();
                double goldModifier = element.GetProperty("GoldModifier").GetDouble();
                double spawnChance = element.GetProperty("SpawnChance").GetDouble();

                templates.Add(new CEnemyTemplate(name, iconName, baseLife,
                                                lifeModifier, baseGold,
                                                goldModifier, spawnChance));
            }
        }

        public (CEnemyTemplate template, string type, double armor, double healChance, int healAmount, double weakenFactor)
            GetRandomTemplateWithType(int level)
        {
            if (templates.Count == 0) return (null, "CNormalEnemy", 0, 0, 0, 0);

            double chance = random.NextDouble();
            double sum = 0;

            foreach (var template in templates)
            {
                sum += template.GetSpawnChance();
                if (sum >= chance)
                {
                    int typeIndex = random.Next(0, 100);
                    if (typeIndex < 60)
                    {
                        return (template, "CNormalEnemy", 0, 0, 0, 0);
                    }
                    else if (typeIndex < 75)
                    {
                        double armor = random.Next(5, 30);
                        return (template, "CArmoredEnemy", armor, 0, 0, 0);
                    }
                    else if (typeIndex < 90)
                    {
                        double healChance = 0.2 + random.NextDouble() * 0.3;
                        int healAmount = random.Next(10, 50);
                        return (template, "CHealingEnemy", 0, healChance, healAmount, 0);
                    }
                    else
                    {
                        double weakenFactor = 0.3 + random.NextDouble() * 0.4;
                        return (template, "CWeakeningEnemy", 0, 0, 0, weakenFactor);
                    }
                }
            }

            return (templates[0], "CNormalEnemy", 0, 0, 0, 0);
        }

        public bool HasTemplates => templates.Count > 0;
    }

    //ОСНОВНОЕ ОКНО ПРИЛОЖЕНИЯ
    public partial class MainWindow : Window
    {
        private Player player;
        private IEnemy currentEnemy;
        private EnemyLoader enemyLoader;
        private int enemiesDefeated;
        private BigNumber totalDamage;
        private BigNumber highestDamage;
        private string iconsFolderPath;
        private DispatcherTimer gameTimer;
        private DateTime lastUpdateTime;
        private PlayerSaver playerSaver;

        public MainWindow()
        {
            InitializeComponent();

            player = new Player();
            enemyLoader = new EnemyLoader();
            playerSaver = new PlayerSaver();

            enemiesDefeated = 0;
            totalDamage = new BigNumber("0");
            highestDamage = new BigNumber("0");

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(50);
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
            lastUpdateTime = DateTime.Now;

            UpdateUI();

            MessageBox.Show("Для начала игры:\n1. Нажмите 'ЗАГРУЗИТЬ ПРОТИВНИКОВ'\n2. Выберите JSON файл с врагами\n3. Укажите папку с иконками\n\nДля сохранения игры используйте кнопку 'СОХРАНИТЬ ИГРУ'",
                           "Инструкция", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double deltaTime = (now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            if (deltaTime > 0.1) deltaTime = 0.1;
            if (deltaTime < 0.01) deltaTime = 0.01;

            player.UpdateCooldown(deltaTime);
            UpdateUI();
        }

        private void UpdateUI()
        {
            LvlText.Text = player.Lvl.ToString();
            GoldText.Text = player.Gold.ToString();
            DamageText.Text = player.Damage.ToString();
            UpgradeCostText.Text = player.UpgradeCost.ToString();
            CooldownText.Text = player.GetCooldownText();
            DamageBoostText.Text = player.GetDamageBoostText();
            WeakenText.Text = player.GetWeakenText();

            BigNumber cooldownUpgradeCost = new BigNumber(((int)(100 * Math.Pow(1.5, player.Lvl - 1))).ToString());
            UpgradeCooldownCostText.Text = cooldownUpgradeCost.ToString();

            EnemiesDefeatedText.Text = $"Побеждено: {enemiesDefeated}";
            TotalDamageText.Text = $"Всего урона: {totalDamage}";
            HighestDamageText.Text = $"Макс. урон: {highestDamage}";

            if (currentEnemy != null && !currentEnemy.IsDead)
            {
                EnemyNameText.Text = currentEnemy.Name;
                EnemyHealthText.Text = currentEnemy.CurrentHitPoints.ToString();
                EnemyRewardText.Text = currentEnemy.GoldReward.ToString();

                string enemyType = "";
                if (currentEnemy is CArmoredEnemy armored)
                    enemyType = $"\n[Бронированный, броня: {armored.Armor}]";
                else if (currentEnemy is CHealingEnemy healing)
                    enemyType = $"\n[Лечащийся, шанс: {healing.HealChance * 100:F0}%]";
                else if (currentEnemy is CWeakeningEnemy weakening)
                    enemyType = $"\n[Ослабляющий, множитель: {weakening.WeakenFactor:F1}]";
                EnemyTypeText.Text = enemyType;

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
                EnemyTypeText.Text = "";
                EnemyImage.Source = null;
            }

            AttackButton.IsEnabled = (currentEnemy != null && !currentEnemy.IsDead && player.CanAttack);
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
                    enemyLoader.LoadTemplates(openDialog.FileName);

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
                        MessageBox.Show($"Противники успешно загружены!\nЗагружено шаблонов: {enemyLoader.HasTemplates}",
                                       "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (enemyLoader.HasTemplates)
            {
                var (template, type, armor, healChance, healAmount, weakenFactor) =
                    enemyLoader.GetRandomTemplateWithType(player.Lvl);

                if (template != null)
                {
                    currentEnemy = EnemyFactory.CreateFromTemplate(template, player.Lvl, type,
                                                                   armor, healChance, healAmount, weakenFactor);
                    UpdateUI();
                }
            }
            else
            {
                currentEnemy = null;
                UpdateUI();
            }
        }

        private void AttackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentEnemy == null || currentEnemy.IsDead)
            {
                if (enemyLoader.HasTemplates)
                {
                    SpawnNewEnemy();
                }
                return;
            }

            if (!player.CanAttack)
                return;

            BigNumber damage = player.DealDamage();

            if (damage.CompareTo(new BigNumber("0")) == 0)
                return;

            totalDamage = totalDamage + damage;

            if (damage.CompareTo(highestDamage) > 0)
                highestDamage = damage.Clone();

            if (currentEnemy is CWeakeningEnemy weakeningEnemy)
            {
                weakeningEnemy.ApplyWeakenEffect(player.ApplyWeaken);
            }

            if (currentEnemy.TakeDamage(damage, out BigNumber goldReward))
            {
                player.AddGold(goldReward);
                enemiesDefeated++;
                UpdateUI();

                string enemyTypeMessage = "";
                if (currentEnemy is CArmoredEnemy)
                    enemyTypeMessage = " (Бронированный)";
                else if (currentEnemy is CHealingEnemy)
                    enemyTypeMessage = " (Лечащийся)";
                else if (currentEnemy is CWeakeningEnemy)
                    enemyTypeMessage = " (Ослабляющий)";

                MessageBox.Show($"Вы победили {currentEnemy.Name}{enemyTypeMessage}!\nПолучено золота: {goldReward}",
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

        private void UpgradeCooldownButton_Click(object sender, RoutedEventArgs e)
        {
            if (player.TryUpgradeCooldown())
            {
                UpdateUI();
                MessageBox.Show($"Улучшение перезарядки успешно!\nНовая перезарядка: {player.AttackCooldown:F1} сек",
                               "Улучшение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                BigNumber cost = new BigNumber(((int)(100 * Math.Pow(1.5, player.Lvl - 1))).ToString());
                MessageBox.Show($"Недостаточно золота!\nНужно: {cost}\nУ вас: {player.Gold}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JSON files (*.json)|*.json";
            saveDialog.DefaultExt = "json";
            saveDialog.FileName = "game_save.json";

            if (saveDialog.ShowDialog() == true)
            {
                PlayerSaveData saveData = new PlayerSaveData
                {
                    Lvl = player.Lvl,
                    Gold = player.Gold.ToString(),
                    Damage = player.Damage.ToString(),
                    UpgradeCost = player.UpgradeCost.ToString(),
                    AttackCooldown = player.AttackCooldown,
                    EnemiesDefeated = enemiesDefeated,
                    TotalDamage = totalDamage.ToString(),
                    HighestDamage = highestDamage.ToString()
                };
                playerSaver.Save(saveData, saveDialog.FileName);
            }
        }

        private void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "JSON files (*.json)|*.json";
            openDialog.Title = "Выберите файл сохранения";

            if (openDialog.ShowDialog() == true)
            {
                PlayerSaveData saveData = playerSaver.Load(openDialog.FileName);

                Player newPlayer = new Player();
                newPlayer.LoadSaveData(saveData);

                player = newPlayer;
                enemiesDefeated = saveData.EnemiesDefeated;
                totalDamage = new BigNumber(saveData.TotalDamage);
                highestDamage = new BigNumber(saveData.HighestDamage);

                UpdateUI();
                MessageBox.Show("Игра успешно загружена!", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}