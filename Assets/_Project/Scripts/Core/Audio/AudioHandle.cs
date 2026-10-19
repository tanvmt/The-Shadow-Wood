using System;

namespace TheShadowWood.Core.Audio
{
    // Ids are never reused, so a stale handle cannot stop a pooled source that now plays something else.
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        public static readonly AudioHandle None = default;

        public AudioHandle(int id)
        {
            Id = id;
        }

        public int Id { get; }
        public bool IsValid => Id != 0;

        public bool Equals(AudioHandle other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
