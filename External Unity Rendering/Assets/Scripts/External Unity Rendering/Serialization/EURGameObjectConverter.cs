using System;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using Newtonsoft.Json.UnityConverters.Math;

namespace ExternalUnityRendering.Serialization
{
    public class EURGameObjectConverter : PartialConverter<EURGameObject>
    {
        private readonly Vector3Converter _vector3Converter = new Vector3Converter();
        private readonly QuaternionConverter _quaternionConverter = new QuaternionConverter();

        public override bool CanRead {
            get { return false; }
        }

        protected override void ReadValue(ref EURGameObject value, string name, JsonReader reader, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        protected override void WriteJsonProperties(JsonWriter writer, EURGameObject value, JsonSerializer serializer)
        {
            writer.WritePropertyName(nameof(value.Name));
            writer.WriteValue(value.Name);

            writer.WritePropertyName(nameof(value.ObjectTransform));
            writer.WriteStartObject();
            {
                writer.WritePropertyName(nameof(value.ObjectTransform.Position));
                _vector3Converter.WriteJson(writer, value.ObjectTransform.Position, serializer);
                writer.WritePropertyName(nameof(value.ObjectTransform.Rotation));
                _quaternionConverter.WriteJson(writer, value.ObjectTransform.Rotation, serializer);
                writer.WritePropertyName(nameof(value.ObjectTransform.Scale));
                _vector3Converter.WriteJson(writer, value.ObjectTransform.Scale, serializer);
            }
            writer.WriteEndObject();

            writer.WritePropertyName(nameof(value.Children));
            writer.WriteStartArray();
            foreach (EURGameObject child in value.Children)
            {
                writer.WriteStartObject();
                WriteJsonProperties(writer, child, serializer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
