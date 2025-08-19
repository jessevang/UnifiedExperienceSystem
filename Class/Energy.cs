namespace UnifiedExperienceSystem
{
    internal sealed class EnergyData
    {
        public const float Max = 100f;

        private float _current = Max;
        public float Current => _current;

        public void Set(float value)
        {
            _current = Math.Max(0f, Math.Min(Max, value));
        }

        public void Add(float amount)
        {
            if (amount == 0) return;
            Set(_current + amount);
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0) return true;
            if (_current + 1e-3f < amount) return false;
            _current -= amount;
            return true;
        }

        public void ResetFull() => _current = Max;
    }
}
