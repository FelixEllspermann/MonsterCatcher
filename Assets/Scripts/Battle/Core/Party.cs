using System;
using System.Collections.Generic;

namespace MonsterCatcher.Battle
{
    public sealed class Party
    {
        private readonly List<Pokemon> _members;

        public BattleSide Side { get; }
        public int MaxSize { get; }
        public int ActiveIndex { get; private set; }

        public Party(BattleSide side, IList<Pokemon> members, int maxSize)
        {
            if (members == null || members.Count == 0)
                throw new ArgumentException("Party needs at least one Pokemon.");
            MaxSize = maxSize;
            if (members.Count > maxSize)
                throw new ArgumentException("Party exceeds max size.");
            Side = side;
            _members = new List<Pokemon>(members);
            ActiveIndex = 0;
            for (int i = 0; i < _members.Count; i++)
            {
                if (!_members[i].IsFainted) { ActiveIndex = i; break; }
            }
        }

        public IReadOnlyList<Pokemon> Members => _members;
        public Pokemon Active => _members[ActiveIndex];

        public bool HasUsablePokemon()
        {
            foreach (var m in _members)
                if (!m.IsFainted) return true;
            return false;
        }

        public bool CanSwitchTo(int index)
        {
            return index >= 0 && index < _members.Count
                   && index != ActiveIndex && !_members[index].IsFainted;
        }

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= _members.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _members[ActiveIndex].ResetStages();
            ActiveIndex = index;
        }

        public int FirstUsableIndex()
        {
            for (int i = 0; i < _members.Count; i++)
                if (!_members[i].IsFainted) return i;
            return -1;
        }
    }
}
