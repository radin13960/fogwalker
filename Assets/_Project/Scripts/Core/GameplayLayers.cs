namespace FogWalker.Core
{
    /// <summary>
    /// ایندکس ثابت لایه‌های Physics پروژه (باید با ProjectSettings/TagManager هم‌خوان باشد؛ کارخانه Setup آن‌ها را می‌سازد).
    /// 6=Player, 7=Enemy, 8=Environment, 9=Hitbox, 10=Interactable, 11=Cover
    /// </summary>
    public static class GameplayLayers
    {
        public const int Player = 6;
        public const int Enemy = 7;
        public const int Environment = 8;
        public const int Hitbox = 9;
        public const int Interactable = 10;
        public const int Cover = 11;

        /// <summary>همه آنچه گلوله می‌تواند بخورد (محیط + Hitboxها).</summary>
        public static int BulletMask => (1 << Environment) | (1 << Hitbox);
        /// <summary>فقط محیط (برای اعتبارسنجی خط دید/دهانه سلاح).</summary>
        public static int EnvironmentMask => 1 << Environment;
        /// <summary>فقط Hitboxها (هدف‌گیری مستقیم).</summary>
        public static int HitboxMask => 1 << Hitbox;
        /// <summary>لایه‌های قابل تعامل.</summary>
        public static int InteractableMask => 1 << Interactable;
        /// <summary>انفجار روی چه چیزهایی اثر می‌گذارد.</summary>
        public static int ExplosionMask => (1 << Hitbox) | (1 << Player) | (1 << Enemy);
    }
}
