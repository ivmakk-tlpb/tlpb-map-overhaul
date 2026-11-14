// Fake IL2CPP interop types and the fake IL2CPP collections.

namespace Il2CppInterop.Runtime.InteropTypes
{
    public class Il2CppObjectBase
    {
        private static long nextPointer = 0x10000;

        public Il2CppObjectBase() { }

        public Il2CppObjectBase(IntPtr pointer) { }

        public IntPtr Pointer { get; } = new IntPtr(nextPointer += 8);

        public T TryCast<T>() where T : Il2CppObjectBase => this as T;
    }
}

namespace Il2CppInterop.Runtime.Injection
{
    public static class ClassInjector
    {
        public static readonly List<Type> Registered = new List<Type>();
        // The type whose register fails. A test sets it.
        public static Type ThrowFor;

        public static void RegisterTypeInIl2Cpp<T>()
        {
            if (typeof(T) == ThrowFor) throw new InvalidOperationException("register failed");
            Registered.Add(typeof(T));
        }
    }
}

namespace Il2CppSystem.Collections.Generic
{
    public class List<T> : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        private readonly System.Collections.Generic.List<T> items = new System.Collections.Generic.List<T>();

        public int Count => items.Count;
        public T this[int index] => items[index];

        public void Add(T item) { items.Add(item); }
    }

    public class HashSet<T> : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        private readonly System.Collections.Generic.List<T> items = new System.Collections.Generic.List<T>();

        public int Count => items.Count;

        public bool Add(T item)
        {
            if (items.Contains(item)) return false;
            items.Add(item);
            return true;
        }

        public Enumerator GetEnumerator() => new Enumerator(items);

        public sealed class Enumerator
        {
            private readonly System.Collections.Generic.List<T> items;
            private int index = -1;

            public Enumerator(System.Collections.Generic.List<T> items) { this.items = items; }

            public T Current => items[index];
            public bool MoveNext() => ++index < items.Count;
        }
    }

    // Keeps the order of Add, as a test expects.
    public class Dictionary<TKey, TValue> : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<TKey, TValue>> entries =
            new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<TKey, TValue>>();

        public int Count => entries.Count;

        public void Add(TKey key, TValue value) { entries.Add(new System.Collections.Generic.KeyValuePair<TKey, TValue>(key, value)); }

        public bool TryGetValue(TKey key, out TValue value)
        {
            foreach (var entry in entries)
            {
                if (!Equals(entry.Key, key)) continue;
                value = entry.Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool ContainsKey(TKey key) => TryGetValue(key, out _);

        public Enumerator GetEnumerator() => new Enumerator(entries);

        public sealed class Enumerator
        {
            private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<TKey, TValue>> entries;
            private int index = -1;

            public Enumerator(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<TKey, TValue>> entries) { this.entries = entries; }

            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current => entries[index];
            public bool MoveNext() => ++index < entries.Count;
        }
    }
}
