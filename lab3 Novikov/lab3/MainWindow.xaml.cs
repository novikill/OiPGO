using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
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
        public string name;
        public string iconName;
        public int baseLife;
        public double lifeModifier;
        public int baseGold;
        public double goldModifier;
        public double spawnChance;

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
        private double attackCooldown;
        private double currentCooldown;
        private double damageBoostMultiplier;
        private double damageBoostDuration;
        private bool isDamageBoosted;

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
    }

    // Абстрактный класс для собираемых объектов (сфер)
    public abstract class CCollectable
    {
        protected Point position;
        protected Size size;
        protected double lifetime;
        protected double maxLifetime;
        protected Ellipse sprite;
        protected bool isActive;

        public CCollectable(Point position, double size, double lifetime)
        {
            this.position = position;
            this.size = new Size(size, size);
            this.lifetime = lifetime;
            this.maxLifetime = lifetime;
            this.isActive = true;

            sprite = new Ellipse();
            sprite.Fill = GetColor();
            sprite.StrokeThickness = 2;
            sprite.Stroke = Brushes.Black;
            sprite.Width = this.size.Width;
            sprite.Height = this.size.Height;
            Canvas.SetLeft(sprite, position.X - size / 2);
            Canvas.SetTop(sprite, position.Y - size / 2);
        }

        protected virtual Brush GetColor()
        {
            return Brushes.BlueViolet;
        }

        public bool IsMouseOnObject(Point mousePosition)
        {
            double left = position.X - size.Width / 2;
            double right = position.X + size.Width / 2;
            double top = position.Y - size.Height / 2;
            double bottom = position.Y + size.Height / 2;

            return mousePosition.X >= left && mousePosition.X <= right &&
                   mousePosition.Y >= top && mousePosition.Y <= bottom;
        }

        public Ellipse GetSprite() => sprite;

        public bool UpdateLifetime(double deltaTime)
        {
            lifetime -= deltaTime;
            if (lifetime <= 0)
            {
                isActive = false;
                return true;
            }

            double alpha = Math.Min(1.0, lifetime / maxLifetime);
            sprite.Opacity = alpha;

            return false;
        }

        public bool IsActive => isActive;
        public Point Position => position;

        public abstract bool OnClick(Player player, SphereManager sphereManager, Point mousePosition);
    }

    // Сфера, дающая золото
    public class CGoldSphere : CCollectable
    {
        private double goldAmount;

        public CGoldSphere(Point position, double size, double lifetime, double goldAmount)
            : base(position, size, lifetime)
        {
            this.goldAmount = goldAmount;
        }

        protected override Brush GetColor()
        {
            return Brushes.Gold;
        }

        public override bool OnClick(Player player, SphereManager sphereManager, Point mousePosition)
        {
            if (!IsMouseOnObject(mousePosition))
                return false;

            player.AddGold(new BigNumber(((int)goldAmount).ToString()));
            return true;
        }
    }

    // Сфера, уменьшающая перезарядку атаки
    public class CCooldownSphere : CCollectable
    {
        private double cooldownReduction;

        public CCooldownSphere(Point position, double size, double lifetime, double cooldownReduction)
            : base(position, size, lifetime)
        {
            this.cooldownReduction = cooldownReduction;
        }

        protected override Brush GetColor()
        {
            return Brushes.LimeGreen;
        }

        public override bool OnClick(Player player, SphereManager sphereManager, Point mousePosition)
        {
            if (!IsMouseOnObject(mousePosition))
                return false;

            player.ReduceAttackCooldown(cooldownReduction);
            return true;
        }
    }

    // Сфера, увеличивающая урон
    public class CDamageSphere : CCollectable
    {
        private double damageMultiplier;
        private double duration;

        public CDamageSphere(Point position, double size, double lifetime, double damageMultiplier, double duration)
            : base(position, size, lifetime)
        {
            this.damageMultiplier = damageMultiplier;
            this.duration = duration;
        }

        protected override Brush GetColor()
        {
            return Brushes.Red;
        }

        public override bool OnClick(Player player, SphereManager sphereManager, Point mousePosition)
        {
            if (!IsMouseOnObject(mousePosition))
                return false;

            player.ApplyDamageBoost(damageMultiplier, duration);
            return true;
        }
    }

    // Сфера, увеличивающая время жизни сфер
    public class CLifetimeSphere : CCollectable
    {
        private double lifetimeIncrease;

        public CLifetimeSphere(Point position, double size, double lifetime, double lifetimeIncrease)
            : base(position, size, lifetime)
        {
            this.lifetimeIncrease = lifetimeIncrease;
        }

        protected override Brush GetColor()
        {
            return Brushes.Cyan;
        }

        public override bool OnClick(Player player, SphereManager sphereManager, Point mousePosition)
        {
            if (!IsMouseOnObject(mousePosition))
                return false;

            sphereManager.IncreaseAllSpheresLifetime(lifetimeIncrease);
            return true;
        }
    }

    // Сфера, уменьшающая время появления новых сфер
    public class CSpawnRateSphere : CCollectable
    {
        private double spawnRateReduction;

        public CSpawnRateSphere(Point position, double size, double lifetime, double spawnRateReduction)
            : base(position, size, lifetime)
        {
            this.spawnRateReduction = spawnRateReduction;
        }

        protected override Brush GetColor()
        {
            return Brushes.Orange;
        }

        public override bool OnClick(Player player, SphereManager sphereManager, Point mousePosition)
        {
            if (!IsMouseOnObject(mousePosition))
                return false;

            sphereManager.ReduceSpawnRate(spawnRateReduction);
            return true;
        }
    }

    // Менеджер для управления сферами
    public class SphereManager
    {
        private List<CCollectable> spheres;
        private Random random;
        private double spawnTimer;
        private double spawnInterval;
        private double baseSpawnInterval;
        private double spawnRateModifier;
        private double spawnRateModifierDuration;
        private bool isSpawnRateModified;
        private double areaWidth;
        private double areaHeight;

        public SphereManager(double width, double height)
        {
            spheres = new List<CCollectable>();
            random = new Random();
            areaWidth = width;
            areaHeight = height;
            baseSpawnInterval = 3.0;
            spawnInterval = baseSpawnInterval;
            spawnTimer = 0;
            spawnRateModifier = 1.0;
            isSpawnRateModified = false;
        }

        public void Update(double deltaTime)
        {
            if (isSpawnRateModified)
            {
                spawnRateModifierDuration -= deltaTime;
                if (spawnRateModifierDuration <= 0)
                {
                    isSpawnRateModified = false;
                    spawnRateModifier = 1.0;
                    spawnInterval = baseSpawnInterval;
                }
            }

            spawnTimer += deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0;
                SpawnSphere();
            }

            for (int i = spheres.Count - 1; i >= 0; i--)
            {
                if (spheres[i].UpdateLifetime(deltaTime))
                {
                    RemoveSphere(spheres[i]);
                }
            }
        }

        private void SpawnSphere()
        {
            // Сферы появляются справа от противника (в области шириной 250px)
            double x = random.Next(50, (int)areaWidth - 50);
            double y = random.Next(50, (int)areaHeight - 50);
            Point position = new Point(x, y);

            double size = random.Next(30, 60);
            double lifetime = random.Next(3, 8);

            int sphereType = random.Next(0, 5);
            CCollectable newSphere = null;

            switch (sphereType)
            {
                case 0:
                    double goldAmount = random.Next(50, 200);
                    newSphere = new CGoldSphere(position, size, lifetime, goldAmount);
                    break;
                case 1:
                    double cooldownReduction = random.Next(1, 5);
                    newSphere = new CCooldownSphere(position, size, lifetime, cooldownReduction);
                    break;
                case 2:
                    double damageMultiplier = 1.5 + random.NextDouble();
                    double duration = random.Next(5, 15);
                    newSphere = new CDamageSphere(position, size, lifetime, damageMultiplier, duration);
                    break;
                case 3:
                    double lifetimeIncrease = random.Next(1, 3);
                    newSphere = new CLifetimeSphere(position, size, lifetime, lifetimeIncrease);
                    break;
                case 4:
                    double spawnRateReduction = random.Next(1, 3);
                    newSphere = new CSpawnRateSphere(position, size, lifetime, spawnRateReduction);
                    break;
            }

            if (newSphere != null)
            {
                spheres.Add(newSphere);
            }
        }

        public void RemoveSphere(CCollectable sphere)
        {
            spheres.Remove(sphere);
        }

        public void IncreaseAllSpheresLifetime(double increase)
        {
            foreach (var sphere in spheres)
            {
                sphere.UpdateLifetime(-increase);
            }
        }

        public void ReduceSpawnRate(double reduction)
        {
            spawnRateModifier = Math.Max(0.3, spawnRateModifier - reduction / 10.0);
            spawnInterval = baseSpawnInterval * spawnRateModifier;
            spawnRateModifierDuration = 10.0;
            isSpawnRateModified = true;
        }

        public List<CCollectable> GetSpheres() => spheres;

        public bool CheckSphereClick(Point mousePosition, Player player)
        {
            for (int i = spheres.Count - 1; i >= 0; i--)
            {
                if (spheres[i].OnClick(player, this, mousePosition))
                {
                    RemoveSphere(spheres[i]);
                    return true;
                }
            }
            return false;
        }

        public void ClearSpheres()
        {
            spheres.Clear();
        }
    }

    // Класс для загрузки шаблонов противников
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
        }

        public CEnemyTemplate GetRandomTemplate(int level)
        {
            if (templates.Count == 0) return null;

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
        private EnemyLoader enemyLoader;
        private SphereManager sphereManager;
        private int enemiesDefeated;
        private BigNumber totalDamage;
        private BigNumber highestDamage;
        private string iconsFolderPath;
        private DispatcherTimer gameTimer;
        private DateTime lastUpdateTime;

        public MainWindow()
        {
            InitializeComponent();

            player = new Player();
            enemyLoader = new EnemyLoader();
            enemiesDefeated = 0;
            totalDamage = new BigNumber("0");
            highestDamage = new BigNumber("0");

            // Инициализируем менеджер сфер с размерами Canvas
            sphereManager = new SphereManager(SphereCanvas.Width, SphereCanvas.Height);

            // Подписываемся на клик по сферам
            SphereCanvas.MouseLeftButtonDown += SphereCanvas_MouseLeftButtonDown;

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(50);
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
            lastUpdateTime = DateTime.Now;

            UpdateUI();

            MessageBox.Show("Для начала игры:\n1. Нажмите 'ЗАГРУЗИТЬ ПРОТИВНИКОВ'\n2. Выберите JSON файл с врагами\n3. Укажите папку с иконками",
                           "Инструкция", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SphereCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(SphereCanvas);
            if (sphereManager.CheckSphereClick(clickPosition, player))
            {
                UpdateUI();
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double deltaTime = (now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            if (deltaTime > 0.1) deltaTime = 0.1;
            if (deltaTime < 0.01) deltaTime = 0.01;

            player.UpdateCooldown(deltaTime);
            sphereManager.Update(deltaTime);
            UpdateSpheresUI();
            UpdateUI();
        }

        private void UpdateSpheresUI()
        {
            if (SphereCanvas == null) return;
            SphereCanvas.Children.Clear();

            foreach (var sphere in sphereManager.GetSpheres())
            {
                var sprite = sphere.GetSprite();
                if (sprite != null)
                {
                    SphereCanvas.Children.Add(sprite);
                }
            }
        }

        private void UpdateUI()
        {
            LvlText.Text = player.Lvl.ToString();
            GoldText.Text = player.Gold.ToString();
            DamageText.Text = player.Damage.ToString();
            UpgradeCostText.Text = player.UpgradeCost.ToString();
            CooldownText.Text = player.GetCooldownText();
            DamageBoostText.Text = player.GetDamageBoostText();

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
                var template = enemyLoader.GetRandomTemplate(player.Lvl);
                if (template != null)
                {
                    currentEnemy = new Enemy(template, player.Lvl);
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

            if (currentEnemy.TakeDamage(damage, out BigNumber goldReward))
            {
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
    }
}