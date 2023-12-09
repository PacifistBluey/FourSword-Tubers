﻿using Newtonsoft.Json;

namespace ItemChanger
{
    public interface ITransition
    {
        string SceneName { get; }
        string GateName { get; }
    }

    //[TypeConverter(typeof(TransitionConverter))]
    public readonly struct Transition : ITransition, IEquatable<Transition>
    {
        public string SceneName { get; }
        public string GateName { get; }
        public const string door_dreamReturn = "door_dreamReturn";

        [JsonConstructor]
        public Transition(string SceneName, string GateName)
        {
            this.SceneName = SceneName;
            this.GateName = GateName;
        }

        public static Transition GetDreamReturn(string sceneName)
        {
            return new(sceneName, door_dreamReturn);
        }

        public override string ToString()
        {
            return $"{SceneName}[{GateName}]";
        }

        public override int GetHashCode()
        {
            return SceneName.GetHashCode() + 31 * (GateName?.GetHashCode() ?? 0);
        }

        public bool Equals(Transition t)
        {
            return SceneName == t.SceneName && GateName == t.GateName;
        }

        public override bool Equals(object obj)
        {
            return obj is Transition t && Equals(t);
        }

        public static bool operator==(Transition t1, Transition t2)
        {
            return t1.SceneName == t2.SceneName && t1.GateName == t2.GateName;
        }

        public static bool operator !=(Transition t1, Transition t2) => !(t1 == t2);

        public class TransitionDictConverter<TValue> : JsonConverter<Dictionary<Transition, TValue>>
        {
            public override Dictionary<Transition, TValue>? ReadJson(
                JsonReader reader, 
                Type objectType, 
                Dictionary<Transition, TValue>? existingValue, 
                bool hasExistingValue, 
                JsonSerializer serializer)
            {
                return serializer.Deserialize<KeyValuePair<Transition, TValue>[]>(reader)?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            public override void WriteJson(JsonWriter writer, Dictionary<Transition, TValue>? value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value?.ToArray());
            }
        }

        /*
        public class TransitionConverter : TypeConverter
        {
            public static readonly Regex r = new(@"([^\[]*)\[([^\]]*)\]");

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string s)
                {
                    try
                    {
                        Match m = r.Match(s);
                        Transition t = new(m.Groups[1].Value, m.Groups[2].Value);
                        return t;
                    }
                    catch (Exception e)
                    {
                        Log(e);
                    }
                }
                return base.ConvertFrom(context, culture, value);
            }
        }
        */
    }
}
