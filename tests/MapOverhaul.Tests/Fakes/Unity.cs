// Fake Unity types: only the members that the mod source reaches.

namespace UnityEngine
{
    public class Material : Object
    {
        public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();

        public Material() { }

        public Material(Material source)
        {
            foreach (var pair in source.Floats) Floats[pair.Key] = pair.Value;
        }

        public void SetFloat(string name, float value) { Floats[name] = value; }

        public float GetFloat(string name) => Floats.TryGetValue(name, out var value) ? value : 0f;
    }

    public static class Resources
    {
        // The objects of the scene, also the inactive ones of a pool.
        public static T[] FindObjectsOfTypeAll<T>() where T : Object => Object.Scene.OfType<T>().ToArray();
    }

    public static class Application
    {
        public static string version = "0.12.7";
    }

    public static class Time
    {
        public static int frameCount;
        public static float realtimeSinceStartup;
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static Quaternion identity => new Quaternion { w = 1f };
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color white => new Color(1f, 1f, 1f, 1f);
    }

    public struct Rect
    {
        public float width;
        public float height;

        public Rect(float x, float y, float width, float height)
        {
            this.width = width;
            this.height = height;
        }
    }

    public enum TextureFormat
    {
        RGBA32 = 4,
    }

    public enum FilterMode
    {
        Bilinear = 1,
    }

    [Flags]
    public enum HideFlags
    {
        HideAndDontSave = 61,
    }

    public class Texture2D : Object
    {
        public int width;
        public int height;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public bool Applied;
        public Color[] Pixels;
        public byte[] RawData;
        public int Uploads;

        public void LoadRawTextureData(byte[] data) { RawData = (byte[])data.Clone(); Uploads++; }

        public Texture2D(int width, int height) : this(width, height, TextureFormat.RGBA32, false) { }

        public Texture2D(int width, int height, TextureFormat format, bool mipChain)
        {
            this.width = width;
            this.height = height;
            Pixels = new Color[width * height];
        }

        public void SetPixel(int x, int y, Color color) { Pixels[y * width + x] = color; }

        public Color GetPixel(int x, int y) => Pixels[y * width + x];

        public void Apply() { Applied = true; }
    }

    // Decodes no image: a test reads the size of the texture and an alpha of 1 at each pixel with a byte of 255.
    public static class ImageConversion
    {
        public static bool LoadImage(Texture2D texture, byte[] data)
        {
            texture.width = 64;
            texture.height = 64;
            texture.Pixels = new Color[64 * 64];
            texture.Pixels[32 * 64 + 32] = new Color(1f, 1f, 1f, 1f);
            return data.Length > 0;
        }
    }

    public class Sprite : Object
    {
        public Texture2D texture;

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => new Sprite { texture = texture };
    }

    public class GameObject : Object
    {
        public readonly System.Collections.Generic.List<Component> Components = new System.Collections.Generic.List<Component>();
        public string name;
        public bool activeSelf = true;
        public bool Destroyed;

        public GameObject() { }

        public GameObject(string name) { this.name = name; }

        public Transform transform => GetComponent<Transform>() ?? AddComponent<RectTransform>();

        public T GetComponent<T>() where T : Component
        {
            foreach (var c in Components)
                if (c is T found) return found;
            return null;
        }

        // As in Unity, a GameObject takes one graphic only: a second one gives null.
        public T AddComponent<T>() where T : Component, new()
        {
            if (IsGraphic(typeof(T)) && Components.Exists(c => IsGraphic(c.GetType()))) return null;
            var component = new T { gameObject = this };
            Components.Add(component);
            return component;
        }

        public void SetActive(bool value) { activeSelf = value; }

        private static bool IsGraphic(Type type) =>
            typeof(UnityEngine.UI.Graphic).IsAssignableFrom(type) || type == typeof(UnityEngine.UI.RawImage) || type == typeof(TMPro.TextMeshProUGUI);
    }

    public class Transform : Component
    {
        public Transform parent;
        public readonly System.Collections.Generic.List<Transform> Children = new System.Collections.Generic.List<Transform>();
        public Vector3 localPosition;

        public void SetParent(Transform newParent, bool worldPositionStays)
        {
            parent?.Children.Remove(this);
            parent = newParent;
            newParent.Children.Add(this);
        }

        public void SetAsLastSibling()
        {
            parent.Children.Remove(this);
            parent.Children.Add(this);
        }

        public int GetSiblingIndex() => parent.Children.IndexOf(this);
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector2 anchoredPosition;
    }

    public enum TextureWrapMode
    {
        Repeat = 0,
        Clamp = 1,
    }

    public class Object : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        public HideFlags hideFlags;

        // The objects that FindObjectOfType finds. A test adds them.
        public static readonly System.Collections.Generic.List<Object> Scene = new System.Collections.Generic.List<Object>();

        public Object() { }

        public Object(IntPtr pointer) : base(pointer) { }

        // The objects that Destroy received. A test reads it.
        public static readonly System.Collections.Generic.List<Object> DestroyedObjects = new System.Collections.Generic.List<Object>();

        public static void Destroy(Object target) { DestroyedObjects.Add(target); }

        public static T FindObjectOfType<T>() where T : Object
        {
            foreach (var o in Scene)
                if (o is T found) return found;
            return null;
        }
    }

    public class Component : Object
    {
        public GameObject gameObject;

        public Component() { }

        public Component(IntPtr pointer) : base(pointer) { }

        public Transform transform => this as Transform ?? gameObject.transform;
    }

    public class Behaviour : Component
    {
        public Behaviour() { }

        public Behaviour(IntPtr pointer) : base(pointer) { }
    }

    public class MonoBehaviour : Behaviour
    {
        public MonoBehaviour() { }

        public MonoBehaviour(IntPtr pointer) : base(pointer) { }
    }
}

namespace UnityEngine.Events
{
    public class UnityAction
    {
        public Action Call;

        public static implicit operator UnityAction(Action action) => new UnityAction { Call = action };
    }

    public class UnityEvent
    {
        private readonly List<UnityAction> listeners = new List<UnityAction>();

        public void AddListener(UnityAction action) { listeners.Add(action); }

        public void Invoke()
        {
            foreach (var listener in listeners.ToArray()) listener.Call();
        }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventTrigger : UnityEngine.Behaviour { }
}

namespace TMPro
{
    public class TMP_FontAsset : UnityEngine.Object
    {
        public string name;
        public UnityEngine.Material material;
    }

    public enum TextAlignmentOptions
    {
        Left = 513,
    }

    public class TextMeshProUGUI : UnityEngine.Behaviour
    {
        private string value;

        // The count of text sets. A test reads it.
        public int TextSets;

        public string text
        {
            get => value;
            set { this.value = value; TextSets++; }
        }

        public TMP_FontAsset font;
        public UnityEngine.Material fontSharedMaterial;
        public float fontSize;
        public UnityEngine.Color color = UnityEngine.Color.white;
        public TextAlignmentOptions alignment;
        public bool raycastTarget = true;

        public UnityEngine.RectTransform rectTransform => (UnityEngine.RectTransform)gameObject.transform;
    }
}

namespace UnityEngine.UI
{
    public class Graphic : UnityEngine.Behaviour { }

    public struct ColorBlock
    {
        public UnityEngine.Color normalColor;
        public UnityEngine.Color highlightedColor;
        public UnityEngine.Color pressedColor;
        public UnityEngine.Color selectedColor;
        public UnityEngine.Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
    }

    public class Selectable : UnityEngine.Behaviour
    {
        public enum Transition
        {
            None = 0,
            ColorTint = 1,
        }

        public Transition transition = Transition.ColorTint;
        public Graphic targetGraphic;
        public ColorBlock colors;
    }

    public class Button : Selectable
    {
        public readonly UnityEngine.Events.UnityEvent onClick = new UnityEngine.Events.UnityEvent();
    }

    public class RawImage : UnityEngine.Behaviour
    {
        public UnityEngine.Texture2D texture;
        public UnityEngine.Color color = UnityEngine.Color.white;
        public bool raycastTarget = true;

        public UnityEngine.RectTransform rectTransform => (UnityEngine.RectTransform)gameObject.transform;
    }

    public class Image : Graphic
    {
        public UnityEngine.Sprite sprite;
        public UnityEngine.Color color;
        public bool raycastTarget = true;

        public UnityEngine.RectTransform rectTransform => (UnityEngine.RectTransform)gameObject.transform;
    }
}
