using System;

using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using Newtonsoft.Json.UnityConverters.Math;

namespace ExternalUnityRendering.Serialization
{
    /// <summary>
    /// Used to convert a <see cref="EURGameObject"/> to JSON. To be used with
    /// <see cref="JsonSerializer"/> and <see cref="JsonSerializerSettings"/>.
    /// </summary>
    public class EURGameObjectConverter : PartialConverter<EURGameObject>
    {
        /// <summary>
        /// Converter to serialize <see cref="UnityEngine.Vector3"/> used in
        /// <see cref="EURGameObject"/>.
        /// </summary>
        private readonly Vector3Converter _vector3Converter = new Vector3Converter();

        /// <summary>
        /// Converter to serialize <see cref="UnityEngine.Quaternion"/> used in
        /// <see cref="EURGameObject"/>.
        /// </summary>
        private readonly QuaternionConverter _quaternionConverter = new QuaternionConverter();

        /// <summary>
        /// Indicate to the serializer whether this converter can read.
        /// </summary>
        public override bool CanRead {
            get { return false; }
        }

        /// <summary>
        /// Using <paramref name="name"/>, deserialize and update the corresponding field in
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The deserialized object.</param>
        /// <param name="name">The name of the field/member.</param>
        /// <param name="reader">The <see cref="JsonReader"/> to read the the data of the
        /// field/member.</param>
        /// <param name="serializer"><see cref="JsonSerializer"/> to deserialise nested objects.
        /// </param>
        protected override void ReadValue(ref EURGameObject value, string name, JsonReader reader,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert <paramref name="value"/> to json using <paramref name="writer"/> and
        /// <paramref name="serializer"/>.
        /// </summary>
        /// <param name="writer"><see cref="JsonWriter"/> Where the JSON data is saved. </param>
        /// <param name="value">The data to be serialized.</param>
        /// <param name="serializer"><see cref="JsonSerializer"/> to serialize nested objects.
        /// </param>
        protected override void WriteJsonProperties(JsonWriter writer, EURGameObject value,
            JsonSerializer serializer)
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
